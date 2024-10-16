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

    static readonly Random Random = new();

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

    public static readonly Dictionary<PrefabGUID, string> RandomVisuals = new()
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
        ProcessUnlock(deathEvent.Source, deathEvent.Target);
    }
    public static void ProcessUnlock(Entity source, Entity target)
    {
        if (target.TryGetComponent(out PrefabGUID targetPrefab) && target.TryGetComponent(out EntityCategory targetCategory))
        {
            string targetPrefabName = targetPrefab.LookupName().ToLower();

            if (DefaultBans.Any(part => targetPrefabName.Contains(part)) || BannedPrefabGUID(targetPrefab) || BannedCategory(targetCategory)) return;
            else if (!target.Has<VBloodUnit>() && (int)targetCategory.UnitCategory < 5) // normal units
            {
                HandleRoll(ConfigService.UnitUnlockChance, target, source);
            }
            else if (ConfigService.AllowVBloods && target.Has<VBloodUnit>()) // vbloods & gatebosses
            {
                HandleRoll(ConfigService.VBloodUnlockChance, target, source);
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
    static void HandleRoll(float dropChance, Entity died, Entity killer)
    {
        if (!died.TryGetComponent(out PrefabGUID prefabGUID)) return;

        if (ConfigService.ShareUnlocks && !died.Has<VBloodConsumeSource>()) // everyone in the vblood feed already gets their own roll, no double-dipping :p
        {
            HashSet<Entity> players = PlayerUtilities.GetDeathParticipants(killer, killer.Read<PlayerCharacter>().UserEntity);

            foreach (Entity player in players)
            {
                if (RollForChance(dropChance)) HandleUnlock(prefabGUID, player);
            }
        }
        else
        {
            if (RollForChance(dropChance)) HandleUnlock(prefabGUID, killer);
        }
    }
    static void HandleUnlock(PrefabGUID famKey, Entity player)
    {
        int familiarKey = famKey.GuidHash;
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
        ulong playerId = user.PlatformId;

        UnlockedFamiliarData data = FamiliarUnlocksManager.LoadUnlockedFamiliars(playerId);
        string lastListName = data.UnlockedFamiliars.Keys.LastOrDefault();

        if (string.IsNullOrEmpty(lastListName) || data.UnlockedFamiliars[lastListName].Count >= 10)
        {
            lastListName = $"box{data.UnlockedFamiliars.Count + 1}";
            data.UnlockedFamiliars[lastListName] = [];
            if (playerId.TryGetFamiliarBox(out var box) && string.IsNullOrEmpty(box))
            {
                playerId.SetFamiliarBox(lastListName);
            }
        }

        bool isAlreadyUnlocked = false;
        bool isShiny = false;

        foreach (var list in data.UnlockedFamiliars.Values)
        {
            if (list.Contains(familiarKey))
            {
                isAlreadyUnlocked = true;
                isShiny = HandleShiny(familiarKey, playerId, 1f);
                break;
            }
        }

        if (!isAlreadyUnlocked)
        {
            List<int> currentList = data.UnlockedFamiliars[lastListName];
            currentList.Add(familiarKey);
            FamiliarUnlocksManager.SaveUnlockedFamiliars(playerId, data);

            isShiny = HandleShiny(familiarKey, playerId, ConfigService.ShinyChance);

            if (!isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New unit unlocked: <color=green>{famKey.GetPrefabName()}</color>");
            else if (isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>shiny</color> unit unlocked: <color=green>{famKey.GetPrefabName()}</color>");
            return;
        }
        else if (isShiny)
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"<color=#00FFFF>Shiny</color> visual unlocked for unit: <color=green>{famKey.GetPrefabName()}</color>");
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
                famBuffs.Add(RandomVisuals.ElementAt(Random.Next(RandomVisuals.Count)).Key.GuidHash);
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
                famBuffs.Add(RandomVisuals.ElementAt(Random.Next(RandomVisuals.Count)).Key.GuidHash);
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