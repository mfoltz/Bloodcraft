using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static readonly bool SoftSynergies = Plugin.SoftSynergies.Value;
    static readonly bool HardSynergies = Plugin.HardSynergies.Value;
    static readonly bool Prestige = Plugin.PrestigeSystem.Value;
    static readonly bool ExoPrestige = Plugin.ExoPrestiging.Value;

    [Command(name: "playerpr", shortHand: "pr", adminOnly: false, usage: ".prestige pr [PrestigeType]", description: "Handles player prestiging.")]
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

        var steamId = ctx.Event.User.PlatformId;

        if (ExoPrestige && parsedPrestigeType.Equals(PrestigeUtilities.PrestigeType.Exo))
        {

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(ctx.Event.User.PlatformId, out var prestigeData) && prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var xpPrestige) && xpPrestige == Plugin.MaxLevelingPrestiges.Value)
            {
                if (!Core.DataStructures.PlayerExperience.TryGetValue(ctx.Event.User.PlatformId, out var expData) && expData.Key == Plugin.MaxPlayerLevel.Value)
                {
                    LocalizationService.HandleReply(ctx, "You must reach max level before <color=#90EE90>Exo</color> prestiging again.");
                    return;
                }
                expData = new KeyValuePair<int, float>(0, 0);
                Core.DataStructures.PlayerExperience[ctx.Event.User.PlatformId] = expData;
                Core.DataStructures.SavePlayerExperience();
                GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);
                prestigeData[PrestigeUtilities.PrestigeType.Exo] += 1;
                Core.DataStructures.SavePlayerPrestiges();
                Entity character = ctx.Event.SenderCharacterEntity;
                PrestigeUtilities.AdjustCharacterStats(character, steamId);
                /*
                Entity character = ctx.Event.SenderCharacterEntity;
                ResistCategoryStats resistCategoryStats = character.Read<ResistCategoryStats>();
                resistCategoryStats.ResistVsBeasts._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                resistCategoryStats.ResistVsHumans._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                resistCategoryStats.ResistVsUndeads._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                resistCategoryStats.ResistVsDemons._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                resistCategoryStats.ResistVsMechanical._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                resistCategoryStats.ResistVsVampires._Value = -Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                character.Write(resistCategoryStats);
                DamageCategoryStats damageCategoryStats = character.Read<DamageCategoryStats>();
                damageCategoryStats.DamageVsBeasts._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                damageCategoryStats.DamageVsHumans._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                damageCategoryStats.DamageVsUndeads._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                damageCategoryStats.DamageVsDemons._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                damageCategoryStats.DamageVsMechanical._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                damageCategoryStats.DamageVsVampires._Value = 1 + Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo];
                character.Write(damageCategoryStats);
                */

                if (Core.ServerGameManager.TryAddInventoryItem(character, new(Plugin.ExoPrestigeReward.Value), Plugin.ExoPrestigeRewardQuantity.Value))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>Exo</color> prestige complete. Damage taken increased by: <color=red>{(Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{new PrefabGUID(Plugin.ExoPrestigeReward.Value).GetPrefabName()}</color>x<color=white>{Plugin.ExoPrestigeRewardQuantity.Value}</color>!");
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, character, new(Plugin.ExoPrestigeReward.Value), Plugin.ExoPrestigeRewardQuantity.Value, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>Exo</color> prestige complete. Damage taken increased by: <color=red>{(Plugin.ExoPrestigeDamageTakenMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(Plugin.ExoPrestigeDamageDealtMultiplier.Value * prestigeData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
                    LocalizationService.HandleReply(ctx, $"You have also been awarded with <color=#ffd9eb>{new PrefabGUID(Plugin.ExoPrestigeReward.Value).GetPrefabName()}</color>x<color=white>{Plugin.ExoPrestigeRewardQuantity.Value}</color>! It dropped on the ground becuase your inventory was full though.");
                }
                return;
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You must reach the maximum level in <color=#90EE90>experience</color> prestige before <color=#90EE90>exo</color> prestiging.");
                return;
            }
        }
        if ((SoftSynergies || HardSynergies) &&
            Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) &&
            classes.Keys.Count == 0 &&
            parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
        {
            LocalizationService.HandleReply(ctx, "You must choose a class before prestiging in experience.");
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
        Entity userEntity = PlayerService.GetUserByName(name, true);
        User user = userEntity.Read<User>();
        ulong steamId = user.PlatformId;
        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
        {
            if (!ExoPrestige)
            {
                LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
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
                Entity character = ctx.Event.SenderCharacterEntity;
                PrestigeUtilities.AdjustCharacterStats(character, steamId);
                LocalizationService.HandleReply(ctx, $"Player <color=green>{user.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
                return;
            }
        }
    
        if ((SoftSynergies || HardSynergies) &&
            Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) &&
            classes.Keys.Count == 0 &&
            parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
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
            PrestigeUtilities.ApplyPrestigeBuffs(ctx, level);
            PrestigeUtilities.ApplyExperiencePrestigeEffects(ctx, steamId, level);
        }
        else
        {
            PrestigeUtilities.ApplyOtherPrestigeEffects(ctx, steamId, parsedPrestigeType, level);
        }
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
        if (!ExoPrestige && parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
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
            if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
            {
                PrestigeUtilities.RemovePrestigeBuffs(ctx, prestigeLevel);
            }
            prestigeData[parsedPrestigeType] = 0;
            Core.DataStructures.SavePlayerPrestiges();
            if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
            {
                Entity character = foundUser.LocalCharacter._Entity;
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

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            PrestigeUtilities.ApplyPrestigeBuffs(ctx, prestigeLevel);
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
            LocalizationService.HandleReply(ctx, $"Damage taken increased by: <color=red>{(Plugin.ExoPrestigeDamageTakenMultiplier.Value * exoData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>, Damage dealt increased by <color=green>{(Plugin.ExoPrestigeDamageDealtMultiplier.Value * exoData[PrestigeUtilities.PrestigeType.Exo] * 100).ToString("F0") + "%"}</color>");
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
        string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeUtilities.PrestigeType)));
        LocalizationService.HandleReply(ctx, $"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
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
        if (!ExoPrestige && parsedPrestigeType == PrestigeUtilities.PrestigeType.Exo)
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
        .Select((p, index) => $"<color=yellow>{index + 1}</color>| <color=green>{PlayerService.playerIdCache[p.Key].Read<PlayerCharacter>().Name.Value}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>")
        .ToList();

        if (leaderboard.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet...");
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
