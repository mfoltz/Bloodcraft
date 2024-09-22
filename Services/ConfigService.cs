using BepInEx.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Bloodcraft.Services;

public static class ConfigService
{
    public static string LanguageLocalization { get; private set; }
    public static bool ClientCompanion { get; private set; }
    public static bool EliteShardBearers { get; private set; }
    public static bool PotionStacking { get; private set; }
    public static bool StarterKit { get; private set; }
    public static string KitPrefabs { get; private set; }
    public static string KitQuantities { get; private set; }
    public static bool QuestSystem { get; private set; }
    public static bool InfiniteDailies { get; private set; }
    public static string QuestRewards { get; private set; }
    public static string QuestRewardAmounts { get; private set; }
    public static int RerollDailyPrefab { get; private set; }
    public static int RerollDailyAmount { get; private set; }
    public static int RerollWeeklyPrefab { get; private set; }
    public static int RerollWeeklyAmount { get; private set; }
    public static bool LevelingSystem { get; private set; }
    public static bool RestedXPSystem { get; private set; }
    public static float RestedXPRate { get; private set; }
    public static int RestedXPMax { get; private set; }
    public static float RestedXPTickRate { get; private set; }
    public static int MaxLevel { get; private set; }
    public static int StartingLevel { get; private set; }
    public static float UnitLevelingMultiplier { get; private set; }
    public static float VBloodLevelingMultiplier { get; private set; }
    public static float DocileUnitMultiplier { get; private set; }
    public static float WarEventMultiplier { get; private set; }
    public static float UnitSpawnerMultiplier { get; private set; }
    public static int ChangeClassItem { get; private set; }
    public static int ChangeClassQuantity { get; private set; }
    public static float GroupLevelingMultiplier { get; private set; }
    public static float LevelScalingMultiplier { get; private set; }
    public static bool PlayerParties { get; private set; }
    public static int MaxPartySize { get; private set; }
    public static float ExpShareDistance { get; private set; }
    public static bool PrestigeSystem { get; private set; }
    public static string PrestigeBuffs { get; private set; }
    public static string PrestigeLevelsToUnlockClassSpells { get; private set; }
    public static int MaxLevelingPrestiges { get; private set; }
    public static float LevelingPrestigeReducer { get; private set; }
    public static float PrestigeRatesReducer { get; private set; }
    public static float PrestigeStatMultiplier { get; private set; }
    public static float PrestigeRateMultiplier { get; private set; }
    public static bool ExoPrestiging { get; private set; }
    public static int ExoPrestiges { get; private set; }
    public static int ExoPrestigeReward { get; private set; }
    public static int ExoPrestigeRewardQuantity { get; private set; }
    public static float ExoPrestigeDamageTakenMultiplier { get; private set; }
    public static float ExoPrestigeDamageDealtMultiplier { get; private set; }
    public static bool ExpertiseSystem { get; private set; }
    public static int MaxExpertisePrestiges { get; private set; }
    public static bool UnarmedSlots { get; private set; }
    public static bool ShiftSlot { get; private set; }
    public static int MaxExpertiseLevel { get; private set; }
    public static float UnitExpertiseMultiplier { get; private set; }
    public static float VBloodExpertiseMultiplier { get; private set; }
    public static int ExpertiseStatChoices { get; private set; }
    public static int ResetExpertiseItem { get; private set; }
    public static int ResetExpertiseItemQuantity { get; private set; }
    public static float MaxHealth { get; private set; }
    public static float MovementSpeed { get; private set; }
    public static float PrimaryAttackSpeed { get; private set; }
    public static float PhysicalLifeLeech { get; private set; }
    public static float SpellLifeLeech { get; private set; }
    public static float PrimaryLifeLeech { get; private set; }
    public static float PhysicalPower { get; private set; }
    public static float SpellPower { get; private set; }
    public static float PhysicalCritChance { get; private set; }
    public static float PhysicalCritDamage { get; private set; }
    public static float SpellCritChance { get; private set; }
    public static float SpellCritDamage { get; private set; }
    public static bool BloodSystem { get; private set; }
    public static int MaxLegacyPrestiges { get; private set; }
    public static bool BloodQualityBonus { get; private set; }
    public static float PrestigeBloodQuality { get; private set; }
    public static int MaxBloodLevel { get; private set; }
    public static float UnitLegacyMultiplier { get; private set; }
    public static float VBloodLegacyMultiplier { get; private set; }
    public static int LegacyStatChoices { get; private set; }
    public static int ResetLegacyItem { get; private set; }
    public static int ResetLegacyItemQuantity { get; private set; }
    public static float HealingReceived { get; private set; }
    public static float DamageReduction { get; private set; }
    public static float PhysicalResistance { get; private set; }
    public static float SpellResistance { get; private set; }
    public static float ResourceYield { get; private set; }
    public static float CCReduction { get; private set; }
    public static float SpellCooldownRecoveryRate { get; private set; }
    public static float WeaponCooldownRecoveryRate { get; private set; }
    public static float UltimateCooldownRecoveryRate { get; private set; }
    public static float MinionDamage { get; private set; }
    public static float ShieldAbsorb { get; private set; }
    public static float BloodEfficiency { get; private set; }
    public static bool ProfessionSystem { get; private set; }
    public static int MaxProfessionLevel { get; private set; }
    public static float ProfessionMultiplier { get; private set; }

