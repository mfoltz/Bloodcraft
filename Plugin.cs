using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using VampireCommandFramework;

namespace Bloodcraft;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static ManualLogSource LogInstance => Instance.Log;

    public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    public static readonly string PlayerExperiencePath = Path.Combine(ConfigPath, "ExperienceLeveling");
    public static readonly string PlayerExpertisePath = Path.Combine(ConfigPath, "WeaponExpertise");
    public static readonly string PlayerBloodPath = Path.Combine(ConfigPath, "BloodLegacies");
    public static readonly string PlayerProfessionPath = Path.Combine(ConfigPath, "Professions");

    private static ConfigEntry<bool> _levelingSystem;
    private static ConfigEntry<int> _maxPlayerLevel;
    private static ConfigEntry<int> _startingLevel;
    private static ConfigEntry<float> _unitLevelingMultiplier;
    private static ConfigEntry<float> _vBloodLevelingMultiplier;
    private static ConfigEntry<float> _groupLevelingMultiplier;
    private static ConfigEntry<float> _levelScalingMultiplier;

    private static ConfigEntry<bool> _expertiseSystem;
    private static ConfigEntry<bool> _sanguimancy;
    private static ConfigEntry<int> _firstSlot;
    private static ConfigEntry<int> _secondSlot;
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
    private static ConfigEntry<float> _bloodDrain;
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

    public static ConfigEntry<bool> LevelingSystem => _levelingSystem;
    public static ConfigEntry<int> MaxPlayerLevel => _maxPlayerLevel;
    public static ConfigEntry<int> StartingLevel => _startingLevel;
    public static ConfigEntry<float> UnitLevelingMultiplier => _unitLevelingMultiplier;
    public static ConfigEntry<float> VBloodLevelingMultiplier => _vBloodLevelingMultiplier;
    public static ConfigEntry<float> GroupLevelingMultiplier => _groupLevelingMultiplier;
    public static ConfigEntry<float> LevelScalingMultiplier => _levelScalingMultiplier;
    public static ConfigEntry<bool> PreparedForTheHunt => _preparedForTheHunt;

    public static ConfigEntry<bool> ExpertiseSystem => _expertiseSystem;
    public static ConfigEntry<bool> Sanguimancy => _sanguimancy;
    public static ConfigEntry<int> FirstSlot => _firstSlot;
    public static ConfigEntry<int> SecondSlot => _secondSlot;
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

    public static ConfigEntry<float> BloodDrain => _bloodDrain;

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
    
    public override void Load()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        CommandRegistry.RegisterAll();
        LoadAllData();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }

    static void InitConfig()
    {
        CreateDirectories(ConfigPath);
        CreateDirectories(PlayerExperiencePath);
        CreateDirectories(PlayerExpertisePath);
        CreateDirectories(PlayerBloodPath);
        CreateDirectories(PlayerProfessionPath);

        _levelingSystem = Instance.Config.Bind("Config", "LevelingSystem", false, "Enable or disable the leveling system.");
        _maxPlayerLevel = Instance.Config.Bind("Config", "MaxLevel", 90, "The maximum level a player can reach.");
        _startingLevel = Instance.Config.Bind("Config", "StartingLevel", 0, "Starting level for players if no data is found.");
        _unitLevelingMultiplier = Instance.Config.Bind("Config", "UnitLevelingMultiplier", 5f, "The multiplier for experience gained from units.");
        _vBloodLevelingMultiplier = Instance.Config.Bind("Config", "VBloodLevelingMultiplier", 15f, "The multiplier for experience gained from VBloods.");
        _groupLevelingMultiplier = Instance.Config.Bind("Config", "GroupLevelingMultiplier", 1f, "The multiplier for experience gained from group kills.");
        _levelScalingMultiplier = Instance.Config.Bind("Config", "LevelScalingMultiplier", 0.05f, "Scaling multiplier for tapering experience gained at higher levels.");

        _expertiseSystem = Instance.Config.Bind("Config", "ExpertiseSystem", false, "Enable or disable the expertise system.");
        _sanguimancy = Instance.Config.Bind("Config", "Sanguimancy", false, "Enable or disable sanguimancy (extra spells for unarmed expertise).");
        _firstSlot = Instance.Config.Bind("Config", "FirstSlot", 25, "Level of sanguimancy required for first slot unlock.");
        _secondSlot = Instance.Config.Bind("Config", "SecondSlot", 50, "Level of sanguimancy required for second slot unlock.");
        _maxExpertiseLevel = Instance.Config.Bind("Config", "MaxExpertiseLevel", 99, "The maximum level a player can reach in weapon expertise.");
        _unitExpertiseMultiplier = Instance.Config.Bind("Config", "UnitExpertiseMultiplier", 2f, "The multiplier for expertise gained from units.");
        _vBloodExpertiseMultiplier = Instance.Config.Bind("Config", "VBloodExpertiseMultiplier", 5f, "The multiplier for expertise gained from VBloods.");
        _expertiseStatChoices = Instance.Config.Bind("Config", "ExpertiseStatChoices", 2, "The maximum number of stat choices a player can pick for a weapon expertise.");
        _resetExpertiseItem = Instance.Config.Bind("Config", "ResetExpertiseItem", 0, "Item PrefabGUID cost for resetting weapon stats.");
        _resetExpertiseItemQuantity = Instance.Config.Bind("Config", "ResetExpertiseItemQuantity", 0, "Quantity of item required for resetting stats.");

        _maxHealth = Instance.Config.Bind("Config", "MaxHealth", 250f, "The base cap for maximum health.");
        _movementSpeed = Instance.Config.Bind("Config", "MovementSpeed", 0.25f, "The base cap for movement speed.");
        _primaryAttackSpeed = Instance.Config.Bind("Config", "PrimaryAttackSpeed", 0.25f, "The base cap for primary attack speed.");
        _physicalLifeLeech = Instance.Config.Bind("Config", "PhysicalLifeLeech", 0.15f, "The base cap for physical life leech.");
        _spellLifeLeech = Instance.Config.Bind("Config", "SpellLifeLeech", 0.15f, "The base cap for spell life leech.");
        _primaryLifeLeech = Instance.Config.Bind("Config", "PrimaryLifeLeech", 0.25f, "The base cap for primary life leech.");
        _physicalPower = Instance.Config.Bind("Config", "PhysicalPower", 15f, "The base cap for physical power.");
        _spellPower = Instance.Config.Bind("Config", "SpellPower", 15f, "The base cap for spell power.");
        _physicalCritChance = Instance.Config.Bind("Config", "PhysicalCritChance", 0.15f, "The base cap for physical critical strike chance.");
        _physicalCritDamage = Instance.Config.Bind("Config", "PhysicalCritDamage", 0.75f, "The base cap for physical critical strike damage.");
        _spellCritChance = Instance.Config.Bind("Config", "SpellCritChance", 0.15f, "The base cap for spell critical strike chance.");
        _spellCritDamage = Instance.Config.Bind("Config", "SpellCritDamage", 0.75f, "The base cap for spell critical strike damage.");

        _bloodSystem = Instance.Config.Bind("Config", "BloodSystem", false, "Enable or disable the blood legacy system.");
        _maxBloodLevel = Instance.Config.Bind("Config", "MaxBloodLevel", 99, "The maximum level a player can reach in blood legacies.");
        _unitLegacyMultiplier = Instance.Config.Bind("Config", "UnitLegacyMultiplier", 1f, "The multiplier for lineage gained from units.");
        _vBloodLegacyMultipler = Instance.Config.Bind("Config", "VBloodLegacyMultipler", 5f, "The multiplier for lineage gained from VBloods.");
        _legacyStatChoices = Instance.Config.Bind("Config", "LegacyStatChoices", 2, "The maximum number of stat choices a player can pick for a blood legacy.");
        _resetLegacyItem = Instance.Config.Bind("Config", "ResetLegacyItem", 0, "Item PrefabGUID cost for resetting blood stats.");
        _resetLegacyItemQuantity = Instance.Config.Bind("Config", "ResetLegacyItemQuantity", 0, "Quantity of item required for resetting blood stats.");

        _healingReceived = Instance.Config.Bind("Config", "HealingReceived", 0.25f, "The base cap for healing received.");
        _damageReduction = Instance.Config.Bind("Config", "DamageReduction", 0.10f, "The base cap for damage reduction.");
        _physicalResistance = Instance.Config.Bind("Config", "PhysicalResistance", 0.20f, "The base cap for physical resistance.");
        _spellResistance = Instance.Config.Bind("Config", "SpellResistance", 0.20f, "The base cap for spell resistance.");
        _bloodDrain = Instance.Config.Bind("Config", "BloodDrain", 0.50f, "The base cap for blood drain.");
        _ccReduction = Instance.Config.Bind("Config", "CCReduction", 0.25f, "The base cap for crowd control reduction.");
        _spellCooldownRecoveryRate = Instance.Config.Bind("Config", "SpellCooldownRecoveryRate", 0.15f, "The base cap for spell cooldown recovery rate.");
        _weaponCooldownRecoveryRate = Instance.Config.Bind("Config", "WeaponCooldownRecoveryRate", 0.15f, "The base cap for weapon cooldown recovery rate.");
        _ultimateCooldownRecoveryRate = Instance.Config.Bind("Config", "UltimateCooldownRecoveryRate", 0.20f, "The base cap for ultimate cooldown recovery rate.");
        _minionDamage = Instance.Config.Bind("Config", "MinionDamage", 0.25f, "The base cap for minion damage.");
        _shieldAbsorb = Instance.Config.Bind("Config", "ShieldAbsorb", 0.50f, "The base cap for shield absorb.");
        _bloodEfficiency = Instance.Config.Bind("Config", "BloodEfficiency", 0.10f, "The base cap for blood efficiency.");

        _professionSystem = Instance.Config.Bind("Config", "ProfessionSystem", false, "Enable or disable the profession system.");
        _maxProfessionLevel = Instance.Config.Bind("Config", "MaxProfessionLevel", 99, "The maximum level a player can reach in professions.");
        _professionMultiplier = Instance.Config.Bind("Config", "ProfessionMultiplier", 10f, "The multiplier for profession experience gained.");
        // Initialize configuration settings
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
            if (Sanguimancy.Value)
            {
                foreach (var loadFunction in loadSanguimancy)
                {
                    loadFunction();
                }
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
    }

    static readonly Action[] loadLeveling =
    [
        Core.DataStructures.LoadPlayerExperience,
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
        Core.DataStructures.LoadPlayerWeaponStats
    ];

    static readonly Action[] loadSanguimancy =
    [
        Core.DataStructures.LoadPlayerSanguimancy,
        Core.DataStructures.LoadPlayerSanguimancySpells
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
}
