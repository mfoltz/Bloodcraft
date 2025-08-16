using Bloodcraft.Interfaces;
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
using static Bloodcraft.Systems.Leveling.PrestigeManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    const int EXO_PRESTIGES = 100;
    const int PRESTIGE_BATCH = 6;
    const int LEADERBOARD_BATCH = 4;

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
            LocalizationService.HandleReply(ctx, "Invalid prestige type, use <color=white>'.prestige l'</color> to see options.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
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
                else if (prestigeData[PrestigeType.Exo] >= EXO_PRESTIGES)
                {
                    LocalizationService.HandleReply(ctx, $"You have reached the maximum amount of <color=#90EE90>Exo</color> prestiges. (<color=white>{EXO_PRESTIGES}</color>)");
                    return;
                }

                if (ConfigService.RestedXPSystem) LevelingSystem.UpdateMaxRestedXP(steamId, expData);

                expData = new KeyValuePair<int, float>(0, 0);
                steamId.SetPlayerExperience(expData);

                LevelingSystem.SetLevel(playerCharacter);

                int exoPrestiges = ++prestigeData[PrestigeType.Exo];

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, Shapeshifts.CalculateFormDuration(exoPrestiges));
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

                if (ServerGameManager.TryAddInventoryItem(playerCharacter, exoReward, ConfigService.ExoPrestigeRewardQuantity))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] prestige complete! You have also been awarded with <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color>!");
                    return;
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, exoReward, ConfigService.ExoPrestigeRewardQuantity, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] prestige complete! You have been awarded with <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color>! It dropped on the ground becuase your inventory was full though.");
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

        var handler = PrestigeFactory.GetPrestige(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
            return;
        }

        var xpData = handler.GetPrestigeData(steamId);
        if (CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PerformPrestige(ctx, steamId, parsedPrestigeType, handler, xpData);
            Buffs.RefreshStats(playerCharacter);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color> or are at maximum prestige level.");
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
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
            return;
        }

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (parsedPrestigeType == PrestigeType.Exo && steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(parsedPrestigeType, out var exoLevel) && exoLevel > 0)
        {
            LocalizationService.HandleReply(ctx, $"Current <color=#90EE90>Exo</color> Prestige Level: <color=yellow>{exoLevel}</color>/{PrestigeTypeToMaxPrestiges[parsedPrestigeType]} | Max Form Duration: <color=green>{(int)Shapeshifts.CalculateFormDuration(exoLevel)}</color>s");
            Shapeshifts.UpdateExoFormChargeStored(steamId);

            if (steamId.TryGetPlayerExoFormData(out var exoFormData))
            {
                if (exoFormData.Value < Shapeshifts.BASE_DURATION)
                {
                    Shapeshifts.ReplyNotEnoughCharge(user, steamId, exoFormData.Value);
                }
                else if (exoFormData.Value >= Shapeshifts.BASE_DURATION)
                {
                    LocalizationService.HandleReply(ctx, $"Enough charge to maintain form for <color=white>{(int)exoFormData.Value}</color>s");
                }

                /*
                var exoFormSkills = Buffs.EvolvedVampireUnlocks
                    .Where(pair => pair.Value != 0)
                    .Select(pair =>
                    {
                        string abilityName = Buffs.DraculaFormAbilityMap[pair.Key].GetPrefabName();
                        int prefabIndex = abilityName.IndexOf("Prefab");
                        if (prefabIndex != -1)
                        {
                            abilityName = abilityName[..prefabIndex].TrimEnd();
                        }

                        return $"<color=yellow>{pair.Value}</color>| <color=white>{abilityName}</color>";
                    })
                    .ToList();

                for (int i = 0; i < exoFormSkills.Count; i += 4)
                {
                    var batch = exoFormSkills.Skip(i).Take(4);
                    string replyMessage = string.Join(", ", batch);
                    LocalizationService.HandleReply(ctx, replyMessage);
                }
                */
            }

            return;
        }
        else if (parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "You have not prestiged in <color=#90EE90>Exo</color> yet.");
            return;
        }

        IPrestige handler = PrestigeFactory.GetPrestige(parsedPrestigeType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
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

    [Command(name: "set", adminOnly: true, usage: ".prestige set [Player] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
    public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
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

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, Shapeshifts.CalculateFormDuration(exoPrestige));
                steamId.SetPlayerExoFormData(timeEnergyPair);

                LocalizationService.HandleReply(ctx, $"Player <color=green>{playerInfo.User.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
                return;
            }
        }

        IPrestige handler = PrestigeFactory.GetPrestige(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
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
            Progression.PlayerProgressionCacheManager.UpdatePlayerProgressionPrestige(steamId, true);
        }
        else
        {
            ReplyOtherPrestigeEffects(playerInfo.User, steamId, parsedPrestigeType, level);
        }

        LocalizationService.HandleReply(ctx, $"Player <color=green>{playerInfo.User.CharacterName.Value}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
    }

    [Command(name: "reset", shortHand: "r", adminOnly: true, usage: ".prestige r [Player] [PrestigeType]", description: "Handles resetting prestiging.")]
    public static void ResetPrestige(ChatCommandContext ctx, string name, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
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
                RemovePrestigeBuffs(character);
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

                if (GetPlayerBool(steamId, SHAPESHIFT_KEY)) SetPlayerBool(steamId, SHAPESHIFT_KEY, false);
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

            LocalizationService.HandleReply(ctx, "Prestige buffs applied! (if they were missing)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{PrestigeType.Experience}</color>.");
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

        List<string> prestigeTypes = [..Enum.GetNames(typeof(PrestigeType)).Select(prestigeType => $"<color=#90EE90>{prestigeType}</color>")];

        LocalizationService.HandleReply(ctx, $"Prestiges:");
        foreach (var batch in prestigeTypes.Batch(PRESTIGE_BATCH))
        {
            string prestiges = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, prestiges);
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

        if (!ConfigService.PrestigeLeaderboard)
        {
            LocalizationService.HandleReply(ctx, "Leaderboards are not enabled.");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use <color=white>'.prestige l'</color> to see valid options.");
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
            LocalizationService.HandleReply(ctx, $"No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet!");
            return;
        }

        var leaderboard = prestigeData
            .Take(10)
            .Select((p, index) =>
            {
                var playerName = SteamIdPlayerInfoCache.Values.FirstOrDefault(x => x.User.CharacterName.Value == p.Key).User.CharacterName.Value ?? "Unknown";
                return $"<color=yellow>{index + 1}</color>| <color=green>{playerName}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>";
            })
            .ToList();

        if (leaderboard.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"No players have prestiged in <color=#90EE90>{parsedPrestigeType}</color> yet!");
        }
        else
        {
            foreach (var batch in leaderboard.Batch(LEADERBOARD_BATCH))
            {
                string replyMessage = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "exoform", adminOnly: false, usage: ".prestige exoform", description: "Toggles taunting to enter exoform.")]
    public static void ToggleExoFormEmote(ChatCommandContext ctx)
    {
        if (!ConfigService.ExoPrestiging)
        {
            ctx.Reply("Exo prestiging is not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) && exoPrestiges > 0)
        {
            if (!Progression.ConsumedMegara(ctx.Event.SenderUserEntity) && !Progression.ConsumedDracula(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("You must consume at least one primordial essence...");
                return;
            }

            TogglePlayerBool(steamId, SHAPESHIFT_KEY);
            ctx.Reply($"Exo form emote action (<color=white>taunt</color>) {(GetPlayerBool(steamId, SHAPESHIFT_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
        }
        else
        {
            ctx.Reply("You are not yet worthy...");
        }
    }

    [Command(name: "selectform", shortHand: "sf", adminOnly: false, usage: ".prestige sf [EvolvedVampire|CorruptedSerpent]", description: "Select active exoform shapeshift.")]
    public static void SelectFormCommand(ChatCommandContext ctx, string shapeshift)
    {
        if (!ConfigService.ExoPrestiging)
        {
            ctx.Reply("Exo prestiging is not enabled.");
            return;
        }

        if (!Enum.TryParse<ShapeshiftType>(shapeshift, ignoreCase: true, out var form))
        {
            var shapeshifts = string.Join(", ", Enum.GetNames(typeof(ShapeshiftType)).Select(name => $"<color=white>{name}</color>"));
            ctx.Reply($"Invalid shapeshift! Valid options: {shapeshifts}");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) && exoPrestiges > 0)
        {
            if (form.Equals(ShapeshiftType.EvolvedVampire) && !Progression.ConsumedDracula(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("You must consume Dracula's essence before manifesting his form...");
                return;
            }

            if (form.Equals(ShapeshiftType.CorruptedSerpent) && !Progression.ConsumedMegara(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("You must consume Megara's essence before manifesting her form...");
                return;
            }

            steamId.SetPlayerShapeshift(form);
            ctx.Reply($"Current Exoform: <color=white>{form}</color>");
        }
        else
        {
            ctx.Reply("You are not yet worthy...");
        }
    }

    [Command(name: "ignoreleaderboard", shortHand: "ignore", adminOnly: true, usage: ".prestige ignore [Player]", description: "Adds (or removes) player to list of those who will not appear on prestige leaderboards. Intended for admin-duties only accounts.")]
    public static void IgnorePrestigeLeaderboardPlayerCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player...");
            return;
        }

        if (!DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Add(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredPrestigeLeaderboard();

            ctx.Reply($"<color=green>{playerInfo.User.CharacterName.Value}</color> added to the ignore prestige leaderboard list!");
        }
        else if (DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Remove(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredPrestigeLeaderboard();

            ctx.Reply($"<color=green>{playerInfo.User.CharacterName.Value}</color> removed from the ignore prestige leaderboard list!");
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

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, SHROUD_KEY);
        if (GetPlayerBool(steamId, SHROUD_KEY))
        {
            LocalizationService.HandleReply(ctx, "Permashroud <color=green>enabled</color>!");

            if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(_shroudBuff) && !playerCharacter.HasBuff(_shroudBuff)
                && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
            {
                Buffs.TryApplyPermanentBuff(playerCharacter, _shroudBuff);
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Permashroud <color=red>disabled</color>!");
            Equipment equipment = playerCharacter.Read<Equipment>();

            if (!equipment.IsEquipped(_shroudCloak, out var _) && playerCharacter.HasBuff(_shroudBuff))
            {
                playerCharacter.TryRemoveBuff(buffPrefabGuid: _shroudBuff);
            }
        }
    }

    [Command(name: "iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen", adminOnly: true, usage: ".prestige iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen", description: "Globally removes prestige buffs from players to facilitate changing prestige buffs in config.")]
    public static void GlobalClassPurgeCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        GlobalPurgePrestigeBuffs(ctx);
    }
}
