using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using VampireCommandFramework;

namespace Bloodcraft;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    public static readonly string ConfigFiles = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME); // Bloodcraft folder

    // current paths
    public static readonly string PlayerLevelingPath = Path.Combine(ConfigFiles, "PlayerLeveling");
    public static readonly string PlayerQuestsPath = Path.Combine(ConfigFiles, "Quests");
    public static readonly string PlayerExpertisePath = Path.Combine(ConfigFiles, "WeaponExpertise");
    public static readonly string PlayerBloodPath = Path.Combine(ConfigFiles, "BloodLegacies");
    public static readonly string PlayerProfessionPath = Path.Combine(ConfigFiles, "Professions");
    public static readonly string PlayerFamiliarsPath = Path.Combine(ConfigFiles, "Familiars");
    public static readonly string FamiliarExperiencePath = Path.Combine(PlayerFamiliarsPath, "FamiliarLeveling");
    public static readonly string FamiliarUnlocksPath = Path.Combine(PlayerFamiliarsPath, "FamiliarUnlocks");

    // config entries
    private static ConfigEntry<string> _languageLocalization;
    private static ConfigEntry<bool> _questSystem;
    private static ConfigEntry<string> _questRewards;
    private static ConfigEntry<string> _questRewardAmounts;
    private static ConfigEntry<bool> _levelingSystem;
    private static ConfigEntry<bool> _prestigeSystem;
    private static ConfigEntry<bool> _exoPrestiging;
    private static ConfigEntry<int> _exoPrestiges;
    private static ConfigEntry<int> _exoPrestigeReward;
    private static ConfigEntry<int> _exoPrestigeRewardQuantity;
    private static ConfigEntry<float> _exoPrestigeDamageTakenMultiplier;
    private static ConfigEntry<float> _exoPrestigeDamageDealtMultiplier;
    private static ConfigEntry<string> _prestigeBuffs;
    private static ConfigEntry<string> _prestigeLevelsToUnlockClassSpells;
    private static ConfigEntry<string> _bloodKnightBuffs;
    private static ConfigEntry<string> _demonHunterBuffs;
    private static ConfigEntry<string> _vampireLordBuffs;
    private static ConfigEntry<string> _shadowBladeBuffs;
    private static ConfigEntry<string> _arcaneSorcererBuffs;
    private static ConfigEntry<string> _deathMageBuffs;
    private static ConfigEntry<int> _maxLevelingPrestiges;
    private static ConfigEntry<float> _levelingPrestigeReducer; // separate factor for reducing experience gain in leveling per level of leveling prestige
    private static ConfigEntry<float> _prestigeRatesReducer; //reduces gains by this percent per level of prestige for expertise/legacy, they get raised by prestiging in leveling
    private static ConfigEntry<float> _prestigeStatMultiplier; //increases stats gained from expertise/legacies per level of prestige
    private static ConfigEntry<float> _prestigeRatesMultiplier; //increases player gains in expertise/legacy by this percent per level of prestige (leveling prestige)
    private static ConfigEntry<int> _maxPlayerLevel;
    private static ConfigEntry<int> _startingLevel;
    private static ConfigEntry<float> _unitLevelingMultiplier;
    private static ConfigEntry<float> _vBloodLevelingMultiplier;
    private static ConfigEntry<float> _groupLevelingMultiplier;
    private static ConfigEntry<float> _levelScalingMultiplier;
    private static ConfigEntry<float> _docileUnitMultiplier;
    private static ConfigEntry<float> _warEventMultiplier;
    private static ConfigEntry<float> _unitSpawnerMultiplier;
    private static ConfigEntry<bool> _parties;
    private static ConfigEntry<int> _maxPartySize;
    private static ConfigEntry<bool> _preventFriendlyFire;
    private static ConfigEntry<float> _expShareDistance;
    private static ConfigEntry<int> _changeClassItem;
    private static ConfigEntry<int> _changeClassItemQuantity;

    private static ConfigEntry<bool> _expertiseSystem;
    private static ConfigEntry<int> _maxExpertisePrestiges;
    private static ConfigEntry<bool> _unarmedSlots;
    private static ConfigEntry<bool> _shiftSlots;
    private static ConfigEntry<int> _maxExpertiseLevel;
    private static ConfigEntry<float> _unitExpertiseMultiplier;
    private static ConfigEntry<float> _vBloodExpertiseMultiplier;
    private static ConfigEntry<int> _expertiseStatChoices;
    private static ConfigEntry<int> _resetExpertiseItem;
    private static ConfigEntry<int> _resetExpertiseItemQuantity;

    private static ConfigEntry<float> _maxHealth;
    private static ConfigEntry<float> _movementSpeed;
    private static ConfigEntry<float> _primaryAttackSpeed;
    private static ConfigEntry<float> _physicalLifeLeech;
    private static ConfigEntry<float> _spellLifeLeech;
    private static ConfigEntry<float> _primaryLifeLeech;
    private static ConfigEntry<float> _physicalPower;
    private static ConfigEntry<float> _spellPower;
    private static ConfigEntry<float> _physicalCritChance;
    private static ConfigEntry<float> _physicalCritDamage;
    private static ConfigEntry<float> _spellCritChance;
    private static ConfigEntry<float> _spellCritDamage;

    private static ConfigEntry<bool> _bloodSystem;
    private static ConfigEntry<int> _maxLegacyPrestiges;
    private static ConfigEntry<bool> _bloodQualityBonus;
    private static ConfigEntry<float> _prestigeBloodQuality;
    private static ConfigEntry<int> _maxBloodLevel;
    private static ConfigEntry<float> _unitLegacyMultiplier;
    private static ConfigEntry<float> _vBloodLegacyMultipler;
    private static ConfigEntry<int> _legacyStatChoices;
    private static ConfigEntry<int> _resetLegacyItem;
    private static ConfigEntry<int> _resetLegacyItemQuantity;

    private static ConfigEntry<float> _healingReceived;
    private static ConfigEntry<float> _damageReduction;
    private static ConfigEntry<float> _physicalResistance;
    private static ConfigEntry<float> _spellResistance;
    private static ConfigEntry<float> _resourceYield;
    private static ConfigEntry<float> _ccReduction;
    private static ConfigEntry<float> _spellCooldownRecoveryRate;
    private static ConfigEntry<float> _weaponCooldownRecoveryRate;
    private static ConfigEntry<float> _ultimateCooldownRecoveryRate;
    private static ConfigEntry<float> _minionDamage;
    private static ConfigEntry<float> _shieldAbsorb;
    private static ConfigEntry<float> _bloodEfficiency;

    private static ConfigEntry<bool> _professionSystem;
    private static ConfigEntry<int> _maxProfessionLevel;
    private static ConfigEntry<float> _professionMultiplier;
    private static ConfigEntry<bool> _extraRecipes;

    private static ConfigEntry<bool> _familiarSystem;
    private static ConfigEntry<bool> _shareUnlocks;
    private static ConfigEntry<bool> _familiarCombat;
    private static ConfigEntry<int> _maxFamiliarLevel;
    private static ConfigEntry<bool> _familiarPrestige;
    private static ConfigEntry<int> _maxFamiliarPrestiges;
    private static ConfigEntry<float> _familiarPrestigeStatMultiplier;
    private static ConfigEntry<bool> _allowVBloods;
    private static ConfigEntry<string> _bannedUnits;
    private static ConfigEntry<string> _bannedTypes;
    private static ConfigEntry<float> _vBloodDamageMultiplier;
    private static ConfigEntry<float> _playerVampireDamageMultiplier;
    private static ConfigEntry<float> _unitFamiliarMultiplier;
    private static ConfigEntry<float> _vBloodFamiliarMultiplier;
    private static ConfigEntry<float> _unitUnlockChance;
    private static ConfigEntry<float> _vBloodUnlockChance;

    private static ConfigEntry<bool> _softSynergies; // allow synergies (turns on class multipliers but doesn't restrict choices)
    private static ConfigEntry<bool> _hardSynergies; // enforce synergies (turns on class multipliers and restricts choices)
    private static ConfigEntry<bool> _classSpellSchoolOnHitEffects;
    private static ConfigEntry<float> _onHitProcChance;
    private static ConfigEntry<float> _statSyngergyMultiplier;
    private static ConfigEntry<string> _bloodKnightWeapon;
    private static ConfigEntry<string> _bloodKnightBlood;
    private static ConfigEntry<string> _bloodKnightSpells;
    private static ConfigEntry<string> _demonHunterWeapon;
    private static ConfigEntry<string> _demonHunterBlood;
    private static ConfigEntry<string> _demonHunterSpells;
    private static ConfigEntry<string> _vampireLordWeapon;
    private static ConfigEntry<string> _vampireLordBlood;
    private static ConfigEntry<string> _vampireLordSpells;
    private static ConfigEntry<string> _shadowBladeWeapon;
    private static ConfigEntry<string> _shadowBladeBlood;
    private static ConfigEntry<string> _shadowBladeSpells;
    private static ConfigEntry<string> _arcaneSorcererWeapon;
    private static ConfigEntry<string> _arcaneSorcererBlood;
    private static ConfigEntry<string> _arcaneSorcererSpells;
    private static ConfigEntry<string> _deathMageWeapon;
    private static ConfigEntry<string> _deathMageBlood;
    private static ConfigEntry<string> _deathMageSpells;

    // public getters, kinda verbose might just get rid of these
    public static ConfigEntry<string> LanguageLocalization => _languageLocalization;
    public static ConfigEntry<bool> QuestSystem => _questSystem;
    public static ConfigEntry<string> QuestRewards => _questRewards;
    public static ConfigEntry<string> QuestRewardAmounts => _questRewardAmounts;
    public static ConfigEntry<bool> LevelingSystem => _levelingSystem;
    public static ConfigEntry<bool> PrestigeSystem => _prestigeSystem;
    public static ConfigEntry<bool> ExoPrestiging => _exoPrestiging;
    public static ConfigEntry<int> ExoPrestiges => _exoPrestiges;
    public static ConfigEntry<int> ExoPrestigeReward => _exoPrestigeReward;
    public static ConfigEntry<int> ExoPrestigeRewardQuantity => _exoPrestigeRewardQuantity;
    public static ConfigEntry<float> ExoPrestigeDamageTakenMultiplier => _exoPrestigeDamageTakenMultiplier;
    public static ConfigEntry<float> ExoPrestigeDamageDealtMultiplier => _exoPrestigeDamageDealtMultiplier;
    public static ConfigEntry<string> PrestigeBuffs => _prestigeBuffs;
    public static ConfigEntry<string> PrestigeLevelsToUnlockClassSpells => _prestigeLevelsToUnlockClassSpells;
    public static ConfigEntry<string> BloodKnightBuffs => _bloodKnightBuffs;
    public static ConfigEntry<string> DemonHunterBuffs => _demonHunterBuffs;
    public static ConfigEntry<string> VampireLordBuffs => _vampireLordBuffs;
    public static ConfigEntry<string> ShadowBladeBuffs => _shadowBladeBuffs;
    public static ConfigEntry<string> ArcaneSorcererBuffs => _arcaneSorcererBuffs;
    public static ConfigEntry<string> DeathMageBuffs => _deathMageBuffs;
    public static ConfigEntry<int> MaxLevelingPrestiges => _maxLevelingPrestiges;
    public static ConfigEntry<float> LevelingPrestigeReducer => _levelingPrestigeReducer;
    public static ConfigEntry<float> PrestigeRatesReducer => _prestigeRatesReducer;
    public static ConfigEntry<float> PrestigeStatMultiplier => _prestigeStatMultiplier;
    public static ConfigEntry<float> PrestigeRatesMultiplier => _prestigeRatesMultiplier;
    public static ConfigEntry<int> MaxPlayerLevel => _maxPlayerLevel;
    public static ConfigEntry<int> StartingLevel => _startingLevel;
    public static ConfigEntry<float> UnitLevelingMultiplier => _unitLevelingMultiplier;
    public static ConfigEntry<float> VBloodLevelingMultiplier => _vBloodLevelingMultiplier;
    public static ConfigEntry<float> GroupLevelingMultiplier => _groupLevelingMultiplier;
    public static ConfigEntry<float> LevelScalingMultiplier => _levelScalingMultiplier;
    public static ConfigEntry<int> MaxPartySize => _maxPartySize;
    public static ConfigEntry<float> ExpShareDistance => _expShareDistance;
    public static ConfigEntry<bool> Parties => _parties;
    public static ConfigEntry<bool> PreventFriendlyFire => _preventFriendlyFire;
    public static ConfigEntry<float> DocileUnitMultiplier => _docileUnitMultiplier;
    public static ConfigEntry<float> WarEventMultiplier => _warEventMultiplier;
    public static ConfigEntry<float> UnitSpawnerMultiplier => _unitSpawnerMultiplier;
    public static ConfigEntry<int> ChangeClassItem => _changeClassItem;
    public static ConfigEntry<int> ChangeClassItemQuantity => _changeClassItemQuantity;
    public static ConfigEntry<bool> ExpertiseSystem => _expertiseSystem;
    public static ConfigEntry<int> MaxExpertisePrestiges => _maxExpertisePrestiges;
    public static ConfigEntry<bool> UnarmedSlots => _unarmedSlots;
    public static ConfigEntry<bool> ShiftSlot => _shiftSlots;
    public static ConfigEntry<int> MaxExpertiseLevel => _maxExpertiseLevel;
    public static ConfigEntry<float> UnitExpertiseMultiplier => _unitExpertiseMultiplier;
    public static ConfigEntry<float> VBloodExpertiseMultiplier => _vBloodExpertiseMultiplier;
    public static ConfigEntry<int> ExpertiseStatChoices => _expertiseStatChoices;
    public static ConfigEntry<int> ResetExpertiseItem => _resetExpertiseItem;
    public static ConfigEntry<int> ResetExpertiseItemQuantity => _resetExpertiseItemQuantity;
    public static ConfigEntry<float> MaxHealth => _maxHealth;
    public static ConfigEntry<float> MovementSpeed => _movementSpeed;
    public static ConfigEntry<float> PrimaryAttackSpeed => _primaryAttackSpeed;
    public static ConfigEntry<float> PhysicalLifeLeech => _physicalLifeLeech;
    public static ConfigEntry<float> SpellLifeLeech => _spellLifeLeech;
    public static ConfigEntry<float> PrimaryLifeLeech => _primaryLifeLeech;
    public static ConfigEntry<float> PhysicalPower => _physicalPower;
    public static ConfigEntry<float> SpellPower => _spellPower;
    public static ConfigEntry<float> PhysicalCritChance => _physicalCritChance;
    public static ConfigEntry<float> PhysicalCritDamage => _physicalCritDamage;
    public static ConfigEntry<float> SpellCritChance => _spellCritChance;
    public static ConfigEntry<float> SpellCritDamage => _spellCritDamage;
    public static ConfigEntry<bool> BloodSystem => _bloodSystem;
    public static ConfigEntry<int> MaxLegacyPrestiges => _maxLegacyPrestiges;
    public static ConfigEntry<bool> BloodQualityBonus => _bloodQualityBonus;
    public static ConfigEntry<float> PrestigeBloodQuality => _prestigeBloodQuality;
    public static ConfigEntry<int> MaxBloodLevel => _maxBloodLevel;
    public static ConfigEntry<float> UnitLegacyMultiplier => _unitLegacyMultiplier;
    public static ConfigEntry<float> VBloodLegacyMultipler => _vBloodLegacyMultipler;
    public static ConfigEntry<int> LegacyStatChoices => _legacyStatChoices;
    public static ConfigEntry<int> ResetLegacyItem => _resetLegacyItem;
    public static ConfigEntry<int> ResetLegacyItemQuantity => _resetLegacyItemQuantity;
    public static ConfigEntry<float> HealingReceived => _healingReceived;
    public static ConfigEntry<float> DamageReduction => _damageReduction;
    public static ConfigEntry<float> PhysicalResistance => _physicalResistance;
    public static ConfigEntry<float> SpellResistance => _spellResistance;
    public static ConfigEntry<float> ResourceYield => _resourceYield;
    public static ConfigEntry<float> CCReduction => _ccReduction;
    public static ConfigEntry<float> SpellCooldownRecoveryRate => _spellCooldownRecoveryRate;
    public static ConfigEntry<float> WeaponCooldownRecoveryRate => _weaponCooldownRecoveryRate;
    public static ConfigEntry<float> UltimateCooldownRecoveryRate => _ultimateCooldownRecoveryRate;
    public static ConfigEntry<float> MinionDamage => _minionDamage;
    public static ConfigEntry<float> ShieldAbsorb => _shieldAbsorb;
    public static ConfigEntry<float> BloodEfficiency => _bloodEfficiency;
    public static ConfigEntry<bool> ProfessionSystem => _professionSystem;
    public static ConfigEntry<int> MaxProfessionLevel => _maxProfessionLevel;
    public static ConfigEntry<float> ProfessionMultiplier => _professionMultiplier;
    public static ConfigEntry<bool> ExtraRecipes => _extraRecipes;
    public static ConfigEntry<bool> FamiliarSystem => _familiarSystem;
    public static ConfigEntry<bool> ShareUnlocks => _shareUnlocks;
    public static ConfigEntry<bool> FamiliarCombat => _familiarCombat;
    public static ConfigEntry<bool> FamiliarPrestige => _familiarPrestige;
    public static ConfigEntry<int> MaxFamiliarPrestiges => _maxFamiliarPrestiges;
    public static ConfigEntry<float> FamiliarPrestigeStatMultiplier => _familiarPrestigeStatMultiplier;
    public static ConfigEntry<int> MaxFamiliarLevel => _maxFamiliarLevel;
    public static ConfigEntry<bool> AllowVBloods => _allowVBloods;
    public static ConfigEntry<string> BannedUnits => _bannedUnits;
    public static ConfigEntry<string> BannedTypes => _bannedTypes;
    public static ConfigEntry<float> UnitFamiliarMultiplier => _unitFamiliarMultiplier;
    public static ConfigEntry<float> VBloodFamiliarMultiplier => _vBloodFamiliarMultiplier;
    public static ConfigEntry<float> UnitUnlockChance => _unitUnlockChance;
    public static ConfigEntry<float> VBloodUnlockChance => _vBloodUnlockChance;
    public static ConfigEntry<float> VBloodDamageMultiplier => _vBloodDamageMultiplier;
    public static ConfigEntry<float> PlayerVampireDamageMultiplier => _playerVampireDamageMultiplier;
    public static ConfigEntry<bool> SoftSynergies => _softSynergies;
    public static ConfigEntry<bool> HardSynergies => _hardSynergies;
    public static ConfigEntry<bool> ClassSpellSchoolOnHitEffects => _classSpellSchoolOnHitEffects;
    public static ConfigEntry<float> OnHitProcChance => _onHitProcChance;
    public static ConfigEntry<float> StatSynergyMultiplier => _statSyngergyMultiplier;
    public static ConfigEntry<string> BloodKnightWeapon => _bloodKnightWeapon;
    public static ConfigEntry<string> BloodKnightBlood => _bloodKnightBlood;
    public static ConfigEntry<string> BloodKnightSpells => _bloodKnightSpells;
    public static ConfigEntry<string> DemonHunterWeapon => _demonHunterWeapon;
    public static ConfigEntry<string> DemonHunterBlood => _demonHunterBlood;
    public static ConfigEntry<string> DemonHunterSpells => _demonHunterSpells;
    public static ConfigEntry<string> VampireLordWeapon => _vampireLordWeapon;
    public static ConfigEntry<string> VampireLordBlood => _vampireLordBlood;
    public static ConfigEntry<string> VampireLordSpells => _vampireLordSpells;
    public static ConfigEntry<string> ShadowBladeWeapon => _shadowBladeWeapon;
    public static ConfigEntry<string> ShadowBladeBlood => _shadowBladeBlood;
    public static ConfigEntry<string> ShadowBladeSpells => _shadowBladeSpells;
    public static ConfigEntry<string> ArcaneSorcererWeapon => _arcaneSorcererWeapon;
    public static ConfigEntry<string> ArcaneSorcererBlood => _arcaneSorcererBlood;
    public static ConfigEntry<string> ArcaneSorcererSpells => _arcaneSorcererSpells;
    public static ConfigEntry<string> DeathMageWeapon => _deathMageWeapon;
    public static ConfigEntry<string> DeathMageBlood => _deathMageBlood;
    public static ConfigEntry<string> DeathMageSpells => _deathMageSpells;

    public override void Load()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        CommandRegistry.RegisterAll();
        LoadAllData();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }

    /*
    static void MigrateData()
    {
        try
        {
            if (File.Exists(OldConfigFile) && !File.Exists(NewConfigFile)) // migrate old config data
            {
                File.Copy(OldConfigFile, NewConfigFile);
                Core.Log.LogInfo($"Migrated {OldConfigFile} => {NewConfigFile}");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo($"Failed to migrate old config: {ex}");
        }

        try
        {
            if (Directory.Exists(OldPlayerExperiencePath) && Directory.Exists(PlayerLevelingPath)) // migrate old exp data if path exists
            {
                // Move contents from the old path to the new path
                Directory.Move(OldPlayerExperiencePath, PlayerLevelingPath);
                Core.Log.LogInfo($"Migrated {OldPlayerExperiencePath} => {PlayerLevelingPath}");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo($"Failed to migrate old player experience data: {ex}");
        }
    }
    */
    static void InitConfig()
    {
        foreach (string path in directoryPaths) // make sure directories exist
        {
            CreateDirectories(path);
        }

        _languageLocalization = InitConfigEntry("Config", "LanguageLocalization", "English", "The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese");

        _questSystem = InitConfigEntry("Config", "QuestSystem", false, "Enable or disable quests (currently only kill quests).");
        _questRewards = InitConfigEntry("Config", "QuestRewards", "28358550,576389135,-257494203", "The PrefabGUID hashes for quest reward pool.");
        _questRewardAmounts = InitConfigEntry("Config", "QuestRewardAmounts", "50,250,50", "The amount of each reward in the pool. Will be multiplied accordingly for weeklies (*5) and vblood kill quests have a *3 multiplier.");
        
        _levelingSystem = InitConfigEntry("Config", "LevelingSystem", false, "Enable or disable the leveling system.");
        _maxPlayerLevel = InitConfigEntry("Config", "MaxLevel", 90, "The maximum level a player can reach.");
        _startingLevel = InitConfigEntry("Config", "StartingLevel", 0, "Starting level for players if no data is found.");
        _unitLevelingMultiplier = InitConfigEntry("Config", "UnitLevelingMultiplier", 7.5f, "The multiplier for experience gained from units.");
        _vBloodLevelingMultiplier = InitConfigEntry("Config", "VBloodLevelingMultiplier", 15f, "The multiplier for experience gained from VBloods.");
        _docileUnitMultiplier = InitConfigEntry("Config", "DocileUnitMultiplier", 0.15f, "The multiplier for experience gained from docile units.");
        _warEventMultiplier = InitConfigEntry("Config", "WarEventMultiplier", 0.2f, "The multiplier for experience gained from war event trash spawns.");
        _unitSpawnerMultiplier = InitConfigEntry("Config", "UnitSpawnerMultiplier", 0f, "The multiplier for experience gained from unit spawners (vermin nests, tombs).");
        _changeClassItem = InitConfigEntry("Config", "ChangeClassItem", 576389135, "Item PrefabGUID cost for changing class.");
        _changeClassItemQuantity = InitConfigEntry("Config", "ChangeClassQuantity", 750, "Quantity of item required for changing class.");
        _groupLevelingMultiplier = InitConfigEntry("Config", "GroupLevelingMultiplier", 1f, "The multiplier for experience gained from group kills.");
        _levelScalingMultiplier = InitConfigEntry("Config", "LevelScalingMultiplier", 0.05f, "reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove.");
        _parties = InitConfigEntry("Config", "PlayerParties", false, "Enable or disable the ability to group with players not in your clan for experience sharing.");
        _preventFriendlyFire = InitConfigEntry("Config", "PreventFriendlyFire", false, "True to prevent damage between players in parties, false to allow. (damage only at the moment)");
        _maxPartySize = InitConfigEntry("Config", "MaxPartySize", 5, "The maximum number of players that can share experience in a group.");
        _expShareDistance = InitConfigEntry("Config", "ExpShareDistance", 25f, "Default is about 5 floor tile lengths.");

        _prestigeSystem = InitConfigEntry("Config", "PrestigeSystem", false, "Enable or disable the prestige system.");
        _prestigeBuffs = InitConfigEntry("Config", "PrestigeBuffs", "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884", "The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level.");
        _prestigeLevelsToUnlockClassSpells = InitConfigEntry("Config", "PrestigeLevelsToUnlockClassSpells", "0,1,2,3", "The prestige levels at which class spells are unlocked. This should match the number of spells per class. Can leave at 0 if you want them unlocked from the start.");
        _maxLevelingPrestiges = InitConfigEntry("Config", "MaxLevelingPrestiges", 10, "The maximum number of prestiges a player can reach in leveling.");
        _levelingPrestigeReducer = InitConfigEntry("Config", "LevelingPrestigeReducer", 0.05f, "Flat factor by which experience is reduced per increment of prestige in leveling.");
        _prestigeRatesReducer = InitConfigEntry("Config", "PrestigeRatesReducer", 0.10f, "Flat factor by which rates are reduced in expertise/legacy per increment of prestige in expertise/legacy.");
        _prestigeStatMultiplier = InitConfigEntry("Config", "PrestigeStatMultiplier", 0.10f, "Flat factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy.");
        _prestigeRatesMultiplier = InitConfigEntry("Config", "PrestigeRateMultiplier", 0.10f, "Flat factor by which rates are increased in expertise/legacy per increment of prestige in leveling.");
        _exoPrestiging = InitConfigEntry("Config", "ExoPrestiging", false, "Enable or disable exo prestiges (need to max normal prestiges first).");
        _exoPrestiges = InitConfigEntry("Config", "ExoPrestiges", 100, "The number of exo prestiges available.");
        _exoPrestigeReward = InitConfigEntry("Config", "ExoPrestigeReward", 28358550, "The reward for exo prestiging (tier 3 nether shards by default).");
        _exoPrestigeRewardQuantity = InitConfigEntry("Config", "ExoPrestigeRewardQuantity", 500, "The quantity of the reward for exo prestiging.");
        _exoPrestigeDamageTakenMultiplier = InitConfigEntry("Config", "ExoPrestigeDamageMultiplier", 0.05f, "The damage multiplier per exo prestige (applies to damage taken by the player).");
        _exoPrestigeDamageDealtMultiplier = InitConfigEntry("Config", "ExoPrestigeDamageDealtMultiplier", 0.025f, "The damage multiplier per exo prestige (applies to damage dealt by the player).");

        _expertiseSystem = InitConfigEntry("Config", "ExpertiseSystem", false, "Enable or disable the expertise system.");
        _maxExpertisePrestiges = InitConfigEntry("Config", "MaxExpertisePrestiges", 10, "The maximum number of prestiges a player can reach in expertise.");
        _unarmedSlots = InitConfigEntry("Config", "UnarmedSlots", false, "Enable or disable the ability to use extra unarmed spell slots.");
        _shiftSlots = InitConfigEntry("Config", "ShiftSlot", false, "Enable or disable using class spell on shift.");
        _maxExpertiseLevel = InitConfigEntry("Config", "MaxExpertiseLevel", 100, "The maximum level a player can reach in weapon expertise.");
        _unitExpertiseMultiplier = InitConfigEntry("Config", "UnitExpertiseMultiplier", 2f, "The multiplier for expertise gained from units.");
        _vBloodExpertiseMultiplier = InitConfigEntry("Config", "VBloodExpertiseMultiplier", 5f, "The multiplier for expertise gained from VBloods.");
        _expertiseStatChoices = InitConfigEntry("Config", "ExpertiseStatChoices", 2, "The maximum number of stat choices a player can pick for a weapon expertise.");
        _resetExpertiseItem = InitConfigEntry("Config", "ResetExpertiseItem", 576389135, "Item PrefabGUID cost for resetting weapon stats.");
        _resetExpertiseItemQuantity = InitConfigEntry("Config", "ResetExpertiseItemQuantity", 500, "Quantity of item required for resetting stats.");

        _maxHealth = InitConfigEntry("Config", "MaxHealth", 250f, "The base cap for maximum health.");
        _movementSpeed = InitConfigEntry("Config", "MovementSpeed", 0.25f, "The base cap for movement speed.");
        _primaryAttackSpeed = InitConfigEntry("Config", "PrimaryAttackSpeed", 0.10f, "The base cap for primary attack speed.");
        _physicalLifeLeech = InitConfigEntry("Config", "PhysicalLifeLeech", 0.10f, "The base cap for physical life leech.");
        _spellLifeLeech = InitConfigEntry("Config", "SpellLifeLeech", 0.10f, "The base cap for spell life leech.");
        _primaryLifeLeech = InitConfigEntry("Config", "PrimaryLifeLeech", 0.15f, "The base cap for primary life leech.");
        _physicalPower = InitConfigEntry("Config", "PhysicalPower", 10f, "The base cap for physical power.");
        _spellPower = InitConfigEntry("Config", "SpellPower", 10f, "The base cap for spell power.");
        _physicalCritChance = InitConfigEntry("Config", "PhysicalCritChance", 0.10f, "The base cap for physical critical strike chance.");
        _physicalCritDamage = InitConfigEntry("Config", "PhysicalCritDamage", 0.50f, "The base cap for physical critical strike damage.");
        _spellCritChance = InitConfigEntry("Config", "SpellCritChance", 0.10f, "The base cap for spell critical strike chance.");
        _spellCritDamage = InitConfigEntry("Config", "SpellCritDamage", 0.50f, "The base cap for spell critical strike damage.");

        _bloodSystem = InitConfigEntry("Config", "BloodSystem", false, "Enable or disable the blood legacy system.");
        _maxLegacyPrestiges = InitConfigEntry("Config", "MaxLegacyPrestiges", 10, "The maximum number of prestiges a player can reach in blood legacies.");
        _bloodQualityBonus = InitConfigEntry("Config", "BloodQualityBonus", false, "Enable or disable blood quality bonus system (if using presige, legacy level will be used with _prestigeBloodQuality multiplier below).");
        _prestigeBloodQuality = InitConfigEntry("Config", "PrestigeBloodQuality", 5f, "Blood quality bonus per prestige legacy level.");
        _maxBloodLevel = InitConfigEntry("Config", "MaxBloodLevel", 100, "The maximum level a player can reach in blood legacies.");
        _unitLegacyMultiplier = InitConfigEntry("Config", "UnitLegacyMultiplier", 1f, "The multiplier for lineage gained from units.");
        _vBloodLegacyMultipler = InitConfigEntry("Config", "VBloodLegacyMultipler", 5f, "The multiplier for lineage gained from VBloods.");
        _legacyStatChoices = InitConfigEntry("Config", "LegacyStatChoices", 2, "The maximum number of stat choices a player can pick for a blood legacy.");
        _resetLegacyItem = InitConfigEntry("Config", "ResetLegacyItem", 576389135, "Item PrefabGUID cost for resetting blood stats.");
        _resetLegacyItemQuantity = InitConfigEntry("Config", "ResetLegacyItemQuantity", 500, "Quantity of item required for resetting blood stats.");

        _healingReceived = InitConfigEntry("Config", "HealingReceived", 0.15f, "The base cap for healing received.");
        _damageReduction = InitConfigEntry("Config", "DamageReduction", 0.05f, "The base cap for damage reduction.");
        _physicalResistance = InitConfigEntry("Config", "PhysicalResistance", 0.10f, "The base cap for physical resistance.");
        _spellResistance = InitConfigEntry("Config", "SpellResistance", 0.10f, "The base cap for spell resistance.");
        _resourceYield = InitConfigEntry("Config", "ResourceYield", 0.25f, "The base cap for resource yield.");
        _ccReduction = InitConfigEntry("Config", "CCReduction", 0.20f, "The base cap for crowd control reduction.");
        _spellCooldownRecoveryRate = InitConfigEntry("Config", "SpellCooldownRecoveryRate", 0.10f, "The base cap for spell cooldown recovery rate.");
        _weaponCooldownRecoveryRate = InitConfigEntry("Config", "WeaponCooldownRecoveryRate", 0.10f, "The base cap for weapon cooldown recovery rate.");
        _ultimateCooldownRecoveryRate = InitConfigEntry("Config", "UltimateCooldownRecoveryRate", 0.20f, "The base cap for ultimate cooldown recovery rate.");
        _minionDamage = InitConfigEntry("Config", "MinionDamage", 0.25f, "The base cap for minion damage.");
        _shieldAbsorb = InitConfigEntry("Config", "ShieldAbsorb", 0.50f, "The base cap for shield absorb.");
        _bloodEfficiency = InitConfigEntry("Config", "BloodEfficiency", 0.10f, "The base cap for blood efficiency.");

        _professionSystem = InitConfigEntry("Config", "ProfessionSystem", false, "Enable or disable the profession system.");
        _maxProfessionLevel = InitConfigEntry("Config", "MaxProfessionLevel", 100, "The maximum level a player can reach in professions.");
        _professionMultiplier = InitConfigEntry("Config", "ProfessionMultiplier", 10f, "The multiplier for profession experience gained.");
        _extraRecipes = InitConfigEntry("Config", "ExtraRecipes", false, "Enable or disable extra recipes.");

        _familiarSystem = InitConfigEntry("Config", "FamiliarSystem", false, "Enable or disable the familiar system.");
        _shareUnlocks = InitConfigEntry("Config", "ShareUnlocks", false, "Enable or disable sharing unlocks between players in clans or parties (uses exp share distance).");
        _familiarCombat = InitConfigEntry("Config", "FamiliarCombat", true, "Enable or disable combat for familiars.");
        _familiarPrestige = InitConfigEntry("Config", "FamiliarPrestige", false, "Enable or disable the prestige system for familiars.");
        _maxFamiliarPrestiges = InitConfigEntry("Config", "MaxFamiliarPrestiges", 10, "The maximum number of prestiges a familiar can reach.");
        _familiarPrestigeStatMultiplier = InitConfigEntry("Config", "FamiliarPrestigeStatMultiplier", 0.10f, "The multiplier for stats gained from familiar prestiges.");
        _maxFamiliarLevel = InitConfigEntry("Config", "MaxFamiliarLevel", 90, "The maximum level a familiar can reach.");
        _allowVBloods = InitConfigEntry("Config", "AllowVBloods", false, "Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list).");
        _bannedUnits = InitConfigEntry("Config", "BannedUnits", "", "The PrefabGUID hashes for units that cannot be used as familiars. Same structure as the buff lists except unit prefabs.");
        _bannedTypes = InitConfigEntry("Config", "BannedTypes", "", "The types of units that cannot be used as familiars go here. (Human, Undead, Demon, Mechanical, Beast)");
        _vBloodDamageMultiplier = InitConfigEntry("Config", "VBloodDamageMultiplier", 1f, "Leave at 1 for no change (controls damage familiars do to VBloods).");
        _playerVampireDamageMultiplier = InitConfigEntry("Config", "PlayerVampireDamageMultiplier", 1f, "Leave at 1 for no change (controls damage familiars do to players. probably).");
        _unitFamiliarMultiplier = InitConfigEntry("Config", "UnitFamiliarMultiplier", 7.5f, "The multiplier for experience gained from units.");
        _vBloodFamiliarMultiplier = InitConfigEntry("Config", "VBloodFamiliarMultiplier", 15f, "The multiplier for experience gained from VBloods.");
        _unitUnlockChance = InitConfigEntry("Config", "UnitUnlockChance", 0.05f, "The chance for a unit to unlock a familiar.");
        _vBloodUnlockChance = InitConfigEntry("Config", "VBloodUnlockChance", 0.01f, "The chance for a VBlood to unlock a familiar.");

        _softSynergies = InitConfigEntry("Config", "SoftSynergies", false, "Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard syergies at the same time).");
        _hardSynergies = InitConfigEntry("Config", "HardSynergies", false, "Enforce class synergies (turns on classes and restricts stat choices, do not use this and soft syergies at the same time).");
        _classSpellSchoolOnHitEffects = InitConfigEntry("Config", "ClassSpellSchoolOnHitEffects", false, "Enable or disable class spell school on hit effects.");
        _onHitProcChance = InitConfigEntry("Config", "OnHitProcChance", 0.075f, "The chance for a class effect to proc on hit.");
        _statSyngergyMultiplier = InitConfigEntry("Config", "StatSynergyMultiplier", 1.5f, "Multiplier for class stat synergies to base stat cap.");
        _bloodKnightWeapon = InitConfigEntry("Config", "BloodKnightWeapon", "0,3,5,6", "Blood Knight weapon synergies.");
        _bloodKnightBlood = InitConfigEntry("Config", "BloodKnightBlood", "1,5,7,10", "Blood Knight blood synergies.");
        _demonHunterWeapon = InitConfigEntry("Config", "DemonHunterWeapon", "1,2,8,9", "Demon Hunter weapon synergies.");
        _demonHunterBlood = InitConfigEntry("Config", "DemonHunterBlood", "2,5,7,9", "Demon Hunter blood synergies.");
        _vampireLordWeapon = InitConfigEntry("Config", "VampireLordWeapon", "0,4,6,7", "Vampire Lord weapon synergies.");
        _vampireLordBlood = InitConfigEntry("Config", "VampireLordBlood", "1,3,8,11", "Vampire Lord blood synergies.");
        _shadowBladeWeapon = InitConfigEntry("Config", "ShadowBladeWeapon", "1,2,6,9", "Shadow Blade weapon synergies.");
        _shadowBladeBlood = InitConfigEntry("Config", "ShadowBladeBlood", "3,5,7,10", "Shadow Blade blood synergies.");
        _arcaneSorcererWeapon = InitConfigEntry("Config", "ArcaneSorcererWeapon", "4,7,10,11", "Arcane Sorcerer weapon synergies.");
        _arcaneSorcererBlood = InitConfigEntry("Config", "ArcaneSorcererBlood", "0,6,8,10", "Arcane Sorcerer blood synergies.");
        _deathMageWeapon = InitConfigEntry("Config", "DeathMageWeapon", "0,4,7,11", "Death Mage weapon synergies.");
        _deathMageBlood = InitConfigEntry("Config", "DeathMageBlood", "2,3,6,8", "Death Mage blood synergies.");
        _bloodKnightBuffs = InitConfigEntry("Config", "BloodKnightBuffs", "1828387635,-534491790,-1055766373,-584203677", "The PrefabGUID hashes for blood knight leveling blood buffs. Granted every MaxLevel/(# of blood buffs), so if max l ");
        _bloodKnightSpells = InitConfigEntry("Config", "BloodKnightSpells", "-433204738,-1161896955,1957691133,-7407393", "Blood Knight shift spells, granted at levels of prestige.");
        _demonHunterBuffs = InitConfigEntry("Config", "DemonHunterBuffs", "-154702686,-285745649,-1510965956,-397097531", "The PrefabGUID hashes for demon hunter leveling blood buffs");
        _demonHunterSpells = InitConfigEntry("Config", "DemonHunterSpells", "-433204738,1611191665,-328617085,-1161896955", "Demon Hunter shift spells, granted at levels of prestige");
        _vampireLordBuffs = InitConfigEntry("Config", "VampireLordBuffs", "1558171501,997154800,-1413561088,1103099361", "The PrefabGUID hashes for vampire lord leveling blood buffs");
        _vampireLordSpells = InitConfigEntry("Config", "VampireLordSpells", "-433204738,716346677,1450902136,-254080557", "Vampire Lord shift spells, granted at levels of prestige");
        _shadowBladeBuffs = InitConfigEntry("Config", "ShadowBladeBuffs", "894725875,-1596803256,-993492354,210193036", "The PrefabGUID hashes for shadow blade leveling blood buffs");
        _shadowBladeSpells = InitConfigEntry("Config", "ShadowBladeSpells", "-433204738,94933870,642767950,1922493152", "Shadow Blade shift spells, granted at levels of prestige");
        _arcaneSorcererBuffs = InitConfigEntry("Config", "ArcaneSorcererBuffs", "1614027598,884683323,-1576592687,-1859298707", "The PrefabGUID hashes for arcane leveling blood buffs");
        _arcaneSorcererSpells = InitConfigEntry("Config", "ArcaneSorcererSpells", "-433204738,495259674,1217615468,-1503327574", "Arcane Sorcerer shift spells, granted at levels of prestige");
        _deathMageBuffs = InitConfigEntry("Config", "DeathMageBuffs", "-901503997,-804597757,1934870645,1201299233", "The PrefabGUID hashes for death mage leveling blood buffs");
        _deathMageSpells = InitConfigEntry("Config", "DeathMageSpells", "-433204738,234226418,1619461812,1006960825", "Death Mage shift spells, granted at levels of prestige");
    }

    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }

        return entry;
    }

    static void CreateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }

    static void LoadAllData()
    {
        Core.DataStructures.LoadPlayerBools();
        if (Parties.Value)
        {
            Core.DataStructures.LoadPlayerParties();
        }
        if (SoftSynergies.Value || HardSynergies.Value)
        {
            Core.DataStructures.LoadPlayerClasses();
        }
        if (QuestSystem.Value)
        {
            Core.DataStructures.LoadPlayerQuests();
        }
        if (LevelingSystem.Value)
        {
            foreach (var loadFunction in loadLeveling)
            {
                loadFunction();
            }
        }
        if (ExpertiseSystem.Value)
        {
            foreach (var loadFunction in loadExpertises)
            {
                loadFunction();
            }

            foreach (var loadFunction in loadSanguimancy)
            {
                loadFunction();
            }
        }
        if (BloodSystem.Value)
        {
            foreach (var loadFunction in loadLegacies)
            {
                loadFunction();
            }
        }
        if (ProfessionSystem.Value)
        {
            foreach (var loadFunction in loadProfessions)
            {
                loadFunction();
            }
        }
        if (FamiliarSystem.Value)
        {
            foreach (var loadFunction in loadFamiliars)
            {
                loadFunction();
            }
        }
    }

    static readonly Action[] loadLeveling =
    [
        Core.DataStructures.LoadPlayerExperience,
        Core.DataStructures.LoadPlayerPrestiges,
    ];

    static readonly Action[] loadExpertises =
    [
        Core.DataStructures.LoadPlayerSwordExpertise,
        Core.DataStructures.LoadPlayerAxeExpertise,
        Core.DataStructures.LoadPlayerMaceExpertise,
        Core.DataStructures.LoadPlayerSpearExpertise,
        Core.DataStructures.LoadPlayerCrossbowExpertise,
        Core.DataStructures.LoadPlayerGreatSwordExpertise,
        Core.DataStructures.LoadPlayerSlashersExpertise,
        Core.DataStructures.LoadPlayerPistolsExpertise,
        Core.DataStructures.LoadPlayerReaperExpertise,
        Core.DataStructures.LoadPlayerLongbowExpertise,
        Core.DataStructures.LoadPlayerWhipExpertise,
        Core.DataStructures.LoadPlayerFishingpoleExpertise,
        Core.DataStructures.LoadPlayerWeaponStats
    ];

    static readonly Action[] loadSanguimancy =
    [
        Core.DataStructures.LoadPlayerSanguimancy,
        Core.DataStructures.LoadPlayerSpells
    ];

    static readonly Action[] loadLegacies =
    [
        Core.DataStructures.LoadPlayerWorkerLegacy,
        Core.DataStructures.LoadPlayerWarriorLegacy,
        Core.DataStructures.LoadPlayerScholarLegacy,
        Core.DataStructures.LoadPlayerRogueLegacy,
        Core.DataStructures.LoadPlayerMutantLegacy,
        Core.DataStructures.LoadPlayerVBloodLegacy,
        Core.DataStructures.LoadPlayerDraculinLegacy,
        Core.DataStructures.LoadPlayerImmortalLegacy,
        Core.DataStructures.LoadPlayerCreatureLegacy,
        Core.DataStructures.LoadPlayerBruteLegacy,
        Core.DataStructures.LoadPlayerBloodStats
    ];

    static readonly Action[] loadProfessions =
    [
        Core.DataStructures.LoadPlayerWoodcutting,
        Core.DataStructures.LoadPlayerMining,
        Core.DataStructures.LoadPlayerFishing,
        Core.DataStructures.LoadPlayerBlacksmithing,
        Core.DataStructures.LoadPlayerTailoring,
        Core.DataStructures.LoadPlayerEnchanting,
        Core.DataStructures.LoadPlayerAlchemy,
        Core.DataStructures.LoadPlayerHarvesting,
    ];

    static readonly Action[] loadFamiliars =
    [
        Core.DataStructures.LoadPlayerFamiliarActives,
        Core.DataStructures.LoadPlayerFamiliarSets
    ];

    static readonly List<string> directoryPaths =
        [
        ConfigFiles,
        PlayerQuestsPath,
        PlayerLevelingPath,
        PlayerExpertisePath,
        PlayerBloodPath,
        PlayerProfessionPath,
        PlayerFamiliarsPath,
        FamiliarExperiencePath,
        FamiliarUnlocksPath
        ];
}