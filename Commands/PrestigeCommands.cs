using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using Bloodcraft.SystemUtilities.Leveling;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool Prestige = Plugin.PrestigeSystem.Value;
    static readonly bool Exo = Plugin.ExoPrestiging.Value;
    static readonly int ExoPrestiges = Plugin.ExoPrestiges.Value;
    static readonly int ExoReward = Plugin.ExoPrestigeReward.Value;
    static readonly int ExoRewardAmount = Plugin.ExoPrestigeRewardQuantity.Value;
    static readonly float ExoPrestigeDamageTakenMultiplier = Plugin.ExoPrestigeDamageTakenMultiplier.Value;
    static readonly float ExoPrestigeDamageDealtMultiplier = Plugin.ExoPrestigeDamageDealtMultiplier.Value;
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value;
    static readonly int MaxLevelingPrestiges = Plugin.MaxLevelingPrestiges.Value;
    static readonly bool RestedXP = Plugin.RestedXP.Value;

    [Command(name: "self", shortHand: "me", adminOnly: false, usage: ".prestige me [PrestigeType]", description: "Handles player prestiging.")]
    public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeUtilities.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Exo && parsedPrestigeType.Equals(PrestigeUtilities.PrestigeType.Exo))
        {
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var xpPrestige) && xpPrestige == MaxLevelingPrestiges)
            {
                if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var expData) && expData.Key < MaxPlayerLevel)
                {
                    LocalizationService.HandleReply(ctx, "You must reach max level before <color=#90EE90>Exo</color> prestiging again.");
                    return;
                }
                else if (prestigeData[PrestigeUtilities.PrestigeType.Exo] >= ExoPrestiges)
                {
                    LocalizationService.HandleReply(ctx, $"You have reached the maximum amount of <color=#90EE90>Exo</color> prestiges. (<color=white>{ExoPrestiges}</color>)");
                    return;
                }

                expData = new KeyValuePair<int, float>(0, 0);
                Core.DataStructures.PlayerExperience[steamId] = expData;
                Core.DataStructures.SavePlayerExperience();

                if (RestedXP) PlayerLevelingUtilities.ResetRestedXP(steamId);

                GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);

                prestigeData[PrestigeUtilities.PrestigeType.Exo] += 1;
                Core.DataStructures.SavePlayerPrestiges();

                Entity character = ctx.Event.SenderCharacterEntity;
                PrestigeUtilities.AdjustCharacterStats(character, steamId);

                PrefabGUID exoReward = new(0);

                if (ExoReward != 0) exoReward = new(ExoReward);
                else if (ExoReward == 0)
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, "No prefab configured for <color=#90EE90>Exo</color> reward...");
                    return;
                }

                if (ServerGameManager.TryAddInventoryItem(character, exoReward, ExoRewardAmount))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{exoReward.GetPrefabName()}</color>x<color=white>{ExoRewardAmount}</color>!");
                    return;
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, character, exoReward, ExoRewardAmount, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige complete. Damage taken increased by: <color=red>{(ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{exoReward.GetPrefabName()}</color>x<color=white>{ExoRewardAmount}</color>! It dropped on the ground becuase your inventory was full though.");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You must reach the maximum level in <color=#90EE90>Experience</color> prestige before <color=#90EE90>Exo</color> prestiging.");
                return;
            }
        }
        else if (!Exo && parsedPrestigeType.Equals(PrestigeUtilities.PrestigeType.Exo))
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

        var xpData = handler.GetExperienceData(steamId);
        if (PrestigeUtilities.CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PrestigeUtilities.PerformPrestige(ctx, steamId, parsedPrestigeType, handler);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prestige set [Name] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
    public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeUtilities.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        Entity foundUserEntity = PlayerService.PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User user = foundUserEntity.Read<User>();
        ulong steamId = user.PlatformId;
        Entity character = user.LocalCharacter._Entity;

        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
        {
            if (!Exo)
            {
                LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestiging is not enabled.");
                return;
            }

            if (level > PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType] || level < 1)
            {
                LocalizationService.HandleReply(ctx, $"The maximum level for <color=#90EE90>{parsedPrestigeType}</color> prestige is {PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType]}.");
                return;
            }

            if (Core.DataStructures.PlayerPrestiges.ContainsKey(steamId) && !Core.DataStructures.PlayerPrestiges[steamId].ContainsKey(PrestigeUtilities.PrestigeType.Exo))
            {
                Core.DataStructures.PlayerPrestiges[steamId].TryAdd(PrestigeUtilities.PrestigeType.Exo, 0);
            }

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var exoData) && exoData.TryGetValue(PrestigeUtilities.PrestigeType.Exo, out var exoPrestige))
            {
                exoPrestige = level;
                exoData[PrestigeUtilities.PrestigeType.Exo] = exoPrestige;
                Core.DataStructures.SavePlayerPrestiges();
                PrestigeUtilities.AdjustCharacterStats(character, steamId);
                LocalizationService.HandleReply(ctx, $"Player <color=green>{user.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
                return;
            }
        }

        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        if (!Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData))
        {
            prestigeData = [];
            Core.DataStructures.PlayerPrestiges[steamId] = prestigeData;
        }

        if (!prestigeData.ContainsKey(parsedPrestigeType))
        {
            prestigeData[parsedPrestigeType] = 0;
        }

        if (level > PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType])
        {
            LocalizationService.HandleReply(ctx, $"The maximum level for <color=#90EE90>{parsedPrestigeType}</color> prestige is {PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType]}.");
            return;
        }

        prestigeData[parsedPrestigeType] = level;
        handler.SaveChanges();

        // Apply effects based on the prestige type
        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
        {
            PrestigeUtilities.ApplyPrestigeBuffs(character, level);
            PrestigeUtilities.ApplyExperiencePrestigeEffects(user, level);
        }
        else
        {
            PrestigeUtilities.ApplyOtherPrestigeEffects(user, steamId, parsedPrestigeType, level);
        }
        Core.DataStructures.SavePlayerPrestiges();
        LocalizationService.HandleReply(ctx, $"Player <color=green>{user.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
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

        if (!PrestigeUtilities.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        if (!Exo && parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
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
        Entity character = foundUser.LocalCharacter._Entity;

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel))
        {
            if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
            {
                PrestigeUtilities.RemovePrestigeBuffs(character, prestigeLevel);
            }
            prestigeData[parsedPrestigeType] = 0;
            Core.DataStructures.SavePlayerPrestiges();
            if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
            {
                PrestigeUtilities.AdjustCharacterStats(character, steamId);
            }
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
        Entity character = ctx.Event.SenderCharacterEntity;

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            PrestigeUtilities.ApplyPrestigeBuffs(character, prestigeLevel);
            LocalizationService.HandleReply(ctx, "Prestige buffs applied.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{PrestigeUtilities.PrestigeType.Experience}</color>.");
        }
    }

    [Command(name: "get", adminOnly: false, usage: ".prestige get [PrestigeType]", description: "Shows information about player's prestige status.")]
    public static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeUtilities.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo && Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var exoData) && exoData.TryGetValue(parsedPrestigeType, out var exoLevel) && exoLevel > 0)
        {
            LocalizationService.HandleReply(ctx, $"Current <color=#90EE90>Exo</color> Prestige Level: <color=yellow>{exoLevel}</color>/{PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType]}");
            LocalizationService.HandleReply(ctx, $"Damage taken increased by: <color=red>{(ExoPrestigeDamageTakenMultiplier * exoData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(ExoPrestigeDamageDealtMultiplier * exoData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
            return;
        }
        else if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
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

        var maxPrestigeLevel = PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
        {
            PrestigeUtilities.DisplayPrestigeInfo(ctx, steamId, parsedPrestigeType, prestigeLevel, maxPrestigeLevel);
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

        string prestigeTypes = string.Join(", ",
            Enum.GetNames(typeof(PrestigeUtilities.PrestigeType))
                .Select(prestigeType => $"<color=#90EE90>{prestigeType}</color>")
        );
        LocalizationService.HandleReply(ctx, $"Available Prestiges: {prestigeTypes}");
    }

    [Command(name: "leaderboard", shortHand: "lb", adminOnly: false, usage: ".prestige lb [PrestigeType]", description: "Lists prestige leaderboard for type.")]
    public static void ListPrestigeTypeLeaderboard(ChatCommandContext ctx, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }
        if (!PrestigeUtilities.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .prestige l to see options.");
            return;
        }
        if (!Exo && parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
            return;
        }

        var prestigeData = PrestigeUtilities.GetPrestigeForType(parsedPrestigeType)
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
            .Select((p, index) => $"<color=yellow>{index + 1}</color>| <color=green>{PlayerService.PlayerCache[p.Key].Read<User>().CharacterName.Value}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>")
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
