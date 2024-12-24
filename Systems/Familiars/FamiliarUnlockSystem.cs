using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Concurrent;
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

    // HashSets of configured banned prefabGUIDs & EntityCategories
    public static readonly HashSet<PrefabGUID> ConfiguredPrefabGUIDBans = [];
    public static readonly HashSet<UnitCategory> ConfiguredUnitCategoryBans = [];

    // HashSet of default banned strings to check against prefab names
    static readonly HashSet<string> _defaultNameBans =
    [
        "trader",
        "carriage",
        "horse",
        "werewolf",
        "tombsummon",
        "servant"
    ];

    static readonly HashSet<PrefabGUID> _defaultPrefabGUIDBans =
    [
        new(-1584807109) // CHAR_Undead_SkeletonSoldier_Withered
    ];

    public static readonly Dictionary<PrefabGUID, string> ShinyBuffColorHexMap = new()
    {
        { new PrefabGUID(348724578), "#A020F0" },   // ignite purple (Hex: A020F0)
        { new PrefabGUID(-1576512627), "#FFD700" },  // static yellow (Hex: FFD700)
        { new PrefabGUID(-1246704569), "#FF0000" },  // leech red (Hex: FF0000)
        { new PrefabGUID(1723455773), "#008080" },   // weaken teal (Hex: 008080)
        { new PrefabGUID(27300215), "#00FFFF" },     // chill cyan (Hex: 00FFFF)
        { new PrefabGUID(-325758519), "#00FF00" }    // condemn green (Hex: 00FF00)
    };

    public static readonly ConcurrentDictionary<ulong, float> Modifiers = [];
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        if (!_shareUnlocks) ProcessUnlock(deathEvent.Source, deathEvent.Target);
        else if (_shareUnlocks) ProcessGroupUnlock(deathEvent.Target, deathEvent.DeathParticipants);
    }
    public static void ProcessUnlock(Entity source, Entity target)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefabGUID) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefabGUID.GetPrefabName().ToLower();

            if (_defaultNameBans.Any(part => targetPrefabName.Contains(part)) || BannedPrefabGUID(targetPrefabGUID) || BannedCategory(targetCategory)) return;
            else if (!target.Has<VBloodUnit>() && (int)targetCategory.UnitCategory < 5) // normal units
            {
                HandleRoll(_unitUnlockChance, targetPrefabGUID, source, false);
            }
            else if (_allowVBloods && target.Has<VBloodUnit>()) // vbloods & gatebosses
            {
                HandleRoll(_vBloodUnlockChance, targetPrefabGUID, source, true);
            }
        }
    }
    public static void ProcessGroupUnlock(Entity target, HashSet<Entity> deathParticipants)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefabGUID) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefabGUID.GetPrefabName().ToLower();

            if (_defaultNameBans.Any(part => targetPrefabName.Contains(part)) || BannedPrefabGUID(targetPrefabGUID) || BannedCategory(targetCategory)) return;
            else if (!target.Has<VBloodUnit>() && (int)targetCategory.UnitCategory < 5) // normal units
            {
                foreach (Entity player in deathParticipants)
                {
                    HandleRoll(_unitUnlockChance, targetPrefabGUID, player, false);
                }
            }
        }
    }
    static bool BannedPrefabGUID(PrefabGUID prefabGUID)
    {
        return (ConfiguredPrefabGUIDBans.Contains(prefabGUID) || _defaultPrefabGUIDBans.Contains(prefabGUID));
    }
    static bool BannedCategory(EntityCategory category)
    {
        return ConfiguredUnitCategoryBans.Contains(category.UnitCategory);
    }
    static void HandleRoll(float dropChance, PrefabGUID targetPrefabGUID, Entity player, bool isVBlood)
    {
        HandleModifier(ref dropChance, player);

        if (!isVBlood && RollForChance(dropChance)) // everyone in the vblood event system already gets their own roll, no double-dipping :p
        {
            HandleUnlock(targetPrefabGUID, player);
        }
        else if (isVBlood && RollForChance(dropChance))
        {
            HandleUnlock(targetPrefabGUID, player);
        }
    }
    static void HandleUnlock(PrefabGUID familiarPrefabGUID, Entity player)
    {
        int famKey = familiarPrefabGUID.GuidHash;
        User user = player.ReadRO<PlayerCharacter>().UserEntity.ReadRO<User>();
        ulong steamId = user.PlatformId;

        UnlockedFamiliarData data = FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
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
            FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, data);

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            famData.FamiliarExperience[famKey] = new(1, Utilities.Progression.ConvertLevelToXp(1));
            FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);

            isShiny = HandleShiny(famKey, steamId, _shinyChance);

            if (!isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New unit unlocked: <color=green>{familiarPrefabGUID.GetLocalizedName()}</color>");
            else if (isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>shiny</color> unit unlocked: <color=green>{familiarPrefabGUID.GetLocalizedName()}</color>");
            return;
        }
        else if (isShiny)
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"<color=#00FFFF>Shiny</color> visual unlocked for unit: <color=green>{familiarPrefabGUID.GetLocalizedName()}</color>");
        }
    }
    public static bool HandleShiny(int famKey, ulong steamId, float chance, int choice = -1)
    {
        FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
        if (chance < 1f && RollForChance(chance)) // roll
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(_random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey)) // only one per fam for now, keep first visual unlocked
            {
                return false;
            }

            FamiliarBuffsManager.SaveFamiliarBuffs(steamId, buffsData);
            return true;
        }
        else if (chance >= 1f && choice == -1) // guaranteed from double unlock
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(_random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey)) // only one per fam for now, keep first visual unlocked
            {
                return false;
            }

            FamiliarBuffsManager.SaveFamiliarBuffs(steamId, buffsData);
            return true;
        }
        else if (chance >= 1f && choice != -1) // guaranteed with choice from shinyChoice
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(choice);
                buffsData.FamiliarBuffs[famKey] = famBuffs;
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey)) // override visual if present in favor of choice
            {
                buffsData.FamiliarBuffs[famKey][0] = choice;
            }

            FamiliarBuffsManager.SaveFamiliarBuffs(steamId, buffsData);
            return true;
        }

        return false;
    }
    static bool RollForChance(float chance)
    {
        // float roll = (float)_random.NextDouble();
        double roll = _random.NextDouble();
        return roll < chance;
    }
    static void HandleModifier(ref float dropChance, Entity player)
    {
        ulong steamId = player.GetSteamId();

        if (Modifiers.TryGetValue(steamId, out float modifier))
        {
            dropChance += modifier;
        }
    }
}