using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static ProjectM.BuffUtility;
namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarUnlockSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly Random Random = new();

    // List of banned PrefabGUIDs
    public static List<int> ExemptPrefabs = [];
    public static List<string> ExemptTypes = [];

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
    public static void ProcessUnlock(Entity killer, Entity died)
    {
        if (!died.Has<PrefabGUID>() || !died.Has<EntityCategory>()) return;

        EntityCategory diedCategory = died.Read<EntityCategory>();
        PrefabGUID diedPrefab = died.Read<PrefabGUID>();

        string lowerName = diedPrefab.LookupName().ToLower();
        if ((lowerName.Contains("trader") || lowerName.Contains("carriage") || lowerName.Contains("horse") || lowerName.Contains("werewolf")) || lowerName.Contains("tombsummon") && !lowerName.Contains("gateboss")) return; // prefab name checks

        if (IsBannedUnit(diedPrefab)) return; // banned prefab checks, no using currently
        if (IsBannedType(diedCategory)) return; // banned type checks, no using currently

        if (!died.Has<VBloodUnit>() && (int)diedCategory.UnitCategory < 5) // units
        {
            HandleRoll(ConfigService.UnitUnlockChance, died, killer);
        }
        else if (died.Has<VBloodUnit>()) // VBloods and Shadow VBloods
        {
            if (ConfigService.AllowVBloods) HandleRoll(ConfigService.VBloodUnlockChance, died, killer);
        }
    }
    static bool IsBannedUnit(PrefabGUID prefab)
    {
        return ExemptPrefabs.Contains(prefab.GuidHash);
    }
    static bool IsBannedType(EntityCategory category)
    {
        return ExemptTypes.Contains(category.UnitCategory.ToString());
    }
    public static void HandleRoll(float dropChance, Entity died, Entity killer)
    {
        if (!died.TryGetComponent(out PrefabGUID prefabGUID)) return;

        if (ConfigService.ShareUnlocks && !died.Has<VBloodConsumeSource>()) // pretty sure everyone in the vblood feed already gets their own roll, no double-dipping
        {
            HashSet<Entity> players = LevelingSystem.GetParticipants(killer, killer.Read<PlayerCharacter>().UserEntity);
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
    public static void HandleUnlock(PrefabGUID famKey, Entity player, bool capture = false)
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

        if (!isAlreadyUnlocked && capture)
        {
            List<int> currentList = data.UnlockedFamiliars[lastListName];
            currentList.Add(familiarKey);
            FamiliarUnlocksManager.SaveUnlockedFamiliars(playerId, data);
            isShiny = HandleShiny(familiarKey, playerId, ConfigService.ShinyChance);
            string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

            FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffs(playerId);
            if (buffsData.FamiliarBuffs.ContainsKey(familiarKey))
            {
                if (RandomVisuals.TryGetValue(new(buffsData.FamiliarBuffs[familiarKey].First()), out var hexColor))
                {
                    colorCode = $"<color={hexColor}>";
                }
            }

            if (!isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>{famKey.GetPrefabName()}</color> successfully dominated!"); // need better message but w/e for now
            else if (isShiny) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>{famKey.GetPrefabName()}</color>{colorCode}*</color> successfully dominated!");
            return;
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

        if (isShiny)
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