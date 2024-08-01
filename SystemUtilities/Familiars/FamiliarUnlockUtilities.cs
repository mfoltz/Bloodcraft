using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Core;
using static Bloodcraft.Services.LocalizationService;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarUnlockUtilities
{
    static readonly float UnitChance = Plugin.UnitUnlockChance.Value;
    static readonly float VBloodChance = Plugin.VBloodUnlockChance.Value;
    static readonly float ShinyChance = Plugin.ShinyChance.Value;
    static readonly bool allowVBloods = Plugin.AllowVBloods.Value;
    static readonly bool shareUnlocks = Plugin.ShareUnlocks.Value;
    static readonly Random Random = new();

    // List of banned PrefabGUIDs
    public static List<int> ExemptPrefabs = [];
    public static List<string> ExemptTypes = [];
    public static readonly PrefabGUID[] RandomVisuals =
    [
        new PrefabGUID(348724578),   // ignite
        new PrefabGUID(-1576512627),    // static
        new PrefabGUID(-1246704569),     // leech
        new PrefabGUID(1723455773),   // weaken
        new PrefabGUID(27300215),    // chill
        new PrefabGUID(-325758519)     // condemn
    ];
    public static void HandleUnitUnlock(Entity killer, Entity died)
    {
        if (!died.Has<PrefabGUID>() || !died.Has<EntityCategory>()) return;

        EntityCategory diedCategory = died.Read<EntityCategory>();
        PrefabGUID diedPrefab = died.Read<PrefabGUID>();
        string lowerName = diedPrefab.LookupName().ToLower();
        //Core.Log.LogInfo(lowerName);
        if (died.Has<Minion>()) return; // component checks
        if ((lowerName.Contains("trader") || lowerName.Contains("carriage") || lowerName.Contains("horse") || lowerName.Contains("werewolf")) && !lowerName.Contains("gateboss")) return; // prefab name checks
        if (IsBannedUnit(diedPrefab)) return; // banned prefab checks, no using currently
        if (IsBannedType(diedCategory)) return; // banned type checks, no using currently

        if (!died.Has<VBloodConsumeSource>() && (int)diedCategory.UnitCategory < 5) // units
        {
            HandleRoll(UnitChance, died, killer);
        }
        else if (died.Has<VBloodConsumeSource>()) // VBloods
        {
            if (allowVBloods) HandleRoll(VBloodChance, died, killer);
        }
        else if (died.Has<VBloodUnit>()) // Shadow VBloods
        {
            if (allowVBloods) HandleRoll(VBloodChance, died, killer);
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
    static void HandleRoll(float dropChance, Entity died, Entity killer)
    {
        if (shareUnlocks && !died.Has<VBloodConsumeSource>()) // pretty sure everyone in the vblood feed already gets their own roll, no double-dipping
        {
            HashSet<Entity> players = PlayerLevelingUtilities.GetParticipants(killer, killer.Read<PlayerCharacter>().UserEntity);
            foreach (Entity player in players)
            {
                if (RollForChance(dropChance)) HandleUnlock(died, player);
            }
        }
        else
        {
            if (RollForChance(dropChance)) HandleUnlock(died, killer);
        }
    }
    static void HandleUnlock(Entity died, Entity player)
    {
        int familiarKey = died.Read<PrefabGUID>().GuidHash;
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
        ulong playerId = user.PlatformId;

        DataStructures.UnlockedFamiliarData data = FamiliarUnlocksManager.LoadUnlockedFamiliars(playerId);
        string lastListName = data.UnlockedFamiliars.Keys.LastOrDefault();

        if (string.IsNullOrEmpty(lastListName) || data.UnlockedFamiliars[lastListName].Count >= 10)
        {
            lastListName = $"box{data.UnlockedFamiliars.Count + 1}";
            data.UnlockedFamiliars[lastListName] = [];
            if (Core.DataStructures.FamiliarSet[playerId] == "")
            {
                Core.DataStructures.FamiliarSet[playerId] = lastListName;
                Core.DataStructures.SavePlayerFamiliarSets();
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
            isShiny = HandleShiny(familiarKey, playerId, ShinyChance);
            if (!isShiny) HandleServerReply(Core.EntityManager, user, $"New unit unlocked: <color=green>{died.Read<PrefabGUID>().GetPrefabName()}</color>");
            else if (isShiny) HandleServerReply(Core.EntityManager, user, $"New <color=#00FFFF>shiny</color> unit unlocked: <color=green>{died.Read<PrefabGUID>().GetPrefabName()}</color>");
            return;
        }
        if (isShiny)
        {
            HandleServerReply(Core.EntityManager, user, $"<color=#00FFFF>Shiny</color> visual unlocked for unit: <color=green>{died.Read<PrefabGUID>().GetPrefabName()}</color>");
        }
    }
    public static bool HandleShiny(int famKey, ulong steamId, float chance, int choice = -1)
    {
        DataStructures.FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
        if (chance < 1f && RollForChance(chance)) // roll
        {
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                List<int> famBuffs = [];
                famBuffs.Add(RandomVisuals.ElementAt(Random.Next(RandomVisuals.Length - 1)).GuidHash);
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
                famBuffs.Add(RandomVisuals.ElementAt(Random.Next(RandomVisuals.Length - 1)).GuidHash);
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