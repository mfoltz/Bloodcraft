﻿using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;
//using static Bloodcraft.Core;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Commands;

[CommandGroup(name: "familiar", "fam")]
internal static class FamiliarCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID combatBuff = new(581443919);
    static readonly PrefabGUID pvpCombatBuff = new(697095869);
    static readonly PrefabGUID dominateBuff = new(-1447419822);

    [Command(name: "bind", shortHand: "b", adminOnly: false, usage: ".fam b [#]", description: "Activates specified familiar from current list.")]
    public static void BindFamiliar(ChatCommandContext ctx, int choice)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;
        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);

        if (Core.ServerGameManager.TryGetBuff(character, combatBuff.ToIdentifier(), out Entity _) || Core.ServerGameManager.TryGetBuff(character, pvpCombatBuff.ToIdentifier(), out Entity _) || Core.ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _))
        {
            LocalizationService.HandleReply(ctx, "You can't bind a familiar while in combat or dominating presence is active.");
            return;
        }

        if (familiar != Entity.Null)
        {
            LocalizationService.HandleReply(ctx, "You already have an active familiar.");
            return;
        }

        string set = Core.DataStructures.FamiliarSet[steamId];

        if (string.IsNullOrEmpty(set))
        {
            LocalizationService.HandleReply(ctx, "You don't have a box selected. Use .fam boxes to see available boxes then choose one with .fam cb [BoxName]");
            return;
        }
        
        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && data.Familiar.Equals(Entity.Null) && data.FamKey.Equals(0) && Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId).UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            Core.DataStructures.PlayerBools[steamId]["Binding"] = true;
            if (choice < 1 || choice > famKeys.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use 1 to {famKeys.Count} (Current List:<color=white>{set}</color>)");
                return;
            }
            if (!Core.DataStructures.FamiliarChoice.ContainsKey(steamId)) // cache, set choice once per session then can use emote to bind same choice
            {
                Core.DataStructures.FamiliarChoice.Add(steamId, choice);
            }
            else
            {
                Core.DataStructures.FamiliarChoice[steamId] = choice;
            }
            data = new(Entity.Null, famKeys[choice - 1]);
            Core.DataStructures.FamiliarActives[steamId] = data;
            Core.DataStructures.SavePlayerFamiliarActives();
            FamiliarSummonUtilities.SummonFamiliar(character, userEntity, famKeys[choice -1]);
            //character.Add<AlertAllies>();

        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar or familiar already active.");
        }
    }

    [Command(name: "unbind", shortHand: "ub", adminOnly: false, usage: ".fam ub", description: "Destroys active familiar.")]
    public static void UnbindFamiliar(ChatCommandContext ctx)
    {
        ulong steamId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);

        if (familiar != Entity.Null)
        {
            if (FamiliarPatches.FamiliarMinions.ContainsKey(familiar)) Core.FamiliarService.HandleFamiliarMinions(familiar);
            DestroyUtility.CreateDestroyEvent(Core.EntityManager, familiar, DestroyReason.Default, DestroyDebugReason.None);
            Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
            Core.DataStructures.SavePlayerFamiliarActives();
            LocalizationService.HandleReply(ctx, "Familiar unbound.");
        }
        else if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && data.Familiar.Equals(Entity.Null) && !data.FamKey.Equals(0))
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar, assuming dead and unbinding...");
            Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
            Core.DataStructures.SavePlayerFamiliarActives();

        }
        else if (!data.Familiar.Equals(Entity.Null) && Core.EntityManager.Exists(data.Familiar))
        {
            if (FamiliarPatches.FamiliarMinions.ContainsKey(data.Familiar)) Core.FamiliarService.HandleFamiliarMinions(familiar);
            if (data.Familiar.Has<Disabled>()) data.Familiar.Remove<Disabled>();
            DestroyUtility.CreateDestroyEvent(Core.EntityManager, data.Familiar, DestroyReason.Default, DestroyDebugReason.None);
            Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
            Core.DataStructures.SavePlayerFamiliarActives();
            LocalizationService.HandleReply(ctx, "Familiar unbound.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find familiar to unbind.");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".fam l", description: "Lists unlocked familiars from current box.")]
    public static void ListFamiliars(ChatCommandContext ctx)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        string set = Core.DataStructures.FamiliarSet[steamId];
        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var _) && data.UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            int count = 1;
            foreach (var famKey in famKeys)
            {
                PrefabGUID famPrefab = new(famKey);
                LocalizationService.HandleReply(ctx, $"<color=white>{count}</color>: <color=green>{famPrefab.GetPrefabName()}</color>");
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
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (data.UnlockedFamiliars.Keys.Count > 0)
        {
            List<string> sets = [];
            foreach (var key in data.UnlockedFamiliars.Keys)
            {
                sets.Add(key);
            }
            string fams = string.Join(", ", sets.Select(set => $"<color=white>{set}</color>"));
            LocalizationService.HandleReply(ctx, $"Available Familiar Boxes: {fams}");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet.");
        }
    }

    [Command(name: "choosebox", shortHand: "cb", adminOnly: false, usage: ".fam cb [Name]", description: "Choose active box of familiars.")]
    public static void ChooseSet(ChatCommandContext ctx, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (data.UnlockedFamiliars.TryGetValue(name, out var _))
        {
            Core.DataStructures.FamiliarSet[steamId] = name;
            LocalizationService.HandleReply(ctx, $"Active Familiar Box: <color=white>{name}</color>");
            Core.DataStructures.SavePlayerFamiliarSets();
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box.");
        }
    }

    [Command(name: "renamebox", shortHand: "rb", adminOnly: false, usage: ".fam rb [CurrentName] [NewName]", description: "Renames a box.")]
    public static void RenameSet(ChatCommandContext ctx, string current, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (data.UnlockedFamiliars.TryGetValue(current, out var familiarSet))
        {
            // Remove the old set
            data.UnlockedFamiliars.Remove(current);

            // Add the set with the new name
            data.UnlockedFamiliars[name] = familiarSet;
            if (Core.DataStructures.FamiliarSet.TryGetValue(steamId, out var set) && set.Equals(current)) // change active set to new name if it was the old name
            {
                Core.DataStructures.FamiliarSet[steamId] = name;
                Core.DataStructures.SavePlayerFamiliarSets();
            }
            // Save changes back to the FamiliarUnlocksManager
            Core.FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=white>{current}</color> renamed to <color=yellow>{name}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find box to rename.");
        }
    }

    [Command(name: "movebox", shortHand: "mb", adminOnly: false, usage: ".fam mb [BoxName]", description: "Moves active familiar to specified box.")]
    public static void TransplantFamiliar(ChatCommandContext ctx, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (data.UnlockedFamiliars.TryGetValue(name, out var familiarSet) && familiarSet.Count < 10)
        {
            // Remove the old set
            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && !actives.FamKey.Equals(0))
            {
                var keys = data.UnlockedFamiliars.Keys;
                foreach (var key in keys)
                {
                    if (data.UnlockedFamiliars[key].Contains(actives.FamKey))
                    {
                        data.UnlockedFamiliars[key].Remove(actives.FamKey);
                        familiarSet.Add(actives.FamKey);
                        Core.FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, data);
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

    [Command(name: "add", shortHand: "a", adminOnly: true, usage: ".fam a [Name] [PrefabGUID]", description: "Unit testing.")]
    public static void AddFamiliar(ChatCommandContext ctx, string name, int unit)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        ulong steamId = foundUser.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (Core.DataStructures.FamiliarSet.TryGetValue(steamId, out var activeSet))
        {
            // Remove the old set
            if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(unit), out var Entity))
            {
                // Add to set
                if (!Entity.Read<PrefabGUID>().LookupName().ToLower().Contains("char"))
                {
                    LocalizationService.HandleReply(ctx, "Invalid unit.");
                    return;
                }

                data.UnlockedFamiliars[activeSet].Add(unit);
                Core.FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, data);
                LocalizationService.HandleReply(ctx, $"<color=green>{unit}</color> added to <color=white>{activeSet}</color>.");

            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid unit.");
                return;
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No active set found to add to for player (unlock at least 1 unit to create one or make sure it is set as their active set)");
        }
    }

    [Command(name: "remove", shortHand: "r", adminOnly: false, usage: ".fam r [#]", description: "Removes familiar from current set permanently.")]
    public static void RemoveFamiliarFromSet(ChatCommandContext ctx, int choice)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (Core.DataStructures.FamiliarSet.TryGetValue(steamId, out var activeSet) && data.UnlockedFamiliars.TryGetValue(activeSet, out var familiarSet))
        {
            // Remove the old set
            if (choice < 1 || choice > familiarSet.Count)
            {
                LocalizationService.HandleReply(ctx, $"Invalid choice, please use 1 to {familiarSet.Count} (Current List:<color=white>{familiarSet}</color>)");
                return;
            }
            PrefabGUID familiarId = new(familiarSet[choice - 1]);
            // remove from set
            familiarSet.RemoveAt(choice - 1);
            Core.FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, data);
            LocalizationService.HandleReply(ctx, $"<color=green>{familiarId.GetPrefabName()}</color> removed from <color=white>{activeSet}</color>.");

        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find set to remove from.");
        }
    }

    [Command(name: "toggle", usage: ".fam toggle", description: "Calls or dismisses familar.", adminOnly: false)]
    public static void ToggleFamiliar(ChatCommandContext ctx)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong platformId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;

        if (Core.ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _))
        {
            LocalizationService.HandleReply(ctx, "You can't call a familiar while dominating presence is active.");
            return;
        }

        EmoteSystemPatch.CallDismiss(userEntity, character, platformId);
    }

    [Command(name: "togglecombat", shortHand: "c", usage: ".fam c", description: "Enables or disables combat for familiar.", adminOnly: false)]
    public static void ToggleCombat(ChatCommandContext ctx)
    {

        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        if (!Plugin.FamiliarCombat.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiar combat is not enabled.");
            return;
        }

        ulong platformId = ctx.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;
        EmoteSystemPatch.CombatMode(userEntity, character, platformId);
    }

    [Command(name: "emotes", shortHand: "e", usage: ".fam e", description: "Toggle emote commands.", adminOnly: false)]
    public static void ToggleEmotes(ChatCommandContext ctx)
    {
        ulong platformId = ctx.User.PlatformId;
        if (Core.DataStructures.PlayerBools.TryGetValue(platformId, out var bools))
        {
            bools["Emotes"] = !bools["Emotes"];
            Core.DataStructures.SavePlayerBools();

            /*
            if (!EmoteSystemPatch.Coordinator.FamiliarEmotes.TryGetValue(platformId, out bool emotes))
            {
                EmoteSystemPatch.Coordinator.FamiliarEmotes[platformId] = bools["Emotes"];
            }
            else
            {
                emotes = bools["Emotes"];
            } 
            */

            LocalizationService.HandleReply(ctx, $"Emotes for familiars are {(bools["Emotes"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
        }
    }

    [Command(name: "listemoteactions", shortHand: "le", usage: ".fam le", description: "List emote actions.", adminOnly: false)]
    public static void ListEmotes(ChatCommandContext ctx)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        List<string> emoteInfoList = [];
        foreach (var emote in EmoteSystemPatch.actions)
        {
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
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && !data.FamKey.Equals(0))
        {
            var xpData = FamiliarLevelingUtilities.GetFamiliarExperience(steamId, data.FamKey);
            int progress = (int)(xpData.Value - FamiliarLevelingUtilities.ConvertLevelToXp(xpData.Key));
            int percent = FamiliarLevelingUtilities.GetLevelProgress(steamId, data.FamKey);

            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(ctx.Event.SenderCharacterEntity);

            int prestigeLevel = 0;
            FamiliarPrestigeData prestigeData = Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamId);
            if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
            {
                prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                Core.FamiliarPrestigeManager.SaveFamiliarPrestige(steamId, prestigeData);
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
                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>MaxHealth</color>: <color=white>{(int)maxHealth}</color>, <color=#00FFFF>PhysicalPower</color>: <color=white>{(int)physicalPower}</color>, <color=#00FFFF>SpellPower</color>: <color=white>{(int)spellPower}</color>, <color=#00FFFF>PhysCritChance</color>: <color=white>{physCrit}</color>, <color=#00FFFF>SpellCritChance</color>: <color=white>{spellCrit}</color>");
                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>HealingReceived</color>: <color=white>{healing}</color>, <color=#00FFFF>PhysResist</color>: <color=white>{physRes}</color>, <color=#00FFFF>SpellResist</color>: <color=white>{spellRes}</color>, <color=#00FFFF>CCReduction</color>: <color=white>{ccRed}</color>, <color=#00FFFF>ShieldAbsorb</color>: <color=white>{shieldAbs}</color>");
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
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        if (level < 1 || level > Plugin.MaxFamiliarLevel.Value)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 1 and {Plugin.MaxFamiliarLevel.Value}");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && !data.FamKey.Equals(0))
        {
            Entity player = ctx.Event.SenderCharacterEntity;
            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
            KeyValuePair<int, float> newXP = new(level, FamiliarLevelingUtilities.ConvertLevelToXp(level));
            FamiliarExperienceData xpData = Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            xpData.FamiliarExperience[data.FamKey] = newXP;
            Core.FamiliarExperienceManager.SaveFamiliarExperience(steamId, xpData);
            FamiliarSummonUtilities.HandleFamiliarModifications(player, familiar, level);
            LocalizationService.HandleReply(ctx, $"Your familiar has been set to level <color=white>{level}</color>.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar to set level for.");
        }
    }

    [Command(name: "prestige", shortHand: "pr", adminOnly: false, usage: ".fam pr [BonusStat]", description: "Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.")]
    public static void PrestigeFamiliarCommand(ChatCommandContext ctx, string bonusStat = "")
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        if (!Plugin.FamiliarPrestige.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiar prestige is not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && !data.FamKey.Equals(0))
        {
            FamiliarExperienceData xpData = Core.FamiliarExperienceManager.LoadFamiliarExperience(ctx.Event.User.PlatformId);
            if (xpData.FamiliarExperience[data.FamKey].Key >= Plugin.MaxFamiliarLevel.Value)
            {
                FamiliarPrestigeData prestigeData = Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamId);
                if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
                {
                    prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
                    Core.FamiliarPrestigeManager.SaveFamiliarPrestige(steamId, prestigeData);
                }

                prestigeData = Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamId);
                List<FamiliarSummonUtilities.FamiliarStatType> stats = prestigeData.FamiliarPrestige[data.FamKey].Value;

                if (prestigeData.FamiliarPrestige[data.FamKey].Key >= Plugin.MaxFamiliarPrestiges.Value)
                {
                    LocalizationService.HandleReply(ctx, "Familiar is already at max prestige!");
                    return;
                }

                if (stats.Count < FamiliarSummonUtilities.familiarStatCaps.Count) // if less than max stats, parse entry and add if set doesnt already contain
                {
                    if (!FamiliarSummonUtilities.TryParseFamiliarStat(bonusStat, out var stat))
                    {
                        var familiarStatsWithCaps = Enum.GetValues(typeof(FamiliarSummonUtilities.FamiliarStatType))
                        .Cast<FamiliarSummonUtilities.FamiliarStatType>()
                        .Select(stat =>
                            $"<color=#00FFFF>{stat}</color>: <color=white>{FamiliarSummonUtilities.familiarStatCaps[stat]}</color>")
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
                else if (stats.Count >= FamiliarSummonUtilities.familiarStatCaps.Count && !string.IsNullOrEmpty(bonusStat))
                {
                    LocalizationService.HandleReply(ctx, "Familiar already has max bonus stats, try again without entering a stat.");
                    return;
                }

                KeyValuePair<int, float> newXP = new(1, FamiliarLevelingUtilities.ConvertLevelToXp(1)); // reset level to 1
                xpData.FamiliarExperience[data.FamKey] = newXP;
                Core.FamiliarExperienceManager.SaveFamiliarExperience(steamId, xpData);

                int prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key + 1;
                prestigeData.FamiliarPrestige[data.FamKey] = new(prestigeLevel, stats);
                Core.FamiliarPrestigeManager.SaveFamiliarPrestige(steamId, prestigeData);

                Entity player = ctx.Event.SenderCharacterEntity;
                Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                FamiliarSummonUtilities.HandleFamiliarModifications(player, familiar, newXP.Key);
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is back to level <color=white>{newXP.Key}</color>.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Familiar must be at max level (<color=white>{Plugin.MaxFamiliarLevel.Value}</color>) to prestige.");
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
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data))
        {
            data = new(Entity.Null, 0);
            Core.DataStructures.FamiliarActives[steamId] = data;
            Core.DataStructures.SavePlayerFamiliarActives();
        }

        var buffer = ctx.Event.SenderCharacterEntity.ReadBuffer<FollowerBuffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (Core.EntityManager.Exists(buffer[i].Entity._Entity))
            {
                if (buffer[i].Entity._Entity.Has<Disabled>()) buffer[i].Entity._Entity.Remove<Disabled>();
                DestroyUtility.CreateDestroyEvent(Core.EntityManager, buffer[i].Entity._Entity, DestroyReason.Default, DestroyDebugReason.None);
            }
        }
        LocalizationService.HandleReply(ctx, "Familiar actives and followers cleared.");
    }

    [Command(name: "search", shortHand: "s", adminOnly: false, usage: ".fam s [Name]", description: "Searches boxes for unit with entered name.")]
    public static void FindFamiliarBox(ChatCommandContext ctx, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }
        ulong steamId = ctx.User.PlatformId;
        UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
        if (data.UnlockedFamiliars.Keys.Count > 0)
        {
            List<string> foundBoxNames = [];
            foreach (var box in data.UnlockedFamiliars)
            {
                foundBoxNames = data.UnlockedFamiliars
                    .Where(box => box.Value.Any(famKey =>
                    {
                        PrefabGUID famPrefab = new(famKey);
                        return famPrefab.GetPrefabName().ToLower().Contains(name.ToLower());
                    }))
                    .Select(box => box.Key)
                    .ToList();
            }
            if (foundBoxNames.Count > 0)
            {
                string foundBoxes = string.Join(", ", foundBoxNames.Select(box => $"<color=white>{box}</color>"));
                LocalizationService.HandleReply(ctx, $"Matching familiar(s) found in: {foundBoxes}");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Couldn't find matching familiar in any boxes.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any unlocked familiars yet.");
        }
    }

    [Command(name: "visual", shortHand: "v", adminOnly: false, usage: ".fam v [SpellSchool]", description: "Chooses visul for current active familiar, one freebie then cost configured amount.")]
    public static void SetFamiliarVisual(ChatCommandContext ctx, string spellSchool = "")
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;

        PrefabGUID visual = FamiliarUnlockUtilities.RandomVisuals
                .SingleOrDefault(prefab => prefab.LookupName().ToLower().Contains(spellSchool.ToLower()));

        if (!FamiliarUnlockUtilities.RandomVisuals.Contains(visual))
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching visual from entered spell school. (options: blood, storm, unholy, chaos, frost, illusion)");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
        int famKey = familiar.Read<PrefabGUID>().GuidHash;

        if (familiar != Entity.Null)
        {
            Core.DataStructures.FamiliarBuffsData buffsData = Core.FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
            if (!buffsData.FamiliarBuffs.ContainsKey(famKey)) // if no shiny unlocked already use the freebie
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && !bools["ShinyChoice"] && FamiliarUnlockUtilities.HandleShiny(famKey, steamId, 1f, visual.GuidHash))
                {
                    bools["ShinyChoice"] = true;
                    Core.DataStructures.SavePlayerBools();
                    LocalizationService.HandleReply(ctx, "Visual assigned succesfully! Rebind familiar for it to take effect. Emote 'yes' with familiar emote actions enabled to enable/disable unlocked visuals displaying for familiars.");
                }
                else if (bools["ShinyChoice"])
                {
                    LocalizationService.HandleReply(ctx, "You've already used your free familiar visual.");
                }
            }
            else if (buffsData.FamiliarBuffs.ContainsKey(famKey)) // if shiny already unlocked use prefab cost and quantity, override shiny visual with choice
            {
                if (!Plugin.ShinyCostItemPrefab.Value.Equals(0))
                {
                    PrefabGUID item = new(Plugin.ShinyCostItemPrefab.Value);
                    int quantity = Plugin.ShinyCostItemQuantity.Value;
                    if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                    {
                        if (Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity) && FamiliarUnlockUtilities.HandleShiny(famKey, steamId, 1f, visual.GuidHash))
                        {
                            LocalizationService.HandleReply(ctx, "Visual assigned for cost succesfully! Rebind familiar for it to take effect. Emote 'yes' with familiar emote actions enabled to enable/disable unlocked visuals displaying for familiars.");
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

    [Command(name: "resetvisualchoice", shortHand: "rv", adminOnly: true, usage: ".fam rv [Name]", description: "Allows player to choose another free visual, however, does not erase any visuals they have chosen previously.")]
    public static void ResetFamiliarVisualChoice(ChatCommandContext ctx, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = foundUserEntity.Read<User>().PlatformId;
        string playerName = foundUserEntity.Read<User>().CharacterName.Value;

        if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && bools["ShinyChoice"])
        {
            bools["ShinyChoice"] = false;
            Core.DataStructures.SavePlayerBools();
            LocalizationService.HandleReply(ctx, $"Visual choice reset for <color=white>{playerName}</color>. (does not remove previously chosen visuals from player data)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Player is already able to choose a free familiar visual.");
        }
    }

    //[Command(name: "name", shortHand: "n", adminOnly: true, usage: ".fam n [Name]", description: "Set current familiar name.")]
    public static void SetFamiliarName(ChatCommandContext ctx, string name)
    {
        if (!Plugin.FamiliarSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);

        if (familiar != Entity.Null)
        {
            if (!familiar.Has<NameableInteractable>()) familiar.Add<NameableInteractable>();
            FixedString64Bytes famName = new(name);
            NameableInteractable nameable = familiar.Read<NameableInteractable>();
            nameable.Name = famName;
            familiar.Write(nameable);
            
            LocalizationService.HandleReply(ctx, $"Renamed familiar to <color=white>{name}</color>!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Make sure familiar is active and out before attempting to rename.");
        }
    }
}

