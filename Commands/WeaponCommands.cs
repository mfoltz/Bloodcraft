using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponStats;
using static Bloodcraft.Services.LocalizationService;

namespace Bloodcraft.Commands;
internal static class WeaponCommands
{
    [Command(name: "getExpertiseProgress", shortHand: "get e", adminOnly: false, usage: ".get e", description: "Displays your current expertise.")] 
    public static void GetExpertiseCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        Entity character = ctx.Event.SenderCharacterEntity;


        ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
        
        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (handler == null)
        {
            HandleReply(ctx, "Invalid weapon.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        var ExpertiseData = handler.GetExpertiseData(steamID);
        int progress = (int)(ExpertiseData.Value - ExpertiseSystem.ConvertLevelToXp(ExpertiseData.Key));
        // ExpertiseData.Key represents the level, and ExpertiseData.Value represents the experience.
        if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
        {
            HandleReply(ctx, $"Your weapon expertise is [<color=white>{ExpertiseData.Key}</color>] and you have <color=yellow>{progress}</color> <color=#FFC0CB>expertise</color> (<color=white>{ExpertiseSystem.GetLevelProgress(steamID, handler)}%</color>) with <color=#c0c0c0>{weaponType}</color>");

            if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
            {
                List<KeyValuePair<WeaponStatManager.WeaponStatType, string>> bonusWeaponStats = [];
                foreach(var stat in stats)
                {
                    float bonus = ModifyUnitStatBuffUtils.CalculateScaledWeaponBonus(handler, steamID, weaponType, stat);
                    if (bonus > 1)
                    {
                        int intBonus = (int)bonus;
                        string bonusString = intBonus.ToString();
                        bonusWeaponStats.Add(new KeyValuePair<WeaponStatManager.WeaponStatType, string>(stat, bonusString));
                    }
                    else
                    {
                        string bonusString = (bonus * 100).ToString("F0") + "%";
                        bonusWeaponStats.Add(new KeyValuePair<WeaponStatManager.WeaponStatType, string>(stat, bonusString));
                    }
                }
                //string bonuses = string.Join(", ", bonusWeaponStats.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                //ctx.Reply($"Current weapon stat bonuses: {bonuses}");
                for (int i = 0; i < bonusWeaponStats.Count; i += 6)
                {
                    var batch = bonusWeaponStats.Skip(i).Take(6);
                    string bonuses = string.Join(", ", batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                    HandleReply(ctx, $"Current weapon stat bonuses: {bonuses}");
                }
            }
            else
            {
                HandleReply(ctx, "No bonuses from currently equipped weapon.");
            }
        }
        else
        {
            HandleReply(ctx, $"You haven't gained any expertise for <color=#c0c0c0>{weaponType}</color> yet.");
        }
    }

    [Command(name: "logExpertiseProgress", shortHand: "log e", adminOnly: false, usage: ".log e", description: "Toggles expertise logging.")]
    public static void LogExpertiseCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExpertiseLogging"] = !bools["ExpertiseLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        HandleReply(ctx, $"Expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");          
    }

    [Command(name: "chooseWeaponStat", shortHand: "cws", adminOnly: false, usage: ".cws [Weapon] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
    public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponType, string statType)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out var StatType))
        {
            HandleReply(ctx, "Invalid stat choice, use .lws to see options.");
            return;
        }

