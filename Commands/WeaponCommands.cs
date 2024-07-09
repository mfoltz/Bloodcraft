using Bloodcraft.Patches;
using Bloodcraft.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.ExpertiseStats;
using Bloodcraft.Services;

namespace Bloodcraft.Commands;

[CommandGroup(name: "weapon", "wep")] 
internal static class WeaponCommands
{
    [Command(name: "getexpertise", shortHand: "get", adminOnly: false, usage: ".wep get", description: "Displays your current expertise.")] 
    public static void GetExpertiseCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ExpertiseUtilities.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);

        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        var ExpertiseData = handler.GetExpertiseData(steamID);
        int progress = (int)(ExpertiseData.Value - ExpertiseUtilities.ConvertLevelToXp(ExpertiseData.Key));
        // ExpertiseData.Key represents the level, and ExpertiseData.Value represents the experience.
        if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
        {
            LocalizationService.HandleReply(ctx, $"Your weapon expertise is [<color=white>{ExpertiseData.Key}</color>] and you have <color=yellow>{progress}</color> <color=#FFC0CB>expertise</color> (<color=white>{ExpertiseUtilities.GetLevelProgress(steamID, handler)}%</color>) with <color=#c0c0c0>{weaponType}</color>");

            if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
            {
                List<KeyValuePair<WeaponStatManager.WeaponStatType, string>> bonusWeaponStats = [];
                foreach (var stat in stats)
                {
                    float bonus = ModifyUnitStatBuffUtils.CalculateScaledWeaponBonus(handler, steamID, weaponType, stat);
                    string formattedBonus = ExpertiseStats.WeaponStatManager.StatFormatMap[stat] switch
                    {
                        "integer" => ((int)bonus).ToString(),
                        "decimal" => bonus.ToString("F2"),
                        "percentage" => (bonus * 100).ToString("F0") + "%",
                        _ => bonus.ToString(),
                    };
                    bonusWeaponStats.Add(new KeyValuePair<WeaponStatManager.WeaponStatType, string>(stat, formattedBonus));
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
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExpertiseLogging"] = !bools["ExpertiseLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");          
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".wep cst [Weapon] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
    public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponType, string statType)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid stat choice, use .lws to see options.");
            return;
        }

        if (!Enum.TryParse<ExpertiseUtilities.WeaponType>(weaponType, true, out var WeaponType))
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon choice.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        //ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
        /*
        if (WeaponType.Equals(ExpertiseUtilities.WeaponType.FishingPole))
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon.");
            return;
        }
        */
        // Ensure that there is a dictionary for the player's stats
        if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats))
        {
            weaponsStats = [];
            Core.DataStructures.PlayerWeaponStats[steamID] = weaponsStats;
        }

        // Choose a stat for the specific weapon stats instance
        if (PlayerWeaponUtilities.ChooseStat(steamID, WeaponType, StatType))
        {
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=#c0c0c0>{WeaponType}</color> and will apply after reequiping.");
            Core.DataStructures.SavePlayerWeaponStats();
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {Plugin.ExpertiseStatChoices.Value} stats for this expertise, the stat has already been chosen for this expertise, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetwepstats", shortHand: "rst", adminOnly: false, usage: ".wep rst", description: "Reset the stats for current weapon.")]
    public static void ResetWeaponStats(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamID = ctx.Event.User.PlatformId;
        ExpertiseUtilities.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);


        /*
        if (WeaponType.Equals(ExpertiseUtilities.WeaponType.FishingPole))
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon.");
            return;
        }
        */

        if (!Plugin.ResetExpertiseItem.Value.Equals(0))
        {
            PrefabGUID item = new(Plugin.ResetExpertiseItem.Value);
            int quantity = Plugin.ResetExpertiseItemQuantity.Value;
            // Check if the player has the item to reset stats
            if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    PlayerWeaponUtilities.ResetStats(steamID, weaponType);
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

        PlayerWeaponUtilities.ResetStats(steamID, weaponType);
        LocalizationService.HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
    }

    [Command(name: "setexpertise", shortHand: "set", adminOnly: true, usage: ".wep set [Name] [Weapon] [Level]", description: "Sets player weapon expertise level.")]
    public static void SetExpertiseCommand(ChatCommandContext ctx, string name, string weapon, int level)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > Plugin.MaxExpertiseLevel.Value)
        {
            string message = $"Level must be between 0 and {Plugin.MaxExpertiseLevel.Value}.";
            if (LocalizationService.LanguageLocalization == "English")
            {
                ctx.Reply(message);
            }
            else
            {
                ctx.Reply(LocalizationService.GetLocalizedWords(message));
            }
            return;
        }

        if (!Enum.TryParse<ExpertiseUtilities.WeaponType>(weapon, true, out var weaponType))
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {Plugin.MaxExpertiseLevel.Value}.");
            return;
        }

        IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

        if (expertiseHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid weapon type.");
            return;
        }

        ulong steamId = foundUser.PlatformId;

        var xpData = new KeyValuePair<int, float>(level, ExpertiseUtilities.ConvertLevelToXp(level));
        expertiseHandler.UpdateExpertiseData(steamId, xpData);
        expertiseHandler.SaveChanges();

        LocalizationService.HandleReply(ctx, $"<color=#c0c0c0>{expertiseHandler.GetWeaponType()}</color> expertise set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");

    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".wep lst", description: "Lists weapon stats available.")]
    public static void ListWeaponStatsCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        var weaponStatsWithCaps = Enum.GetValues(typeof(WeaponStatManager.WeaponStatType))
            .Cast<WeaponStatManager.WeaponStatType>()
            .Select(stat =>
                $"<color=#00FFFF>{stat}</color>: <color=white>{WeaponStatManager.BaseCaps[stat]}</color>")
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
        if (!Plugin.ExpertiseSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        string weaponTypes = string.Join(", ", Enum.GetNames(typeof(ExpertiseUtilities.WeaponType)));
        LocalizationService.HandleReply(ctx, $"Available Weapon Expertises: <color=#c0c0c0>{weaponTypes}</color>");
    }



    [Command(name: "setspells", shortHand: "spell", adminOnly: true, usage: ".wep spell [Name] [Slot] [PrefabGUID]", description: "Manually sets spells for testing.")]
    public static void SetSpellCommand(ChatCommandContext ctx, string name, int slot, int ability)
    { 
        if (!Plugin.UnarmedSlots.Value)
        {
            LocalizationService.HandleReply(ctx, "Extra spell slots are not enabled.");
            return;
        }
        if (slot < 1 || slot > 2)
        {
            LocalizationService.HandleReply(ctx, "Invalid slot.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        ulong SteamID = foundUser.PlatformId;

        if (Core.DataStructures.PlayerSpells.TryGetValue(SteamID, out var spells))
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
            Core.DataStructures.PlayerSpells[SteamID] = spells;
            Core.DataStructures.SavePlayerSpells();
        }    
    }

    [Command(name: "restorelevels", shortHand: "restore", adminOnly: false, usage: ".wep restore", description: "Fixes weapon levels if they are not correct. Don't use this unless you need to.")]
    public static void ResetWeaponLevels(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;

        if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, character, out Entity inventoryEntity) && Core.ServerGameManager.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
        {
            for (int i = 0; i < inventoryBuffer.Length; i++)
            {
                Entity itemEntity = inventoryBuffer[i].ItemEntity._Entity;
                if (itemEntity.Has<WeaponLevelSource>())
                {
                    Entity originalWeapon = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[itemEntity.Read<PrefabGUID>()];
                    WeaponLevelSource weaponLevelSource = originalWeapon.Read<WeaponLevelSource>();
                    if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        PrefabGUID PrefabGUID = buffer[tier].TierPrefab;
                        weaponLevelSource = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID].Read<WeaponLevelSource>();
                    }
                    itemEntity.Write(weaponLevelSource);
                }
            }
            LocalizationService.HandleReply(ctx, "Set weapon levels to original values.");
        }
    }
}