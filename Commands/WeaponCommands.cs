using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
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
using static Bloodcraft.Systems.Expertise.WeaponSystem;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Commands;

[CommandGroup(name: "weapon", "wep")]
internal static class WeaponCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    [Command(name: "getexpertise", shortHand: "get", adminOnly: false, usage: ".wep get", description: "Displays current weapon expertise details.")]
    public static void GetExpertiseCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        WeaponType weaponType = GetCurrentWeaponType(character);

        IWeaponHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        var ExpertiseData = handler.GetExpertiseData(steamId);
        int progress = (int)(ExpertiseData.Value - ConvertLevelToXp(ExpertiseData.Key));

        int prestigeLevel = steamId.TryGetPlayerPrestiges(out var prestiges) ? prestiges[WeaponSystem.WeaponPrestigeMap[weaponType]] : 0;

        if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
        {
            LocalizationService.HandleReply(ctx, $"Your weapon expertise is [<color=white>{ExpertiseData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and you have <color=yellow>{progress}</color> <color=#FFC0CB>expertise</color> (<color=white>{WeaponSystem.GetLevelProgress(steamId, handler)}%</color>) with <color=#c0c0c0>{weaponType}</color>");

            if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
            {
                List<KeyValuePair<WeaponStats.WeaponStatType, string>> bonusWeaponStats = [];
                foreach (var stat in stats)
                {
                    float bonus = CalculateScaledWeaponBonus(handler, steamId, weaponType, stat);
                    string formattedBonus = Misc.FormatWeaponStatValue(stat, bonus);

                    /*
                    string formattedBonus = WeaponStats.WeaponStatFormats[stat] switch
                    {
                        "integer" => ((int)bonus).ToString(),
                        "decimal" => bonus.ToString("F2"),
                        "percentage" => (bonus * 100).ToString("F0") + "%",
                        _ => bonus.ToString(),
                    };
                    */

                    bonusWeaponStats.Add(new KeyValuePair<WeaponStats.WeaponStatType, string>(stat, formattedBonus));
                }

                for (int i = 0; i < bonusWeaponStats.Count; i += 6)
                {
                    var batch = bonusWeaponStats.Skip(i).Take(6);
                    string bonuses = string.Join(", ", batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                    LocalizationService.HandleReply(ctx, $"Current weapon stat bonuses: {bonuses}");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "No bonuses from currently equipped weapon.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You haven't gained any expertise for <color=#c0c0c0>{weaponType}</color> yet.");
        }
    }

    [Command(name: "logexpertise", shortHand: "log", adminOnly: false, usage: ".wep log", description: "Toggles expertise logging.")]
    public static void LogExpertiseCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        TogglePlayerBool(steamId, "ExpertiseLogging");

        LocalizationService.HandleReply(ctx, $"Expertise logging is now {(GetPlayerBool(steamId, "ExpertiseLogging") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".wep cst [Weapon] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
    public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponType, string statType)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        if (int.TryParse(statType, out int value))
        {
            int length = Enum.GetValues(typeof(WeaponStats.WeaponStatType)).Length;

            if (value < 1 || value > length)
            {
                LocalizationService.HandleReply(ctx, $"Invalid integer, please use the corresponding stat number shown when using '<color=white>.wep lst</color>'. (<color=white>1</color>-<color=white>{length}</color>)");
                return;
            }

            --value;
            statType = value.ToString();
        }

        if (!Enum.TryParse<WeaponStats.WeaponStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid stat choice, use .wep lst to see options.");
            return;
        }

        if (!Enum.TryParse<WeaponType>(weaponType, true, out var WeaponType))
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon choice.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (ChooseStat(steamId, WeaponType, StatType))
        {
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=#c0c0c0>{WeaponType}</color> and will apply after reequiping.");
            //WeaponManager.UpdateWeaponStats(ctx.Event.SenderCharacterEntity);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {ConfigService.ExpertiseStatChoices} stats for this expertise, the stat has already been chosen for this expertise, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetstats", shortHand: "rst", adminOnly: false, usage: ".wep rst", description: "Reset the stats for current weapon.")]
    public static void ResetWeaponStats(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;
        WeaponType weaponType = GetCurrentWeaponType(character);

        if (!ConfigService.ResetExpertiseItem.Equals(0))
        {
            PrefabGUID item = new(ConfigService.ResetExpertiseItem);
            int quantity = ConfigService.ResetExpertiseItemQuantity;

            if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    ResetStats(steamId, weaponType);
                    LocalizationService.HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You do not have the required item to reset your weapon stats (<color=#ffd9eb>{item.GetLocalizedName()}</color> x<color=white>{quantity}</color>)");
                return;
            }

        }

        ResetStats(steamId, weaponType);
        LocalizationService.HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
    }

    [Command(name: "setexpertise", shortHand: "set", adminOnly: true, usage: ".wep set [Name] [Weapon] [Level]", description: "Sets player weapon expertise level.")]
    public static void SetExpertiseCommand(ChatCommandContext ctx, string name, string weapon, int level)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        if (level < 0 || level > ConfigService.MaxExpertiseLevel)
        {
            string message = $"Level must be between 0 and {ConfigService.MaxExpertiseLevel}.";
            LocalizationService.HandleReply(ctx, message);
            return;
        }

        if (!Enum.TryParse<WeaponType>(weapon, true, out var weaponType))
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxExpertiseLevel}.");
            return;
        }

        IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (expertiseHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon type.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        var xpData = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
        if (SetExtensionMap.TryGetValue(weaponType, out var setFunc))
        {
            setFunc(steamId, xpData);
            LocalizationService.HandleReply(ctx, $"<color=#c0c0c0>{expertiseHandler.GetWeaponType()}</color> expertise set to [<color=white>{level}</color>] for <color=green>{playerInfo.User.CharacterName.Value}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find matching save method from weapon type...");
        }
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".wep lst", description: "Lists weapon stats available.")]
    public static void ListWeaponStatsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        var weaponStatsWithCaps = Enum.GetValues(typeof(WeaponStats.WeaponStatType))
            .Cast<WeaponStats.WeaponStatType>()
            .Select((stat, index) =>
                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>: <color=white>{Misc.FormatWeaponStatValue(stat, WeaponStats.WeaponStatValues[stat])}</color>")
            .ToList();

        if (weaponStatsWithCaps.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No weapon stats available at this time.");
        }
        else
        {
            for (int i = 0; i < weaponStatsWithCaps.Count; i += 4)
            {
                var batch = weaponStatsWithCaps.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".wep l", description: "Lists weapon expertises available.")]
    public static void ListWeaponsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        string weaponTypes = string.Join(", ", Enum.GetNames(typeof(WeaponType)));
        LocalizationService.HandleReply(ctx, $"Available Weapon Expertises: <color=#c0c0c0>{weaponTypes}</color>");
    }

    [Command(name: "setspells", shortHand: "spell", adminOnly: true, usage: ".wep spell [Name] [Slot] [PrefabGUID] [Radius]", description: "Manually sets spells for testing (if you enter a radius it will apply to players around the entered name).")]
    public static void SetSpellCommand(ChatCommandContext ctx, string name, int slot, int ability, float radius = 0f)
    {
        if (!ConfigService.UnarmedSlots)
        {
            LocalizationService.HandleReply(ctx, "Extra spell slots are not enabled.");
            return;
        }

        if (slot < 1 || slot > 2)
        {
            LocalizationService.HandleReply(ctx, "Invalid slot (<color=white>1</color> for Q or <color=white>2</color> for E)");
            return;
        }

        if (radius > 0f)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            float3 charPosition = character.Read<Translation>().Value;

            HashSet<PlayerInfo> processed = [];
            Dictionary<ulong, PlayerInfo> players = new(OnlineCache);

            foreach (PlayerInfo playerInfo in players.Values)
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
                            LocalizationService.HandleReply(ctx, $"First unarmed slot set to <color=white>{new PrefabGUID(ability).GetPrefabName()}</color> for <color=green>{playerInfo.User.CharacterName.Value}</color>.");
                        }
                        else if (slot == 2)
                        {
                            spells.SecondUnarmed = ability;
                            LocalizationService.HandleReply(ctx, $"Second unarmed slot set to <color=white>{new PrefabGUID(ability).GetPrefabName()}</color> for <color=green>{playerInfo.User.CharacterName.Value}</color>.");
                        }

                        steamId.SetPlayerSpells(spells);
                    }

                    processed.Add(playerInfo);
                }
            }
        }
        else if (radius < 0f)
        {
            LocalizationService.HandleReply(ctx, "Radius must be positive if entering a value!");
            return;
        }
        else
        {
            PlayerInfo playerInfo = GetPlayerInfo(name);
            if (!playerInfo.UserEntity.Exists())
            {
                ctx.Reply($"Couldn't find player.");
                return;
            }

            ulong steamId = playerInfo.User.PlatformId;

            if (steamId.TryGetPlayerSpells(out var spells))
            {
                if (slot == 1)
                {
                    spells.FirstUnarmed = ability;
                    LocalizationService.HandleReply(ctx, $"First unarmed slot set to <color=white>{new PrefabGUID(ability).GetPrefabName()}</color> for <color=green>{playerInfo.User.CharacterName.Value}</color>.");
                }
                else if (slot == 2)
                {
                    spells.SecondUnarmed = ability;
                    LocalizationService.HandleReply(ctx, $"Second unarmed slot set to <color=white>{new PrefabGUID(ability).GetPrefabName()}</color> for <color=green>{playerInfo.User.CharacterName.Value}</color>.");
                }

                steamId.SetPlayerSpells(spells);
            }
        }
    }

    /*
    [Command(name: "restorelevels", shortHand: "restore", adminOnly: false, usage: ".wep restore", description: "Fixes weapon levels if they are not correct. Don't use this unless you need to.")]
    public static void ResetWeaponLevels(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;

        if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
        {
            for (int i = 0; i < inventoryBuffer.Length; i++)
            {
                Entity itemEntity = inventoryBuffer[i].ItemEntity.GetEntityOnServer();

                if (itemEntity.Has<WeaponLevelSource>())
                {
                    Entity originalWeapon = PrefabCollectionSystem._PrefabGuidToEntityMap[itemEntity.ReadRO<PrefabGUID>()];
                    WeaponLevelSource weaponLevelSource = originalWeapon.ReadRO<WeaponLevelSource>();

                    if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        int tier = itemEntity.ReadRO<UpgradeableLegendaryItem>().CurrentTier;

                        weaponLevelSource = PrefabCollectionSystem._PrefabGuidToEntityMap[buffer[tier].TierPrefab].ReadRO<WeaponLevelSource>();
                    }

                    itemEntity.Write(weaponLevelSource);
                }
            }

            LocalizationService.HandleReply(ctx, "Set weapon levels to original values.");
        }
    }
    */
}