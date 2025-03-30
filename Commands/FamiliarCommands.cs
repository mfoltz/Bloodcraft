using BepInEx;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Services.BattleService;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBattleGroupsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager_V2;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarLevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.Familiars.ActiveFamiliarManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Commands;

[CommandGroup(name: "familiar", "fam")]
internal static class FamiliarCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    const int BOX_SIZE = 10;
    const int BOX_CAP = 50;

    const float SHINY_CHANGE_COST = 0.25f;
    const int SCHEMATICS_MIN = 500;
    const int SCHEMATICS_MAX = 2000;
    const int VAMPIRIC_DUST_MIN = 50;
    const int VAMPIRIC_DUST_MAX = 200;
    const int ECHOES_MIN = 1;
    const int ECHOES_MAX = 4;

    static readonly int _minLevel = PrefabCollectionSystem._PrefabGuidToEntityMap[Prefabs.CHAR_Forest_Wolf_VBlood].GetUnitLevel();
    static readonly int _maxLevel = PrefabCollectionSystem._PrefabGuidToEntityMap[Prefabs.CHAR_Vampire_Dracula_VBlood].GetUnitLevel();

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _tauntEmote = new(-158502505);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);

    static readonly PrefabGUID _itemSchematic = new(2085163661);
    static readonly PrefabGUID _vampiricDust = new(805157024);

    static readonly Dictionary<string, Action<ChatCommandContext, ulong>> _familiarSettings = new()
    {
        {"VBloodEmotes", ToggleVBloodEmotes},
        {"Shiny", ToggleShinies}
    };

    [Command(name: "bind", shortHand: "b", adminOnly: false, usage: ".fam b [#]", description: "Activates specified familiar from current list.")]
    public static void BindFamiliar(ChatCommandContext ctx, int boxIndex)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        Familiars.BindFamiliar(user, playerCharacter, boxIndex);
    }

    [Command(name: "unbind", shortHand: "ub", adminOnly: false, usage: ".fam ub", description: "Destroys active familiar.")]
    public static void UnbindFamiliar(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        Familiars.UnbindFamiliar(user, playerCharacter);
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".fam l", description: "Lists unlocked familiars from current box.")]
    public static void ListFamiliars(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;

        FamiliarUnlocksData familiarUnlocksData = LoadFamiliarUnlocksData(steamId);
        FamiliarBuffsData familiarBuffsData = LoadFamiliarBuffsData(steamId);
        FamiliarExperienceData familiarExperienceData = LoadFamiliarExperienceData(steamId);
        FamiliarPrestigeData_V2 familiarPrestigeData_V2 = LoadFamiliarPrestigeData_V2(steamId);

        string box = steamId.TryGetFamiliarBox(out box) ? box : string.Empty;

        if (!string.IsNullOrEmpty(box) && familiarUnlocksData.UnlockedFamiliars.TryGetValue(box, out var famKeys))
        {
            int count = 1;
            LocalizationService.HandleReply(ctx, $"<color=white>{box}</color>:");

            foreach (var famKey in famKeys)
            {
                PrefabGUID famPrefab = new(famKey);

                string famName = famPrefab.GetLocalizedName();
                string colorCode = "<color=#FF69B4>";

                if (familiarBuffsData.FamiliarBuffs.ContainsKey(famKey))
                {
                    if (ShinyBuffColorHexes.TryGetValue(new(familiarBuffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                    {
                        colorCode = $"<color={hexColor}>";
                    }
                }

                int level = familiarExperienceData.FamiliarExperience.TryGetValue(famKey, out var experienceData) ? experienceData.Key : 1;
                int prestiges = familiarPrestigeData_V2.FamiliarPrestige.TryGetValue(famKey, out var prestigeData) ? prestigeData.Key : 0;

                string levelAndPrestiges = prestiges > 0 ? $"[<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>]" : $"[<color=white>{level}</color>]";
                LocalizationService.HandleReply(ctx, $"<color=yellow>{count}</color>| <color=green>{famName}</color>{(familiarBuffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color> {levelAndPrestiges}" : $" {levelAndPrestiges}")}");
                count++;
            }
        }
        else if (string.IsNullOrEmpty(box))
        {
            // LocalizationService.HandleReply(ctx, "No active box! Try using <color=white>'.fam sb [Name]'</color> if you know the familiar you're looking for. (use quotes for names with a space)");
            LocalizationService.HandleReply(ctx, "Couldn't find active box!");
        }
    }

    [Command(name: "listboxes", shortHand: "boxes", adminOnly: false, usage: ".fam boxes", description: "Shows the available familiar boxes.")]
    public static void ListFamiliarSets(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (data.UnlockedFamiliars.Keys.Count > 0)
        {
            List<string> sets = [];
            foreach (var key in data.UnlockedFamiliars.Keys)
            {
                sets.Add(key);
            }

            LocalizationService.HandleReply(ctx, $"Familiar Boxes:");

            List<string> colorizedBoxes = [..sets.Select(set => $"<color=white>{set}</color>")];
            const int maxPerMessage = 6;
            for (int i = 0; i < colorizedBoxes.Count; i += maxPerMessage)
            {
                var batch = colorizedBoxes.Skip(i).Take(maxPerMessage);
                string fams = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, $"{fams}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocks yet!");
        }
    }

    [Command(name: "choosebox", shortHand: "cb", adminOnly: false, usage: ".fam cb [Name]", description: "Choose active box of familiars.")]
    public static void SelectBoxCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var _))
        {
            steamId.SetFamiliarBox(name);
            LocalizationService.HandleReply(ctx, $"Box Selected - <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box!");
        }
    }

    [Command(name: "renamebox", shortHand: "rb", adminOnly: false, usage: ".fam rb [CurrentName] [NewName]", description: "Renames a box.")]
    public static void RenameBoxCommand(ChatCommandContext ctx, string current, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (!data.UnlockedFamiliars.ContainsKey(name) && data.UnlockedFamiliars.TryGetValue(current, out var familiarBox))
        {
            // Remove the old set
            data.UnlockedFamiliars.Remove(current);

            // Add the set with the new name
            data.UnlockedFamiliars[name] = familiarBox;

            if (steamId.TryGetFamiliarBox(out var set) && set.Equals(current)) // change active set to new name if it was the old name
            {
                steamId.SetFamiliarBox(name);
            }

            // Save changes back to the FamiliarUnlocksManager
            SaveFamiliarUnlocksData(steamId, data);
            LocalizationService.HandleReply(ctx, $"Box <color=white>{current}</color> renamed - <color=yellow>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box to rename or there's already a box with that name!");
        }
    }

    [Command(name: "movebox", shortHand: "mb", adminOnly: false, usage: ".fam mb [BoxName]", description: "Moves active familiar to specified box.")]
    public static void MoveFamiliar(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var familiarSet) && familiarSet.Count < 10)
        {
            if (steamId.HasActiveFamiliar())
            {
                ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
                int familiarId = activeFamiliar.FamiliarId;

                var keys = data.UnlockedFamiliars.Keys;

                foreach (var key in keys)
                {
                    if (data.UnlockedFamiliars[key].Contains(familiarId))
                    {
                        data.UnlockedFamiliars[key].Remove(familiarId);
                        familiarSet.Add(familiarId);

                        SaveFamiliarUnlocksData(steamId, data);
                    }
                }

                PrefabGUID PrefabGUID = new(familiarId);
                LocalizationService.HandleReply(ctx, $"<color=green>{PrefabGUID.GetLocalizedName()}</color> moved - <color=white>{name}</color>");
            }
        }
        else if (data.UnlockedFamiliars.ContainsKey(name))
        {
            LocalizationService.HandleReply(ctx, "Box is full!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box!");
        }
    }

    [Command(name: "deletebox", shortHand: "db", adminOnly: false, usage: ".fam db [BoxName]", description: "Deletes specified box if empty.")]
    public static void DeleteBoxCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var familiarSet) && familiarSet.Count == 0)
        {
            // Delete the box
            data.UnlockedFamiliars.Remove(name);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"Deleted box - <color=white>{name}</color>");
        }
        else if (data.UnlockedFamiliars.ContainsKey(name))
        {
            LocalizationService.HandleReply(ctx, "Box is not empty!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box!");
        }
    }

    [Command(name: "addbox", shortHand: "ab", adminOnly: false, usage: ".fam ab [BoxName]", description: "Adds empty box with name.")]
    public static void AddBoxCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (data.UnlockedFamiliars.Count > 0 && data.UnlockedFamiliars.Count < BOX_CAP)
        {
            // Add the box
            data.UnlockedFamiliars.Add(name, []);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"Added box - <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Must have at least one unit unlocked to start adding boxes. Additionally, the total number of boxes cannot exceed <color=yellow>{BOX_CAP}</color>.");
        }
    }

    [Command(name: "add", shortHand: "a", adminOnly: true, usage: ".fam a [PlayerName] [PrefabGuid/CHAR_Unit_Name]", description: "Unit testing.")]
    public static void AddFamiliar(ChatCommandContext ctx, string name, string unit)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = playerInfo.User;
        ulong steamId = foundUser.PlatformId;

        if (steamId.TryGetFamiliarBox(out string activeSet) && !string.IsNullOrEmpty(activeSet))
        {
            ParseAddedFamiliar(ctx, steamId, unit, activeSet);
        }
        else
        {
            FamiliarUnlocksData unlocksData = LoadFamiliarUnlocksData(steamId);
            string lastListName = unlocksData.UnlockedFamiliars.Keys.LastOrDefault();

            if (string.IsNullOrEmpty(lastListName))
            {
                lastListName = $"box{unlocksData.UnlockedFamiliars.Count + 1}";
                unlocksData.UnlockedFamiliars[lastListName] = [];

                SaveFamiliarUnlocksData(steamId, unlocksData);

                ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
            else
            {
                ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
        }
    }

    [Command(name: "echoes", adminOnly: false, usage: ".fam echoes [VBloodName]", description: "VBlood purchasing for exo reward with quantity scaling to unit tier.")] // reminding me to deal with werewolves, eventually >_>
    public static void PurchaseVBloodCommand(ChatCommandContext ctx, string vBlood)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        else if (!ConfigService.AllowVBloods)
        {
            LocalizationService.HandleReply(ctx, "VBlood familiars are not enabled.");
            return;
        }
        else if (!ConfigService.PrimalEchoes)
        {
            LocalizationService.HandleReply(ctx, "VBlood purchasing is not enabled.");
            return;
        }

        List<PrefabGUID> vBloodPrefabGuids = [..VBloodNamePrefabGuidMap
            .Where(kvp => kvp.Key.Contains(vBlood, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)];

        if (!vBloodPrefabGuids.Any())
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching vBlood!");
            return;
        }
        else if (vBloodPrefabGuids.Count > 1)
        {
            LocalizationService.HandleReply(ctx, "Multiple matches, please be more specific!");
            return;
        }

        PrefabGUID vBloodPrefabGuid = vBloodPrefabGuids.First();

        if (IsBannedPrefabGuid(vBloodPrefabGuid))
        {
            LocalizationService.HandleReply(ctx, $"<color=white>{vBloodPrefabGuid.GetLocalizedName()}</color> is not available per configured familiar bans!");
            return;
        }
        else
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(vBloodPrefabGuid, out Entity prefabEntity) && prefabEntity.TryGetBuffer<SpawnBuffElement>(out var buffer) && !buffer.IsEmpty)
            {
                ulong steamId = ctx.Event.User.PlatformId;
                FamiliarUnlocksData unlocksData = LoadFamiliarUnlocksData(steamId);

                if (unlocksData.UnlockedFamiliars.Values.Any(list => list.Contains(vBloodPrefabGuid.GuidHash)))
                {
                    LocalizationService.HandleReply(ctx, $"<color=white>{vBloodPrefabGuid.GetLocalizedName()}</color> is already unlocked!");
                    return;
                }

                int unitLevel = prefabEntity.GetUnitLevel();
                int scaledCostFactor = Mathf.RoundToInt(Mathf.Lerp(1, 25, (unitLevel - _minLevel) / (float)(_maxLevel - _minLevel)));

                PrefabGUID exoItem = new(ConfigService.ExoPrestigeReward);

                int baseCost = ConfigService.ExoPrestigeRewardQuantity * scaledCostFactor;
                int clampedFactor = Mathf.Clamp(ConfigService.EchoesFactor, ECHOES_MIN, ECHOES_MAX);

                int factoredCost = clampedFactor * baseCost;

                if (factoredCost <= 0)
                {
                    LocalizationService.HandleReply(ctx, $"Unable to verify cost for {vBloodPrefabGuid.GetPrefabName()}!");
                }
                else if (!PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(exoItem))
                {
                    LocalizationService.HandleReply(ctx, $"Unable to verify exo prestige reward item! (<color=yellow>{exoItem}</color>)");
                }
                else if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.Event.SenderCharacterEntity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, exoItem) >= factoredCost)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, exoItem, factoredCost))
                    {
                        string lastBoxName = unlocksData.UnlockedFamiliars.Keys.LastOrDefault();

                        if (string.IsNullOrEmpty(lastBoxName) || unlocksData.UnlockedFamiliars.TryGetValue(lastBoxName, out var box) && box.Count >= BOX_SIZE)
                        {
                            lastBoxName = $"box{unlocksData.UnlockedFamiliars.Count + 1}";

                            unlocksData.UnlockedFamiliars[lastBoxName] = [];
                            unlocksData.UnlockedFamiliars[lastBoxName].Add(vBloodPrefabGuid.GuidHash);

                            SaveFamiliarUnlocksData(steamId, unlocksData);
                            LocalizationService.HandleReply(ctx, $"New unit unlocked: <color=green>{vBloodPrefabGuid.GetLocalizedName()}</color>");
                        }
                        else if (unlocksData.UnlockedFamiliars.ContainsKey(lastBoxName))
                        {
                            unlocksData.UnlockedFamiliars[lastBoxName].Add(vBloodPrefabGuid.GuidHash);

                            SaveFamiliarUnlocksData(steamId, unlocksData);
                            LocalizationService.HandleReply(ctx, $"New unit unlocked: <color=green>{vBloodPrefabGuid.GetLocalizedName()}</color>");
                        }
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Not enough <color=#ffd9eb>{exoItem.GetLocalizedName()}</color>x<color=white>{factoredCost}</color> for {vBloodPrefabGuid.GetPrefabName()}!");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Unable to verify tier for {vBloodPrefabGuid.GetPrefabName()}! Shouldn't really happen at this point and may want to inform the developer.");
                return;
            }
        }
    }

    [Command(name: "remove", shortHand: "r", adminOnly: false, usage: ".fam r [#]", description: "Removes familiar from current set permanently.")]
    public static void DeleteFamiliarCommand(ChatCommandContext ctx, int choice)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (steamId.TryGetFamiliarBox(out var activeBox) && data.UnlockedFamiliars.TryGetValue(activeBox, out var familiarSet))
        {
            if (choice < 1 || choice > familiarSet.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use <color=white>1</color> to <color=white>{familiarSet.Count}</color> (Current List:<color=yellow>{activeBox}</color>)");
                return;
            }

            PrefabGUID familiarId = new(familiarSet[choice - 1]);

            familiarSet.RemoveAt(choice - 1);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=green>{familiarId.GetLocalizedName()}</color> removed from <color=white>{activeBox}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar box to remove from...");
        }
    }

    [Command(name: "toggle", shortHand: "t", usage: ".fam t", description: "Calls or dismisses familar.", adminOnly: false)]
    public static void ToggleFamiliarCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;

        if (ServerGameManager.HasBuff(playerCharacter, _dominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't call a familiar when using dominating presence!");
            return;
        }
        else if (ServerGameManager.HasBuff(playerCharacter, _takeFlightBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't call a familiar when using batform!");
            return;
        }

        EmoteSystemPatch.CallDismiss(ctx.Event.User, playerCharacter, steamId);
    }

    [Command(name: "togglecombat", shortHand: "c", usage: ".fam c", description: "Enables or disables combat for familiar.", adminOnly: false)]
    public static void ToggleCombatCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;

        if (ServerGameManager.HasBuff(playerCharacter, _dominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't toggle combat for a familiar when using dominating presence!");
            return;
        }
        else if (ServerGameManager.HasBuff(playerCharacter, _takeFlightBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't toggle combat for a familiar when using batform!");
            return;
        }
        else if (playerCharacter.HasBuff(_pveCombatBuff) || playerCharacter.HasBuff(_pvpCombatBuff))
        {
            LocalizationService.HandleReply(ctx, "You can't toggle combat for a familiar in PvP/PvE combat!");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        EmoteSystemPatch.CombatMode(ctx.Event.User, playerCharacter, steamId);
    }

    [Command(name: "emotes", shortHand: "e", usage: ".fam e", description: "Toggle emote actions.", adminOnly: false)]
    public static void ToggleEmoteActionsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        TogglePlayerBool(steamId, EMOTE_ACTIONS_KEY);

        LocalizationService.HandleReply(ctx, $"Emote actions {(GetPlayerBool(steamId, EMOTE_ACTIONS_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}!");
    }

    [Command(name: "emoteactions", shortHand: "actions", usage: ".fam actions", description: "Shows available emote actions.", adminOnly: false)]
    public static void ListEmoteActionsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        List<string> emoteInfoList = [];
        foreach (var emote in EmoteSystemPatch.EmoteActions)
        {
            if (emote.Key.Equals(_tauntEmote)) continue;

            string emoteName = emote.Key.GetLocalizedName();
            string actionName = emote.Value.Method.Name;
            emoteInfoList.Add($"<color=#FFC0CB>{emoteName}</color>: <color=yellow>{actionName}</color>");
        }

        string emotes = string.Join(", ", emoteInfoList);
        LocalizationService.HandleReply(ctx, emotes);
    }

    [Command(name: "getlevel", shortHand: "gl", adminOnly: false, usage: ".fam gl", description: "Display current familiar leveling progress.")]
    public static void GetFamiliarLevelCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.HasActiveFamiliar())
        {
            ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
            int familiarId = activeFamiliar.FamiliarId;

            var xpData = GetFamiliarExperience(steamId, familiarId);
            int progress = (int)(xpData.Value - ConvertLevelToXp(xpData.Key));
            int percent = GetLevelProgress(steamId, familiarId);

            Entity familiar = GetActiveFamiliar(ctx.Event.SenderCharacterEntity);

            int prestigeLevel = 0;

            FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

            if (!prestigeData.FamiliarPrestige.ContainsKey(familiarId))
            {
                prestigeData.FamiliarPrestige[familiarId] = new(0, []);
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);
            }
            else
            {
                prestigeLevel = prestigeData.FamiliarPrestige[familiarId].Key;
            }

            LocalizationService.HandleReply(ctx, $"Your familiar is level [<color=white>{xpData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and has <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)!");
            
            if (familiar.Exists())
            {
                Health health = familiar.Read<Health>();
                UnitStats unitStats = familiar.Read<UnitStats>();
                AbilityBar_Shared abilityBar_Shared = familiar.Read<AbilityBar_Shared>();

                AiMoveSpeeds originalMoveSpeeds = familiar.GetPrefabEntity().Read<AiMoveSpeeds>();
                AiMoveSpeeds aiMoveSpeeds = familiar.Read<AiMoveSpeeds>();

                LifeLeech lifeLeech = new()
                {
                    PrimaryLeechFactor = new(0f),
                    PhysicalLifeLeechFactor = new(0f),
                    SpellLifeLeechFactor = new(0f),
                    AffectRecovery = false
                };
                
                if (familiar.Has<LifeLeech>())
                {
                    LifeLeech familiarLifeLeech = familiar.Read<LifeLeech>();

                    lifeLeech.PrimaryLeechFactor._Value = familiarLifeLeech.PrimaryLeechFactor._Value;
                    lifeLeech.PhysicalLifeLeechFactor._Value = familiarLifeLeech.PhysicalLifeLeechFactor._Value;
                    lifeLeech.SpellLifeLeechFactor._Value = familiarLifeLeech.SpellLifeLeechFactor._Value;
                }

                List<KeyValuePair<string, string>> statPairs = [];

                foreach (FamiliarStatType statType in Enum.GetValues(typeof(FamiliarStatType)))
                {
                    string statName = statType.ToString();
                    string displayValue;

                    switch (statType)
                    {
                        case FamiliarStatType.MaxHealth:
                            displayValue = ((int)health.MaxHealth._Value).ToString();
                            break;
                        case FamiliarStatType.PhysicalPower:
                            displayValue = ((int)unitStats.PhysicalPower._Value).ToString();
                            break;
                        case FamiliarStatType.SpellPower:
                            displayValue = ((int)unitStats.SpellPower._Value).ToString();
                            break;
                        case FamiliarStatType.PrimaryLifeLeech:
                            displayValue = lifeLeech.PrimaryLeechFactor._Value == 0f
                                ? string.Empty
                                : (lifeLeech.PrimaryLeechFactor._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.PhysicalLifeLeech:
                            displayValue = lifeLeech.PhysicalLifeLeechFactor._Value == 0f
                                ? string.Empty
                                : (lifeLeech.PhysicalLifeLeechFactor._Value * 100).ToString("F1") + "%"; 
                            break;
                        case FamiliarStatType.SpellLifeLeech:
                            displayValue = lifeLeech.SpellLifeLeechFactor._Value == 0f
                                ? string.Empty
                                : (lifeLeech.SpellLifeLeechFactor._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.PhysicalCritChance:
                            displayValue = unitStats.PhysicalCriticalStrikeChance._Value == 0f
                                ? string.Empty
                                : (unitStats.PhysicalCriticalStrikeChance._Value * 100).ToString("F1") + "%"; 
                            break;
                        case FamiliarStatType.PhysicalCritDamage:
                            displayValue = unitStats.PhysicalCriticalStrikeDamage._Value == 2f
                                ? string.Empty
                                : ((unitStats.PhysicalCriticalStrikeDamage._Value - 1f) * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.SpellCritChance:
                            displayValue = unitStats.SpellCriticalStrikeChance._Value == 0f
                                ? string.Empty
                                : (unitStats.SpellCriticalStrikeChance._Value * 100).ToString("F1") + "%"; 
                            break;
                        case FamiliarStatType.SpellCritDamage:
                            displayValue = unitStats.SpellCriticalStrikeDamage._Value == 2f
                                ? string.Empty
                                : ((unitStats.SpellCriticalStrikeDamage._Value - 1f) * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.HealingReceived:
                            displayValue = unitStats.HealingReceived._Value == 0f
                                ? string.Empty
                                : (unitStats.HealingReceived._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.DamageReduction:
                            displayValue = unitStats.DamageReduction._Value == 0f
                                ? string.Empty
                                : (unitStats.DamageReduction._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.PhysicalResistance:
                            displayValue = unitStats.PhysicalResistance._Value == 0f
                                ? string.Empty
                                : (unitStats.PhysicalResistance._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.SpellResistance:
                            displayValue = unitStats.SpellResistance._Value == 0f
                                ? string.Empty
                                : (unitStats.SpellResistance._Value * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.MovementSpeed:
                            displayValue = aiMoveSpeeds.Walk._Value == originalMoveSpeeds.Walk._Value
                                ? string.Empty
                                : ((aiMoveSpeeds.Walk._Value / originalMoveSpeeds.Walk._Value) * 100).ToString("F1") + "%";
                            break;
                        case FamiliarStatType.CastSpeed:
                            displayValue = abilityBar_Shared.AttackSpeed._Value == 1f
                                ? string.Empty
                                : (abilityBar_Shared.AttackSpeed._Value * 100).ToString("F1") + "%";
                            break;
                        default:
                            continue;
                    }

                    if (!string.IsNullOrEmpty(displayValue)) statPairs.Add(new KeyValuePair<string, string>(statName, displayValue));
                }

                string shinyInfo = GetShinyInfo(buffsData, familiar, familiarId);
                string familiarName = GetFamiliarName(familiarId, buffsData);

                string infoHeader = string.IsNullOrEmpty(shinyInfo) ? $"{familiarName}:" : $"{familiarName} - {shinyInfo}";
                LocalizationService.HandleReply(ctx, infoHeader);

                for (int i = 0; i < statPairs.Count; i += 4)
                {
                    var batch = statPairs.Skip(i).Take(4);
                    string line = string.Join(
                        ", ",
                        batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>")
                    );

                    LocalizationService.HandleReply(ctx, $"{line}");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar!");
        }
    }

    [Command(name: "setlevel", shortHand: "sl", adminOnly: true, usage: ".fam sl [Player] [Level]", description: "Set current familiar level.")]
    public static void SetFamiliarLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (level < 1 || level > ConfigService.MaxFamiliarLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 1 and {ConfigService.MaxFamiliarLevel}");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User user = playerInfo.User;
        ulong steamId = user.PlatformId;

        if (steamId.HasActiveFamiliar())
        {
            Entity playerCharacter = playerInfo.CharEntity;
            Entity familiar = GetActiveFamiliar(playerCharacter);

            ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
            int familiarId = activeFamiliar.FamiliarId;

            KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
            FamiliarExperienceData xpData = LoadFamiliarExperienceData(steamId);
            xpData.FamiliarExperience[familiarId] = newXP;
            SaveFamiliarExperienceData(steamId, xpData);

            if (ModifyFamiliar(user, steamId, familiarId, playerCharacter, familiar, level))
            {
                LocalizationService.HandleReply(ctx, $"Active familiar for <color=green>{user.CharacterName.Value}</color> has been set to level <color=white>{level}</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar....");
        }
    }

    [Command(name: "listprestigestats", shortHand: "lst", adminOnly: false, usage: ".fam lst", description: "Display options for familiar prestige stats.")]
    public static void ListPrestigeStatsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        // Determine which stats the user's active familiar already has.
        ulong steamId = ctx.Event.User.PlatformId;
        List<int> existingStats = [];

        if (steamId.HasActiveFamiliar())
        {
            ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
            int familiarId = activeFamiliar.FamiliarId;

            FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);
            if (prestigeData.FamiliarPrestige.TryGetValue(familiarId, out var famPrestige))
            {
                existingStats = famPrestige.Value;
            }
        }

        // Build the prestigeStats list, skipping any the user already has
        var prestigeStats = FamiliarPrestigeStats
            .Select((stat, index) => new { stat, index })
            .Where(x => !existingStats.Contains(x.index)) // skip stats the familiar already has
            .Select(x =>
                $"<color=yellow>{x.index + 1}</color>| " +
                $"<color=#00FFFF>{x.stat}</color>: " +
                $"<color=white>{Misc.FormatPercentStatValue(FamiliarBaseStatValues[x.stat])}</color>"
            )
            .ToList();

        // Output them in batches of 4 as before
        for (int i = 0; i < prestigeStats.Count; i += 4)
        {
            var batch = prestigeStats.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);

            LocalizationService.HandleReply(ctx, replyMessage);
        }

        /*
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        var prestigeStats = FamiliarPrestigeStats
            .Select((stat, index) =>
                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>: <color=white>{Misc.FormatPercentStatValue(FamiliarBaseStatValues[stat])}</color>")
            .ToList();

        for (int i = 0; i < prestigeStats.Count; i += 4)
        {
            var batch = prestigeStats.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);

            LocalizationService.HandleReply(ctx, replyMessage);
        }
        */
    }

    [Command(name: "prestige", shortHand: "pr", adminOnly: false, usage: ".fam pr [Stat]", description: "Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.")]
    public static void PrestigeFamiliarCommand(ChatCommandContext ctx, string statType = "")
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarPrestige)
        {
            LocalizationService.HandleReply(ctx, "Familiar prestige is not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (steamId.HasActiveFamiliar())
        {
            ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
            int familiarId = activeFamiliar.FamiliarId;

            FamiliarExperienceData xpData = LoadFamiliarExperienceData(ctx.Event.User.PlatformId);
            int clampedCost = Mathf.Clamp(ConfigService.PrestigeCostItemQuantity, SCHEMATICS_MIN, SCHEMATICS_MAX);

            if (InventoryUtilities.TryGetInventoryEntity(EntityManager, playerCharacter, out Entity inventory) && ServerGameManager.GetInventoryItemCount(inventory, _itemSchematic) >= clampedCost)
            {
                HandleFamiliarPrestige(ctx, statType, clampedCost);
            }
            else if (xpData.FamiliarExperience[familiarId].Key >= ConfigService.MaxFamiliarLevel)
            {
                FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

                if (!prestigeData.FamiliarPrestige.ContainsKey(familiarId))
                {
                    prestigeData.FamiliarPrestige[familiarId] = new(0, []);
                    SaveFamiliarPrestigeData_V2(steamId, prestigeData);
                }

                prestigeData = LoadFamiliarPrestigeData_V2(steamId);
                List<int> stats = prestigeData.FamiliarPrestige[familiarId].Value;

                if (prestigeData.FamiliarPrestige[familiarId].Key >= ConfigService.MaxFamiliarPrestiges)
                {
                    LocalizationService.HandleReply(ctx, "Familiar is already at maximum number of prestiges!");
                    return;
                }

                int value = -1;

                if (stats.Count < FamiliarPrestigeStats.Count) // if less than max stats, parse entry and add if set doesnt already contain
                {
                    if (int.TryParse(statType, out value))
                    {
                        int length = FamiliarPrestigeStats.Count;

                        if (value < 1 || value > length)
                        {
                            LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat type, use '<color=white>.fam lst</color>' to see options.");
                            return;
                        }
                        
                        --value;

                        if (!stats.Contains(value))
                        {
                            stats.Add(value);
                        }
                        else
                        {
                            LocalizationService.HandleReply(ctx, $"Familiar already has <color=#00FFFF>{FamiliarPrestigeStats[value]}</color> (<color=yellow>{value + 1}</color>) from prestiging, use '<color=white>.fam lst</color>' to see options.");
                            return;
                        }
                    }
                    else
                    {
                        LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat, use '<color=white>.fam lst</color>' to see options.");
                        return;
                    }
                }
                else if (stats.Count >= FamiliarPrestigeStats.Count && !string.IsNullOrEmpty(statType))
                {
                    LocalizationService.HandleReply(ctx, "Familiar already has all prestige stats! ('<color=white>.fam pr</color>' instead of '<color=white>.fam pr [PrestigeStat]</color>')");
                    return;
                }

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1)); // reset level to 1
                xpData.FamiliarExperience[familiarId] = newXP;
                SaveFamiliarExperienceData(steamId, xpData);

                int prestigeLevel = prestigeData.FamiliarPrestige[familiarId].Key + 1;
                prestigeData.FamiliarPrestige[familiarId] = new(prestigeLevel, stats);
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);

                Entity familiar = GetActiveFamiliar(playerCharacter);
                ModifyUnitStats(familiar, newXP.Key, steamId, familiarId);

                if (value == -1)
                {
                    LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>!");
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>! (+<color=#00FFFF>{FamiliarPrestigeStats[value]}</color>)");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Familiar attempting to prestige must be at max level (<color=white>{ConfigService.MaxFamiliarLevel}</color>) or requires <color=#ffd9eb>{_itemSchematic.GetLocalizedName()}</color><color=yellow>x</color><color=white>{clampedCost}</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar for prestiging!");
        }
    }

    [Command(name: "reset", adminOnly: false, usage: ".fam reset", description: "Resets (destroys) entities found in followerbuffer and clears familiar actives data.")]
    public static void ResetFamiliarsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        Entity familiar = GetActiveFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            ctx.Reply("Looks like your familiar is still able to be found; unbind it normally after calling if it's dismissed instead.");
            return;
        }

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        var buffer = ctx.Event.SenderCharacterEntity.ReadBuffer<FollowerBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            Entity follower = buffer[i].Entity.GetEntityOnServer();

            if (follower.Exists())
            {
                follower.TryRemove<Disabled>();
                follower.TryDestroy();
            }
        }

        ResetActiveFamiliarData(steamId);
        AutoCallMap.TryRemove(playerCharacter, out Entity _);

        LocalizationService.HandleReply(ctx, "Familiar actives and followers cleared.");
    }

    [Command(name: "search", shortHand: "s", adminOnly: false, usage: ".fam s [Name]", description: "Searches boxes for familiar(s) with matching name.")]
    public static void FindFamiliarCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;

        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
        int count = data.UnlockedFamiliars.Keys.Count;

        if (count > 0)
        {
            List<string> foundBoxNames = [];

            if (name.Equals("vblood", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var box in data.UnlockedFamiliars)
                {
                    var matchingFamiliars = box.Value.Where(famKey =>
                    {
                        Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(famKey), out prefabEntity) ? prefabEntity : Entity.Null;
                        return (prefabEntity.Has<VBloodConsumeSource>() || prefabEntity.Has<VBloodUnit>());
                    }).ToList();

                    if (matchingFamiliars.Count > 0)
                    {
                        bool boxHasShiny = matchingFamiliars.Any(familiar => buffsData.FamiliarBuffs.ContainsKey(familiar));

                        if (boxHasShiny)
                        {
                            foundBoxNames.Add($"<color=white>{box.Key}</color><color=#AA336A>*</color>");
                        }
                        else
                        {
                            foundBoxNames.Add($"<color=white>{box.Key}</color>");
                        }
                    }
                }

                if (foundBoxNames.Count > 0)
                {
                    string foundBoxes = string.Join(", ", foundBoxNames);
                    string message = $"VBlood familiar(s) found in: {foundBoxes}";
                    LocalizationService.HandleReply(ctx, message);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Couldn't find matching familiar in boxes.");
                }
            }
            else if (!name.IsNullOrWhiteSpace())
            {
                foreach (var box in data.UnlockedFamiliars)
                {
                    var matchingFamiliars = box.Value.Where(famKey =>
                    {
                        PrefabGUID famPrefab = new(famKey);
                        return famPrefab.GetLocalizedName().Contains(name, StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                    if (matchingFamiliars.Count > 0)
                    {
                        bool boxHasShiny = matchingFamiliars.Any(familiar => buffsData.FamiliarBuffs.ContainsKey(familiar));

                        if (boxHasShiny)
                        {
                            foundBoxNames.Add($"<color=white>{box.Key}</color><color=#AA336A>*</color>");
                        }
                        else
                        {
                            foundBoxNames.Add($"<color=white>{box.Key}</color>");
                        }
                    }
                }

                if (foundBoxNames.Count > 0)
                {
                    string foundBoxes = string.Join(", ", foundBoxNames);
                    string message = $"Matching familiar(s) found in: {foundBoxes}";
                    LocalizationService.HandleReply(ctx, message);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Couldn't find any matches...");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet.");
        }
    }

    [Command(name: "smartbind", shortHand: "sb", adminOnly: false, usage: ".fam sb [Name]", description: "Searches and binds a familiar. If multiple matches are found, returns a list for clarification.")]
    public static void SmartBindFamiliarCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        ulong steamId = user.PlatformId;

        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

        var shinyFamiliars = buffsData.FamiliarBuffs;
        Dictionary<string, Dictionary<string, int>> foundBoxMatches = [];

        if (data.UnlockedFamiliars.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "You haven't unlocked any familiars yet!");
            return;
        }

        foreach (var box in data.UnlockedFamiliars)
        {
            var matchingFamiliars = box.Value
                .Select((famKey, index) => new { FamKey = famKey, Index = index })
                .Where(item =>
                {
                    PrefabGUID famPrefab = new(item.FamKey);
                    return famPrefab.GetLocalizedName().Contains(name, StringComparison.OrdinalIgnoreCase);
                })
                .ToDictionary(
                    item => item.FamKey,
                    item => item.Index + 1
                );

            if (matchingFamiliars.Any())
            {
                foreach (var keyValuePair in matchingFamiliars)
                {
                    if (!foundBoxMatches.ContainsKey(box.Key))
                    {
                        foundBoxMatches[box.Key] = [];
                    }

                    string familiarName = GetFamiliarName(keyValuePair.Key, buffsData);
                    foundBoxMatches[box.Key][familiarName] = keyValuePair.Value;
                }
            }
        }

        if (!foundBoxMatches.Any())
        {
            LocalizationService.HandleReply(ctx, $"Couldn't find any matches...");
        }
        else if (foundBoxMatches.Count == 1)
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);
            steamId.SetFamiliarBox(foundBoxMatches.Keys.First());

            if (familiar.Exists() && steamId.TryGetFamiliarBox(out string box) && foundBoxMatches.TryGetValue(box, out Dictionary<string, int> nameAndIndex))
            {
                int index = nameAndIndex.Any() ? nameAndIndex.First().Value : -1;
                Familiars.UnbindFamiliar(user, playerCharacter, true, index);
            }
            else if (steamId.TryGetFamiliarBox(out box) && foundBoxMatches.TryGetValue(box, out nameAndIndex))
            {
                int index = nameAndIndex.Any() ? nameAndIndex.First().Value : -1;
                Familiars.BindFamiliar(user, playerCharacter, index);
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Multiple matches found! SmartBind doesn't support this yet... (WIP)");
        }
    }

    [Command(name: "shinybuff", shortHand: "shiny", adminOnly: false, usage: ".fam shiny [SpellSchool]", description: "Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.")]
    public static void ShinyFamiliarCommand(ChatCommandContext ctx, string spellSchool = "")
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PrefabGUID spellSchoolPrefabGuid = ShinyBuffColorHexes.Keys
                .SingleOrDefault(prefab => prefab.GetPrefabName().ToLower().Contains(spellSchool.ToLower()));

        if (!ShinyBuffColorHexes.ContainsKey(spellSchoolPrefabGuid))
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching shinyBuff from entered spell school. (options: blood, storm, unholy, chaos, frost, illusion)");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.User.PlatformId;

        Entity familiar = GetActiveFamiliar(character);
        int famKey = familiar.GetGuidHash();

        int clampedCost = Mathf.Clamp(ConfigService.ShinyCostItemQuantity, VAMPIRIC_DUST_MIN, VAMPIRIC_DUST_MAX);

        if (familiar.Exists())
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, _vampiricDust) >= clampedCost)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, _vampiricDust, clampedCost) && HandleShiny(famKey, steamId, 1f, spellSchoolPrefabGuid.GuidHash))
                    {
                        LocalizationService.HandleReply(ctx, "Shiny added! Rebind familiar to see effects. Use '<color=white>.fam option shiny</color>' to toggle (no chance to apply spell school debuff on hit if shiny buffs are disabled).");
                        return;
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You don't have the required amount of <color=#ffd9eb>{_vampiricDust.GetLocalizedName()}</color>! (x<color=white>{clampedCost}</color>)");
                }
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                int changeQuantity = (int)(clampedCost * SHINY_CHANGE_COST);

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, _vampiricDust) >= changeQuantity)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, _vampiricDust, changeQuantity) && HandleShiny(famKey, steamId, 1f, spellSchoolPrefabGuid.GuidHash))
                    {
                        LocalizationService.HandleReply(ctx, "Shiny changed! Rebind familiar to see effects. Use '<color=white>.fam option shiny</color>' to toggle (no chance to apply spell school debuff on hit if shiny buffs are disabled).");
                        return;
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You don't have the required amount of <color=#ffd9eb>{_vampiricDust.GetLocalizedName()}</color>! (x<color=white>{changeQuantity}</color>)");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar...");
        }
    }

    [Command(name: "toggleoption", shortHand: "option", adminOnly: false, usage: ".fam option [Setting]", description: "Toggles various familiar settings.")]
    public static void ToggleFamiliarSettingCommand(ChatCommandContext ctx, string option)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        var action = _familiarSettings
            .Where(kvp => kvp.Key.ToLower() == option.ToLower())
            .Select(kvp => kvp.Value)
            .FirstOrDefault();

        if (action != null)
        {
            action(ctx, steamId);
        }
        else
        {
            string validOptions = string.Join(", ", _familiarSettings.Keys.Select(kvp => $"<color=white>{kvp}</color>"));
            LocalizationService.HandleReply(ctx, $"Valid options: {validOptions}");
        }
    }

    [Command(name: "listbattlegroups", shortHand: "bgs", adminOnly: false, usage: ".fam bgs", description: "Lists available battle groups.")]
    public static void ListBattleGroupsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        FamiliarBattleGroupsData data = LoadFamiliarBattleGroupsData(steamId);

        if (data.BattleGroups.Count > 0)
        {
            List<string> battleGroupNames = [..data.BattleGroups.Select(bg => bg.Name)];
            LocalizationService.HandleReply(ctx, "Familiar Battle Groups:");

            List<string> formattedGroups = [..battleGroupNames.Select(bg => $"<color=white>{bg}</color>")];
            const int maxPerMessage = 5;

            for (int i = 0; i < formattedGroups.Count; i += maxPerMessage)
            {
                string groups = string.Join(", ", formattedGroups.Skip(i).Take(maxPerMessage));
                LocalizationService.HandleReply(ctx, groups);
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You have no battle groups.");
        }
    }

    [Command(name: "listbattlegroup", shortHand: "bg", adminOnly: false, usage: ".fam bg [BattleGroup]", description: "Displays details of the specified battle group, or the active one if none is given.")]
    public static void ShowBattleGroupCommand(ChatCommandContext ctx, string groupName = "")
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (string.IsNullOrEmpty(groupName))
        {
            groupName = GetActiveBattleGroupName(steamId);
            if (string.IsNullOrEmpty(groupName))
            {
                LocalizationService.HandleReply(ctx, "No active battle group selected! Use <color=white>.fam cbg [Name]</color> to select one.");
                return;
            }
        }

        var battleGroup = GetFamiliarBattleGroup(steamId, groupName);
        FamiliarBattleGroupsManager.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);
    }

    [Command(name: "choosebattlegroup", shortHand: "cbg", adminOnly: false, usage: ".fam cbg [BattleGroup]", description: "Sets active battle group.")]
    public static void ChooseBattleGroupCommand(ChatCommandContext ctx, string groupName)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (SetActiveBattleGroup(ctx, steamId, groupName))
        {
            LocalizationService.HandleReply(ctx, $"Active battle group set to <color=white>{groupName}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Battle group not found.");
        }
    }

    [Command(name: "addbattlegroup", shortHand: "abg", adminOnly: false, usage: ".fam abg [BattleGroup]", description: "Creates new battle group.")]
    public static void AddBattleGroupCommand(ChatCommandContext ctx, string groupName)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (CreateBattleGroup(ctx, steamId, groupName))
        {
            LocalizationService.HandleReply(ctx, $"Battle group <color=white>{groupName}</color> created.");
        }
    }

    [Command(name: "slotbattlegroup", shortHand: "sbg", adminOnly: false, usage: ".fam sbg [BattleGroupOrSlot] [Slot]", description: "Assigns active familiar to a battle group slot. If no battle group is specified, assigns to active group.")]
    public static void SetFamiliarInBattleGroupCommand(ChatCommandContext ctx, string groupOrSlot, int slotIndex = default)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        string groupName;

        if (int.TryParse(groupOrSlot, out int parsedSlot))
        {
            slotIndex = parsedSlot;
            groupName = GetActiveBattleGroupName(steamId);
        }
        else
        {
            groupName = groupOrSlot;
        }

        if (string.IsNullOrEmpty(groupName))
        {
            LocalizationService.HandleReply(ctx, "No active battle group selected! Use <color=white>.fam cbg [Name]</color> to select one.");
            return;
        }

        if (slotIndex < 1 || slotIndex > 3)
        {
            LocalizationService.HandleReply(ctx, "Slot input out of range! (use <color=white>1, 2,</color> or <color=white>3</color>)");
            return;
        }

        if (AssignFamiliarToGroup(ctx, steamId, groupName, slotIndex))
        {
            LocalizationService.HandleReply(ctx, $"Familiar assigned to <color=white>{groupName}</color> in slot {slotIndex}.");
        }
    }

    [Command(name: "deletebattlegroup", shortHand: "dbg", adminOnly: false, usage: ".fam dbg [BattleGroup]", description: "Deletes a battle group.")]
    public static void DeleteBattleGroupCommand(ChatCommandContext ctx, string groupName)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (DeleteBattleGroup(ctx, steamId, groupName))
        {
            LocalizationService.HandleReply(ctx, $"Deleted battle group <color=white>{groupName}</color>.");
        }
    }

    [Command(name: "challenge", adminOnly: false, usage: ".fam challenge [PlayerName]", description: "Challenges a player to battle or displays queue details.")]
    public static void ChallengePlayerCommand(ChatCommandContext ctx, string name = "")
    {
        if (!ConfigService.FamiliarSystem || !ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        bool isQueued = Matchmaker.QueuedPlayers.Contains(steamId);

        if (string.IsNullOrEmpty(name))
        {
            if (isQueued)
            {
                var (position, timeRemaining) = GetQueuePositionAndTime(steamId);
                LocalizationService.HandleReply(ctx, $"Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You're not currently queued for battle! Use '<color=white>.fam challenge [PlayerName]</color>' to challenge another player.");
            }
            return;
        }

        if (isQueued)
        {
            var (position, timeRemaining) = GetQueuePositionAndTime(steamId);
            LocalizationService.HandleReply(ctx, $"You can't challenge another player while queued for battle! Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply("Couldn't find player.");
            return;
        }

        if (playerInfo.User.PlatformId == steamId)
        {
            ctx.Reply("You can't challenge yourself!");
            return;
        }

        if (Matchmaker.QueuedPlayers.Contains(playerInfo.User.PlatformId))
        {
            LocalizationService.HandleReply(ctx, $"<color=green>{playerInfo.User.CharacterName}</color> is already queued for battle!");
            return;
        }

        if (EmoteSystemPatch.BattleChallenges.Any(challenge => challenge.Item1 == steamId || challenge.Item2 == steamId))
        {
            ctx.Reply("Can't challenge another player until an existing challenge expires or is declined!");
            return;
        }

        if (EmoteSystemPatch.BattleChallenges.Any(challenge => challenge.Item1 == playerInfo.User.PlatformId || challenge.Item2 == playerInfo.User.PlatformId))
        {
            ctx.Reply($"<color=green>{playerInfo.User.CharacterName}</color> already has a pending challenge!");
            return;
        }

        EmoteSystemPatch.BattleChallenges.Add((ctx.User.PlatformId, playerInfo.User.PlatformId));
        ctx.Reply($"Challenged <color=white>{playerInfo.User.CharacterName.Value}</color> to a battle! (<color=yellow>30s</color> until it expires)");
        LocalizationService.HandleServerReply(EntityManager, playerInfo.User, $"<color=white>{ctx.User.CharacterName.Value}</color> has challenged you to a battle! (<color=yellow>30s</color> until it expires, accept by emoting '<color=green>Yes</color>' or decline by emoting '<color=red>No</color>')");

        ChallengeExpiredRoutine((ctx.User.PlatformId, playerInfo.User.PlatformId)).Start();
    }

    [Command(name: "setbattlearena", shortHand: "sba", adminOnly: true, usage: ".fam sba", description: "Set current position as the center for the familiar battle arena.")]
    public static void SetBattleArenaCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!ConfigService.FamiliarBattles)
        {
            LocalizationService.HandleReply(ctx, "Familiar battles are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;

        float3 location = character.Read<Translation>().Value;
        List<float> floats = [location.x, location.y, location.z];

        DataService.PlayerDictionaries._familiarBattleCoords.Clear();
        DataService.PlayerDictionaries._familiarBattleCoords.Add(floats);
        DataService.PlayerPersistence.SaveFamiliarBattleCoords();

        if (_battlePosition.Equals(float3.zero))
        {
            Initialize();
            LocalizationService.HandleReply(ctx, "Familiar arena position set, battle service started! (only one arena currently allowed)");
        }
        else
        {
            FamiliarBattleCoords.Clear();
            FamiliarBattleCoords.Add(location);

            LocalizationService.HandleReply(ctx, "Familiar arena position changed! (only one arena currently allowed)");
        }
    }
}

