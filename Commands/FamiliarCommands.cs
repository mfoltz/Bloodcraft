﻿using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Core;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Commands
{
    public class FamiliarCommands
    {
        [Command(name: "bindFamiliar", shortHand: "bind", adminOnly: false, usage: ".bind [#]", description: "Activates specified familiar from current list.")]
        public static void BindFamiliar(ChatCommandContext ctx, int choice)
        {
            ulong steamId = ctx.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            Entity userEntity = ctx.Event.SenderUserEntity;
            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character);

            if (familiar != Entity.Null)
            {
                ctx.Reply("You already have an active familiar.");
                return;
            }

            string set = Core.DataStructures.FamiliarSet[steamId];

            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && data.Item1.Equals(Entity.Null) && data.Item2.Equals(0) && Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId).UnlockedFamiliars.TryGetValue(set, out var famKeys))
            {
                Core.DataStructures.PlayerBools[steamId]["Binding"] = true;
                if (choice < 1 || choice > famKeys.Count)
                {
                     ctx.Reply($"Invalid choice, please use 1 to {famKeys.Count} (Current List:<color=white>{set}</color>)");
                     return;
                }
                data = new(Entity.Null, famKeys[choice - 1]);
                Core.DataStructures.FamiliarActives[steamId] = data;
                Core.DataStructures.SavePlayerFamiliarActives();
                FamiliarSummonSystem.SummonFamiliar(character, userEntity, famKeys[choice -1]);
            }
            else
            {
                ctx.Reply("You already have an active familiar. If that doesn't seem correct, try unbinding.");
            }
        }

        [Command(name: "unbindFamiliar", shortHand: "unbind", adminOnly: false, usage: ".unbind", description: "Destroys active familiar.")]
        public static void UnbindFamiliar(ChatCommandContext ctx)
        {
            ulong steamId = ctx.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character);

            if (familiar != Entity.Null)
            {
                DestroyUtility.CreateDestroyEvent(Core.EntityManager, familiar, DestroyReason.Default, DestroyDebugReason.None);
                Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
                Core.DataStructures.SavePlayerFamiliarActives();
                ctx.Reply("Familiar unbound.");
            }
            else if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && data.Item1.Equals(Entity.Null) && !data.Item2.Equals(0))
            {
                ctx.Reply("Couldn't find familiar, assuming dead and unbinding...");
                Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
                Core.DataStructures.SavePlayerFamiliarActives();
            }
            else if (!data.Item1.Equals(Entity.Null) && Core.EntityManager.Exists(data.Item1))
            {
                DestroyUtility.CreateDestroyEvent(Core.EntityManager, data.Item1, DestroyReason.Default, DestroyDebugReason.None);
                Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
                Core.DataStructures.SavePlayerFamiliarActives();
                ctx.Reply("Familiar unbound.");
            }
        }

        [Command(name: "listFamiliars", shortHand: "lf", adminOnly: false, usage: ".lf", description: "Lists unlocked familiars from current set.")]
        public static void ListFamiliars(ChatCommandContext ctx)
        {
            ulong steamId = ctx.User.PlatformId;
            UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
            string set = Core.DataStructures.FamiliarSet[steamId];
            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && data.UnlockedFamiliars.TryGetValue(set, out var famKeys))
            {
                int count = 1;
                foreach (var famKey in famKeys)
                {
                    PrefabGUID famPrefab = new(famKey);
                    ctx.Reply($"<color=white>{count}</color>: <color=green>{famPrefab.LookupName()}</color>");
                    count++;
                }
            }
            else
            {
                ctx.Reply("Couldn't locate set.");
                return;
            }
        }

        [Command(name: "familiarSets", shortHand: "famsets", adminOnly: false, usage: ".famsets", description: "Shows the available familiar lists.")]
        public static void ListFamiliarSets(ChatCommandContext ctx)
        {
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
                ctx.Reply($"Available Familiar Sets: {fams}");
            }
            else
            {
                ctx.Reply("You don't have any unlocked familiars yet.");
            }
        }

        [Command(name: "chooseFamiliarSet", shortHand: "cfs", adminOnly: false, usage: ".cfs [Name]", description: "Choose active set of familiars.")]
        public static void ChooseSet(ChatCommandContext ctx, string name)
        {
            ulong steamId = ctx.User.PlatformId;
            UnlockedFamiliarData data = Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);
            if (data.UnlockedFamiliars.TryGetValue(name, out var _))
            {
                Core.DataStructures.FamiliarSet[steamId] = name;
                ctx.Reply($"Active Familiar Set: <color=white>{name}</color>");
            }
            else
            {
                ctx.Reply("Couldn't find set.");
            }
        }
        [Command(name: "setRename", shortHand: "sr", adminOnly: false, usage: ".sr [CurrentName] [NewName]", description: "Renames set.")]
        public static void RenameSet(ChatCommandContext ctx, string current, string name)
        {
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

                ctx.Reply($"<color=white>{current}</color> renamed to <color=yellow>{name}</color>.");
            }
            else
            {
                ctx.Reply("Couldn't find set to rename.");
            }
        }

        [Command(name: "toggleFamiliar", shortHand: "toggle", usage: ".toggle", description: "Calls or dismisses familar.", adminOnly: false)]
        public static void ToggleFamiliar(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            Entity userEntity = ctx.Event.SenderUserEntity;
            EmoteSystemPatch.CallDismiss(userEntity, character, platformId);
        }

        [Command(name: "toggleEmotes", shortHand: "emotes", usage: ".emotes", description: "Toggle emote commands.", adminOnly: false)]
        public static void ToggleEmotes(ChatCommandContext ctx)
        {
            ulong platformId = ctx.User.PlatformId;
            if (Core.DataStructures.PlayerBools.TryGetValue(platformId, out var bools))
            {
                bools["Emotes"] = !bools["Emotes"];
                Core.DataStructures.SavePlayerBools();
                ctx.Reply($"Emotes for familiars are {(bools["Emotes"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
            }
        }

        [Command(name: "listEmoteActions", shortHand: "le", usage: ".le", description: "List emote actions.", adminOnly: false)]
        public static void ListEmotes(ChatCommandContext ctx)
        {
            List<string> emoteInfoList = [];
            foreach (var emote in EmoteSystemPatch.actions)
            {
                string emoteName = emote.Key.LookupName();
                string actionName = emote.Value.Method.Name;
                emoteInfoList.Add($"<color=#FFC0CB>{emoteName}</color>: <color=yellow>{actionName}</color>");
            }
            string emotes = string.Join(", ", emoteInfoList);
            ctx.Reply($"{emotes}");
        }

        [Command(name: "getFamiliarLevel", shortHand: "get fl", adminOnly: false, usage: ".get fl", description: "Display current familiar leveling progress.")]
        public static void GetLevelCommand(ChatCommandContext ctx)
        {
            if (!Plugin.FamiliarSystem.Value)
            {
                ctx.Reply("Familiars are not enabled.");
                return;
            }

            ulong steamId = ctx.Event.User.PlatformId;

            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && !data.Item2.Equals(0))
            {
                var xpData = FamiliarLevelingSystem.GetFamiliarExperience(steamId, data.Item2);
                int progress = (int)(xpData.Value - FamiliarLevelingSystem.ConvertLevelToXp(xpData.Key));
                int percent = FamiliarLevelingSystem.GetLevelProgress(steamId, data.Item2);
                ctx.Reply($"You're familiar is level [<color=white>{xpData.Key}</color>] and has <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
            }
            else
            {
                ctx.Reply("Couldn't find any experience data for familiar.");
            }
        }

        [Command(name: "setFamiliarLevel", shortHand: "sfl", adminOnly: false, usage: ".sfl [Level]", description: "Set current familiar level.")]
        public static void SetFamiliarLevel(ChatCommandContext ctx, int level)
        {
            if (!Plugin.FamiliarSystem.Value)
            {
                ctx.Reply("Familiars are not enabled.");
                return;
            }
            if (level < 1 || level > Plugin.MaxFamiliarLevel.Value)
            {
                ctx.Reply($"Level must be between 1 and {Plugin.MaxFamiliarLevel.Value}");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;

            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && !data.Item2.Equals(0))
            {
                KeyValuePair<int, float> newXP = new(level, FamiliarLevelingSystem.ConvertLevelToXp(level));
                FamiliarExperienceData xpData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
                xpData.FamiliarExperience[data.Item2] = newXP;
                FamiliarExperienceManager.SaveFamiliarExperience(steamId, xpData);
                ctx.Reply($"You're familiar has been set to level <color=white>{level}</color>, rebind to update stats.");
            }
            else
            {
                ctx.Reply("Couldn't find active familiar to set level for.");
            }
        }
    }
}
