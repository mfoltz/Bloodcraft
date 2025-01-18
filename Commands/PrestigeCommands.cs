using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Leveling.PrestigeSystem;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID _shroudBuff = new(1504279833);
    static readonly PrefabGUID _shroudCloak = new(1063517722);

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

        Entity character = ctx.Event.SenderCharacterEntity;
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

                if (ConfigService.RestedXPSystem) LevelingSystem.UpdateMaxRestedXP(steamId, expData);

                expData = new KeyValuePair<int, float>(0, 0);
                steamId.SetPlayerExperience(expData);

                LevelingSystem.SetLevel(character);

                int exoPrestiges = ++prestigeData[PrestigeType.Exo];

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, ExoForm.CalculateFormDuration(exoPrestiges));
                steamId.SetPlayerExoFormData(timeEnergyPair);

                prestigeData[PrestigeType.Exo] = exoPrestiges;
                steamId.SetPlayerPrestiges(prestigeData);

                PrefabGUID exoReward = PrefabGUID.Empty;

                if (ConfigService.ExoPrestigeReward != 0) exoReward = new(ConfigService.ExoPrestigeReward);
                else if (ConfigService.ExoPrestigeReward == 0)
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] prestige complete!");
                    return;
                }

                if (ServerGameManager.TryAddInventoryItem(character, exoReward, ConfigService.ExoPrestigeRewardQuantity))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] prestige complete! You have also been awarded with <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color>!");
                    return;
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, character, exoReward, ConfigService.ExoPrestigeRewardQuantity, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] prestige complete! You have been awarded with <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeReward}</color>! It dropped on the ground becuase your inventory was full though.");
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
            PerformPrestige(ctx, steamId, parsedPrestigeType, handler, xpData);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color> or are at maximum prestige level.");
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

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        Entity character = playerInfo.CharEntity;
        ulong steamId = playerInfo.User.PlatformId;

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

            if (steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(PrestigeType.Exo, out int exoPrestige))
            {
                exoPrestige = level;

                exoData[PrestigeType.Exo] = exoPrestige;
                steamId.SetPlayerPrestiges(exoData);

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, ExoForm.CalculateFormDuration(exoPrestige));
                steamId.SetPlayerExoFormData(timeEnergyPair);

                LocalizationService.HandleReply(ctx, $"Player <color=green>{playerInfo.User.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
                return;
            }
        }

        IPrestigeHandler handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type, use <color=white>.prestige l</color> to see options.");
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
            ReplyExperiencePrestigeEffects(playerInfo.User, level);
        }
        else
        {
            ReplyOtherPrestigeEffects(playerInfo.User, steamId, parsedPrestigeType, level);
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

        List<int> buffs = Configuration.ParseConfigIntegerString(ConfigService.PrestigeBuffs);

        if (buffs.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No prestige buffs configured...");
            return;
        }

        var prestigeBuffs = buffs.Select((buff, index) =>
        {
            string prefab = new PrefabGUID(buff).GetPrefabName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=yellow>{index + 1}</color>| <color=white>{prefab}</color> at prestige <color=green>{index + 1}</color>";
        }).ToList();

        if (prestigeBuffs.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No prestige buffs available.");
        }
        else
        {
            for (int i = 0; i < prestigeBuffs.Count; i += 4)
            {
                var batch = prestigeBuffs.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }

        /*
        var prestigeBuffs = buffs.Select((buff, index) =>
        {
            int level = index + 1;
            string prefab = new PrefabGUID(buff).GetPrefabName();
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
        */
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
            LocalizationService.HandleReply(ctx, "Invalid prestige type, use <color=white>.prestige l</color> to see options.");
            return;
        }

        if (!ConfigService.ExoPrestiging && parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo prestiging is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player...");
            return;
        }

        Entity character = playerInfo.CharEntity;
        ulong steamId = playerInfo.User.PlatformId;

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
                if (steamId.TryGetPlayerExoFormData(out var exoFormData))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MinValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }

                if (GetPlayerBool(steamId, EXO_FORM_KEY)) SetPlayerBool(steamId, EXO_FORM_KEY, false);
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

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
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

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (parsedPrestigeType == PrestigeType.Exo && steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(parsedPrestigeType, out var exoLevel) && exoLevel > 0)
        {
            LocalizationService.HandleReply(ctx, $"Current <color=#90EE90>Exo</color> Prestige Level: <color=yellow>{exoLevel}</color>/{PrestigeTypeToMaxPrestiges[parsedPrestigeType]} | Max Form Duration: <color=green>{(int)ExoForm.CalculateFormDuration(exoLevel)}</color>s");
            ExoForm.UpdateExoFormChargeStored(steamId);

            if (steamId.TryGetPlayerExoFormData(out var exoFormData))
            {
                if (exoFormData.Value < ExoForm.BASE_DURATION)
                {
                    ExoForm.ReplyNotEnoughCharge(user, steamId);
                }
                else if (exoFormData.Value >= ExoForm.BASE_DURATION)
                {
                    LocalizationService.HandleReply(ctx, $"Enough charge to maintain form for <color=white>{(int)exoFormData.Value}</color>s");
                }

                // Generate a list of ability unlock messages based on non-zero levels
                var exoFormSkills = Buffs.ExoFormAbilityUnlockMap
                    .Where(pair => pair.Value != 0) // Filter out abilities unlocked at level 0
                    .Select(pair =>
                    {
                        // Get the ability name from ExoFormAbilityMap and remove the "Prefab" suffix
                        string abilityName = Buffs.ExoFormAbilityMap[pair.Key].GetPrefabName();
                        int prefabIndex = abilityName.IndexOf("Prefab");
                        if (prefabIndex != -1)
                        {
                            abilityName = abilityName[..prefabIndex].TrimEnd();
                        }

                        // Return formatted ability name with unlock level
                        return $"<color=white>{abilityName}</color> at <color=#90EE90>Exo</color> level <color=yellow>{pair.Value}</color>";
                    })
                    .ToList();

                // Group and send the messages in batches of 4
                for (int i = 0; i < exoFormSkills.Count; i += 4)
                {
                    var batch = exoFormSkills.Skip(i).Take(4);
                    string replyMessage = string.Join(", ", batch);
                    LocalizationService.HandleReply(ctx, replyMessage);
                }
            }

            return;
        }
        else if (parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "You have not prestiged in <color=#90EE90>Exo</color> yet.");
            return;
        }

        IPrestigeHandler handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        int maxPrestigeLevel = PrestigeTypeToMaxPrestiges[parsedPrestigeType];
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

        List<string> prestigeTypes = Enum.GetNames(typeof(PrestigeType))
                .Select(prestigeType => $"<color=#90EE90>{prestigeType}</color>")
                .ToList();

        const int maxPerMessage = 6;

        LocalizationService.HandleReply(ctx, $"Available Prestiges:");
        for (int i = 0; i < prestigeTypes.Count; i += maxPerMessage)
        {
            var batch = prestigeTypes.Skip(i).Take(maxPerMessage);
            string prestiges = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{prestiges}");
        }
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
        .Where(p => p.Value > 0)
        .OrderByDescending(p => p.Value)
        .ToList();

        if (!prestigeData.Any())
        {
            LocalizationService.HandleReply(ctx, $"No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet...");
            return;
        }

        var leaderboard = prestigeData
            .Take(10)
            .Select((p, index) =>
            {
                var playerName = PlayerCache.Values.FirstOrDefault(x => x.User.CharacterName.Value == p.Key).User.CharacterName.Value ?? "Unknown";
                return $"<color=yellow>{index + 1}</color>| <color=green>{playerName}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>";
            })
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

    [Command(name: "permashroud", shortHand: "shroud", adminOnly: false, usage: ".prestige shroud", description: "Toggles permashroud if applicable.")]
    public static void PermaShroudToggle(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, SHROUD_KEY);
        if (GetPlayerBool(steamId, SHROUD_KEY))
        {
            LocalizationService.HandleReply(ctx, "Permashroud <color=green>enabled</color>!");

            if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(_shroudBuff) && !character.HasBuff(_shroudBuff)
                && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
            {
                Buffs.ApplyPermanentBuff(character, _shroudBuff);
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Permashroud <color=red>disabled</color>!");
            Equipment equipment = character.Read<Equipment>();

            if (!equipment.IsEquipped(_shroudCloak, out var _) && character.TryGetBuff(_shroudBuff, out Entity shroudBuff))
            {
                character.TryRemoveBuff(_shroudBuff);
            }
        }
    }
}
