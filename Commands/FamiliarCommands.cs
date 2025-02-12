using BepInEx;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.BattleService;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager_V2;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarLevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
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

    const int MAX_FAMS_BOX = 10;
    const int MAX_BOXES = 25;
    const float SHINY_CHANGE = 0.25f;

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _tauntEmote = new(-158502505);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);

    static readonly PrefabGUID _itemSchematic = new(2085163661);
    static readonly PrefabGUID _vampiricDust = new(805157024);

    static readonly PrefabGUID _cursedMountainBeast = new(-1936575244);
    static readonly PrefabGUID _vBloodEndGameSpawnBuff = new(-2071666138);

    static readonly Dictionary<string, Action<ChatCommandContext, ulong>> _familiarSettings = new()
    {
        {"VBloodEmotes", Familiars.ToggleVBloodEmotes},
        {"Shiny", Familiars.ToggleShinies}
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

            foreach (var famKey in famKeys)
            {
                PrefabGUID famPrefab = new(famKey);

                string famName = famPrefab.GetLocalizedName();
                string colorCode = "<color=#FF69B4>";

                if (familiarBuffsData.FamiliarBuffs.ContainsKey(famKey))
                {
                    if (ShinyBuffColorHexMap.TryGetValue(new(familiarBuffsData.FamiliarBuffs[famKey][0]), out var hexColor))
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

    [Command(name: "listboxes", shortHand: "box", adminOnly: false, usage: ".fam box", description: "Shows the available familiar boxes.")]
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

            List<string> colorizedBoxes = sets.Select(set => $"<color=white>{set}</color>").ToList();
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
            // Remove the old set
            if (steamId.TryGetFamiliarActives(out var actives) && !actives.FamKey.Equals(0))
            {
                var keys = data.UnlockedFamiliars.Keys;
                foreach (var key in keys)
                {
                    if (data.UnlockedFamiliars[key].Contains(actives.FamKey))
                    {
                        data.UnlockedFamiliars[key].Remove(actives.FamKey);
                        familiarSet.Add(actives.FamKey);

                        SaveFamiliarUnlocksData(steamId, data);
                    }
                }

                PrefabGUID PrefabGUID = new(actives.FamKey);
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

        if (data.UnlockedFamiliars.Count > 0 && data.UnlockedFamiliars.Count < MAX_BOXES)
        {
            // Add the box
            data.UnlockedFamiliars.Add(name, []);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"Added box - <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Must have at least one unit unlocked to start adding boxes. Additionally, the total number of boxes cannot exceed <color=yellow>{MAX_BOXES}</color>.");
        }
    }

    [Command(name: "add", shortHand: "a", adminOnly: true, usage: ".fam a [PlayerName] [PrefabGUID/CHAR_Unit_Name]", description: "Unit testing.")]
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
            Familiars.ParseAddedFamiliar(ctx, steamId, unit, activeSet);
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

                Familiars.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
            else
            {
                Familiars.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
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

        List<PrefabGUID> vBloodPrefabGuids = Familiars.VBloodNamePrefabGuidMap
            .Where(kvp => kvp.Key.Contains(vBlood, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .ToList();

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

                PrefabGUID spawnBuff = buffer[0].Buff;
                int tier = 0;

                if (vBloodPrefabGuid.Equals(_cursedMountainBeast))
                {
                    Familiars.VBloodSpawnBuffTierMap.TryGetValue(_vBloodEndGameSpawnBuff, out tier);
                }
                else
                {
                    Familiars.VBloodSpawnBuffTierMap.TryGetValue(spawnBuff, out tier);
                }

                PrefabGUID exoItem = new(ConfigService.ExoPrestigeReward);
                int vBloodCost = ConfigService.ExoPrestigeRewardQuantity * tier;

                if (vBloodCost <= 0)
                {
                    LocalizationService.HandleReply(ctx, $"Unable to verify cost for {vBloodPrefabGuid.GetPrefabName()}!");
                }
                else if (!PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(exoItem))
                {
                    LocalizationService.HandleReply(ctx, $"Unable to verify exo reward item! (<color=yellow>{exoItem}</color>)");
                }
                else if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.Event.SenderCharacterEntity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, exoItem) >= vBloodCost)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, exoItem, vBloodCost))
                    {
                        string lastBoxName = unlocksData.UnlockedFamiliars.Keys.LastOrDefault();

                        if (string.IsNullOrEmpty(lastBoxName) || unlocksData.UnlockedFamiliars.TryGetValue(lastBoxName, out var box) && box.Count >= MAX_FAMS_BOX)
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
                    LocalizationService.HandleReply(ctx, $"Not enough <color=#ffd9eb>{exoItem.GetLocalizedName()}</color> to verify tier for {vBloodPrefabGuid.GetPrefabName()}!");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Unable to verify tier for {vBloodPrefabGuid.GetPrefabName()}! Shouldn't really happen at this point and may want to inform the dev.");
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

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0))
        {
            var xpData = GetFamiliarExperience(steamId, data.FamKey);
            int progress = (int)(xpData.Value - ConvertLevelToXp(xpData.Key));
            int percent = GetLevelProgress(steamId, data.FamKey);

            Entity familiar = Familiars.GetActiveFamiliar(ctx.Event.SenderCharacterEntity);

            int prestigeLevel = 0;

            FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

            if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
            {
                prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);
            }
            else
            {
                prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key;
            }

            LocalizationService.HandleReply(ctx, $"Your familiar is level [<color=white>{xpData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and has <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>) ");
            
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
                        case FamiliarStatType.SpellCritChance:
                            displayValue = unitStats.SpellCriticalStrikeChance._Value == 0f
                                ? string.Empty
                                : (unitStats.SpellCriticalStrikeChance._Value * 100).ToString("F1") + "%"; 
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

                LocalizationService.HandleReply(ctx, $"<color=green>Familiar Stats:</color>");

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

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0))
        {
            Entity player = playerInfo.CharEntity;
            Entity familiar = Familiars.GetActiveFamiliar(player);
            int famKey = data.FamKey;

            KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
            FamiliarExperienceData xpData = LoadFamiliarExperienceData(steamId);
            xpData.FamiliarExperience[data.FamKey] = newXP;
            SaveFamiliarExperienceData(steamId, xpData);

            if (ModifyFamiliar(user, steamId, famKey, player, familiar, level))
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

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0))
        {
            FamiliarExperienceData xpData = LoadFamiliarExperienceData(ctx.Event.User.PlatformId);

            if (xpData.FamiliarExperience[data.FamKey].Key >= ConfigService.MaxFamiliarLevel)
            {
                FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

                if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
                {
                    prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                    SaveFamiliarPrestigeData_V2(steamId, prestigeData);
                }

                prestigeData = LoadFamiliarPrestigeData_V2(steamId);
                List<int> stats = prestigeData.FamiliarPrestige[data.FamKey].Value;

                if (prestigeData.FamiliarPrestige[data.FamKey].Key >= ConfigService.MaxFamiliarPrestiges)
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
                        // statType = value.ToString();
                        // FamiliarStatType stat = FamiliarPrestigeStats[value];

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
                xpData.FamiliarExperience[data.FamKey] = newXP;
                SaveFamiliarExperienceData(steamId, xpData);

                int prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key + 1;
                prestigeData.FamiliarPrestige[data.FamKey] = new(prestigeLevel, stats);
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);

                Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                ModifyUnitStats(familiar, newXP.Key, steamId, data.FamKey);

                if (value == -1)
                {
                    LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>!");
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>! (+<color=#00FFFF>{FamiliarPrestigeStats[value]}</color>)");
                }
            }
            else if (InventoryUtilities.TryGetInventoryEntity(EntityManager, playerCharacter, out Entity inventory) && ServerGameManager.GetInventoryItemCount(inventory, _itemSchematic) >= ConfigService.PrestigeCostItemQuantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(playerCharacter, _itemSchematic, ConfigService.PrestigeCostItemQuantity))
                {
                    Familiars.HandleFamiliarPrestige(ctx, statType, ConfigService.MaxFamiliarLevel - 1);
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Familiar attempting to prestige must be at max level (<color=white>{ConfigService.MaxFamiliarLevel}</color>) or requires <color=#ffd9eb>{_itemSchematic.GetLocalizedName()}</color><color=yellow>x</color><color=white>{ConfigService.PrestigeCostItemQuantity}</color>.");
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
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            ctx.Reply("Looks like your familiar is still able to be found; unbind it normally after calling if needed instead.");
            return;
        }

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        var buffer = playerCharacter.ReadBuffer<FollowerBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            Entity follower = buffer[i].Entity.GetEntityOnServer();

            if (follower.Exists())
            {
                follower.TryRemoveComponent<Disabled>();
                follower.Destroy();
            }
        }

        Familiars.ClearFamiliarActives(steamId);
        Familiars.AutoCallMap.TryRemove(playerCharacter, out Entity _);

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

                    string familiarName = Familiars.GetFamiliarName(new(keyValuePair.Key), buffsData);
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
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
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

        PrefabGUID spellSchoolPrefabGuid = ShinyBuffColorHexMap.Keys
                .SingleOrDefault(prefab => prefab.GetPrefabName().ToLower().Contains(spellSchool.ToLower()));

        if (!ShinyBuffColorHexMap.ContainsKey(spellSchoolPrefabGuid))
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching shinyBuff from entered spell school. (options: blood, storm, unholy, chaos, frost, illusion)");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.User.PlatformId;

        Entity familiar = Familiars.GetActiveFamiliar(character);
        int famKey = familiar.Read<PrefabGUID>().GuidHash;

        int quantity = ConfigService.ShinyCostItemQuantity;

        if (familiar.Exists())
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, _vampiricDust) >= quantity)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, _vampiricDust, quantity) && HandleShiny(famKey, steamId, 1f, spellSchoolPrefabGuid.GuidHash))
                    {
                        LocalizationService.HandleReply(ctx, "Shiny added! Rebind familiar to see effects. Use '<color=white>.fam option shiny</color>' to toggle (no chance to apply spell school debuff on hit if shiny buffs are disabled).");
                        return;
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You don't have the required amount of <color=#ffd9eb>{_vampiricDust.GetLocalizedName()}</color>! (x<color=white>{quantity}</color>)");
                }
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                quantity = (int)(quantity * SHINY_CHANGE);

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, _vampiricDust) >= quantity)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, _vampiricDust, quantity) && HandleShiny(famKey, steamId, 1f, spellSchoolPrefabGuid.GuidHash))
                    {
                        LocalizationService.HandleReply(ctx, "Shiny changed! Rebind familiar to see effects. Use '<color=white>.fam option shiny</color>' to toggle (no chance to apply spell school debuff on hit if shiny buffs are disabled).");
                        return;
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You don't have the required amount of <color=#ffd9eb>{_vampiricDust.GetLocalizedName()}</color>! (x<color=white>{quantity}</color>)");
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
            LocalizationService.HandleReply(ctx, $"Invalid option. Please choose from the following: {validOptions}");
        }
    }

    [Command(name: "battlegroup", shortHand: "bg", adminOnly: false, usage: ".bg [1/2/3]", description: "Set active familiar to battle group slot or list group if no slot entered.")]
    public static void FamiliarBattleGroupCommand(ChatCommandContext ctx, int slot = -1)
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

        if (Matchmaker.QueuedPlayers.Contains(steamId) && steamId.TryGetFamiliarBattleGroup(out var battleGroup))
        {
            var (position, timeRemaining) = GetQueuePositionAndTime(steamId);

            LocalizationService.HandleReply(ctx, $"You can't make changes to your battle group while queued! Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)");
            Familiars.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);

            return;
        }
        else if (slot == -1 && steamId.TryGetFamiliarBattleGroup(out battleGroup))
        {
            Familiars.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);

            return;
        }
        else if (slot < 1 || slot > 3)
        {
            LocalizationService.HandleReply(ctx, $"Please choose from 1-{TEAM_SIZE}.");

            return;
        }

        int slotIndex = --slot;

        if (steamId.TryGetFamiliarActives(out var actives) && !actives.FamKey.Equals(0) && steamId.TryGetFamiliarBattleGroup(out battleGroup))
        {
            Familiars.HandleBattleGroupAddAndReply(ctx, steamId, battleGroup, actives, slotIndex);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar...");
        }
    }

    [Command(name: "challenge", adminOnly: false, usage: ".fam challenge [PlayerName/cancel]", description: "Challenges player if found, use cancel to exit queue after entering if needed.")]
    public static void ChallengePlayerCommand(ChatCommandContext ctx, string name)
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

        if (name.ToLower() == "cancel")
        {
            foreach (var matchPairs in Matchmaker.MatchPairs)
            {
                if (PlayerBattleFamiliars.TryGetValue(steamId, out List<Entity> familiarsInBattle) && familiarsInBattle.Count > 0)
                {
                    ctx.Reply("Can't cancel challenge until battle is over!");
                    return;
                }
                else if (matchPairs.Item1 == steamId || matchPairs.Item2 == steamId)
                {
                    NotifyBothPlayers(matchPairs.Item1, matchPairs.Item2, "Challenge cancelled, removed from queue...");
                    CancelAndRemovePairFromQueue(matchPairs);

                    return;
                }
            }

            ctx.Reply("You're not currently queued for a battle!");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        if (playerInfo.User.PlatformId == steamId)
        {
            ctx.Reply("You can't challenge yourself!");
            return;
        }

        foreach (var challenge in EmoteSystemPatch.BattleChallenges)
        {
            if (challenge.Item1 == steamId || challenge.Item2 == steamId)
            {
                ctx.Reply("Can't challenge another player until existing challenge expires!");
                return;
            }
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

