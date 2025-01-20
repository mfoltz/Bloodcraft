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
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarLevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Commands;

[CommandGroup(name: "familiar", "fam")]
internal static class FamiliarCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    const int BOX_MAX = 25;
    const float SHINY_CHANGE = 0.25f;

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _tauntEmote = new(-158502505);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);

    static readonly PrefabGUID _itemSchematic = new(2085163661);
    static readonly PrefabGUID _vampiricDust = new(805157024);

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

        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);

        string set = steamId.TryGetFamiliarBox(out set) ? set : "";

        if (data.UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            int count = 1;

            foreach (var famKey in famKeys)
            {
                PrefabGUID famPrefab = new(famKey);

                string famName = famPrefab.GetLocalizedName();
                string colorCode = "<color=#FF69B4>";

                if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                {
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
            LocalizationService.HandleReply(ctx, "Couldn't find familiar box...");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.Keys.Count > 0)
        {
            List<string> sets = [];
            foreach (var key in data.UnlockedFamiliars.Keys)
            {
                sets.Add(key);
            }

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
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet...");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var _))
        {
            steamId.SetFamiliarBox(name);
            LocalizationService.HandleReply(ctx, $"Active Familiar Box: <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar box...");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

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
            LocalizationService.HandleReply(ctx, "Couldn't find familiar box to rename or there's an existing box with that name already!");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

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
                LocalizationService.HandleReply(ctx, $"<color=green>{PrefabGUID.GetLocalizedName()}</color> moved to <color=white>{name}</color>.");
            }
        }
        else if (data.UnlockedFamiliars.ContainsKey(name))
        {
            LocalizationService.HandleReply(ctx, "Familiar box is full!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar box...");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.TryGetValue(name, out var familiarSet) && familiarSet.Count == 0)
        {
            // Delete the box
            data.UnlockedFamiliars.Remove(name);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"Deleted familiar box: <color=white>{name}</color>");
        }
        else if (data.UnlockedFamiliars.ContainsKey(name))
        {
            LocalizationService.HandleReply(ctx, "Familiar box is not empty!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar box...");
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (data.UnlockedFamiliars.Count > 0 && data.UnlockedFamiliars.Count < BOX_MAX)
        {
            // Add the box
            data.UnlockedFamiliars.Add(name, []);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"Added familiar box - <color=white>{name}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Must have at least one unit unlocked to start adding boxes. Additionally, the total number of boxes cannot exceed <color=yellow>{BOX_MAX}</color>.");
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
            FamiliarUnlocksData unlocksData = LoadUnlockedFamiliars(steamId);
            string lastListName = unlocksData.UnlockedFamiliars.Keys.LastOrDefault();

            if (string.IsNullOrEmpty(lastListName))
            {
                lastListName = $"box{unlocksData.UnlockedFamiliars.Count + 1}";
                unlocksData.UnlockedFamiliars[lastListName] = [];

                SaveUnlockedFamiliars(steamId, unlocksData);

                Familiars.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
            }
            else
            {
                Familiars.ParseAddedFamiliar(ctx, steamId, unit, lastListName);
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
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (steamId.TryGetFamiliarBox(out var activeBox) && data.UnlockedFamiliars.TryGetValue(activeBox, out var familiarSet))
        {
            if (choice < 1 || choice > familiarSet.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use <color=white>1</color> to <color=white>{familiarSet.Count}</color> (Current List:<color=yellow>{activeBox}</color>)");
                return;
            }

            PrefabGUID familiarId = new(familiarSet[choice - 1]);

            familiarSet.RemoveAt(choice - 1);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=green>{familiarId.GetLocalizedName()}</color> removed from <color=white>{activeBox}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar box to remove from...");
        }
    }

    [Command(name: "toggle", shortHand: "t", usage: ".fam t", description: "Calls or dismisses familar.", adminOnly: false)]
    public static void ToggleFamiliar(ChatCommandContext ctx)
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
    public static void ToggleCombat(ChatCommandContext ctx)
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
    public static void ToggleEmotes(ChatCommandContext ctx)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        Misc.PlayerBoolsManager.TogglePlayerBool(steamId, "Emotes");

        LocalizationService.HandleReply(ctx, $"Emotes for familiars are {(Misc.PlayerBoolsManager.GetPlayerBool(steamId, "Emotes") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
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
            var xpData = Systems.Familiars.FamiliarLevelingSystem.GetFamiliarExperience(steamId, data.FamKey);
            int progress = (int)(xpData.Value - ConvertLevelToXp(xpData.Key));
            int percent = GetLevelProgress(steamId, data.FamKey);

            Entity familiar = Familiars.FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);

            int prestigeLevel = 0;

            FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);
            if (!prestigeData.FamiliarPrestiges.ContainsKey(data.FamKey))
            {
                prestigeData.FamiliarPrestiges[data.FamKey] = new(0, []);
                SaveFamiliarPrestige(steamId, prestigeData);
            }
            else
            {
                prestigeLevel = prestigeData.FamiliarPrestiges[data.FamKey].Key;
            }

            LocalizationService.HandleReply(ctx, $"Your familiar is level [<color=white>{xpData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and has <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>) ");
            if (familiar != Entity.Null)
            {
                // read stats and such here
                Health health = familiar.Read<Health>();
                UnitStats unitStats = familiar.Read<UnitStats>();
                AbilityBar_Shared abilityBar_Shared = familiar.Read<AbilityBar_Shared>();

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
                float primaryAttackSpeed = abilityBar_Shared.PrimaryAttackSpeed._Value;
                string primaryAttack = (primaryAttackSpeed * 100).ToString("F0") + "%";
                float shieldAbsorb = unitStats.ShieldAbsorbModifier._Value;
                string shieldAbs = (shieldAbsorb * 100).ToString("F0") + "%";

                string familiarPrestigeStats = string.Join(", ",
                    Enum.GetNames(typeof(FamiliarStatType))
                        .Select(name => $"<color=white>{name}</color>")
                );

                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>MaxHealth</color>: <color=white>{(int)maxHealth}</color>, <color=#00FFFF>PhysicalPower</color>: <color=white>{(int)physicalPower}</color>, <color=#00FFFF>SpellPower</color>: <color=white>{(int)spellPower}</color>, <color=#00FFFF>PhysCritChance</color>: <color=white>{physCrit}</color>, <color=#00FFFF>SpellCritChance</color>: <color=white>{spellCrit}</color>");
                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>HealingReceived</color>: <color=white>{healing}</color>, <color=#00FFFF>PhysResist</color>: <color=white>{physRes}</color>, <color=#00FFFF>SpellResist</color>: <color=white>{spellRes}</color>, <color=#00FFFF>CCReduction</color>: <color=white>{primaryAttack}</color>, <color=#00FFFF>ShieldAbsorb</color>: <color=white>{shieldAbs}</color>");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find any experience for familiar.");
        }
    }

    [Command(name: "setlevel", shortHand: "sl", adminOnly: true, usage: ".fam sl [Player] [Level]", description: "Set current familiar level.")]
    public static void SetFamiliarLevel(ChatCommandContext ctx, string name, int level)
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
            Entity familiar = Familiars.FindPlayerFamiliar(player);
            int famKey = data.FamKey;

            KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
            FamiliarExperienceData xpData = LoadFamiliarExperience(steamId);
            xpData.FamiliarLevels[data.FamKey] = newXP;
            SaveFamiliarExperience(steamId, xpData);

            if (ModifyFamiliar(user, steamId, famKey, player, familiar, level))
            {
                LocalizationService.HandleReply(ctx, $"Active familiar for <color=green>{user.CharacterName.Value}</color> has been set to level <color=white>{level}</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar to set level for.");
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
                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>")
            .ToList();

        for (int i = 0; i < prestigeStats.Count; i += 4)
        {
            var batch = prestigeStats.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, replyMessage);
        }
    }

    [Command(name: "prestige", shortHand: "pr", adminOnly: false, usage: ".fam pr [PrestigeStat]", description: "Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.")]
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
            FamiliarExperienceData xpData = LoadFamiliarExperience(ctx.Event.User.PlatformId);

            if (xpData.FamiliarLevels[data.FamKey].Key >= ConfigService.MaxFamiliarLevel)
            {
                FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);
                if (!prestigeData.FamiliarPrestiges.ContainsKey(data.FamKey))
                {
                    prestigeData.FamiliarPrestiges[data.FamKey] = new(0, []);
                    SaveFamiliarPrestige(steamId, prestigeData);
                }

                prestigeData = LoadFamiliarPrestige(steamId);
                List<FamiliarStatType> stats = prestigeData.FamiliarPrestiges[data.FamKey].Value;

                if (prestigeData.FamiliarPrestiges[data.FamKey].Key >= ConfigService.MaxFamiliarPrestiges)
                {
                    LocalizationService.HandleReply(ctx, "Familiar is already at maximum prestiges!");
                    return;
                }

                if (stats.Count < FamiliarStatValues.Count) // if less than max stats, parse entry and add if set doesnt already contain
                {
                    if (int.TryParse(statType, out int value))
                    {
                        int length = Enum.GetValues(typeof(FamiliarStatType)).Length;

                        if (value < 1 || value > length)
                        {
                            LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat type, use '<color=white>.fam lst</color>' to see options.");
                            return;
                        }

                        --value;
                        statType = value.ToString();
                    }

                    if (!Familiars.TryParseFamiliarStat(statType, out var stat))
                    {
                        /*
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

                        var prestigeStats = Enum.GetValues(typeof(FamiliarStatType))
                            .Cast<FamiliarStatType>()
                            .Select((stat, index) =>
                                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>")
                            .ToList();

                        if (prestigeStats.Count == 0)
                        {
                            LocalizationService.HandleReply(ctx, "No familiar prestige stats available at this time.");
                        }
                        else
                        {
                            for (int i = 0; i < prestigeStats.Count; i += 4)
                            {
                                var batch = prestigeStats.Skip(i).Take(4);
                                string replyMessage = string.Join(", ", batch);
                                LocalizationService.HandleReply(ctx, replyMessage);
                            }
                        }

                        return;
                        */

                        LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat type, use '<color=white>.fam lst</color>' to see options.");
                        return;
                    }
                    else if (!stats.Contains(stat))
                    {
                        stats.Add(stat);
                    }
                    else
                    {
                        LocalizationService.HandleReply(ctx, $"Familiar already has <color=#00FFFF>{stat}</color> (<color=yellow>{FamiliarPrestigeStats.IndexOf(stat) + 1}</color>) from prestiging, use '<color=white>.fam lst</color>' to see options.");
                        return;
                    }
                }
                else if (stats.Count >= FamiliarStatValues.Count && !string.IsNullOrEmpty(statType))
                {
                    LocalizationService.HandleReply(ctx, "Familiar has all possible added stat types from prestiging, use '<color=white>.fam pr</color>' instead of '<color=white>.fam pr [PrestigeStat]</color>'.");
                    return;
                }

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1)); // reset level to 1
                xpData.FamiliarLevels[data.FamKey] = newXP;
                SaveFamiliarExperience(steamId, xpData);

                int prestigeLevel = prestigeData.FamiliarPrestiges[data.FamKey].Key + 1;
                prestigeData.FamiliarPrestiges[data.FamKey] = new(prestigeLevel, stats);
                SaveFamiliarPrestige(steamId, prestigeData);

                Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                ModifyUnitStats(familiar, newXP.Key, steamId, data.FamKey);
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>.");
            }
            else if (InventoryUtilities.TryGetInventoryEntity(EntityManager, playerCharacter, out Entity inventory) && ServerGameManager.GetInventoryItemCount(inventory, _itemSchematic) >= ConfigService.PrestigeCostItemQuantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(playerCharacter, _itemSchematic, ConfigService.PrestigeCostItemQuantity))
                {
                    Familiars.HandleFamiliarPrestige(ctx, statType, ConfigService.MaxFamiliarLevel);
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
            Entity follower = buffer[i].Entity.GetEntityOnServer();

            if (follower.Exists())
            {
                if (follower.IsDisabled()) follower.Remove<Disabled>();

                DestroyUtility.Destroy(EntityManager, follower);
            }
        }

        Familiars.ClearFamiliarActives(steamId);
        if (Familiars.AutoCallMap.TryRemove(character, out Entity _))

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

        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);
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

    [Command(name: "smartbind", shortHand: "sb", adminOnly: false, usage: ".fam sb [Name] [OptionalIndex]", description: "Searches and binds a familiar. If multiple matches are found, returns a list for clarification.")]
    public static void SmartBindFamiliar(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        ulong steamId = user.PlatformId;

        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);
        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);

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
            Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);
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

        /*
        else if (foundBoxMatches.Count > 1 && index == -1)
        {
            // List options for user clarification
            string options = string.Join("\n", matchedFamiliars.Select((name, idx) => $"{idx + 1}: {name}"));
            string message = $"Multiple matches found. Use `.fam sb [Name] [Index]` to specify:\n{options}";
            LocalizationService.HandleReply(ctx, message);
        }
        */
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

        Entity familiar = Familiars.FindPlayerFamiliar(character);
        int famKey = familiar.Read<PrefabGUID>().GuidHash;

        int quantity = ConfigService.ShinyCostItemQuantity;

        if (familiar.Exists())
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);

            if (!buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                /*
                bool madeShinyChoice = Misc.PlayerBoolsManager.GetPlayerBool(steamId, "ShinyChoice");

                if (!madeShinyChoice && HandleShiny(famKey, steamId, 1f, schoolPrefabGuid.GuidHash)) // if false use free visual then set to true
                {
                    Misc.PlayerBoolsManager.SetPlayerBool(steamId, "ShinyChoice", true);
                    LocalizationService.HandleReply(ctx, "Shiny added! Rebind familiar to see effects. Use '<color=white>.fam option shiny</color>' to toggle (no chance to apply spell school debuff on hit if shiny buffs are disabled).");
                }
                else if (madeShinyChoice)
                {
                    LocalizationService.HandleReply(ctx, $"Shiny freebie already used! (<color=#ffd9eb>{_vampiricDust.GetLocalizedName()}</color>x<color=white>{quantity}</color> required to change existing shiny familiar buffs)");
                }
                */

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
    public static void ToggleFamiliarSetting(ChatCommandContext ctx, string option)
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
    public static void SetBattleGroupSlot(ChatCommandContext ctx, int slot = -1)
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
    public static void SetBattleArenaCoords(ChatCommandContext ctx)
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
            LocalizationService.HandleReply(ctx, "Familiar arena position changed! (only one arena currently allowed)");
        }
    }
}

