using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Expertise;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.SystemUtilities.Expertise.WeaponHandler;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Commands;

[CommandGroup(name: "weapon", "wep")] 
internal static class WeaponCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PlayerService PlayerService => Core.PlayerService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    [Command(name: "getexpertise", shortHand: "get", adminOnly: false, usage: ".wep get", description: "Displays your current expertise.")] 
    public static void GetExpertiseCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        WeaponSystem.WeaponType weaponType = GetCurrentWeaponType(character);

        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        var ExpertiseData = handler.GetExpertiseData(steamID);
        int progress = (int)(ExpertiseData.Value - WeaponSystem.ConvertLevelToXp(ExpertiseData.Key));

        int prestigeLevel = PlayerPrestiges.TryGetValue(steamID, out var prestiges) ? prestiges[WeaponSystem.WeaponPrestigeMap[weaponType]] : 0;

        if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
        {
            LocalizationService.HandleReply(ctx, $"Your weapon expertise is [<color=white>{ExpertiseData.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and you have <color=yellow>{progress}</color> <color=#FFC0CB>expertise</color> (<color=white>{WeaponSystem.GetLevelProgress(steamID, handler)}%</color>) with <color=#c0c0c0>{weaponType}</color>");

            if (PlayerWeaponStats.TryGetValue(steamID, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
            {
                List<KeyValuePair<WeaponStats.WeaponStatType, string>> bonusWeaponStats = [];
                foreach (var stat in stats)
                {
                    float bonus = CalculateScaledWeaponBonus(handler, steamID, weaponType, stat);
                    string formattedBonus = WeaponStats.WeaponStatFormats[stat] switch
                    {
                        "integer" => ((int)bonus).ToString(),
                        "decimal" => bonus.ToString("F2"),
                        "percentage" => (bonus * 100).ToString("F0") + "%",
                        _ => bonus.ToString(),
                    };
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
        var SteamID = ctx.Event.User.PlatformId;

        if (PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExpertiseLogging"] = !bools["ExpertiseLogging"];
        }
        SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");          
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".wep cst [Weapon] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
    public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponType, string statType)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        if (!Enum.TryParse<WeaponStats.WeaponStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid stat choice, use .wep lst to see options.");
            return;
        }

        if (!Enum.TryParse<WeaponSystem.WeaponType>(weaponType, true, out var WeaponType))
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon choice.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        if (ChooseStat(steamID, WeaponType, StatType))
        {
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=#c0c0c0>{WeaponType}</color> and will apply after reequiping.");
            SavePlayerWeaponStats();
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {ConfigService.ExpertiseStatChoices} stats for this expertise, the stat has already been chosen for this expertise, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetwepstats", shortHand: "rst", adminOnly: false, usage: ".wep rst", description: "Reset the stats for current weapon.")]
    public static void ResetWeaponStats(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamID = ctx.Event.User.PlatformId;
        WeaponSystem.WeaponType weaponType = GetCurrentWeaponType(character);

        if (!ConfigService.ResetExpertiseItem.Equals(0))
        {
            PrefabGUID item = new(ConfigService.ResetExpertiseItem);
            int quantity = ConfigService.ResetExpertiseItemQuantity;
            // Check if the player has the item to reset stats
            if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    ResetStats(steamID, weaponType);
                    LocalizationService.HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You do not have the required item to reset your weapon stats (<color=#ffd9eb>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>)");
                return;
            }
    
        }

        ResetStats(steamID, weaponType);
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

        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > ConfigService.MaxExpertiseLevel)
        {
            string message = $"Level must be between 0 and {ConfigService.MaxExpertiseLevel}.";
            LocalizationService.HandleReply(ctx, message);
            return;
        }

        if (!Enum.TryParse<WeaponSystem.WeaponType>(weapon, true, out var weaponType))
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxExpertiseLevel}.");
            return;
        }

        IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

        if (expertiseHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon type.");
            return;
        }

        ulong steamId = foundUser.PlatformId;

        var xpData = new KeyValuePair<int, float>(level, WeaponSystem.ConvertLevelToXp(level));
        expertiseHandler.UpdateExpertiseData(steamId, xpData);
        expertiseHandler.SaveChanges();

        LocalizationService.HandleReply(ctx, $"<color=#c0c0c0>{expertiseHandler.GetWeaponType()}</color> expertise set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");
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
            .Select(stat =>
                $"<color=#00FFFF>{stat}</color>: <color=white>{WeaponStats.WeaponStatValues[stat]}</color>")
            .ToArray();

        int halfLength = weaponStatsWithCaps.Length / 2;

        string weaponStatsLine1 = string.Join(", ", weaponStatsWithCaps.Take(halfLength));
        string weaponStatsLine2 = string.Join(", ", weaponStatsWithCaps.Skip(halfLength));

        LocalizationService.HandleReply(ctx, $"Available weapon stats (1/2): {weaponStatsLine1}");
        LocalizationService.HandleReply(ctx, $"Available weapon stats (2/2): {weaponStatsLine2}");
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".wep l", description: "Lists weapon expertises available.")]
    public static void ListWeaponsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ExpertiseSystem)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        string weaponTypes = string.Join(", ", Enum.GetNames(typeof(WeaponSystem.WeaponType)));
        LocalizationService.HandleReply(ctx, $"Available Weapon Expertises: <color=#c0c0c0>{weaponTypes}</color>");
    }

    [Command(name: "setspells", shortHand: "spell", adminOnly: true, usage: ".wep spell [Name] [Slot] [PrefabGUID]", description: "Manually sets spells for testing.")]
    public static void SetSpellCommand(ChatCommandContext ctx, string name, int slot, int ability)
    { 
        if (!ConfigService.UnarmedSlots)
        {
            LocalizationService.HandleReply(ctx, "Extra spell slots are not enabled.");
            return;
        }
        if (slot < 1 || slot > 2)
        {
            LocalizationService.HandleReply(ctx, "Invalid slot.");
            return;
        }

        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        ulong SteamID = foundUser.PlatformId;

        if (PlayerSpells.TryGetValue(SteamID, out var spells))
        {
            if (slot == 1)
            {
                spells.FirstUnarmed = ability;
                LocalizationService.HandleReply(ctx, $"First unarmed slot set to <color=white>{new PrefabGUID(ability).LookupName()}</color> for <color=green>{foundUser.CharacterName.Value}</color>.");
            }
            else
            {
                spells.SecondUnarmed = ability;
                LocalizationService.HandleReply(ctx, $"First unarmed slot set to <color=white>{new PrefabGUID(ability).LookupName()}</color> for <color=green>{foundUser.CharacterName.Value}</color>.");
            }
            PlayerSpells[SteamID] = spells;
            SavePlayerSpells();
        }    
    }

    [Command(name: "restorelevels", shortHand: "restore", adminOnly: false, usage: ".wep restore", description: "Fixes weapon levels if they are not correct. Don't use this unless you need to.")]
    public static void ResetWeaponLevels(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;

        if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && ServerGameManager.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
        {
            for (int i = 0; i < inventoryBuffer.Length; i++)
            {
                Entity itemEntity = inventoryBuffer[i].ItemEntity._Entity;
                if (itemEntity.Has<WeaponLevelSource>())
                {
                    Entity originalWeapon = PrefabCollectionSystem._PrefabGuidToEntityMap[itemEntity.Read<PrefabGUID>()];
                    WeaponLevelSource weaponLevelSource = originalWeapon.Read<WeaponLevelSource>();
                    if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        PrefabGUID PrefabGUID = buffer[tier].TierPrefab;
                        weaponLevelSource = PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID].Read<WeaponLevelSource>();
                    }
                    itemEntity.Write(weaponLevelSource);
                }
            }
            LocalizationService.HandleReply(ctx, "Set weapon levels to original values.");
        }
    }
}