using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarUnlockSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly Random Random = new();

    static readonly float UnitUnlockChance = ConfigService.UnitUnlockChance;
    static readonly float VBloodUnlockChance = ConfigService.VBloodUnlockChance;
    static readonly float ShinyChance = ConfigService.ShinyChance;

    static readonly bool ShareUnlocks = ConfigService.ShareUnlocks;
    static readonly bool AllowVBloods = ConfigService.AllowVBloods;

    // HashSets of configured banned prefabGUIDs & EntityCategories
    public static readonly HashSet<PrefabGUID> ExemptPrefabGUIDs = [];
    public static readonly HashSet<UnitCategory> ExemptCategories = [];

    // HashSet of default banned strings to check against prefab names
    static readonly HashSet<string> DefaultBans =
    [
        "trader",
        "carriage",
        "horse",
        "werewolf",
        "tombsummon"
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
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        if (!ShareUnlocks) ProcessUnlock(deathEvent.Source, deathEvent.Target);
        else if (ShareUnlocks) ProcessGroupUnlock(deathEvent.Target, deathEvent.DeathParticipants);
    }
    public static void ProcessUnlock(Entity source, Entity target)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefabGUID) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefabGUID.LookupName().ToLower();

            if (DefaultBans.Any(part => targetPrefabName.Contains(part)) || BannedPrefabGUID(targetPrefabGUID) || BannedCategory(targetCategory)) return;
            else if (!target.Has<VBloodUnit>() && (int)targetCategory.UnitCategory < 5) // normal units
            {
                HandleRoll(UnitUnlockChance, targetPrefabGUID, source, false);
            }
            else if (AllowVBloods && target.Has<VBloodUnit>()) // vbloods & gatebosses
            {
                HandleRoll(VBloodUnlockChance, targetPrefabGUID, source, true);
            }
        }
    }
    public static void ProcessGroupUnlock(Entity target, HashSet<Entity> deathParticipants)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefabGUID) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefabGUID.LookupName().ToLower();

            if (DefaultBans.Any(part => targetPrefabName.Contains(part)) || BannedPrefabGUID(targetPrefabGUID) || BannedCategory(targetCategory)) return;
            else if (!target.Has<VBloodUnit>() && (int)targetCategory.UnitCategory < 5) // normal units
            {
                foreach (Entity player in deathParticipants)
                {
                    HandleRoll(UnitUnlockChance, targetPrefabGUID, player, false);
                }
            }
        }
    }
    static bool BannedPrefabGUID(PrefabGUID prefab)
    {
        return ExemptPrefabGUIDs.Contains(prefab);
    }
    static bool BannedCategory(EntityCategory category)
    {
        return ExemptCategories.Contains(category.UnitCategory);
    }
    static void HandleRoll(float dropChance, PrefabGUID targetPrefabGUID, Entity player, bool isVBlood)
    {
        if (!isVBlood && RollForChance(dropChance)) // everyone in the vblood event system already gets their own roll, no double-dipping :p
        {
            //HashSet<Entity> players = PlayerUtilities.GetDeathParticipants(source, source.Read<PlayerCharacter>().UserEntity);
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
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
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
            famData.FamiliarExperience[famKey] = new(1, FamiliarLevelingSystem.ConvertLevelToXp(1));
            FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);

            isShiny = HandleShiny(famKey, steamId, ShinyChance);

            if (!isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New unit unlocked: <color=green>{familiarPrefabGUID.GetPrefabName()}</color>");
            else if (isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>shiny</color> unit unlocked: <color=green>{familiarPrefabGUID.GetPrefabName()}</color>");
            return;
        }
        else if (isShiny)
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"<color=#00FFFF>Shiny</color> visual unlocked for unit: <color=green>{familiarPrefabGUID.GetPrefabName()}</color>");
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
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(Random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
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
                famBuffs.Add(ShinyBuffColorHexMap.ElementAt(Random.Next(ShinyBuffColorHexMap.Count)).Key.GuidHash);
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
        float roll = (float)Random.NextDouble();
        return roll < chance;
    }
}