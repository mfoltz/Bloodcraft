using Bloodcraft.Services;
using Stunlock.Core;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager_V2;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Utilities;
internal static class Battles
{
    public static bool TryGetMatch(this HashSet<(ulong, ulong)> hashSet, ulong value, out (ulong, ulong) matchingPair)
    {
        matchingPair = default;

        foreach (var pair in hashSet)
        {
            if (pair.Item1 == value || pair.Item2 == value)
            {
                matchingPair = pair;

                return true;
            }
        }

        return false;
    }
    public static bool TryGetMatchPairInfo(this (ulong, ulong) matchPair, out (PlayerInfo, PlayerInfo) matchPairInfo)
    {
        matchPairInfo = default;

        ulong playerOne = matchPair.Item1;
        ulong playerTwo = matchPair.Item2;

        if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) && playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
        {
            matchPairInfo = (playerOneInfo, playerTwoInfo);

            return true;
        }

        return false;
    }
    public static void BuildBattleGroupDetailsReply(ulong steamId, FamiliarBuffsData buffsData, FamiliarPrestigeData_V2 prestigeData, List<int> battleGroup, ref List<string> familiars)
    {
        foreach (int famKey in battleGroup)
        {
            if (famKey == 0) continue;

            PrefabGUID famPrefab = new(famKey);
            string famName = famPrefab.GetLocalizedName();
            string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

            int level = Systems.Familiars.FamiliarLevelingSystem.GetFamiliarExperience(steamId, famKey).Key;
            int prestiges = 0;

            // Check if the familiar has buffs and update the color based on RandomVisuals
            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                // Look up the color from the RandomVisuals dictionary if it exists
                if (ShinyBuffColorHexes.TryGetValue(new(buffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                {
                    colorCode = $"<color={hexColor}>";
                }
            }

            if (!prestigeData.FamiliarPrestige.ContainsKey(famKey))
            {
                prestigeData.FamiliarPrestige[famKey] = new(0, []);
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);
            }
            else
            {
                prestiges = prestigeData.FamiliarPrestige[famKey].Key;
            }

            familiars.Add($"<color=white>{battleGroup.IndexOf(famKey) + 1}</color>: <color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>]");
        }
    }

    /*
    public static void HandleBattleGroupAddAndReply(ChatCommandContext ctx, ulong steamId, string groupName, ActiveFamiliarData actives, int slotIndex)
    {
        int familiarId = actives.FamiliarId;

        if (!FamiliarBattleGroupsManager.SaveFamiliarBattleGroupSlot(ctx, steamId, groupName, familiarId, --slotIndex))
        {
            return;
        }

        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
        FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

        int level = Systems.Familiars.FamiliarLevelingSystem.GetFamiliarExperience(steamId, familiarId).Key;
        int prestiges = 0;

        PrefabGUID famPrefab = new(familiarId);
        string famName = famPrefab.GetLocalizedName();
        string colorCode = "<color=#FF69B4>";

        if (buffsData.FamiliarBuffs.ContainsKey(familiarId) &&
            ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[familiarId][0]), out var hexColor))
        {
            colorCode = $"<color={hexColor}>";
        }

        if (!prestigeData.FamiliarPrestige.ContainsKey(familiarId))
        {
            prestigeData.FamiliarPrestige[familiarId] = new(0, []);
            SaveFamiliarPrestigeData_V2(steamId, prestigeData);
        }
        else
        {
            prestiges = prestigeData.FamiliarPrestige[familiarId].Key;
        }

        LocalizationService.HandleReply(ctx, $"<color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(familiarId) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>] added to <color=white>{groupName}</color>! (<color=yellow>{slotIndex}</color>)");
    }
    */
    public static void HandleBattleGroupDetailsReply(ChatCommandContext ctx, ulong steamId, FamiliarBattleGroupsManager.FamiliarBattleGroup battleGroup)
    {
        if (battleGroup.Familiars.Any(x => x != 0))
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
            FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);
            List<string> familiars = [];

            BuildBattleGroupDetailsReply(steamId, buffsData, prestigeData, battleGroup.Familiars, ref familiars);

            string familiarReply = string.Join(", ", familiars);
            LocalizationService.HandleReply(ctx, $"Battle Group - {familiarReply}");
            return;
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No familiars in battle group!");
            return;
        }
    }
}
