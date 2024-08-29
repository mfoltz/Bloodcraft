﻿using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Leveling.PrestigeSystem;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    [Command(name: "self", shortHand: "me", adminOnly: false, usage: ".prestige me [PrestigeType]", description: "Handles player prestiging.")]
    public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (ConfigService.ExoPrestiging && parsedPrestigeType.Equals(PrestigeType.Exo))
        {
            if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var xpPrestige) && xpPrestige == ConfigService.MaxLevelingPrestiges)
            {
                if (steamId.TryGetPlayerExperience(out var expData) && expData.Key < ConfigService.MaxLevel)
                {
                    LocalizationService.HandleReply(ctx, "You must reach max level before <color=#90EE90>Exo</color> prestiging again.");
                    return;
                }
                else if (prestigeData[PrestigeType.Exo] >= ConfigService.ExoPrestiges)
                {
                    LocalizationService.HandleReply(ctx, $"You have reached the maximum amount of <color=#90EE90>Exo</color> prestiges. (<color=white>{ConfigService.ExoPrestiges}</color>)");
                    return;
                }

                expData = new KeyValuePair<int, float>(0, 0);
                steamId.SetPlayerExperience(expData);

                if (ConfigService.RestedXPSystem) LevelingSystem.ResetRestedXP(steamId);

                LevelingSystem.SetLevel(ctx.Event.SenderCharacterEntity);

                prestigeData[PrestigeType.Exo] += 1;
                steamId.SetPlayerPrestiges(prestigeData);

                Entity character = ctx.Event.SenderCharacterEntity;
                AdjustCharacterStats(character, steamId);

                PrefabGUID exoReward = new(0);

                if (ConfigService.ExoPrestigeReward != 0) exoReward = new(ConfigService.ExoPrestigeReward);
                else if (ConfigService.ExoPrestigeReward == 0)
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ConfigService.ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ConfigService.ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, "No prefab configured for <color=#90EE90>Exo</color> reward...");
                    return;
                }

                if (ServerGameManager.TryAddInventoryItem(character, exoReward, ConfigService.ExoPrestigeRewardQuantity))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ConfigService.ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ConfigService.ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{exoReward.GetPrefabName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color>!");
                    return;
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, character, exoReward, ConfigService.ExoPrestigeRewardQuantity, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ConfigService.ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ConfigService.ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{exoReward.GetPrefabName()}</color>x<color=white>{ConfigService.ExoPrestigeReward}</color>! It dropped on the ground becuase your inventory was full though.");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You must reach the maximum level in <color=#90EE90>Experience</color> prestige before <color=#90EE90>Exo</color> prestiging.");
                return;
            }
        }
        else if (!ConfigService.ExoPrestiging && parsedPrestigeType.Equals(PrestigeType.Exo))
        {
            LocalizationService.HandleReply(ctx, "<color=#90EE90>Exo</color> prestiging is not enabled.");
            return;
        }

        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        var xpData = handler.GetPrestigeTypeData(steamId);
        if (CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PerformPrestige(ctx, steamId, parsedPrestigeType, handler);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prestige set [Name] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
    public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;
        Entity character = playerInfo.CharEntity;

        if (parsedPrestigeType == PrestigeType.Exo)
        {
            if (!ConfigService.ExoPrestiging)
            {
                LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestiging is not enabled.");
                return;
            }

            if (level > PrestigeTypeToMaxPrestiges[parsedPrestigeType] || level < 1)
            {
                LocalizationService.HandleReply(ctx, $"The maximum level for <color=#90EE90>{parsedPrestigeType}</color> prestige is {PrestigeTypeToMaxPrestiges[parsedPrestigeType]}.");
                return;
            }

            if (steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(PrestigeType.Exo, out var exoPrestige))
            {
                exoPrestige = level;
                exoData[PrestigeType.Exo] = exoPrestige;
                steamId.SetPlayerPrestiges(exoData);
                AdjustCharacterStats(character, steamId);
                LocalizationService.HandleReply(ctx, $"Player <color=green>{playerInfo.User.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
                return;
            }
        }

        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        if (!steamId.TryGetPlayerPrestiges(out var prestigeData))
        {
            prestigeData = [];
            steamId.SetPlayerPrestiges(prestigeData);
        }

        if (!prestigeData.ContainsKey(parsedPrestigeType))
        {
            prestigeData[parsedPrestigeType] = 0;
        }

        if (level > PrestigeTypeToMaxPrestiges[parsedPrestigeType])
        {
            LocalizationService.HandleReply(ctx, $"The maximum level for <color=#90EE90>{parsedPrestigeType}</color> prestige is {PrestigeTypeToMaxPrestiges[parsedPrestigeType]}.");
            return;
        }

        prestigeData[parsedPrestigeType] = level;
        steamId.SetPlayerPrestiges(prestigeData);

        // Apply effects based on the prestige type
        if (parsedPrestigeType == PrestigeType.Experience)
        {
            ApplyPrestigeBuffs(character, level);
            ApplyExperiencePrestigeEffects(playerInfo.User, level);
        }
        else
        {
            ApplyOtherPrestigeEffects(playerInfo.User, steamId, parsedPrestigeType, level);
        }
        LocalizationService.HandleReply(ctx, $"Player <color=green>{playerInfo.User.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
    }

    [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".prestige lb", description: "Lists prestige buff names.")]
    public static void PrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        List<int> buffs = ParseConfigString(ConfigService.PrestigeBuffs);

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
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        if (!ConfigService.ExoPrestiging && parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;
        Entity character = playerInfo.CharEntity;

        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel))
        {
            if (parsedPrestigeType == PrestigeType.Experience)
            {
                RemovePrestigeBuffs(character, prestigeLevel);
            }

            prestigeData[parsedPrestigeType] = 0;
            steamId.SetPlayerPrestiges(prestigeData);

            if (parsedPrestigeType == PrestigeType.Exo)
            {
                AdjustCharacterStats(character, steamId);
            }

            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige reset for <color=white>{playerInfo.User.CharacterName.Value}</color>.");
        }
    }

    [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".prestige sb", description: "Applies prestige buffs appropriately if not present.")]
    public static void SyncPrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;

        if (steamId.TryGetPlayerPrestiges( out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            ApplyPrestigeBuffs(character, prestigeLevel);
            LocalizationService.HandleReply(ctx, "Prestige buffs applied.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{PrestigeType.Experience}</color>.");
        }
    }

    [Command(name: "get", adminOnly: false, usage: ".prestige get [PrestigeType]", description: "Shows information about player's prestige status.")]
    public static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (parsedPrestigeType == PrestigeType.Exo && steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(parsedPrestigeType, out var exoLevel) && exoLevel > 0)
        {
            LocalizationService.HandleReply(ctx, $"Current <color=#90EE90>Exo</color> Prestige Level: <color=yellow>{exoLevel}</color>/{PrestigeTypeToMaxPrestiges[parsedPrestigeType]}");
            LocalizationService.HandleReply(ctx, $"Damage taken increased by: <color=red>{(ConfigService.ExoPrestigeDamageTakenMultiplier * exoData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ConfigService.ExoPrestigeDamageDealtMultiplier * exoData[PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
            return;
        }
        else if (parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "You have not prestiged in <color=#90EE90>Exo</color> yet.");
            return;
        }

        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        var maxPrestigeLevel = PrestigeTypeToMaxPrestiges[parsedPrestigeType];

        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
        {
            DisplayPrestigeInfo(ctx, steamId, parsedPrestigeType, prestigeLevel, maxPrestigeLevel);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prestige l", description: "Lists prestiges available.")]
    public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        string prestigeTypes = string.Join(", ",
            Enum.GetNames(typeof(PrestigeType))
                .Select(prestigeType => $"<color=#90EE90>{prestigeType}</color>")
        );

        LocalizationService.HandleReply(ctx, $"Available Prestiges: {prestigeTypes}");
    }

    [Command(name: "leaderboard", shortHand: "lb", adminOnly: false, usage: ".prestige lb [PrestigeType]", description: "Lists prestige leaderboard for type.")]
    public static void ListPrestigeTypeLeaderboard(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }
        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }
        if (!ConfigService.ExoPrestiging && parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
            return;
        }

        var prestigeData = GetPrestigeForType(parsedPrestigeType)
        .Where(p => p.Value > 0)  // Filter out values of 0
        .OrderByDescending(p => p.Value)
        .ToList();

        if (!prestigeData.Any())
        {
            LocalizationService.HandleReply(ctx, $"No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet...");
            return;
        }

        var leaderboard = prestigeData
            .Take(10)
            .Select((p, index) => $"<color=yellow>{index + 1}</color>| <color=green>{PlayerCache[p.Key].User.CharacterName.Value}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>")
            .ToList();

        if (leaderboard.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet...");
        }
        else
        {
            for (int i = 0; i < leaderboard.Count; i += 4)
            {
                var batch = leaderboard.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }
    }
}
