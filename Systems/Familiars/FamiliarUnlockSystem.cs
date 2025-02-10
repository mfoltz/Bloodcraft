using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarUnlockSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly Random _random = new();

    static readonly float _unitUnlockChance = ConfigService.UnitUnlockChance;
    static readonly float _vBloodUnlockChance = ConfigService.VBloodUnlockChance;
    static readonly float _shinyChance = ConfigService.ShinyChance;

    static readonly bool _shareUnlocks = ConfigService.ShareUnlocks;
    static readonly bool _allowVBloods = ConfigService.AllowVBloods;

    static readonly PrefabGUID _shinyUnlockBuff = new(104224016);
    static readonly PrefabGUID _familiarUnlockBuff = new(620130895);

    public static readonly HashSet<PrefabGUID> ConfiguredPrefabGuidBans = [];
    public static readonly HashSet<UnitCategory> ConfiguredCategoryBans = [];

    static readonly HashSet<string> _defaultNameBans =
    [
        "trader",
        "carriage",
        "horse",
        "tombsummon",
        "servant"
    ];

    static readonly HashSet<PrefabGUID> _defaultPrefabGUIDBans =
    [
        new(-1584807109) // CHAR_Undead_SkeletonSoldier_Withered
    ];

    public static readonly Dictionary<PrefabGUID, string> ShinyBuffColorHexMap = new()
    {
        { new(348724578), "#A020F0" },   // ignite purple (Hex: A020F0)
        { new(-1576512627), "#FFD700" },  // static yellow (Hex: FFD700)
        { new(-1246704569), "#FF0000" },  // leech red (Hex: FF0000)
        { new(1723455773), "#008080" },   // weaken teal (Hex: 008080)
        { new(27300215), "#00FFFF" },     // chill cyan (Hex: 00FFFF)
        { new(-325758519), "#00FF00" }    // condemn green (Hex: 00FF00)
    };
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        if (!_shareUnlocks) ProcessUnlock(deathEvent.Source, deathEvent.Target);
        else if (_shareUnlocks) HandleGroupUnlock(deathEvent.Target, deathEvent.DeathParticipants);
    }
    public static void ProcessUnlock(Entity source, Entity target)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefabGuid) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefabGuid.GetPrefabName();

            if (!ValidTarget(targetPrefabName, targetPrefabGuid, targetCategory)) return;
            else if (!target.IsVBloodOrGateBoss() && (int)targetCategory.UnitCategory < 5)
            {
                HandleRoll(_unitUnlockChance, targetPrefabGuid, source);
            }
            else if (_allowVBloods && (target.IsVBlood() || target.IsGateBoss()))
            {
                HandleRoll(_vBloodUnlockChance, targetPrefabGuid, source);
            }
        }
    }
    public static void HandleGroupUnlock(Entity target, HashSet<Entity> deathParticipants)
    {
        foreach (Entity player in deathParticipants)
        {
            ProcessUnlock(player, target);
        }
    }
    static bool ValidTarget(string targetPrefabName, PrefabGUID targetPrefabGuid, EntityCategory targetCategory)
    {
        if (_defaultNameBans.Any(part => targetPrefabName.Contains(part, StringComparison.OrdinalIgnoreCase)) || IsBannedPrefabGuid(targetPrefabGuid) || BannedCategory(targetCategory)) return false;
        return true;
    }
    public static bool IsBannedPrefabGuid(PrefabGUID prefabGuid)
    {
        return (ConfiguredPrefabGuidBans.Contains(prefabGuid) || _defaultPrefabGUIDBans.Contains(prefabGuid));
    }
    static bool BannedCategory(EntityCategory category)
    {
        return ConfiguredCategoryBans.Contains(category.UnitCategory);
    }
    static void HandleRoll(float dropChance, PrefabGUID targetPrefabGuid, Entity playerCharacter)
    {
        if (RollForChance(dropChance))
        {
            HandleUnlock(targetPrefabGuid, playerCharacter);
        }
    }
    static void HandleUnlock(PrefabGUID targetPrefabGuid, Entity playerCharacter)
    {
        User user = playerCharacter.GetUser();
        ulong steamId = user.PlatformId;
        int famKey = targetPrefabGuid.GuidHash;

        FamiliarUnlocksData data = FamiliarUnlocksManager.LoadFamiliarUnlocksData(steamId);
        string lastListName = data.UnlockedFamiliars.Keys.LastOrDefault();

        if (string.IsNullOrEmpty(lastListName) || data.UnlockedFamiliars[lastListName].Count >= 10)
        {
            lastListName = $"box{data.UnlockedFamiliars.Count + 1}";
            data.UnlockedFamiliars[lastListName] = [];

            if (steamId.TryGetFamiliarBox(out var box) && string.IsNullOrEmpty(box))
            {
                steamId.SetFamiliarBox(lastListName);
            }
        }

        bool isAlreadyUnlocked = false;
        bool isShiny = false;

        foreach (var list in data.UnlockedFamiliars.Values)
        {
            if (list.Contains(famKey))
            {
                isAlreadyUnlocked = true;
                isShiny = HandleShiny(famKey, steamId, 1f);
                break;
            }
        }

        if (!isAlreadyUnlocked)
        {
            List<int> currentList = data.UnlockedFamiliars[lastListName];
            currentList.Add(famKey);
            FamiliarUnlocksManager.SaveFamiliarUnlocksData(steamId, data);

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);
            famData.FamiliarExperience[famKey] = new(FamiliarSummonSystem.BASE_LEVEL, Progression.ConvertLevelToXp(FamiliarSummonSystem.BASE_LEVEL));
            FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, famData);

            isShiny = HandleShiny(famKey, steamId, _shinyChance);

            if (!isShiny)
            {
                playerCharacter.TryApplyBuff(_familiarUnlockBuff);

                LocalizationService.HandleServerReply(EntityManager, user, $"New unit unlocked: <color=green>{targetPrefabGuid.GetLocalizedName()}</color>");
            }
            else if (isShiny)
            {
                playerCharacter.TryApplyBuff(_familiarUnlockBuff);
                HandleShinyUnlockBuff(playerCharacter);

                LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>shiny</color> unit unlocked: <color=green>{targetPrefabGuid.GetLocalizedName()}</color>");
            }
        }
        else if (isShiny)
        {
            HandleShinyUnlockBuff(playerCharacter);

            LocalizationService.HandleServerReply(EntityManager, user, $"<color=#00FFFF>Shiny</color> unlocked: <color=green>{targetPrefabGuid.GetLocalizedName()}</color>");
        }
    }
    public static bool HandleShiny(int famKey, ulong steamId, float chance, int choice = -1)
    {
        FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffsData(steamId);
        if (chance < 1f && RollForChance(chance)) // roll
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(_random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                return false;
            }

            FamiliarBuffsManager.SaveFamiliarBuffsData(steamId, buffsData);
            return true;
        }
        else if (chance >= 1f && choice == -1)
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(_random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                return false;
            }

            FamiliarBuffsManager.SaveFamiliarBuffsData(steamId, buffsData);
            return true;
        }
        else if (chance >= 1f && choice != -1)
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(choice);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                buffsData.FamiliarBuffs[famKey][0] = choice;
            }

            FamiliarBuffsManager.SaveFamiliarBuffsData(steamId, buffsData);
            return true;
        }

        return false;
    }
    static bool RollForChance(float chance)
    {
        // float roll = (float)_random.NextDouble();
        return _random.NextDouble() < chance;
    }
    static void HandleShinyUnlockBuff(Entity playerCharacter)
    {
        if (playerCharacter.TryApplyAndGetBuff(_shinyUnlockBuff, out Entity buffEntity))
        {
            if (!buffEntity.Has<LifeTime>())
            {
                buffEntity.AddWith((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 3f;
                    lifeTime.EndAction = LifeTimeEndAction.Destroy;
                });
            }
            if (buffEntity.Has<ServerControlsPositionBuff>()) buffEntity.Remove<ServerControlsPositionBuff>();
            if (buffEntity.Has<BuffModificationFlagData>()) buffEntity.Remove<BuffModificationFlagData>();
            if (buffEntity.Has<BlockFeedBuff>()) buffEntity.Remove<BlockFeedBuff>();
        }
    }
}