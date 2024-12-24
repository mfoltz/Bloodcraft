using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Utilities.Progression;
using Random = System.Random;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Professions;
internal static class ProfessionSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly Random _random = new();

    static readonly WaitForSeconds _sCTDelay = new(0.75f);

    static readonly float _professionMultiplier = ConfigService.ProfessionMultiplier;
    static readonly int _maxProfessionLevel = ConfigService.MaxProfessionLevel;

    static readonly AssetGuid _assetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb"); // experience hexString key
    static readonly PrefabGUID _professionsSCT = new(1876501183); // SCT resource gain prefabguid
    public static void UpdateProfessions(Entity Killer, Entity Victim)
    {
        Entity userEntity = Killer.ReadRO<PlayerCharacter>().UserEntity;
        User user = userEntity.ReadRO<User>();
        ulong SteamID = user.PlatformId;

        PrefabGUID PrefabGUID = new(0);
        if (Victim.Has<YieldResourcesOnDamageTaken>() && Victim.Has<EntityCategory>())
        {
            var yield = Victim.ReadBuffer<YieldResourcesOnDamageTaken>();
            if (yield.IsCreated && !yield.IsEmpty)
            {
                PrefabGUID = yield[0].ItemType;
            }
        }
        else
        {
            return;
        }

        float ProfessionValue = Victim.ReadRO<EntityCategory>().ResourceLevel._Value;

        PrefabGUID prefab = Victim.ReadRO<PrefabGUID>();
        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];

        if (original.Has<EntityCategory>() && original.ReadRO<EntityCategory>().ResourceLevel._Value > ProfessionValue)
        {
            ProfessionValue = original.ReadRO<EntityCategory>().ResourceLevel._Value;
        }

        if (Victim.ReadRO<UnitLevel>().Level > ProfessionValue && !Victim.ReadRO<PrefabGUID>().GetPrefabName().ToLower().Contains("iron"))
        {
            ProfessionValue = Victim.ReadRO<UnitLevel>().Level;
        }

        if (ProfessionValue.Equals(0))
        {
            ProfessionValue = 10;
        }

        ProfessionValue = (int)(ProfessionValue * _professionMultiplier);

        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID);

        if (handler != null)
        {
            if (handler.GetProfessionName().Contains("Woodcutting"))
            {
                ProfessionValue *= ProfessionMappings.GetWoodcuttingModifier(PrefabGUID);
            }

            SetProfession(Victim, Killer, SteamID, ProfessionValue, handler);
            GiveProfessionBonus(prefab, Killer, user, SteamID, handler);
        }
    }
    public static void GiveProfessionBonus(PrefabGUID prefab, Entity Killer, User user, ulong SteamID, IProfessionHandler handler)
    {
        Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
        int level = GetLevel(SteamID, handler);
        string name = handler.GetProfessionName();
        if (name.Contains("Fishing"))
        {
            List<PrefabGUID> fishDrops = ProfessionMappings.GetFishingAreaDrops(prefab);
            int bonus = level / 20;
            if (bonus.Equals(0)) return;
            int index = _random.Next(fishDrops.Count);
            PrefabGUID fish = fishDrops[index];
            if (ServerGameManager.TryAddInventoryItem(Killer, fish, bonus))
            {
                if (Misc.PlayerBoolsManager.GetPlayerBool(SteamID, "ProfessionLogging")) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{fishDrops[index].GetLocalizedName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}");
            }
            else
            {
                InventoryUtilitiesServer.CreateDropItem(EntityManager, Killer, fish, bonus, new Entity());
                if (Misc.PlayerBoolsManager.GetPlayerBool(SteamID, "ProfessionLogging")) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{fishDrops[index].GetLocalizedName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}, but it dropped on the ground since your inventory was full.");
            }
        }
        else if (prefabEntity.Has<DropTableBuffer>())
        {
            var dropTableBuffer = prefabEntity.ReadBuffer<DropTableBuffer>();
            foreach (var drop in dropTableBuffer)
            {
                switch (drop.DropTrigger)
                {
                    case DropTriggerType.YieldResourceOnDamageTaken:
                        Entity dropTable = PrefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (var dropTableData in dropTableDataBuffer)
                        {
                            if (dropTableData.ItemGuid.GetPrefabName().ToLower().Contains("ingredient") || dropTableData.ItemGuid.GetPrefabName().ToLower().Contains("trippyshroom"))
                            {
                                int bonus = 0;
                                if (dropTableData.ItemGuid.GetPrefabName().ToLower().Contains("plant") || dropTableData.ItemGuid.GetPrefabName().ToLower().Contains("trippyshroom"))
                                {
                                    bonus = level / 10;
                                }
                                else
                                {
                                    bonus = level / 2;
                                }
                                if (bonus.Equals(0)) return;
                                if (ServerGameManager.TryAddInventoryItem(Killer, dropTableData.ItemGuid, bonus))
                                {
                                    if (Misc.PlayerBoolsManager.GetPlayerBool(SteamID, "ProfessionLogging")) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{dropTableData.ItemGuid.GetLocalizedName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}");
                                    break;
                                }
                                else
                                {
                                    InventoryUtilitiesServer.CreateDropItem(EntityManager, Killer, dropTableData.ItemGuid, bonus, new Entity());
                                    if (Misc.PlayerBoolsManager.GetPlayerBool(SteamID, "ProfessionLogging")) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{dropTableData.ItemGuid.GetLocalizedName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}, but it dropped on the ground since your inventory was full.");
                                    break;
                                }
                            }
                        }
                        break;
                    case DropTriggerType.OnDeath:
                        /* WIP
                        dropTable = prefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (var dropTableData in dropTableDataBuffer)
                        {
                            prefabEntity = prefabCollectionSystem._PrefabGuidToEntityMap[dropTableData.ItemGuid];
                            if (!prefabEntity.Has<ItemDataDropGroupBuffer>()) continue;
                            var itemDataDropGroupBuffer = prefabEntity.ReadBuffer<ItemDataDropGroupBuffer>();
                            foreach (var itemDataDropGroup in itemDataDropGroupBuffer)
                            {
                                Core.Log.LogInfo($"{itemDataDropGroup.DropItemPrefab.GetPrefabName()} | {itemDataDropGroup.Quantity} | {itemDataDropGroup.Weight}");
                            }
                        }
                        */
                        break;
                    default:
                        //Core.Log.LogInfo($"{drop.DropTableGuid.GetPrefabName()} | {drop.DropTrigger}");
                        break;
                }
            }
        }
    }
    public static void SetProfession(Entity target, Entity source, ulong steamID, float value, IProfessionHandler handler)
    {
        var xpData = handler.GetProfessionData(steamID);

        if (xpData.Key >= _maxProfessionLevel) return;

        UpdateProfessionExperience(target, source, steamID, xpData, value, handler);
    }
    static void UpdateProfessionExperience(Entity target, Entity source, ulong steamID, KeyValuePair<int, float> xpData, float gainedXP, IProfessionHandler handler)
    {
        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);
        bool leveledUp = false;

        if (newLevel > xpData.Key)
        {
            leveledUp = true;
            if (newLevel > _maxProfessionLevel)
            {
                newLevel = _maxProfessionLevel;
                newExperience = ConvertLevelToXp(_maxProfessionLevel);
            }
        }

        var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
        handler.SetProfessionData(steamID, updatedXPData);

        NotifyPlayer(target, source, steamID, gainedXP, leveledUp, handler);
    }
    static void NotifyPlayer(Entity target, Entity source, ulong steamID, float gainedXP, bool leveledUp, IProfessionHandler handler)
    {
        Entity userEntity = source.ReadRO<PlayerCharacter>().UserEntity;
        User user = userEntity.ReadRO<User>();

        string professionName = handler.GetProfessionName();

        if (leveledUp)
        {
            int newLevel = ConvertXpToLevel(handler.GetProfessionData(steamID).Value);
            if (newLevel < _maxProfessionLevel) LocalizationService.HandleServerReply(EntityManager, user, $"{professionName} improved to [<color=white>{newLevel}</color>]");
        }

        if (Misc.PlayerBoolsManager.GetPlayerBool(steamID, "ProfessionLogging"))
        {
            int levelProgress = GetLevelProgress(steamID, handler);
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{(int)gainedXP}</color> <color=#FFC0CB>proficiency</color> in {professionName.ToLower()} (<color=white>{levelProgress}%</color>)");
        }

        if (Misc.PlayerBoolsManager.GetPlayerBool(steamID, "ScrollingText"))
        {
            float3 targetPosition = target.ReadRO<Translation>().Value;
            float3 professionColor = handler.GetProfessionColor();

            Core.StartCoroutine(DelayedProfessionSCT(user.LocalCharacter.GetEntityOnServer(), userEntity, targetPosition, professionColor, gainedXP));
        }
    }
    static IEnumerator DelayedProfessionSCT(Entity character, Entity userEntity, float3 position, float3 color, float gainedXP)
    {
        yield return _sCTDelay;

        Entity scrollingTextEntity = ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _assetGuid, position, color, character, gainedXP, _professionsSCT, userEntity);
    }

    /*
    static int ConvertXpToLevel(float xp)
    {
        return (int)(_professionConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / _professionConstant, ProfessionPower);
    }
    */
    static float GetXp(ulong steamID, IProfessionHandler handler)
    {
        var xpData = handler.GetProfessionData(steamID);
        return xpData.Value;
    }
    static int GetLevel(ulong steamID, IProfessionHandler handler)
    {
        return ConvertXpToLevel(GetXp(steamID, handler));
    }
    public static int GetLevelProgress(ulong steamID, IProfessionHandler handler)
    {
        float currentXP = GetXp(steamID, handler);
        int currentLevelXP = ConvertLevelToXp(GetLevel(steamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevel(steamID, handler) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;
        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
}
internal static class ProfessionMappings
{
    static readonly Dictionary<string, int> _fishingMultipliers = new()
    {
        { "farbane", 1 },
        { "dunley", 2 },
        { "gloomrot", 3 },
        { "cursed", 4 },
        { "silverlight", 4 }
    };

    static readonly List<PrefabGUID> _farbaneFishDrops = new()
    {
        { new(-1642545082)} //goby
    };

    static readonly List<PrefabGUID> _dunleyFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) } //rainbow
    };

    static readonly List<PrefabGUID> _gloomrotFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) } //bloodsnapper
    };

    static readonly List<PrefabGUID> _cursedFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(177845365) } //swampdweller
    };

    static readonly List<PrefabGUID> _silverlightFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(67930804) } //goldenbassriver
    };

    static readonly Dictionary<string, List<PrefabGUID>> _fishingAreaDrops = new()
    {
        { "farbane", _farbaneFishDrops},
        { "dunley", _dunleyFishDrops},
        { "gloomrot", _gloomrotFishDrops},
        { "cursed", _cursedFishDrops},
        { "silverlight", _silverlightFishDrops}
    };

    static readonly Dictionary<string, int> _woodcuttingMultipliers = new()
    {
        { "hallow", 2 },
        { "gloom", 3 },
        { "cursed", 4 }
    };

    static readonly Dictionary<string, int> _tierMultiplier = new()
    {
        { "t01", 1 },
        { "t02", 2 },
        { "t03", 3 },
        { "t04", 4 },
        { "t05", 5 },
        { "t06", 6 },
        { "t07", 7 },
        { "t08", 8 },
        { "t09", 9 },
    };
    public static int GetFishingModifier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> location in _fishingMultipliers)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }
        return 1;
    }
    public static List<PrefabGUID> GetFishingAreaDrops(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, List<PrefabGUID>> location in _fishingAreaDrops)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
            else if (prefab.GetPrefabName().ToLower().Contains("general"))
            {
                return _farbaneFishDrops;
            }
        }
        throw new InvalidOperationException("Unrecognized fishing area");
    }
    public static int GetWoodcuttingModifier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> location in _woodcuttingMultipliers)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }

        return 1;
    }
    public static int GetTierMultiplier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> tier in _tierMultiplier)
        {
            if (prefab.GetPrefabName().ToLower().Contains(tier.Key))
            {
                return tier.Value;
            }
        }

        return 1;
    }
}