        if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weaponType, true, out var WeaponType))
        {
            HandleReply(ctx, "Invalid weapon choice.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        //ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
        
        if (WeaponType.Equals(ExpertiseSystem.WeaponType.FishingPole))
        {
            HandleReply(ctx, "Invalid weapon.");
            return;
        }

        // Ensure that there is a dictionary for the player's stats
        if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats))
        {
            weaponsStats = [];
            Core.DataStructures.PlayerWeaponStats[steamID] = weaponsStats;
        }

        // Choose a stat for the specific weapon stats instance
        if (PlayerWeaponUtilities.ChooseStat(steamID, WeaponType, StatType))
        {
            HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=#c0c0c0>{WeaponType}</color> and will apply after reequiping.");
            Core.DataStructures.SavePlayerWeaponStats();
        }
        else
        {
            HandleReply(ctx, $"You have already chosen {Plugin.ExpertiseStatChoices.Value} stats for this expertise, the stat has already been chosen for this expertise, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: false, usage: ".rws", description: "Reset the stats for current weapon.")]
    public static void ResetWeaponStats(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamID = ctx.Event.User.PlatformId;
        ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);

       
        if (weaponType.Equals(ExpertiseSystem.WeaponType.FishingPole))
        {
            HandleReply(ctx, "Invalid weapon.");
            return;
        }

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
                    HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
                    return;
                }
            }
            else
            {
                HandleReply(ctx, $"You do not have the required item to reset your weapon stats ({item.GetPrefabName()}x{quantity})");
                return;
            }
            
        }

        PlayerWeaponUtilities.ResetStats(steamID, weaponType);
        HandleReply(ctx, $"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
    }

    [Command(name: "setWeaponExpertise", shortHand: "swe", adminOnly: true, usage: ".swe [Name] [Weapon] [Level]", description: "Sets player weapon expertise level.")]
    public static void SetExpertiseCommand(ChatCommandContext ctx, string name, string weapon, int level)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }

        Entity foundUserEntity = GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "Player not found...");
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
                ctx.Reply(Core.Localization.GetLocalizedWords(message));
            }
            return;
        }

        if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weapon, true, out var weaponType))
        {
            HandleReply(ctx, $"Level must be between 0 and {Plugin.MaxExpertiseLevel.Value}.");
            return;
        }
        
        IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

        if (expertiseHandler == null)
        {
            HandleReply(ctx, "Invalid weapon type.");
            return;
        }

        ulong steamId = foundUser.PlatformId;

        var xpData = new KeyValuePair<int, float>(level, ExpertiseSystem.ConvertLevelToXp(level));
        expertiseHandler.UpdateExpertiseData(steamId, xpData);
        expertiseHandler.SaveChanges();

        HandleReply(ctx, $"<color=#c0c0c0>{expertiseHandler.GetWeaponType()}</color> expertise set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");

    }

    [Command(name: "listWeaponStats", shortHand: "lws", adminOnly: false, usage: ".lws", description: "Lists weapon stats available.")]
    public static void ListWeaponStatsCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
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

        HandleReply(ctx, $"Available weapon stats (1/2): {weaponStatsLine1}");
        HandleReply(ctx, $"Available weapon stats (2/2): {weaponStatsLine2}");
    }

    [Command(name: "listWeaponExpertises", shortHand: "lwe", adminOnly: false, usage: ".lwe", description: "Lists weapon expertises available.")]
    public static void ListWeaponsCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ExpertiseSystem.Value)
        {
            HandleReply(ctx, "Expertise is not enabled.");
            return;
        }
        string weaponTypes = string.Join(", ", Enum.GetNames(typeof(ExpertiseSystem.WeaponType)));
        HandleReply(ctx, $"Available Weapon Expertises: <color=#c0c0c0>{weaponTypes}</color>");
    }

    [Command(name: "lockSpells", shortHand: "lock", adminOnly: false, usage: ".lock", description: "Locks in the next spells equipped to use in your unarmed slots.")]
    public static void LockPlayerSpells(ChatCommandContext ctx)
    {
        if (!Plugin.UnarmedSlots.Value)
        {
            HandleReply(ctx, "Extra spell slots for unarmed are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data))
        {
            bools["SpellLock"] = !bools["SpellLock"];
            if (bools["SpellLock"])
            {
                HandleReply(ctx, "Change spells to the ones you want in your unarmed slots. When done, toggle this again.");
            }
            else
            {
                HandleReply(ctx, "Spells set.");
            }
            Core.DataStructures.SavePlayerBools();
        }
    }

    [Command(name: "shiftLock", shortHand: "shift", adminOnly: false, usage: ".shift", description: "Locks in second spell to shift on weapons.")]
    public static void ShiftPlayerSpells(ChatCommandContext ctx)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled and spells can't be set to shift.");
            return;
        }
        if (!Plugin.ShiftSlot.Value)
        {
            HandleReply(ctx, "Shift slots are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data))
        {
            bools["ShiftLock"] = !bools["ShiftLock"];
            if (bools["ShiftLock"])
            {
                HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
            }
            else
            {
                HandleReply(ctx, "Shift spell <color=red>disabled</color>.");
            }
            Core.DataStructures.SavePlayerBools();
        }
    }

    [Command(name: "setSpells", shortHand: "spell", adminOnly: true, usage: ".spell [Slot] [PrefabGUID]", description: "Manually sets spells for testing.")]
    public static void SetSpellCommand(ChatCommandContext ctx, int slot, int ability)
    {
        if (!Plugin.UnarmedSlots.Value)
        {
            HandleReply(ctx, "Extra spell slots are not enabled.");
            return;
        }
        if (slot < 1 || slot > 2)
        {
            HandleReply(ctx, "Invalid slot.");
            return;
        }

        var user = ctx.Event.User;
        var SteamID = user.PlatformId;

        if (Core.DataStructures.PlayerSpells.TryGetValue(SteamID, out var spells))
        {
            if (slot == 1)
            {
                spells.FirstUnarmed = ability;
            }
            else
            {
                spells.SecondUnarmed = ability;
            }
            Core.DataStructures.PlayerSpells[SteamID] = spells;
            Core.DataStructures.SavePlayerSpells();
        }
    }

    [Command(name: "restoreWeaponLevels", shortHand: "rwl", adminOnly: false, usage: ".rwl", description: "Fixes weapon levels if they are not correct. Don't use this unless you need to.")]
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
            HandleReply(ctx, "Set weapon levels to original values.");
        }
    }
}