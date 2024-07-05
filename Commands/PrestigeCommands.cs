using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    [CommandGroup(name: "prestige", ".prestige")]
    class PrestigeCommands
    {
        static readonly bool SoftSynergies = Plugin.SoftSynergies.Value;
        static readonly bool HardSynergies = Plugin.HardSynergies.Value;
        static readonly bool ShiftSlot = Plugin.ShiftSlot.Value;
        static readonly bool PlayerParties = Plugin.Parties.Value;
        static readonly bool Prestige = Plugin.PrestigeSystem.Value;

        [Command(name: "me", shortHand: "me", adminOnly: false, usage: ".prestige me [PrestigeType]", description: "Handles player prestiging.")]
        public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
                return;
            }

            if ((SoftSynergies || HardSynergies) &&
                Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) &&
                classes.Keys.Count == 0 &&
                parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
            {
                LocalizationService.HandleReply(ctx, "You must choose a class before prestiging in experience.");
                return;
            }

            var steamId = ctx.Event.User.PlatformId;
            var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

            if (handler == null)
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige type.");
                return;
            }

            var xpData = handler.GetExperienceData(steamId);
            if (PrestigeSystem.CanPrestige(steamId, parsedPrestigeType, xpData.Key))
            {
                PrestigeSystem.PerformPrestige(ctx, steamId, parsedPrestigeType, handler);
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
            }
        }

        [Command(name: "set", shortHand: "set", adminOnly: true, usage: ".prestige set [PlayerID] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
        public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige type, use .lpp to see options.");
                return;
            }

            Entity userEntity = PlayerService.GetUserByName(name, true);
            ulong playerId = userEntity.Read<User>().PlatformId;

            if ((SoftSynergies || HardSynergies) &&
                Core.DataStructures.PlayerClasses.TryGetValue(playerId, out var classes) &&
                classes.Keys.Count == 0 &&
                parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
            {
                LocalizationService.HandleReply(ctx, "The player must choose a class before prestiging in experience.");
                return;
            }

            var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

            if (handler == null)
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige type.");
                return;
            }

            if (!Core.DataStructures.PlayerPrestiges.TryGetValue(playerId, out var prestigeData))
            {
                prestigeData = [];
                Core.DataStructures.PlayerPrestiges[playerId] = prestigeData;
            }

            if (!prestigeData.ContainsKey(parsedPrestigeType))
            {
                prestigeData[parsedPrestigeType] = 0;
            }

            if (level > PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType])
            {
                LocalizationService.HandleReply(ctx, $"The maximum level for {parsedPrestigeType} prestige is {PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType]}.");
                return;
            }

            prestigeData[parsedPrestigeType] = level;
            handler.SaveChanges();

            // Apply effects based on the prestige type
            if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
            {
                PrestigeSystem.ApplyPrestigeBuffs(ctx, level);
                PrestigeSystem.ApplyExperiencePrestigeEffects(ctx, playerId, level);
            }
            else
            {
                PrestigeSystem.ApplyOtherPrestigeEffects(ctx, playerId, parsedPrestigeType, level);
            }

            LocalizationService.HandleReply(ctx, $"Player <color=green>{playerId}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
        }

        [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".prestige lb", description: "Lists prestige buff names.")]
        public static void PrestigeBuffsCommand(ChatCommandContext ctx)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);

            if (buffs.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Prestiging buffs not found.");
                return;
            }

            var prestigeBuffs = buffs.Select((buff, index) =>
            {
                int level = index + 1;
                string prefab = new PrefabGUID(buff).LookupName();
                int prefabIndex = prefab.IndexOf("Prefab");
                if (prefabIndex != -1)
                {
                    prefab = prefab[..prefabIndex].TrimEnd();
                }
                return $"<color=white>{prefab}</color> at prestige <color=yellow>{level}</color>";
            }).ToList();

            for (int i = 0; i < prestigeBuffs.Count; i += 4)
            {
                var batch = prestigeBuffs.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }

        [Command(name: "reset", shortHand: "r", adminOnly: true, usage: ".prestige r [Name] [PrestigeType]", description: "Handles resetting prestiging.")]
        public static void ResetPrestige(ChatCommandContext ctx, string name, string prestigeType)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
                return;
            }

            Entity foundUserEntity = PlayerService.GetUserByName(name, true);

            if (foundUserEntity.Equals(Entity.Null))
            {
                LocalizationService.HandleReply(ctx, "Player not found...");
                return;
            }

            User foundUser = foundUserEntity.Read<User>();
            ulong steamId = foundUser.PlatformId;

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel))
            {
                if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
                {
                    PrestigeSystem.RemovePrestigeBuffs(ctx, prestigeLevel);
                }
                prestigeData[parsedPrestigeType] = 0;
                Core.DataStructures.SavePlayerPrestiges();
                LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige reset for <color=white>{foundUser.CharacterName}</color>.");
            }
        }

        [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".prestige sb", description: "Applies prestige buffs appropriately if not present.")]
        public static void SyncPrestigeBuffsCommand(ChatCommandContext ctx)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            var steamId = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
            {
                PrestigeSystem.ApplyPrestigeBuffs(ctx, prestigeLevel);
                LocalizationService.HandleReply(ctx, "Prestige buffs applied.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{PrestigeSystem.PrestigeType.Experience}</color>.");
            }
        }

        [Command(name: "get", shortHand: "get", adminOnly: false, usage: ".prestige get [PrestigeType]", description: "Shows information about player's prestige status.")]
        public static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }

            if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
                return;
            }

            var steamId = ctx.Event.User.PlatformId;
            var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

            if (handler == null)
            {
                LocalizationService.HandleReply(ctx, "Invalid prestige type.");
                return;
            }

            var maxPrestigeLevel = PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
            {
                PrestigeSystem.DisplayPrestigeInfo(ctx, steamId, parsedPrestigeType, prestigeLevel, maxPrestigeLevel);
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{parsedPrestigeType}</color>.");
            }
        }

        [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prestige l", description: "Lists prestiges available.")]
        public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
        {
            if (!Prestige)
            {
                LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
                return;
            }
            string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeSystem.PrestigeType)));
            LocalizationService.HandleReply(ctx, $"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
        }
    }
}