    /*
    public static float DurabilityMultiplier { get; private set; }
    public static float AlchemyStatMultiplier { get; private set; }
    public static float BlacksmithingStatMultiplier { get; private set; }
    public static float EnchantingStatMultiplier { get; private set; }
    public static float TailoringStatMultiplier { get; private set; }
    */
    public static bool ExtraRecipes { get; private set; }
    public static bool FamiliarSystem { get; private set; }
    public static bool ShareUnlocks { get; private set; }
    public static bool FamiliarCombat { get; private set; }
    public static bool FamiliarPrestige { get; private set; }
    public static int MaxFamiliarPrestiges { get; private set; }
    public static float FamiliarPrestigeStatMultiplier { get; private set; }
    public static int MaxFamiliarLevel { get; private set; }
    public static bool AllowVBloods { get; private set; }
    public static string BannedUnits { get; private set; }
    public static string BannedTypes { get; private set; }
    public static float VBloodDamageMultiplier { get; private set; }
    public static float UnitFamiliarMultiplier { get; private set; }
    public static float VBloodFamiliarMultiplier { get; private set; }
    public static float UnitUnlockChance { get; private set; }
    public static float VBloodUnlockChance { get; private set; }
    public static float ShinyChance { get; private set; }
    public static int ShinyCostItemPrefab { get; private set; }
    public static int ShinyCostItemQuantity { get; private set; }
    public static bool SoftSynergies { get; private set; }
    public static bool HardSynergies { get; private set; }
    public static bool ClassSpellSchoolOnHitEffects { get; private set; }
    public static float OnHitProcChance { get; private set; }
    public static float StatSynergyMultiplier { get; private set; }
    public static string BloodKnightWeapon { get; private set; }
    public static string BloodKnightBlood { get; private set; }
    public static string DemonHunterWeapon { get; private set; }
    public static string DemonHunterBlood { get; private set; }
    public static string VampireLordWeapon { get; private set; }
    public static string VampireLordBlood { get; private set; }
    public static string ShadowBladeWeapon { get; private set; }
    public static string ShadowBladeBlood { get; private set; }
    public static string ArcaneSorcererWeapon { get; private set; }
    public static string ArcaneSorcererBlood { get; private set; }
    public static string DeathMageWeapon { get; private set; }
    public static string DeathMageBlood { get; private set; }
    public static int DefaultClassSpell { get; private set; }
    public static string BloodKnightBuffs { get; private set; }
    public static string BloodKnightSpells { get; private set; }
    public static string DemonHunterBuffs { get; private set; }
    public static string DemonHunterSpells { get; private set; }
    public static string VampireLordBuffs { get; private set; }
    public static string VampireLordSpells { get; private set; }
    public static string ShadowBladeBuffs { get; private set; }
    public static string ShadowBladeSpells { get; private set; }
    public static string ArcaneSorcererBuffs { get; private set; }
    public static string ArcaneSorcererSpells { get; private set; }
    public static string DeathMageBuffs { get; private set; }
    public static string DeathMageSpells { get; private set; }
    public static class ConfigInitialization
    {
        static readonly Regex regex = new(@"^\[(.+)\]$");

