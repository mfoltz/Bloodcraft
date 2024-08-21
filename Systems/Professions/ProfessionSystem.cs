using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.SystemUtilities.Professions;
internal static class ProfessionSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    const float ProfessionConstant = 0.1f; // constant for calculating level from xp
    const int ProfessionPower = 2; // power for calculating level from xp
    public static void UpdateProfessions(Entity Killer, Entity Victim)
    {
        if (Killer == Victim) return;

        Entity userEntity = Killer.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();
        ulong SteamID = user.PlatformId;

        if (!Victim.Has<UnitLevel>() || Victim.Has<Movement>()) return;

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

        float ProfessionValue = Victim.Read<EntityCategory>().ResourceLevel._Value;

        PrefabGUID prefab = Victim.Read<PrefabGUID>();
        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];

        if (original.Has<EntityCategory>() && original.Read<EntityCategory>().ResourceLevel._Value > ProfessionValue)
        {
            ProfessionValue = original.Read<EntityCategory>().ResourceLevel._Value;
        }

        if (Victim.Read<UnitLevel>().Level > ProfessionValue && !Victim.Read<PrefabGUID>().LookupName().ToLower().Contains("iron"))
        {
            ProfessionValue = Victim.Read<UnitLevel>().Level;
        }

        if (ProfessionValue.Equals(0))
        {
            ProfessionValue = 10;
        }

        ProfessionValue = (int)(ProfessionValue * ConfigService.ProfessionMultiplier);

        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID);

        if (handler != null)
        {
            if (handler.GetProfessionName().Contains("Woodcutting"))
            {
                ProfessionValue *= ProfessionMappings.GetWoodcuttingModifier(PrefabGUID);
            }

            SetProfession(user, SteamID, ProfessionValue, handler);
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
            Random random = new();
            int bonus = level / 20;
            if (bonus.Equals(0)) return;
            int index = random.Next(fishDrops.Count);
            PrefabGUID fish = fishDrops[index];
            if (ServerGameManager.TryAddInventoryItem(Killer, fish, bonus))
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var Bools) && Bools["ProfessionLogging"]) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{fishDrops[index].GetPrefabName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}");
            }
            else
            {
                InventoryUtilitiesServer.CreateDropItem(EntityManager, Killer, fish, bonus, new Entity());
                if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var Bools) && Bools["ProfessionLogging"]) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{fishDrops[index].GetPrefabName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}, but it dropped on the ground since your inventory was full.");
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
                            if (dropTableData.ItemGuid.LookupName().ToLower().Contains("ingredient") || dropTableData.ItemGuid.LookupName().ToLower().Contains("trippyshroom"))
                            {
                                int bonus = 0;
                                if (dropTableData.ItemGuid.LookupName().ToLower().Contains("plant") || dropTableData.ItemGuid.LookupName().ToLower().Contains("trippyshroom"))
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
                                    if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var Bools) && Bools["ProfessionLogging"]) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{dropTableData.ItemGuid.GetPrefabName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}");
                                    break;
                                }
                                else 
                                {
                                    InventoryUtilitiesServer.CreateDropItem(EntityManager, Killer, dropTableData.ItemGuid, bonus, new Entity());
                                    if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var Bools) && Bools["ProfessionLogging"]) LocalizationService.HandleServerReply(EntityManager, user, $"Bonus <color=green>{dropTableData.ItemGuid.GetPrefabName()}</color>x<color=white>{bonus}</color> received from {handler.GetProfessionName()}, but it dropped on the ground since your inventory was full.");
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
    public static void SetProfession(User user, ulong steamID, float value, IProfessionHandler handler)
    {
        var xpData = handler.GetExperienceData(steamID);

        if (xpData.Key >= ConfigService.MaxProfessionLevel) return;

        UpdateProfessionExperience(user, steamID, xpData, value, handler);
    }
    static void UpdateProfessionExperience(User user, ulong steamID, KeyValuePair<int, float> xpData, float gainedXP, IProfessionHandler handler)
    {
        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);
        bool leveledUp = false;

        if (newLevel > xpData.Key)
        {
            leveledUp = true;
            if (newLevel > ConfigService.MaxProfessionLevel)
            {
                newLevel = ConfigService.MaxProfessionLevel;
                newExperience = ConvertLevelToXp(ConfigService.MaxProfessionLevel);
            }
        }

        var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
        handler.UpdateExperienceData(steamID, updatedXPData);
        handler.SaveChanges();

        NotifyPlayer(user, steamID, gainedXP, leveledUp, handler);
    }
    static void NotifyPlayer(User user, ulong steamID, float gainedXP, bool leveledUp, IProfessionHandler handler)
    {
        string professionName = handler.GetProfessionName();
        if (leveledUp)
        {
            int newLevel = ConvertXpToLevel(handler.GetExperienceData(steamID).Value);
            if (newLevel < ConfigService.MaxProfessionLevel) LocalizationService.HandleServerReply(EntityManager, user, $"{professionName} improved to [<color=white>{newLevel}</color>]");
        }
        if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ProfessionLogging"])
        {
            int levelProgress = GetLevelProgress(steamID, handler);
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{(int)gainedXP}</color> <color=#FFC0CB>proficiency</color> in {professionName.ToLower()} (<color=white>{levelProgress}%</color>)");
        }
    }
    static int ConvertXpToLevel(float xp)
    {
        // Assuming a basic square root scaling for experience to level conversion
        return (int)(ProfessionConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        // Reversing the formula used in ConvertXpToLevel for consistency
        return (int)Math.Pow(level / ProfessionConstant, ProfessionPower);
    }
    static float GetXp(ulong steamID, IProfessionHandler handler)
    {
        var xpData = handler.GetExperienceData(steamID);
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
    static readonly Dictionary<string, int> FishingMultipliers = new()
    {
        { "farbane", 1 },
        { "dunley", 2 },
        { "gloomrot", 3 },
        { "cursed", 4 },
        { "silverlight", 4 }
    };

    static readonly List<PrefabGUID> FarbaneFishDrops = new ()
    {
        { new(-1642545082)} //goby
    };

    static readonly List<PrefabGUID> DunleyFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) } //rainbow
    };

    static readonly List<PrefabGUID> GloomrotFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) } //bloodsnapper
    };

    static readonly List<PrefabGUID> CursedFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(177845365) } //swampdweller
    };

    static readonly List<PrefabGUID> SilverlightFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(67930804) } //goldenbassriver
    };

    static readonly Dictionary<string, List<PrefabGUID>> FishingAreaDrops = new()
    {
        { "farbane", FarbaneFishDrops},
        { "dunley", DunleyFishDrops},
        { "gloomrot", GloomrotFishDrops},
        { "cursed", CursedFishDrops},
        { "silverlight", SilverlightFishDrops}
    };

    static readonly Dictionary<string, int> WoodcuttingMultipliers = new()
    {
        { "hallow", 2 },
        { "gloom", 3 },
        { "cursed", 4 }
    };

    static readonly Dictionary<string, int> TierMultiplier = new()
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
        foreach (KeyValuePair<string, int> location in FishingMultipliers)
        {
            if (prefab.LookupName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }
        return 1;
    }
    public static List<PrefabGUID> GetFishingAreaDrops(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, List<PrefabGUID>> location in FishingAreaDrops)
        {
            if (prefab.LookupName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
            else if (prefab.LookupName().ToLower().Contains("general"))
            {
                return FarbaneFishDrops;
            }
        }
        throw new InvalidOperationException("Unrecognized fishing area");
    }
    public static int GetWoodcuttingModifier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> location in WoodcuttingMultipliers)
        {
            if (prefab.LookupName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }
        return 1;
    }
    public static int GetTierMultiplier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> tier in TierMultiplier)
        {
            if (prefab.LookupName().ToLower().Contains(tier.Key))
            {
                return tier.Value;
            }
        }
        return 1;
    }
}