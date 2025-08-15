using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Expertise.WeaponSystem;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static Bloodcraft.Utilities.Progression;
using static Bloodcraft.Utilities.Progression.ModifyUnitStatBuffSettings;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Commands;

[CommandGroup(name: "weapon", "wep")]
internal static class WeaponCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID _exoFormBuff = new(-31099041);

    [Command(name: "get", adminOnly: false, usage: ".wep get", description: "Displays current weapon expertise details.")]
    public static void GetExpertiseCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        WeaponType weaponType = GetCurrentWeaponType(playerCharacter);

        IWeaponExpertise handler = WeaponExpertiseFactory.GetExpertise(weaponType);
        if (handler == null)
        {
            LocalizationService.Reply(ctx, "Expertise handler for weapon is null; this shouldn't happen and you may want to inform the developer.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        var ExpertiseData = handler.GetExpertiseData(steamId);

        int progress = (int)(ExpertiseData.Value - ConvertLevelToXp(ExpertiseData.Key));
        int prestigeLevel = steamId.TryGetPlayerPrestiges(out var prestiges) ? prestiges[WeaponPrestigeTypes[weaponType]] : 0;

        if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
        {
            LocalizationService.Reply(ctx,
                "Your weapon expertise is [<color=white>{0}</color>][<color=#90EE90>{1}</color>] and you have <color=yellow>{2}</color> <color=#FFC0CB>expertise</color> (<color=white>{3}%</color>) with <color=#c0c0c0>{4}</color>!",
                ExpertiseData.Key,
                prestigeLevel,
                progress,
                GetLevelProgress(steamId, handler),
                weaponType);

            if (steamId.TryGetPlayerWeaponStats(out var weaponTypeStats) && weaponTypeStats.TryGetValue(weaponType, out var weaponStatTypes))
            {
                List<KeyValuePair<WeaponStatType, string>> weaponExpertiseStats = [];
                foreach (WeaponStatType weaponStatType in weaponStatTypes)
                {
                    if (!TryGetScaledModifyUnitExpertiseStat(handler, playerCharacter, steamId, weaponType, 
                        weaponStatType, out float statValue, out ModifyUnitStatBuff modifyUnitStatBuff)) continue;

                    string weaponStatString = Misc.FormatWeaponStatValue(weaponStatType, statValue);
                    weaponExpertiseStats.Add(new KeyValuePair<WeaponStatType, string>(weaponStatType, weaponStatString));
                }

                for (int i = 0; i < weaponExpertiseStats.Count; i += 6)
                {
                    var batch = weaponExpertiseStats.Skip(i).Take(6);
                    string bonuses = string.Join(", ", batch.Select(stat =>
                        string.Format("<color=#00FFFF>{0}</color>: <color=white>{1}</color>", stat.Key, stat.Value)));
                    LocalizationService.Reply(ctx, "<color=#c0c0c0>{0}</color> Stats: {1}", weaponType, bonuses);
                }
            }
            else
            {
                LocalizationService.Reply(ctx, "No bonuses from currently equipped weapon.");
            }
        }
        else
        {
            LocalizationService.Reply(ctx, "You haven't gained any expertise for <color=#c0c0c0>{0}</color> yet!", weaponType);
        }
    }

    [Command(name: "log", adminOnly: false, usage: ".wep log", description: "Toggles expertise logging.")]
    public static void LogExpertiseCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        TogglePlayerBool(steamId, WEAPON_LOG_KEY);

        LocalizationService.Reply(ctx,
            "Expertise logging is now {0}.",
            GetPlayerBool(steamId, WEAPON_LOG_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>");
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".wep cst [WeaponOrStat] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
    public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponOrStat, int statType = default)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        WeaponType finalWeaponType;
        WeaponStats.WeaponStatType finalWeaponStat;

        if (int.TryParse(weaponOrStat, out int numericStat))
        {
            numericStat--;

            if (!Enum.IsDefined(typeof(WeaponStats.WeaponStatType), numericStat))
            {
                LocalizationService.Reply(ctx,
                    "Invalid stat, use '<color=white>.wep lst</color>' to see valid options.");
                return;
            }

            finalWeaponStat = (WeaponStats.WeaponStatType)numericStat;
            finalWeaponType = GetCurrentWeaponType(playerCharacter);

            if (ChooseStat(steamId, finalWeaponType, finalWeaponStat))
            {
                Buffs.RefreshStats(playerCharacter);
                LocalizationService.Reply(ctx,
                    "<color=#00FFFF>{0}</color> has been chosen for <color=#c0c0c0>{1}</color>!",
                    finalWeaponStat,
                    finalWeaponType);
            }
        }
        else
        {
            if (!Enum.TryParse(weaponOrStat, true, out finalWeaponType))
            {
                LocalizationService.Reply(ctx,
                    "Invalid weapon choice, use '<color=white>.wep lst</color>' to see valid options.");
                return;
            }

            if (statType <= 0)
            {
                LocalizationService.Reply(ctx,
                    "Invalid stat, use '<color=white>.wep lst</color>' to see valid options.");
                return;
            }

            int typedStat = --statType;

            if (!Enum.IsDefined(typeof(WeaponStats.WeaponStatType), typedStat))
            {
                LocalizationService.Reply(ctx,
                    "Invalid stat, use '<color=white>.wep lst</color>' to see valid options.");
                return;
            }

            finalWeaponStat = (WeaponStats.WeaponStatType)typedStat;

            if (ChooseStat(steamId, finalWeaponType, finalWeaponStat))
            {
                Buffs.RefreshStats(playerCharacter);
                LocalizationService.Reply(ctx,
                    "<color=#00FFFF>{0}</color> has been chosen for <color=#c0c0c0>{1}</color>!",
                    finalWeaponStat,
                    finalWeaponType);
            }
        }
    }

    [Command(name: "resetstats", shortHand: "rst", adminOnly: false, usage: ".wep rst", description: "Reset the stats for current weapon.")]
    public static void ResetWeaponStats(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        WeaponType weaponType = GetCurrentWeaponType(playerCharacter);

        string freeKey = weaponType.ToString();
        if (GetPlayerBool(steamId, freeKey))
        {
            ResetStats(steamId, weaponType);
            Buffs.RefreshStats(playerCharacter);

            SetPlayerBool(steamId, freeKey, false);
            LocalizationService.Reply(ctx, "Your weapon stats have been reset for <color=#c0c0c0>{0}</color>!", weaponType);
            return;
        }

        if (!ConfigService.ResetExpertiseItem.Equals(0))
        {
            PrefabGUID item = new(ConfigService.ResetExpertiseItem);
            int quantity = ConfigService.ResetExpertiseItemQuantity;

            if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    ResetStats(steamId, weaponType);
                    Buffs.RefreshStats(playerCharacter);

                    LocalizationService.Reply(ctx, "Your weapon stats have been reset for <color=#c0c0c0>{0}</color>!", weaponType);
                    return;
                }
            }
            else
            {
                LocalizationService.Reply(ctx,
                    "You don't have the required item to reset your weapon stats! (<color=#ffd9eb>{0}</color>x<color=white>{1}</color>)",
                    item.GetLocalizedName(),
                    quantity);
                return;
            }

        }

        ResetStats(steamId, weaponType);
        Buffs.RefreshStats(playerCharacter);

        LocalizationService.Reply(ctx, "Your weapon stats have been reset for <color=#c0c0c0>{0}</color>!", weaponType);
    }

    [Command(name: "set", adminOnly: true, usage: ".wep set [Name] [Weapon] [Level]", description: "Sets player weapon expertise level.")]
    public static void SetExpertiseCommand(ChatCommandContext ctx, string name, string weapon, int level)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            LocalizationService.Reply(ctx, "Couldn't find player.");
            return;
        }

        if (level < 0 || level > ConfigService.MaxExpertiseLevel)
        {
            LocalizationService.Reply(ctx, "Level must be between 0 and {0}.", ConfigService.MaxExpertiseLevel);
            return;
        }

        if (!Enum.TryParse<WeaponType>(weapon, true, out var weaponType))
        {
            LocalizationService.Reply(ctx, "Level must be between 0 and {0}.", ConfigService.MaxExpertiseLevel);
            return;
        }

        IWeaponExpertise expertiseHandler = WeaponExpertiseFactory.GetExpertise(weaponType);
        if (expertiseHandler == null)
        {
            LocalizationService.Reply(ctx, "Invalid weapon type.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        var xpData = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
        if (SetExtensionMap.TryGetValue(weaponType, out var setFunc))
        {
            setFunc(steamId, xpData);
            Buffs.RefreshStats(playerInfo.CharEntity);

            LocalizationService.Reply(ctx,
                "<color=#c0c0c0>{0}</color> expertise set to [<color=white>{1}</color>] for <color=green>{2}</color>",
                expertiseHandler.GetWeaponType(),
                level,
                playerInfo.User.CharacterName.Value);
        }
        else
        {
            LocalizationService.Reply(ctx, "Couldn't find matching save method for weapon type...");
        }
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".wep lst", description: "Lists weapon stats available.")]
    public static void ListWeaponStatsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        var weaponStatsWithCaps = Enum.GetValues(typeof(WeaponStats.WeaponStatType))
            .Cast<WeaponStats.WeaponStatType>()
            .Select((stat, index) =>
                string.Format("<color=yellow>{0}</color>| <color=#00FFFF>{1}</color>: <color=white>{2}</color>",
                    index + 1,
                    stat,
                    Misc.FormatWeaponStatValue(stat, WeaponStats.WeaponStatBaseCaps[stat])))
            .ToList();

        if (weaponStatsWithCaps.Count == 0)
        {
            LocalizationService.Reply(ctx, "No weapon stats available at this time.");
        }
        else
        {
            for (int i = 0; i < weaponStatsWithCaps.Count; i += 4)
            {
                var batch = weaponStatsWithCaps.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);

                LocalizationService.Reply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".wep l", description: "Lists weapon expertises available.")]
    public static void ListWeaponsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.Reply(ctx, "Expertise is not enabled.");
            return;
        }

        string weaponTypes = string.Join(", ", Enum.GetNames(typeof(WeaponType)));
        LocalizationService.Reply(ctx, "Available Weapon Expertises: <color=#c0c0c0>{0}</color>", weaponTypes);
    }

    [Command(name: "setspells", shortHand: "spell", adminOnly: true, usage: ".wep spell [Name] [Slot] [PrefabGuid] [Radius]", description: "Manually sets spells for testing (if you enter a radius it will apply to players around the entered name).")]
    public static void SetSpellCommand(ChatCommandContext ctx, string name, int slot, int ability, float radius = 0f)
    {
        if (!ConfigService.UnarmedSlots)
        {
            LocalizationService.Reply(ctx, "Extra spell slots are not enabled.");
            return;
        }

        if (slot < 1 || slot > 7)
        {
            LocalizationService.Reply(ctx, "Invalid slot (<color=white>1</color> for Q or <color=white>2</color> for E)");
            return;
        }

        if (radius > 0f)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            float3 charPosition = character.Read<Translation>().Value;

            HashSet<PlayerInfo> processed = [];

            foreach (PlayerInfo playerInfo in SteamIdOnlinePlayerInfoCache.Values)
            {
                if (processed.Contains(playerInfo)) continue;
                else if (playerInfo.CharEntity.TryGetComponent(out Translation translation) && math.distance(charPosition, translation.Value) <= radius)
                {
                    ulong steamId = playerInfo.User.PlatformId;

                    if (steamId.TryGetPlayerSpells(out var spells))
                    {
                        if (slot == 1)
                        {
                            spells.FirstUnarmed = ability;
                            LocalizationService.Reply(ctx,
                                "First unarmed slot set to <color=white>{0}</color> for <color=green>{1}</color>.",
                                new PrefabGUID(ability).GetPrefabName(),
                                playerInfo.User.CharacterName.Value);
                        }
                        else if (slot == 2)
                        {
                            spells.SecondUnarmed = ability;
                            LocalizationService.Reply(ctx,
                                "Second unarmed slot set to <color=white>{0}</color> for <color=green>{1}</color>.",
                                new PrefabGUID(ability).GetPrefabName(),
                                playerInfo.User.CharacterName.Value);
                        }

                        steamId.SetPlayerSpells(spells);
                    }

                    processed.Add(playerInfo);
                }
            }
        }
        else if (radius < 0f)
        {
            LocalizationService.Reply(ctx, "Radius must be positive!");
            return;
        }
        else
        {
            PlayerInfo playerInfo = GetPlayerInfo(name);
            if (!playerInfo.UserEntity.Exists())
            {
                LocalizationService.Reply(ctx, "Couldn't find player.");
                return;
            }

            ulong steamId = playerInfo.User.PlatformId;

            if (steamId.TryGetPlayerSpells(out var spells))
            {
                if (slot == 1)
                {
                    spells.FirstUnarmed = ability;
                    LocalizationService.Reply(ctx,
                        "First unarmed slot set to <color=white>{0}</color> for <color=green>{1}</color>.",
                        new PrefabGUID(ability).GetPrefabName(),
                        playerInfo.User.CharacterName.Value);
                }
                else if (slot == 2)
                {
                    spells.SecondUnarmed = ability;
                    LocalizationService.Reply(ctx,
                        "Second unarmed slot set to <color=white>{0}</color> for <color=green>{1}</color>.",
                        new PrefabGUID(ability).GetPrefabName(),
                        playerInfo.User.CharacterName.Value);
                }

                steamId.SetPlayerSpells(spells);
            }
        }
    }

    [Command(name: "lockspells", shortHand: "locksp", adminOnly: false, usage: ".wep locksp", description: "Locks in the next spells equipped to use in your unarmed slots.")]
    public static void LockPlayerSpells(ChatCommandContext ctx)
    {
        if (!ConfigService.UnarmedSlots)
        {
            LocalizationService.Reply(ctx, "Extra spell slots for unarmed are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;

        if (playerCharacter.HasBuff(_exoFormBuff))
        {
            LocalizationService.Reply(ctx, "Spells cannot be locked when using exoform.");
            return;
        }

        TogglePlayerBool(SteamID, SPELL_LOCK_KEY);

        if (GetPlayerBool(SteamID, SPELL_LOCK_KEY))
        {
            LocalizationService.Reply(ctx, "Change spells to the ones you want in your unarmed slots. When done, toggle this again.");
        }
        else
        {
            LocalizationService.Reply(ctx, "Spells locked.");
        }
    }
}