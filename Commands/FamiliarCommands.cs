using BepInEx;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarLevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;

namespace Bloodcraft.Commands;

[CommandGroup(name: "familiar", "fam")]
internal static class FamiliarCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly PrefabGUID CombatBuff = new(581443919);
    static readonly PrefabGUID PvPCombatBuff = new(697095869);
    static readonly PrefabGUID DominateBuff = new(-1447419822);
    static readonly PrefabGUID TakeFlightBuff = new(1205505492);

    static readonly Dictionary<string, Action<ChatCommandContext, ulong>> FamiliarSettings = new()
    {
        {"VBloodEmotes", FamiliarUtilities.ToggleVBloodEmotes},
        {"Shiny", FamiliarUtilities.ToggleShinies}
    };

    static readonly ComponentType[] NetworkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<InteractEvents_Client.RenameInteractable>())
    ];

    static readonly NetworkEventType EventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_RenameInteractable,
        IsDebugEvent = false
    };

    [Command(name: "bind", shortHand: "b", adminOnly: false, usage: ".fam b [#]", description: "Activates specified familiar from current list.")]
    public static void BindFamiliar(ChatCommandContext ctx, int boxIndex)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;

        FamiliarUtilities.BindFamiliar(character, userEntity, steamId, boxIndex);
    }

    //[Command(name: "forcebind", shortHand: "fb", adminOnly: true, usage: ".fam fb [Name] [Box] [#]", description: "Activates specified familiar from entered player box.")]
    public static void ForceBindFamiliar(ChatCommandContext ctx, string name, string box, int choice)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        Entity character = playerInfo.CharEntity;
        Entity userEntity = playerInfo.UserEntity;
        ulong steamId = playerInfo.User.PlatformId;

        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);

        /* skip this for forcebind
        if (ServerGameManager.HasBuff(character, combatBuff.ToIdentifier()) || ServerGameManager.HasBuff(character, pvpCombatBuff.ToIdentifier()) || ServerGameManager.HasBuff(character, dominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't bind a familiar while in combat or dominating presence is active.");
            return;
        }
        */

        // this is still a good check though
        if (familiar.Exists())
        {
            LocalizationService.HandleReply(ctx, $"<color=white>{playerInfo.User.CharacterName.Value}</color> already has an active familiar.");
            return;
        }

        UnlockedFamiliarData unlocksData = LoadUnlockedFamiliars(steamId);

        string set = unlocksData.UnlockedFamiliars.ContainsKey(box) ? box : "";
        if (string.IsNullOrEmpty(set))
        {
            LocalizationService.HandleReply(ctx, $"Couldn't find box for <color=white>{playerInfo.User.CharacterName.Value}</color>. List player boxes by entering '<color=white>.fam lpf [Name]</color>' without a following specific box.");
            return;
        }

        if (steamId.TryGetFamiliarActives(out var data) && data.Familiar.Equals(Entity.Null) && data.FamKey.Equals(0) && unlocksData.UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            if (choice < 1 || choice > famKeys.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use <color=white>1</color> to <color=white>{famKeys.Count}</color> (Current List: <color=yellow>{set}</color>)");
                return;
            }

            PlayerUtilities.SetPlayerBool(steamId, "Binding", true);
            steamId.SetFamiliarDefault(choice);

            data = new(Entity.Null, famKeys[choice - 1]);
            steamId.SetFamiliarActives(data);

            SummonFamiliar(character, userEntity, famKeys[choice - 1]);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar or familiar already active.");
        }
    }

    //[Command(name: "listplayerfams", shortHand: "lpf", adminOnly: true, usage: ".fam lpf [Name] [Box]", description: "Lists unlocked familiars from players active box if entered and found or list all player boxes if left blank.")]
    public static void ListPlayerFamiliars(ChatCommandContext ctx, string name, string box = "")
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        if (string.IsNullOrEmpty(box))
        {
            UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

            if (data.UnlockedFamiliars.Keys.Count > 0)
            {
                List<string> sets = [];
                foreach (var key in data.UnlockedFamiliars.Keys)
                {
                    sets.Add(key);
                }

                string fams = string.Join(", ", sets.Select(set => $"<color=yellow>{set}</color>"));
                LocalizationService.HandleReply(ctx, $"Familiar Boxes for <color=white>{playerInfo.User.CharacterName.Value}</color>: {fams}");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"<color=white>{playerInfo.User.CharacterName.Value}</color> doesn't have any unlocked familiars yet.");
            }

            return;
        }
        else
        {
            UnlockedFamiliarData unlocksData = LoadUnlockedFamiliars(steamId);
            FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);

            string set = unlocksData.UnlockedFamiliars.ContainsKey(box) ? box : "";
            if (unlocksData.UnlockedFamiliars.TryGetValue(set, out var famKeys))
            {
                int count = 1;

                foreach (var famKey in famKeys)
                {
                    PrefabGUID famPrefab = new(famKey);
                    string famName = famPrefab.GetPrefabName();
                    string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

                    // Check if the familiar has buffs and update the color based on RandomVisuals
                    if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                    {
                        // Look up the color from the RandomVisuals dictionary if it exists
                        if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                        {
                            colorCode = $"<color={hexColor}>";
                        }
                    }

                    LocalizationService.HandleReply(ctx, $"<color=white>{count}</color>: <color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color>" : "")}");
                    count++;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Couldn't locate player box.");
            }
        }
    }

    [Command(name: "unbind", shortHand: "ub", adminOnly: false, usage: ".fam ub", description: "Destroys active familiar.")]
    public static void UnbindFamiliar(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;

        FamiliarUtilities.UnbindFamiliar(character, userEntity, steamId);
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

        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);

        string set = steamId.TryGetFamiliarBox(out set) ? set : "";

        if (data.UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            int count = 1;
            foreach (var famKey in famKeys)
            {
                PrefabGUID famPrefab = new(famKey);
                string famName = famPrefab.GetPrefabName();
                string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

                // Check if the familiar has buffs and update the color based on RandomVisuals
                if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                {
                    // Look up the color from the RandomVisuals dictionary if it exists
                    if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                    {
                        colorCode = $"<color={hexColor}>";
                    }
                }

                LocalizationService.HandleReply(ctx, $"<color=white>{count}</color>: <color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color>" : "")}");
                count++;
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't locate box.");
        }
    }

    [Command(name: "boxes", shortHand: "box", adminOnly: false, usage: ".fam box", description: "Shows the available familiar boxes.")]
    public static void ListFamiliarSets(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.Keys.Count > 0)
        {
            List<string> sets = [];
            foreach (var key in data.UnlockedFamiliars.Keys)
            {
                sets.Add(key);
            }

            //string fams = string.Join(", ", sets.Select(set => $"<color=white>{set}</color>"));
            //LocalizationService.HandleReply(ctx, $"Available Familiar Boxes: {fams}");

            // Chunk the response into batches of 6
            LocalizationService.HandleReply(ctx, $"Available Familiar Boxes:");

            List<string> colorizedSets = sets.Select(set => $"<color=white>{set}</color>").ToList();
            const int maxPerMessage = 6;
            for (int i = 0; i < colorizedSets.Count; i += maxPerMessage)
            {
                var batch = colorizedSets.Skip(i).Take(maxPerMessage);
                string fams = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, $"{fams}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet.");
        }
    }

    [Command(name: "choosebox", shortHand: "cb", adminOnly: false, usage: ".fam cb [Name]", description: "Choose active box of familiars.")]
    public static void ChooseSet(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var _))
        {
            steamId.SetFamiliarBox(name);
            LocalizationService.HandleReply(ctx, $"Active Familiar Box: <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box.");
        }
    }

    [Command(name: "renamebox", shortHand: "rb", adminOnly: false, usage: ".fam rb [CurrentName] [NewName]", description: "Renames a box.")]
    public static void RenameSet(ChatCommandContext ctx, string current, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (!data.UnlockedFamiliars.ContainsKey(name) && data.UnlockedFamiliars.TryGetValue(current, out var familiarSet))
        {
            // Remove the old set
            data.UnlockedFamiliars.Remove(current);

            // Add the set with the new name
            data.UnlockedFamiliars[name] = familiarSet;

            if (steamId.TryGetFamiliarBox(out var set) && set.Equals(current)) // change active set to new name if it was the old name
            {
                steamId.SetFamiliarBox(name);
            }

            // Save changes back to the FamiliarUnlocksManager
            SaveUnlockedFamiliars(steamId, data);
            LocalizationService.HandleReply(ctx, $"<color=white>{current}</color> renamed to <color=yellow>{name}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box to rename or already an existing box with desired name.");
        }
    }

    [Command(name: "movebox", shortHand: "mb", adminOnly: false, usage: ".fam mb [BoxName]", description: "Moves active familiar to specified box.")]
    public static void TransplantFamiliar(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

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
                        SaveUnlockedFamiliars(steamId, data);
                    }
                }

                PrefabGUID PrefabGUID = new(actives.FamKey);
                LocalizationService.HandleReply(ctx, $"<color=green>{PrefabGUID.GetPrefabName()}</color> moved to <color=white>{name}</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box or box is full.");
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
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var familiarSet) && familiarSet.Count == 0)
        {
            // Delete the box
            data.UnlockedFamiliars.Remove(name);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"Deleted familiar box: <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box or box is not empty.");
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
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.Count > 0 && data.UnlockedFamiliars.Count < 25)
        {
            // Add the box
            data.UnlockedFamiliars.Add(name, []);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"Added familiar box: <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Must have at least one unit unlocked and total number of boxes cannot exceed <color=yellow>25</color>.");
        }
    }

    [Command(name: "add", shortHand: "a", adminOnly: true, usage: ".fam a [Name] [PrefabGUID/CHAR_Unit_Name]", description: "Unit testing.")]
    public static void AddFamiliar(ChatCommandContext ctx, string name, string unit)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = playerInfo.User;
        ulong steamId = foundUser.PlatformId;

        if (steamId.TryGetFamiliarBox(out string activeSet)) // add to active box if one exists
        {
            FamiliarUtilities.ParseAddedFamiliar(ctx, steamId, unit, activeSet);
        }
        else // add to last existing box if one exists or add a new box
        {
            UnlockedFamiliarData unlocksData = LoadUnlockedFamiliars(steamId);
            string lastListName = unlocksData.UnlockedFamiliars.Keys.LastOrDefault();

            if (string.IsNullOrEmpty(lastListName)) // add a box if none created yet
            {
                lastListName = $"box{unlocksData.UnlockedFamiliars.Count + 1}";
                unlocksData.UnlockedFamiliars[lastListName] = [];
                SaveUnlockedFamiliars(steamId, unlocksData);
                FamiliarUtilities.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
            else
            {
                FamiliarUtilities.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
        }
    }

    [Command(name: "remove", shortHand: "r", adminOnly: false, usage: ".fam r [#]", description: "Removes familiar from current set permanently.")]
    public static void RemoveFamiliarFromSet(ChatCommandContext ctx, int choice)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (steamId.TryGetFamiliarBox(out var activeSet) && data.UnlockedFamiliars.TryGetValue(activeSet, out var familiarSet))
        {
            // Remove the old set
            if (choice < 1 || choice > familiarSet.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use <color=white>1</color> to <color=white>{familiarSet.Count}</color> (Current List:<color=yellow>{activeSet}</color>)");
                return;
            }
            PrefabGUID familiarId = new(familiarSet[choice - 1]);

            // remove from set
            familiarSet.RemoveAt(choice - 1);
            SaveUnlockedFamiliars(steamId, data);
            LocalizationService.HandleReply(ctx, $"<color=green>{familiarId.GetPrefabName()}</color> removed from <color=white>{activeSet}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find set to remove from.");
        }
    }

    [Command(name: "toggle", shortHand: "t", usage: ".fam toggle", description: "Calls or dismisses familar.", adminOnly: false)]
    public static void ToggleFamiliar(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong platformId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;

        if (ServerGameManager.HasBuff(character, DominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't call a familiar when using dominating presence!");
            return;
        }
        else if (ServerGameManager.HasBuff(character, TakeFlightBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't call a familiar when using batform!");
            return;
        }

        EmoteSystemPatch.CallDismiss(ctx.Event.User, character, platformId);
    }

    [Command(name: "togglecombat", shortHand: "c", usage: ".fam c", description: "Enables or disables combat for familiar.", adminOnly: false)]
    public static void ToggleCombat(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;

        if (ServerGameManager.HasBuff(character, DominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't toggle combat for a familiar when using dominating presence!");
            return;
        }
        else if (ServerGameManager.HasBuff(character, TakeFlightBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "You can't toggle combat for a familiar when using batform!");
            return;
        }

        ulong platformId = ctx.User.PlatformId;
        EmoteSystemPatch.CombatMode(ctx.Event.User, character, platformId);
    }

    [Command(name: "emotes", shortHand: "e", usage: ".fam e", description: "Toggle emote actions.", adminOnly: false)]
    public static void ToggleEmotes(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong platformId = ctx.User.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(platformId, "Emotes");
        LocalizationService.HandleReply(ctx, $"Emotes for familiars are {(PlayerUtilities.GetPlayerBool(platformId, "Emotes") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
    }

    [Command(name: "emoteactions", shortHand: "actions", usage: ".fam actions", description: "Shows available emote actions.", adminOnly: false)]
    public static void ListEmotes(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        List<string> emoteInfoList = [];
        foreach (var emote in EmoteSystemPatch.actions)
        {
            if (emote.Key.Equals(EmoteSystemPatch.TauntEmote)) continue;

            string emoteName = emote.Key.GetPrefabName();
            string actionName = emote.Value.Method.Name;
            emoteInfoList.Add($"<color=#FFC0CB>{emoteName}</color>: <color=yellow>{actionName}</color>");
        }

        string emotes = string.Join(", ", emoteInfoList);
        LocalizationService.HandleReply(ctx, emotes);
    }

    [Command(name: "getlevel", shortHand: "gl", adminOnly: false, usage: ".fam gl", description: "Display current familiar leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
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

            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);

            int prestigeLevel = 0;

            FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);
            if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
            {
                prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                SaveFamiliarPrestige(steamId, prestigeData);
            }
            else
            {
                prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key;
            }

            LocalizationService.HandleReply(ctx, $"Your familiar is level [<color=white>{xpData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and has <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>) ");
            if (familiar != Entity.Null)
            {
                // read stats and such here
                Health health = familiar.Read<Health>();
                UnitStats unitStats = familiar.Read<UnitStats>();

                float physicalPower = unitStats.PhysicalPower._Value;
                float spellPower = unitStats.SpellPower._Value;
                float maxHealth = health.MaxHealth._Value;
                float physCritChance = unitStats.PhysicalCriticalStrikeChance._Value;
                string physCrit = (physCritChance * 100).ToString("F0") + "%";
                float spellCritChance = unitStats.SpellCriticalStrikeChance._Value;
                string spellCrit = (spellCritChance * 100).ToString("F0") + "%";
                float healingReceived = unitStats.HealingReceived._Value;
                string healing = (healingReceived * 100).ToString("F0") + "%";
                float physResist = unitStats.PhysicalResistance._Value;
                string physRes = (physResist * 100).ToString("F0") + "%";
                float spellResist = unitStats.SpellResistance._Value;
                string spellRes = (spellResist * 100).ToString("F0") + "%";
                float ccReduction = unitStats.CCReduction._Value;
                string ccRed = (ccReduction * 100).ToString("F0") + "%";
                float shieldAbsorb = unitStats.ShieldAbsorbModifier._Value;
                string shieldAbs = (shieldAbsorb * 100).ToString("F0") + "%";

                string familiarPrestigeStats = string.Join(", ",
                    Enum.GetNames(typeof(FamiliarStatType))
                        .Select(name => $"<color=white>{name}</color>")
                );

                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>MaxHealth</color>: <color=white>{(int)maxHealth}</color>, <color=#00FFFF>PhysicalPower</color>: <color=white>{(int)physicalPower}</color>, <color=#00FFFF>SpellPower</color>: <color=white>{(int)spellPower}</color>, <color=#00FFFF>PhysCritChance</color>: <color=white>{physCrit}</color>, <color=#00FFFF>SpellCritChance</color>: <color=white>{spellCrit}</color>");
                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>HealingReceived</color>: <color=white>{healing}</color>, <color=#00FFFF>PhysResist</color>: <color=white>{physRes}</color>, <color=#00FFFF>SpellResist</color>: <color=white>{spellRes}</color>, <color=#00FFFF>CCReduction</color>: <color=white>{ccRed}</color>, <color=#00FFFF>ShieldAbsorb</color>: <color=white>{shieldAbs}</color>");
                LocalizationService.HandleReply(ctx, $"Valid options for prestige stat choice (each may only be chosen once): {familiarPrestigeStats}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find any experience for familiar.");
        }
    }

    [Command(name: "setlevel", shortHand: "sl", adminOnly: true, usage: ".fam sl [Level]", description: "Set current familiar level.")]
    public static void SetFamiliarLevel(ChatCommandContext ctx, int level)
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

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0))
        {
            Entity player = ctx.Event.SenderCharacterEntity;
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
            int famKey = data.FamKey;

            KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
            FamiliarExperienceData xpData = LoadFamiliarExperience(steamId);
            xpData.FamiliarExperience[data.FamKey] = newXP;
            SaveFamiliarExperience(steamId, xpData);

            if (ModifyFamiliar(user, steamId, famKey, player, familiar, level))
            {
                LocalizationService.HandleReply(ctx, $"Your familiar has been set to level <color=white>{level}</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar to set level for.");
        }
    }

    [Command(name: "prestige", shortHand: "pr", adminOnly: false, usage: ".fam pr [BonusStat]", description: "Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.")]
    public static void PrestigeFamiliarCommand(ChatCommandContext ctx, string bonusStat = "")
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

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0))
        {
            FamiliarExperienceData xpData = LoadFamiliarExperience(ctx.Event.User.PlatformId);
            if (xpData.FamiliarExperience[data.FamKey].Key >= ConfigService.MaxFamiliarLevel)
            {
                FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);
                if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
                {
                    prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                    SaveFamiliarPrestige(steamId, prestigeData);
                }

                prestigeData = LoadFamiliarPrestige(steamId);
                List<FamiliarStatType> stats = prestigeData.FamiliarPrestige[data.FamKey].Value;

                if (prestigeData.FamiliarPrestige[data.FamKey].Key >= ConfigService.MaxFamiliarPrestiges)
                {
                    LocalizationService.HandleReply(ctx, "Familiar is already at max prestige!");
                    return;
                }

                if (stats.Count < FamiliarStatValues.Count) // if less than max stats, parse entry and add if set doesnt already contain
                {
                    if (!FamiliarUtilities.TryParseFamiliarStat(bonusStat, out var stat))
                    {
                        var familiarStatsWithCaps = Enum.GetValues(typeof(FamiliarStatType))
                        .Cast<FamiliarStatType>()
                        .Select(stat =>
                            $"<color=#00FFFF>{stat}</color>: <color=white>{FamiliarStatValues[stat]}</color>")
                        .ToArray();

                        int halfLength = familiarStatsWithCaps.Length / 2;

                        string familiarStatsLine1 = string.Join(", ", familiarStatsWithCaps.Take(halfLength));
                        string familiarStatsLine2 = string.Join(", ", familiarStatsWithCaps.Skip(halfLength));

                        LocalizationService.HandleReply(ctx, "Invalid stat, please choose from the following:");
                        LocalizationService.HandleReply(ctx, $"Available familiar stats (1/2): {familiarStatsLine1}");
                        LocalizationService.HandleReply(ctx, $"Available familiar stats (2/2): {familiarStatsLine2}");
                        return;
                    }
                    else if (!stats.Contains(stat))
                    {
                        stats.Add(stat);
                    }
                    else
                    {
                        LocalizationService.HandleReply(ctx, $"Familiar already has <color=#00FFFF>{stat}</color> as a bonus stat, pick another.");
                        return;
                    }
                }
                else if (stats.Count >= FamiliarStatValues.Count && !string.IsNullOrEmpty(bonusStat))
                {
                    LocalizationService.HandleReply(ctx, "Familiar already has max bonus stats, try again without entering a stat.");
                    return;
                }

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1)); // reset level to 1
                xpData.FamiliarExperience[data.FamKey] = newXP;
                SaveFamiliarExperience(steamId, xpData);

                int prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key + 1;
                prestigeData.FamiliarPrestige[data.FamKey] = new(prestigeLevel, stats);
                SaveFamiliarPrestige(steamId, prestigeData);

                Entity player = ctx.Event.SenderCharacterEntity;
                Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                if (ModifyFamiliar(user, steamId, data.FamKey, player, familiar, newXP.Key))
                {
                    LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is back to level <color=white>{newXP.Key}</color>.");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Familiar must be at max level (<color=white>{ConfigService.MaxFamiliarLevel}</color>) to prestige.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar to check for prestige.");
        }
    }

    [Command(name: "reset", adminOnly: false, usage: ".fam reset", description: "Resets (destroys) entities found in followerbuffer and clears familiar actives data.")]
    public static void ResetFamiliars(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;

        var buffer = ctx.Event.SenderCharacterEntity.ReadBuffer<FollowerBuffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (EntityManager.Exists(buffer[i].Entity._Entity))
            {
                if (buffer[i].Entity._Entity.Has<Disabled>()) buffer[i].Entity._Entity.Remove<Disabled>();
                DestroyUtility.CreateDestroyEvent(EntityManager, buffer[i].Entity._Entity, DestroyReason.Default, DestroyDebugReason.None);
            }
        }

        FamiliarUtilities.ClearFamiliarActives(steamId);
        if (FamiliarUtilities.AutoCallMap.ContainsKey(character)) FamiliarUtilities.AutoCallMap.Remove(character); 

        LocalizationService.HandleReply(ctx, "Familiar actives and followers cleared.");
    }

    [Command(name: "search", shortHand: "s", adminOnly: false, usage: ".fam s [Name]", description: "Searches boxes for unit with entered name.")]
    public static void FindFamiliarBox(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;

        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
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
                        return famPrefab.GetPrefabName().ToLower().Contains(name.ToLower());
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
                    LocalizationService.HandleReply(ctx, $"Couldn't find matching familiar in boxes.");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet.");
        }
    }

    [Command(name: "shinybuff", shortHand: "shiny", adminOnly: false, usage: ".fam shiny [SpellSchool]", description: "Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.")]
    public static void SetFamiliarVisual(ChatCommandContext ctx, string spellSchool = "")
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;

        PrefabGUID visual = ShinyBuffColorHexMap.Keys
                .SingleOrDefault(prefab => prefab.LookupName().ToLower().Contains(spellSchool.ToLower()));

        if (!ShinyBuffColorHexMap.ContainsKey(visual))
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching shinyBuff from entered spell school. (options: blood, storm, unholy, chaos, frost, illusion)");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
        int famKey = familiar.Read<PrefabGUID>().GuidHash;

        if (familiar != Entity.Null)
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey)) // if no shiny unlocked already use the freebie
            {
                bool madeShinyChoice = PlayerUtilities.GetPlayerBool(steamId, "ShinyChoice");

                if (!madeShinyChoice && HandleShiny(famKey, steamId, 1f, visual.GuidHash)) // if false use free visual then set to true
                {
                    PlayerUtilities.SetPlayerBool(steamId, "ShinyChoice", true);
                    LocalizationService.HandleReply(ctx, "Visual assigned succesfully! Rebind familiar for it to take effect. Use '.fam option shiny' to enable/disable familiars showing their visual.");
                }
                else if (madeShinyChoice)
                {
                    LocalizationService.HandleReply(ctx, "You've already used your free familiar visual.");
                }
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey)) // if shiny already unlocked use prefab cost and quantity, override shiny visual with choice
            {
                if (!ConfigService.ShinyCostItemPrefab.Equals(0))
                {
                    PrefabGUID item = new(ConfigService.ShinyCostItemPrefab);
                    int quantity = ConfigService.ShinyCostItemQuantity;
                    if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                    {
                        if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity) && HandleShiny(famKey, steamId, 1f, visual.GuidHash))
                        {
                            LocalizationService.HandleReply(ctx, "Visual assigned for cost succesfully! Rebind familiar for it to take effect. Use '.fam option shiny' to enable/disable familiars showing their visual.");
                            return;
                        }
                    }
                    else
                    {
                        LocalizationService.HandleReply(ctx, $"You do not have the required item quantity to change your familiar visual (<color=#ffd9eb>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>)");
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, "No item set for visual cost.");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Make sure familiar is active and present before choosing your visual for it.");
        }
    }

    //[Command(name: "resetshiny", shortHand: "rs", adminOnly: true, usage: ".fam rs [Name]", description: "Allows player to choose another free visual, however, does not erase any visuals they have chosen previously. Mainly for testing.")] // only for testing so commenting out for now
    public static void ResetFamiliarVisualChoice(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;
        string playerName = playerInfo.User.CharacterName.Value;
        bool madeShinyChoice = PlayerUtilities.GetPlayerBool(steamId, "ShinyChoice");

        if (madeShinyChoice)
        {
            PlayerUtilities.SetPlayerBool(steamId, "ShinyChoice", false);
            LocalizationService.HandleReply(ctx, $"Visual choice reset for <color=white>{playerName}</color>. (does not remove previously chosen visuals from player data)");
        }
        else if (!madeShinyChoice)
        {
            LocalizationService.HandleReply(ctx, "Player is already able to choose a free familiar visual.");
        }
    }

    [Command(name: "toggleoption", shortHand: "option", adminOnly: false, usage: ".fam option [Setting]", description: "Toggles various familiar settings.")]
    public static void ToggleFamiliarSetting(ChatCommandContext ctx, string option)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        var action = FamiliarSettings
            .Where(kvp => kvp.Key.ToLower() == option.ToLower())
            .Select(kvp => kvp.Value)
            .FirstOrDefault();

        if (action != null)
        {
            action(ctx, steamId);
        }
        else
        {
            string validOptions = string.Join(", ", FamiliarSettings.Keys.Select(kvp => $"<color=white>{kvp}</color>"));
            LocalizationService.HandleReply(ctx, $"Invalid option. Please choose from the following: {validOptions}");
        }
    }

    //[Command(name: "name", shortHand: "n", adminOnly: false, usage: ".fam n [Name]", description: "testing")] does not work at all D:
    public static void NameFamiliar(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);
        familiar.Add<NameableInteractable>();

        if (!familiar.Exists() || !familiar.Has<NameableInteractable>())
        {
            LocalizationService.HandleReply(ctx, "Make sure familiar is active and present before naming it.");
            return;
        }

        /*
        SendEventToUser sendEventToUser = new()
        {
            UserIndex = ctx.Event.User.Index
        };
        networkEntity.Write(sendEventToUser);
        */

        FromCharacter fromCharacter = new()
        {
            Character = ctx.Event.SenderCharacterEntity,
            User = ctx.Event.SenderUserEntity
        };

        InteractEvents_Client.RenameInteractable renameInteractable = new() // named means they show their nameplate?
        {
            InteractableId = familiar.Read<NetworkId>(),
            NewName = new Unity.Collections.FixedString64Bytes(name)
        };
        
        Entity networkEntity = EntityManager.CreateEntity(NetworkEventComponents);
        networkEntity.Write(fromCharacter);
        networkEntity.Write(EventType);
        networkEntity.Write(renameInteractable);
    }
}