        public static readonly List<string> DirectoryPaths =
        [
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME), // 0
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PlayerLeveling"), // 1
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Quests"), // 2
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "WeaponExpertise"), // 3
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "BloodLegacies"), // 4
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Professions"), // 5
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars"), // 6
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarLeveling"), // 7
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarUnlocks") // 8
        ];

        static readonly List<string> SectionOrder =
        [
            "General",
            "StarterKit",
            "Quests",
            "Leveling",
            "Prestige",
            "Expertise",
            "Legacies",
            "Professions",
            "Familiars",
            "Classes"
        ];
        public class ConfigEntryDefinition(string section, string key, object defaultValue, string description)
        {
            public string Section { get; } = section;
            public string Key { get; } = key;
            public object DefaultValue { get; } = defaultValue;
            public string Description { get; } = description;
        }

        static readonly List<ConfigEntryDefinition> ConfigEntries =
        [
            new ConfigEntryDefinition("General", "LanguageLocalization", "English", "The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese"),
            new ConfigEntryDefinition("General", "ClientCompanion", false, "Enable if using the client companion mod, can configure what's displayed in the client config."),
            new ConfigEntryDefinition("General", "EliteShardBearers", false, "Enable or disable elite shard bearers."),
            new ConfigEntryDefinition("General", "PotionStacking", true, "Enable or disable potion stacking (can have t01 effects and t02 effects at the same time. also requires professions enabled)."),
            new ConfigEntryDefinition("StarterKit", "StarterKit", false, "Enable or disable the starter kit."),
            new ConfigEntryDefinition("StarterKit", "KitPrefabs", "862477668,-1531666018,-1593377811,1821405450", "The PrefabGUID hashes for the starter kit."),
            new ConfigEntryDefinition("StarterKit", "KitQuantities", "500,1000,1000,250", "The quantity of each item in the starter kit."),
            new ConfigEntryDefinition("Quests", "QuestSystem", false, "Enable or disable quests (currently only kill quests)."),
            new ConfigEntryDefinition("Quests", "InfiniteDailies", false, "Enable or disable infinite dailies."),
            new ConfigEntryDefinition("Quests", "QuestRewards", "28358550,576389135,-257494203", "The PrefabGUID hashes for quest reward pool."),
            new ConfigEntryDefinition("Quests", "QuestRewardAmounts", "50,250,50", "The amount of each reward in the pool. Will be multiplied accordingly for weeklies (*5) and vblood kill quests (*3)."),
            new ConfigEntryDefinition("Quests", "RerollDailyPrefab", -949672483, "Prefab item for rerolling daily."),
            new ConfigEntryDefinition("Quests", "RerollDailyAmount", 50, "Cost of prefab for rerolling daily."),
            new ConfigEntryDefinition("Quests", "RerollWeeklyPrefab", -949672483, "Prefab item for rerolling weekly."),
            new ConfigEntryDefinition("Quests", "RerollWeeklyAmount", 50, "Cost of prefab for rerolling weekly. Won't work if already completed for the week."),
            new ConfigEntryDefinition("Leveling", "LevelingSystem", false, "Enable or disable the leveling system."),
            new ConfigEntryDefinition("Leveling", "RestedXPSystem", false, "Enable or disable rested experience for players logging out inside of coffins (half for wooden, full for stone). Prestiging level will reset accumulated rested xp."),
            new ConfigEntryDefinition("Leveling", "RestedXPRate", 0.05f, "Rate of Rested XP accumulation per tick (as a percentage of maximum allowed rested XP, if configured to one tick per hour 20 hours offline in a stone coffin will provide maximum current rested XP)."),
            new ConfigEntryDefinition("Leveling", "RestedXPMax", 5, "Maximum extra levels worth of rested XP a player can accumulate."),
            new ConfigEntryDefinition("Leveling", "RestedXPTickRate", 120f, "Minutes required to accumulate one tick of Rested XP."),
            new ConfigEntryDefinition("Leveling", "MaxLevel", 90, "The maximum level a player can reach."),
            new ConfigEntryDefinition("Leveling", "StartingLevel", 0, "Starting level for players if no data is found."),
            new ConfigEntryDefinition("Leveling", "UnitLevelingMultiplier", 7.5f, "The multiplier for experience gained from units."),
            new ConfigEntryDefinition("Leveling", "VBloodLevelingMultiplier", 15f, "The multiplier for experience gained from VBloods."),
            new ConfigEntryDefinition("Leveling", "DocileUnitMultiplier", 0.15f, "The multiplier for experience gained from docile units."),
            new ConfigEntryDefinition("Leveling", "WarEventMultiplier", 0.2f, "The multiplier for experience gained from war event trash spawns."),
            new ConfigEntryDefinition("Leveling", "UnitSpawnerMultiplier", 0f, "The multiplier for experience gained from unit spawners (vermin nests, tombs)."),
            new ConfigEntryDefinition("Leveling", "GroupLevelingMultiplier", 1f, "The multiplier for experience gained from group kills."),
            new ConfigEntryDefinition("Leveling", "LevelScalingMultiplier", 0.05f, "Reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove."),
            new ConfigEntryDefinition("Leveling", "PlayerParties", false, "Enable or disable the ability to group with players not in your clan for experience/familiar unlock sharing."),
            new ConfigEntryDefinition("Leveling", "MaxPartySize", 5, "The maximum number of players that can share experience in a group."),
            new ConfigEntryDefinition("Leveling", "ExpShareDistance", 25f, "Default is ~5 floor tile lengths."),
            new ConfigEntryDefinition("Prestige", "PrestigeSystem", false, "Enable or disable the prestige system (requires leveling to be enabled)"),
            new ConfigEntryDefinition("Prestige", "PrestigeBuffs", "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884", "The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level"),
            new ConfigEntryDefinition("Prestige", "PrestigeLevelsToUnlockClassSpells", "0,1,2,3,4,5", "The prestige levels at which class spells are unlocked. This should match the number of spells per class +1 to account for the default class spell. Can leave at 0 if you want them unlocked from the start."),
            new ConfigEntryDefinition("Prestige", "MaxLevelingPrestiges", 10, "The maximum number of prestiges a player can reach in leveling"),
            new ConfigEntryDefinition("Prestige", "LevelingPrestigeReducer", 0.05f, "Flat factor by which experience is reduced per increment of prestige in leveling"),
            new ConfigEntryDefinition("Prestige", "PrestigeRatesReducer", 0.10f, "Flat factor by which rates are reduced in expertise/legacy per increment of prestige in expertise/legacy"),
            new ConfigEntryDefinition("Prestige", "PrestigeStatMultiplier", 0.10f, "Flat factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy"),
            new ConfigEntryDefinition("Prestige", "PrestigeRateMultiplier", 0.10f, "Flat factor by which rates are increased in expertise/legacy per increment of prestige in leveling"),
            new ConfigEntryDefinition("Prestige", "ExoPrestiging", false, "Enable or disable exo prestiges (need to max normal prestiges first)"),
            new ConfigEntryDefinition("Prestige", "ExoPrestiges", 100, "The number of exo prestiges available"),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeReward", 28358550, "The reward for exo prestiging (tier 3 nether shards by default)"),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeRewardQuantity", 500, "The quantity of the reward for exo prestiging"),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeDamageTakenMultiplier", 0.05f, "The damage multiplier per exo prestige (applies to damage taken by the player)"),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeDamageDealtMultiplier", 0.025f, "The damage multiplier per exo prestige (applies to damage dealt by the player)"),
            new ConfigEntryDefinition("Expertise", "ExpertiseSystem", false, "Enable or disable the expertise system"),
            new ConfigEntryDefinition("Expertise", "MaxExpertisePrestiges", 10, "The maximum number of prestiges a player can reach in expertise"),
            new ConfigEntryDefinition("Expertise", "UnarmedSlots", false, "Enable or disable the ability to use extra unarmed spell slots"),
            new ConfigEntryDefinition("Expertise", "ShiftSlot", false, "Enable or disable using class spell on shift"),
            new ConfigEntryDefinition("Expertise", "MaxExpertiseLevel", 100, "The maximum level a player can reach in weapon expertise"),
            new ConfigEntryDefinition("Expertise", "UnitExpertiseMultiplier", 2f, "The multiplier for expertise gained from units"),
            new ConfigEntryDefinition("Expertise", "VBloodExpertiseMultiplier", 5f, "The multiplier for expertise gained from VBloods"),
            new ConfigEntryDefinition("Expertise", "ExpertiseStatChoices", 2, "The maximum number of stat choices a player can pick for a weapon expertise. Max of 3 will be sent to client UI for display."),
            new ConfigEntryDefinition("Expertise", "ResetExpertiseItem", 576389135, "Item PrefabGUID cost for resetting weapon stats"),
            new ConfigEntryDefinition("Expertise", "ResetExpertiseItemQuantity", 500, "Quantity of item required for resetting stats"),
            new ConfigEntryDefinition("Expertise", "MaxHealth", 250f, "The base cap for maximum health"),
            new ConfigEntryDefinition("Expertise", "MovementSpeed", 0.25f, "The base cap for movement speed"),
            new ConfigEntryDefinition("Expertise", "PrimaryAttackSpeed", 0.10f, "The base cap for primary attack speed"),
            new ConfigEntryDefinition("Expertise", "PhysicalLifeLeech", 0.10f, "The base cap for physical life leech"),
            new ConfigEntryDefinition("Expertise", "SpellLifeLeech", 0.10f, "The base cap for spell life leech"),
            new ConfigEntryDefinition("Expertise", "PrimaryLifeLeech", 0.15f, "The base cap for primary life leech"),
            new ConfigEntryDefinition("Expertise", "PhysicalPower", 20f, "The base cap for physical power"),
            new ConfigEntryDefinition("Expertise", "SpellPower", 10f, "The base cap for spell power"),
            new ConfigEntryDefinition("Expertise", "PhysicalCritChance", 0.10f, "The base cap for physical critical strike chance"),
            new ConfigEntryDefinition("Expertise", "PhysicalCritDamage", 0.50f, "The base cap for physical critical strike damage"),
            new ConfigEntryDefinition("Expertise", "SpellCritChance", 0.10f, "The base cap for spell critical strike chance"),
            new ConfigEntryDefinition("Expertise", "SpellCritDamage", 0.50f, "The base cap for spell critical strike damage"),
            new ConfigEntryDefinition("Legacies", "BloodSystem", false, "Enable or disable the blood legacy system"),
            new ConfigEntryDefinition("Legacies", "MaxLegacyPrestiges", 10, "The maximum number of prestiges a player can reach in blood legacies"),
            new ConfigEntryDefinition("Legacies", "BloodQualityBonus", false, "Enable or disable blood quality bonus system (if using prestige, legacy level will be used with _prestigeBloodQuality multiplier below)"),
            new ConfigEntryDefinition("Legacies", "PrestigeBloodQuality", 0.05f, "Blood quality bonus per prestige legacy level."),
            new ConfigEntryDefinition("Legacies", "MaxBloodLevel", 100, "The maximum level a player can reach in blood legacies"),
            new ConfigEntryDefinition("Legacies", "UnitLegacyMultiplier", 1f, "The multiplier for lineage gained from units"),
            new ConfigEntryDefinition("Legacies", "VBloodLegacyMultiplier", 5f, "The multiplier for lineage gained from VBloods"),
            new ConfigEntryDefinition("Legacies", "LegacyStatChoices", 2, "The maximum number of stat choices a player can pick for a blood legacy. Max of 3 will be sent to client UI for display."),
            new ConfigEntryDefinition("Legacies", "ResetLegacyItem", 576389135, "Item PrefabGUID cost for resetting blood stats"),
            new ConfigEntryDefinition("Legacies", "ResetLegacyItemQuantity", 500, "Quantity of item required for resetting blood stats"),
            new ConfigEntryDefinition("Legacies", "HealingReceived", 0.15f, "The base cap for healing received"),
            new ConfigEntryDefinition("Legacies", "DamageReduction", 0.05f, "The base cap for damage reduction"),
            new ConfigEntryDefinition("Legacies", "PhysicalResistance", 0.10f, "The base cap for physical resistance"),
            new ConfigEntryDefinition("Legacies", "SpellResistance", 0.10f, "The base cap for spell resistance"),
            new ConfigEntryDefinition("Legacies", "ResourceYield", 0.25f, "The base cap for resource yield"),
            new ConfigEntryDefinition("Legacies", "CCReduction", 0.20f, "The base cap for crowd control reduction"),
            new ConfigEntryDefinition("Legacies", "SpellCooldownRecoveryRate", 0.10f, "The base cap for spell cooldown recovery rate"),
            new ConfigEntryDefinition("Legacies", "WeaponCooldownRecoveryRate", 0.10f, "The base cap for weapon cooldown recovery rate"),
            new ConfigEntryDefinition("Legacies", "UltimateCooldownRecoveryRate", 0.20f, "The base cap for ultimate cooldown recovery rate"),
            new ConfigEntryDefinition("Legacies", "MinionDamage", 0.25f, "The base cap for minion damage"),
            new ConfigEntryDefinition("Legacies", "ShieldAbsorb", 0.50f, "The base cap for shield absorb"),
            new ConfigEntryDefinition("Legacies", "BloodEfficiency", 0.10f, "The base cap for blood efficiency"),
            new ConfigEntryDefinition("Professions", "ProfessionSystem", false, "Enable or disable the profession system"),
            new ConfigEntryDefinition("Professions", "MaxProfessionLevel", 100, "The maximum level a player can reach in professions"),
            new ConfigEntryDefinition("Professions", "ProfessionMultiplier", 10f, "The multiplier for profession experience gained"),
            new ConfigEntryDefinition("Professions", "ExtraRecipes", false, "Enable or disable extra recipes"),
            /*
            new ConfigEntryDefinition("Professions", "DurabilityMultiplier", 1f, "Extra durability percentage from blacksmithing, enchanting, and tailoring (1 is 100% extra, 0.5 is 50% extra, etc)."),
            new ConfigEntryDefinition("Professions", "AlchemyStatMultiplier", 0.5f, "Extra stat/duration percentage gained on potions from alchemy at max level."),
            new ConfigEntryDefinition("Professions", "BlacksmithingStatMultiplier", 0.10f, "Extra stat percentage for stats gained on armor from blacksmithing at max level."),
            new ConfigEntryDefinition("Professions", "EnchantingStatMultiplier", 0.10f, "Extra stat percentage for stats gained on armor from enchanting at max level."),
            new ConfigEntryDefinition("Professions", "TailoringStatMultiplier", 0.10f, "Extra stat percentage for stats gained on armor from tailoring at max level."),
            */
            new ConfigEntryDefinition("Familiars", "FamiliarSystem", false, "Enable or disable the familiar system"),
            new ConfigEntryDefinition("Familiars", "ShareUnlocks", false, "Enable or disable sharing unlocks between players in clans or parties (uses exp share distance)"),
            new ConfigEntryDefinition("Familiars", "FamiliarCombat", true, "Enable or disable combat for familiars"),
            new ConfigEntryDefinition("Familiars", "FamiliarPrestige", false, "Enable or disable the prestige system for familiars"),
            new ConfigEntryDefinition("Familiars", "MaxFamiliarPrestiges", 10, "The maximum number of prestiges a familiar can reach"),
            new ConfigEntryDefinition("Familiars", "FamiliarPrestigeStatMultiplier", 0.10f, "The multiplier for stats gained from familiar prestiges"),
            new ConfigEntryDefinition("Familiars", "MaxFamiliarLevel", 90, "The maximum level a familiar can reach"),
            new ConfigEntryDefinition("Familiars", "AllowVBloods", false, "Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list)"),
            new ConfigEntryDefinition("Familiars", "BannedUnits", "", "The PrefabGUID hashes for units that cannot be used as familiars. Same structure as the buff lists except unit prefabs"),
            new ConfigEntryDefinition("Familiars", "BannedTypes", "", "The types of units that cannot be used as familiars go here (Human, Undead, Demon, Mechanical, Beast)"),
            new ConfigEntryDefinition("Familiars", "VBloodDamageMultiplier", 1f, "Leave at 1 for no change (controls damage familiars do to VBloods)"),
            new ConfigEntryDefinition("Familiars", "UnitFamiliarMultiplier", 7.5f, "The multiplier for experience gained from units"),
            new ConfigEntryDefinition("Familiars", "VBloodFamiliarMultiplier", 15f, "The multiplier for experience gained from VBloods"),
            new ConfigEntryDefinition("Familiars", "UnitUnlockChance", 0.05f, "The chance for a unit to unlock a familiar"),
            new ConfigEntryDefinition("Familiars", "VBloodUnlockChance", 0.01f, "The chance for a VBlood to unlock a familiar"),
            new ConfigEntryDefinition("Familiars", "ShinyChance", 0.2f, "The chance for a visual when unlocking a familiar"),
            new ConfigEntryDefinition("Familiars", "ShinyCostItemPrefab", -77477508, "Item PrefabGUID cost for changing shiny visual if one is already unlocked (currently demon fragment by default)"),
            new ConfigEntryDefinition("Familiars", "ShinyCostItemQuantity", 1, "Quantity of item required for changing shiny visual"),
            new ConfigEntryDefinition("Classes", "SoftSynergies", false, "Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard syergies at the same time)"),
            new ConfigEntryDefinition("Classes", "HardSynergies", false, "Enforce class synergies (turns on classes and restricts stat choices, do not use this and soft syergies at the same time)"),
            new ConfigEntryDefinition("Classes", "ChangeClassItem", 576389135, "Item PrefabGUID cost for changing class."),
            new ConfigEntryDefinition("Classes", "ChangeClassQuantity", 750, "Quantity of item required for changing class."),
            new ConfigEntryDefinition("Classes", "ClassSpellSchoolOnHitEffects", false, "Enable or disable class spell school on hit effects"),
            new ConfigEntryDefinition("Classes", "OnHitProcChance", 0.075f, "The chance for a class effect to proc on hit"),
            new ConfigEntryDefinition("Classes", "StatSynergyMultiplier", 1.5f, "Multiplier for class stat synergies to base stat cap"),
            new ConfigEntryDefinition("Classes", "BloodKnightWeapon", "0,3,5,6", "Blood Knight weapon synergies"),
            new ConfigEntryDefinition("Classes", "BloodKnightBlood", "1,5,7,10", "Blood Knight blood synergies"),
            new ConfigEntryDefinition("Classes", "DemonHunterWeapon", "1,2,8,9", "Demon Hunter weapon synergies"),
            new ConfigEntryDefinition("Classes", "DemonHunterBlood", "2,5,7,9", "Demon Hunter blood synergies"),
            new ConfigEntryDefinition("Classes", "VampireLordWeapon", "0,4,6,7", "Vampire Lord weapon synergies"),
            new ConfigEntryDefinition("Classes", "VampireLordBlood", "1,3,8,11", "Vampire Lord blood synergies"),
            new ConfigEntryDefinition("Classes", "ShadowBladeWeapon", "1,2,6,9", "Shadow Blade weapon synergies"),
            new ConfigEntryDefinition("Classes", "ShadowBladeBlood", "3,5,7,10", "Shadow Blade blood synergies"),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererWeapon", "4,7,10,11", "Arcane Sorcerer weapon synergies"),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererBlood", "0,6,8,10", "Arcane Sorcerer blood synergies"),
            new ConfigEntryDefinition("Classes", "DeathMageWeapon", "0,4,7,11", "Death Mage weapon synergies"),
            new ConfigEntryDefinition("Classes", "DeathMageBlood", "2,3,6,9", "Death Mage blood synergies"),
            new ConfigEntryDefinition("Classes", "DefaultClassSpell", -433204738, "Default spell (veil of shadow) available to all classes."),
            new ConfigEntryDefinition("Classes", "BloodKnightBuffs", "1828387635,-534491790,-1055766373,-584203677", "The PrefabGUID hashes for blood knight leveling blood buffs. Granted every MaxLevel/(# of blood buffs)"),
            new ConfigEntryDefinition("Classes", "BloodKnightSpells", "-880131926,651613264,2067760264,189403977,375131842", "Blood Knight shift spells, granted at levels of prestige"),
            new ConfigEntryDefinition("Classes", "DemonHunterBuffs", "-154702686,-285745649,-1510965956,-397097531", "The PrefabGUID hashes for demon hunter leveling blood buffs"),
            new ConfigEntryDefinition("Classes", "DemonHunterSpells", "-356990326,-987810170,1071205195,1249925269,-914344112", "Demon Hunter shift spells, granted at levels of prestige"),
            new ConfigEntryDefinition("Classes", "VampireLordBuffs", "1558171501,997154800,-1413561088,1103099361", "The PrefabGUID hashes for vampire lord leveling blood buffs"),
            new ConfigEntryDefinition("Classes", "VampireLordSpells", "78384915,295045820,-1000260252,91249849,1966330719", "Vampire Lord shift spells, granted at levels of prestige"),
            new ConfigEntryDefinition("Classes", "ShadowBladeBuffs", "894725875,-1596803256,-993492354,210193036", "The PrefabGUID hashes for shadow blade leveling blood buffs"),
            new ConfigEntryDefinition("Classes", "ShadowBladeSpells", "1019568127,1575317901,1112116762,-358319417,1174831223", "Shadow Blade shift spells, granted at levels of prestige"),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererBuffs", "1614027598,884683323,-1576592687,-1859298707", "The PrefabGUID hashes for arcane leveling blood buffs"),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererSpells", "247896794,268059675,-242769430,-2053450457,1650878435", "Arcane Sorcerer shift spells, granted at levels of prestige"),
            new ConfigEntryDefinition("Classes", "DeathMageBuffs", "-901503997,-804597757,1934870645,1201299233", "The PrefabGUID hashes for death mage leveling blood buffs"),
            new ConfigEntryDefinition("Classes", "DeathMageSpells", "-1204819086,481411985,1961570821,2138402840,-1781779733", "Death Mage shift spells, granted at levels of prestige")
        ];
        public static void InitializeConfig()
        {
            foreach (string path in DirectoryPaths)
            {
                CreateDirectory(path);
            }

            // Load old config file if it exists
            var oldConfigFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
            Dictionary<string, string> oldConfigValues = [];

            if (File.Exists(oldConfigFile))
            {
                string[] oldConfigLines = File.ReadAllLines(oldConfigFile);
                foreach (var line in oldConfigLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var configKey = keyValue[0].Trim();
                        var configValue = keyValue[1].Trim();
                        oldConfigValues[configKey] = configValue;
                    }
                }
            }

            foreach (ConfigEntryDefinition entry in ConfigEntries)
            {
                // Get the type of DefaultValue
                Type entryType = entry.DefaultValue.GetType();

                // Reflect on the nested ConfigInitialization class within ConfigService
                Type nestedClassType = typeof(ConfigService).GetNestedType("ConfigInitialization", BindingFlags.Static | BindingFlags.Public);

                // Use reflection to call InitConfigEntry with the appropriate type
                MethodInfo method = nestedClassType.GetMethod("InitConfigEntry", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo generic = method.MakeGenericMethod(entryType);

                // Check if the old config has the key
                if (oldConfigValues.TryGetValue(entry.Key, out var oldValue))
                {
                    // Convert the old value to the correct type
                    try
                    {
                        var convertedValue = Convert.ChangeType(oldValue, entryType);
                        var configEntry = generic.Invoke(null, [entry.Section, entry.Key, convertedValue, entry.Description]);
                        UpdateConfigProperty(entry.Key, configEntry);
                        //Plugin.LogInstance.LogInfo($"Migrated key {entry.Key} from old config to section {entry.Section} with value {convertedValue}");
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogInstance.LogError($"Failed to convert old config value for {entry.Key}: {ex.Message}");
                    }
                }
                else
                {
                    // Use default value if key is not in the old config
                    var configEntry = generic.Invoke(null, [entry.Section, entry.Key, entry.DefaultValue, entry.Description]);

                    UpdateConfigProperty(entry.Key, configEntry);
                }
                /*
                // Invoke the generic method
                var configEntry = generic.Invoke(null, [entry.Section, entry.Key, entry.DefaultValue, entry.Description]);

                PropertyInfo propertyInfo = typeof(ConfigService).GetProperty(entry.Key, BindingFlags.Static | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    object value = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);

                    if (value != null)
                    {
                        propertyInfo.SetValue(null, Convert.ChangeType(value, propertyInfo.PropertyType));
                    }
                    else
                    {
                        throw new Exception($"Value property on configEntry is null for section {entry.Section} and key {entry.Key}.");
                    }
                }
                */
            }

            var configFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
            if (File.Exists(configFile)) CleanAndOrganizeConfig(configFile);
        }
        static void UpdateConfigProperty(string key, object configEntry)
        {
            PropertyInfo propertyInfo = typeof(ConfigService).GetProperty(key, BindingFlags.Static | BindingFlags.Public);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                object value = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);

                if (value != null)
                {
                    propertyInfo.SetValue(null, Convert.ChangeType(value, propertyInfo.PropertyType));
                }
                else
                {
                    throw new Exception($"Value property on configEntry is null for key {key}.");
                }
            }
        }
        static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
        {
            // Bind the configuration entry with the default value in the new section
            var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

            // Define the path to the configuration file
            var configFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

            // Ensure the configuration file is only loaded if it exists
            if (File.Exists(configFile))
            {
                string[] configLines = File.ReadAllLines(configFile);
                //Plugin.LogInstance.LogInfo(configLines);
                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var configKey = keyValue[0].Trim();
                        var configValue = keyValue[1].Trim();

                        // Check if the key matches the provided key
                        if (configKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            // Try to convert the string value to the expected type
                            try
                            {
                                var convertedValue = (T)Convert.ChangeType(configValue, typeof(T));
                                entry.Value = convertedValue;
                                //Plugin.LogInstance.LogInfo(line);
                                //Plugin.LogInstance.LogInfo($"Loaded existing config entry: {section} - {key} = {entry.Value} | {configKey}:{configValue}");
                            }
                            catch (Exception ex)
                            {
                                Plugin.LogInstance.LogError($"Failed to convert config value for {key}: {ex.Message}");
                            }
                            break; // Stop searching once the key is found
                        }
                    }
                }
            }
            return entry;
        }
        static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        static void CleanAndOrganizeConfig(string configFile)
        {
            Dictionary<string, List<string>> OrderedSections = [];
            string currentSection = "";

            string[] lines = File.ReadAllLines(configFile);
            string[] fileHeader = lines[0..3];

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                var match = regex.Match(trimmedLine);

                if (match.Success)
                {
                    currentSection = match.Groups[1].Value;
                    if (!OrderedSections.ContainsKey(currentSection))
                    {
                        OrderedSections[currentSection] = [];
                    }
                }
                else if (SectionOrder.Contains(currentSection))
                {
                    OrderedSections[currentSection].Add(trimmedLine);
                }
            }

            using StreamWriter writer = new(configFile, false);

            foreach (var header in fileHeader)
            {
                writer.WriteLine(header);
            }

            foreach (var section in SectionOrder)
            {
                if (OrderedSections.ContainsKey(section))
                {
                    writer.WriteLine($"[{section}]");
                    foreach (var line in OrderedSections[section])
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
