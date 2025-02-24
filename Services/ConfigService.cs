using BepInEx.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Bloodcraft.Services;
internal static class ConfigService
{
    static readonly Lazy<string> _languageLocalization = new(() => GetConfigValue<string>("LanguageLocalization"));
    public static string LanguageLocalization => _languageLocalization.Value;

    static readonly Lazy<bool> _clientCompanion = new(() => GetConfigValue<bool>("ClientCompanion"));
    public static bool ClientCompanion => _clientCompanion.Value;

    static readonly Lazy<bool> _eliteShardBearers = new(() => GetConfigValue<bool>("EliteShardBearers"));
    public static bool EliteShardBearers => _eliteShardBearers.Value;

    static readonly Lazy<int> _shardBearerLevel = new(() => GetConfigValue<int>("ShardBearerLevel"));
    public static int ShardBearerLevel => _shardBearerLevel.Value;

    static readonly Lazy<bool> _potionStacking = new(() => GetConfigValue<bool>("PotionStacking"));
    public static bool PotionStacking => _potionStacking.Value;

    static readonly Lazy<bool> _bearFormDash = new(() => GetConfigValue<bool>("BearFormDash"));
    public static bool BearFormDash => _bearFormDash.Value;

    // static readonly Lazy<bool> _performanceAuditing = new(() => GetConfigValue<bool>("PerformanceAuditing"));
    // public static bool PerformanceAuditing => _performanceAuditing.Value;

    static readonly Lazy<int> _primalJewelCost = new(() => GetConfigValue<int>("PrimalJewelCost"));
    public static int PrimalJewelCost => _primalJewelCost.Value;

    static readonly Lazy<bool> _starterKit = new(() => GetConfigValue<bool>("StarterKit"));
    public static bool StarterKit => _starterKit.Value;

    static readonly Lazy<string> _kitPrefabs = new(() => GetConfigValue<string>("KitPrefabs"));
    public static string KitPrefabs => _kitPrefabs.Value;

    static readonly Lazy<string> _kitQuantities = new(() => GetConfigValue<string>("KitQuantities"));
    public static string KitQuantities => _kitQuantities.Value;

    static readonly Lazy<bool> _questSystem = new(() => GetConfigValue<bool>("QuestSystem"));
    public static bool QuestSystem => _questSystem.Value;

    static readonly Lazy<bool> _infiniteDailies = new(() => GetConfigValue<bool>("InfiniteDailies"));
    public static bool InfiniteDailies => _infiniteDailies.Value;

    static readonly Lazy<string> _questRewards = new(() => GetConfigValue<string>("QuestRewards"));
    public static string QuestRewards => _questRewards.Value;

    static readonly Lazy<string> _questRewardAmounts = new(() => GetConfigValue<string>("QuestRewardAmounts"));
    public static string QuestRewardAmounts => _questRewardAmounts.Value;

    static readonly Lazy<int> _rerollDailyPrefab = new(() => GetConfigValue<int>("RerollDailyPrefab"));
    public static int RerollDailyPrefab => _rerollDailyPrefab.Value;

    static readonly Lazy<int> _rerollDailyAmount = new(() => GetConfigValue<int>("RerollDailyAmount"));
    public static int RerollDailyAmount => _rerollDailyAmount.Value;

    static readonly Lazy<int> _rerollWeeklyPrefab = new(() => GetConfigValue<int>("RerollWeeklyPrefab"));
    public static int RerollWeeklyPrefab => _rerollWeeklyPrefab.Value;

    static readonly Lazy<int> _rerollWeeklyAmount = new(() => GetConfigValue<int>("RerollWeeklyAmount"));
    public static int RerollWeeklyAmount => _rerollWeeklyAmount.Value;

    static readonly Lazy<bool> _levelingSystem = new(() => GetConfigValue<bool>("LevelingSystem"));
    public static bool LevelingSystem => _levelingSystem.Value;

    static readonly Lazy<bool> _restedXPSystem = new(() => GetConfigValue<bool>("RestedXPSystem"));
    public static bool RestedXPSystem => _restedXPSystem.Value;

    static readonly Lazy<float> _restedXPRate = new(() => GetConfigValue<float>("RestedXPRate"));
    public static float RestedXPRate => _restedXPRate.Value;

    static readonly Lazy<int> _restedXPMax = new(() => GetConfigValue<int>("RestedXPMax"));
    public static int RestedXPMax => _restedXPMax.Value;

    static readonly Lazy<float> _restedXPTickRate = new(() => GetConfigValue<float>("RestedXPTickRate"));
    public static float RestedXPTickRate => _restedXPTickRate.Value;

    static readonly Lazy<int> _maxLevel = new(() => GetConfigValue<int>("MaxLevel"));
    public static int MaxLevel => _maxLevel.Value;

    static readonly Lazy<int> _startingLevel = new(() => GetConfigValue<int>("StartingLevel"));
    public static int StartingLevel => _startingLevel.Value;

    static readonly Lazy<float> _unitLevelingMultiplier = new(() => GetConfigValue<float>("UnitLevelingMultiplier"));
    public static float UnitLevelingMultiplier => _unitLevelingMultiplier.Value;

    static readonly Lazy<float> _vBloodLevelingMultiplier = new(() => GetConfigValue<float>("VBloodLevelingMultiplier"));
    public static float VBloodLevelingMultiplier => _vBloodLevelingMultiplier.Value;

    static readonly Lazy<float> _docileUnitMultiplier = new(() => GetConfigValue<float>("DocileUnitMultiplier"));
    public static float DocileUnitMultiplier => _docileUnitMultiplier.Value;

    static readonly Lazy<float> _warEventMultiplier = new(() => GetConfigValue<float>("WarEventMultiplier"));
    public static float WarEventMultiplier => _warEventMultiplier.Value;

    static readonly Lazy<float> _unitSpawnerMultiplier = new(() => GetConfigValue<float>("UnitSpawnerMultiplier"));
    public static float UnitSpawnerMultiplier => _unitSpawnerMultiplier.Value;

    static readonly Lazy<float> _unitSpawnerExpertiseFactor = new(() => GetConfigValue<float>("UnitSpawnerExpertiseFactor"));
    public static float UnitSpawnerExpertiseFactor => _unitSpawnerExpertiseFactor.Value;

    static readonly Lazy<int> _changeClassItem = new(() => GetConfigValue<int>("ChangeClassItem"));
    public static int ChangeClassItem => _changeClassItem.Value;

    static readonly Lazy<int> _changeClassQuantity = new(() => GetConfigValue<int>("ChangeClassQuantity"));
    public static int ChangeClassQuantity => _changeClassQuantity.Value;

    static readonly Lazy<float> _groupLevelingMultiplier = new(() => GetConfigValue<float>("GroupLevelingMultiplier"));
    public static float GroupLevelingMultiplier => _groupLevelingMultiplier.Value;

    static readonly Lazy<float> _levelScalingMultiplier = new(() => GetConfigValue<float>("LevelScalingMultiplier"));
    public static float LevelScalingMultiplier => _levelScalingMultiplier.Value;

    static readonly Lazy<bool> _playerParties = new(() => GetConfigValue<bool>("PlayerParties"));
    public static bool PlayerParties => _playerParties.Value;

    static readonly Lazy<int> _maxPartySize = new(() => GetConfigValue<int>("MaxPartySize"));
    public static int MaxPartySize => _maxPartySize.Value;

    static readonly Lazy<float> _expShareDistance = new(() => GetConfigValue<float>("ExpShareDistance"));
    public static float ExpShareDistance => _expShareDistance.Value;

    static readonly Lazy<bool> _prestigeSystem = new(() => GetConfigValue<bool>("PrestigeSystem"));
    public static bool PrestigeSystem => _prestigeSystem.Value;

    static readonly Lazy<string> _prestigeBuffs = new(() => GetConfigValue<string>("PrestigeBuffs"));
    public static string PrestigeBuffs => _prestigeBuffs.Value;

    static readonly Lazy<string> _prestigeLevelsToUnlockClassSpells = new(() => GetConfigValue<string>("PrestigeLevelsToUnlockClassSpells"));
    public static string PrestigeLevelsToUnlockClassSpells => _prestigeLevelsToUnlockClassSpells.Value;

    static readonly Lazy<int> _maxLevelingPrestiges = new(() => GetConfigValue<int>("MaxLevelingPrestiges"));
    public static int MaxLevelingPrestiges => _maxLevelingPrestiges.Value;

    static readonly Lazy<float> _levelingPrestigeReducer = new(() => GetConfigValue<float>("LevelingPrestigeReducer"));
    public static float LevelingPrestigeReducer => _levelingPrestigeReducer.Value;

    static readonly Lazy<float> _prestigeRatesReducer = new(() => GetConfigValue<float>("PrestigeRatesReducer"));
    public static float PrestigeRatesReducer => _prestigeRatesReducer.Value;

    static readonly Lazy<float> _prestigeStatMultiplier = new(() => GetConfigValue<float>("PrestigeStatMultiplier"));
    public static float PrestigeStatMultiplier => _prestigeStatMultiplier.Value;

    static readonly Lazy<float> _prestigeRateMultiplier = new(() => GetConfigValue<float>("PrestigeRateMultiplier"));
    public static float PrestigeRateMultiplier => _prestigeRateMultiplier.Value;

    static readonly Lazy<bool> _exoPrestiging = new(() => GetConfigValue<bool>("ExoPrestiging"));
    public static bool ExoPrestiging => _exoPrestiging.Value;

    static readonly Lazy<int> _exoPrestigeReward = new(() => GetConfigValue<int>("ExoPrestigeReward"));
    public static int ExoPrestigeReward => _exoPrestigeReward.Value;

    static readonly Lazy<int> _exoPrestigeRewardQuantity = new(() => GetConfigValue<int>("ExoPrestigeRewardQuantity"));
    public static int ExoPrestigeRewardQuantity => _exoPrestigeRewardQuantity.Value;

    static readonly Lazy<bool> _trueImmortal = new(() => GetConfigValue<bool>("TrueImmortal"));
    public static bool TrueImmortal => _trueImmortal.Value;

    static readonly Lazy<bool> _expertiseSystem = new(() => GetConfigValue<bool>("ExpertiseSystem"));
    public static bool ExpertiseSystem => _expertiseSystem.Value;

    static readonly Lazy<int> _maxExpertisePrestiges = new(() => GetConfigValue<int>("MaxExpertisePrestiges"));
    public static int MaxExpertisePrestiges => _maxExpertisePrestiges.Value;

    static readonly Lazy<bool> _unarmedSlots = new(() => GetConfigValue<bool>("UnarmedSlots"));
    public static bool UnarmedSlots => _unarmedSlots.Value;

    static readonly Lazy<bool> _shiftSlot = new(() => GetConfigValue<bool>("ShiftSlot"));
    public static bool ShiftSlot => _shiftSlot.Value;

    static readonly Lazy<int> _maxExpertiseLevel = new(() => GetConfigValue<int>("MaxExpertiseLevel"));
    public static int MaxExpertiseLevel => _maxExpertiseLevel.Value;

    static readonly Lazy<float> _unitExpertiseMultiplier = new(() => GetConfigValue<float>("UnitExpertiseMultiplier"));
    public static float UnitExpertiseMultiplier => _unitExpertiseMultiplier.Value;

    static readonly Lazy<float> _vBloodExpertiseMultiplier = new(() => GetConfigValue<float>("VBloodExpertiseMultiplier"));
    public static float VBloodExpertiseMultiplier => _vBloodExpertiseMultiplier.Value;

    static readonly Lazy<int> _expertiseStatChoices = new(() => GetConfigValue<int>("ExpertiseStatChoices"));
    public static int ExpertiseStatChoices => _expertiseStatChoices.Value;

    static readonly Lazy<int> _resetExpertiseItem = new(() => GetConfigValue<int>("ResetExpertiseItem"));
    public static int ResetExpertiseItem => _resetExpertiseItem.Value;

    static readonly Lazy<int> _resetExpertiseItemQuantity = new(() => GetConfigValue<int>("ResetExpertiseItemQuantity"));
    public static int ResetExpertiseItemQuantity => _resetExpertiseItemQuantity.Value;

    static readonly Lazy<float> _maxHealth = new(() => GetConfigValue<float>("MaxHealth"));
    public static float MaxHealth => _maxHealth.Value;

    static readonly Lazy<float> _movementSpeed = new(() => GetConfigValue<float>("MovementSpeed"));
    public static float MovementSpeed => _movementSpeed.Value;

    static readonly Lazy<float> _primaryAttackSpeed = new(() => GetConfigValue<float>("PrimaryAttackSpeed"));
    public static float PrimaryAttackSpeed => _primaryAttackSpeed.Value;

    static readonly Lazy<float> _physicalLifeLeech = new(() => GetConfigValue<float>("PhysicalLifeLeech"));
    public static float PhysicalLifeLeech => _physicalLifeLeech.Value;

    static readonly Lazy<float> _spellLifeLeech = new(() => GetConfigValue<float>("SpellLifeLeech"));
    public static float SpellLifeLeech => _spellLifeLeech.Value;

    static readonly Lazy<float> _primaryLifeLeech = new(() => GetConfigValue<float>("PrimaryLifeLeech"));
    public static float PrimaryLifeLeech => _primaryLifeLeech.Value;

    static readonly Lazy<float> _physicalPower = new(() => GetConfigValue<float>("PhysicalPower"));
    public static float PhysicalPower => _physicalPower.Value;

    static readonly Lazy<float> _spellPower = new(() => GetConfigValue<float>("SpellPower"));
    public static float SpellPower => _spellPower.Value;

    static readonly Lazy<float> _physicalCritChance = new(() => GetConfigValue<float>("PhysicalCritChance"));
    public static float PhysicalCritChance => _physicalCritChance.Value;

    static readonly Lazy<float> _physicalCritDamage = new(() => GetConfigValue<float>("PhysicalCritDamage"));
    public static float PhysicalCritDamage => _physicalCritDamage.Value;

    static readonly Lazy<float> _spellCritChance = new(() => GetConfigValue<float>("SpellCritChance"));
    public static float SpellCritChance => _spellCritChance.Value;

    static readonly Lazy<float> _spellCritDamage = new(() => GetConfigValue<float>("SpellCritDamage"));
    public static float SpellCritDamage => _spellCritDamage.Value;

    static readonly Lazy<bool> _bloodSystem = new(() => GetConfigValue<bool>("BloodSystem"));
    public static bool BloodSystem => _bloodSystem.Value;

    static readonly Lazy<int> _maxLegacyPrestiges = new(() => GetConfigValue<int>("MaxLegacyPrestiges"));
    public static int MaxLegacyPrestiges => _maxLegacyPrestiges.Value;

    static readonly Lazy<bool> _bloodQualityBonus = new(() => GetConfigValue<bool>("BloodQualityBonus"));
    public static bool BloodQualityBonus => _bloodQualityBonus.Value;

    static readonly Lazy<float> _prestigeBloodQuality = new(() => GetConfigValue<float>("PrestigeBloodQuality"));
    public static float PrestigeBloodQuality => _prestigeBloodQuality.Value;

    static readonly Lazy<int> _maxBloodLevel = new(() => GetConfigValue<int>("MaxBloodLevel"));
    public static int MaxBloodLevel => _maxBloodLevel.Value;

    static readonly Lazy<float> _unitLegacyMultiplier = new(() => GetConfigValue<float>("UnitLegacyMultiplier"));
    public static float UnitLegacyMultiplier => _unitLegacyMultiplier.Value;

    static readonly Lazy<float> _vBloodLegacyMultiplier = new(() => GetConfigValue<float>("VBloodLegacyMultiplier"));
    public static float VBloodLegacyMultiplier => _vBloodLegacyMultiplier.Value;

    static readonly Lazy<int> _legacyStatChoices = new(() => GetConfigValue<int>("LegacyStatChoices"));
    public static int LegacyStatChoices => _legacyStatChoices.Value;

    static readonly Lazy<int> _resetLegacyItem = new(() => GetConfigValue<int>("ResetLegacyItem"));
    public static int ResetLegacyItem => _resetLegacyItem.Value;

    static readonly Lazy<int> _resetLegacyItemQuantity = new(() => GetConfigValue<int>("ResetLegacyItemQuantity"));
    public static int ResetLegacyItemQuantity => _resetLegacyItemQuantity.Value;

    static readonly Lazy<float> _healingReceived = new(() => GetConfigValue<float>("HealingReceived"));
    public static float HealingReceived => _healingReceived.Value;

    static readonly Lazy<float> _damageReduction = new(() => GetConfigValue<float>("DamageReduction"));
    public static float DamageReduction => _damageReduction.Value;

    static readonly Lazy<float> _physicalResistance = new(() => GetConfigValue<float>("PhysicalResistance"));
    public static float PhysicalResistance => _physicalResistance.Value;

    static readonly Lazy<float> _spellResistance = new(() => GetConfigValue<float>("SpellResistance"));
    public static float SpellResistance => _spellResistance.Value;

    static readonly Lazy<float> _resourceYield = new(() => GetConfigValue<float>("ResourceYield"));
    public static float ResourceYield => _resourceYield.Value;

    static readonly Lazy<float> _bloodDrain = new(() => GetConfigValue<float>("BloodDrain"));
    public static float BloodDrain => _bloodDrain.Value;

    static readonly Lazy<float> _spellCooldownRecoveryRate = new(() => GetConfigValue<float>("SpellCooldownRecoveryRate"));
    public static float SpellCooldownRecoveryRate => _spellCooldownRecoveryRate.Value;

    static readonly Lazy<float> _weaponCooldownRecoveryRate = new(() => GetConfigValue<float>("WeaponCooldownRecoveryRate"));
    public static float WeaponCooldownRecoveryRate => _weaponCooldownRecoveryRate.Value;

    static readonly Lazy<float> _ultimateCooldownRecoveryRate = new(() => GetConfigValue<float>("UltimateCooldownRecoveryRate"));
    public static float UltimateCooldownRecoveryRate => _ultimateCooldownRecoveryRate.Value;

    static readonly Lazy<float> _minionDamage = new(() => GetConfigValue<float>("MinionDamage"));
    public static float MinionDamage => _minionDamage.Value;

    static readonly Lazy<float> _shieldAbsorb = new(() => GetConfigValue<float>("ShieldAbsorb"));
    public static float ShieldAbsorb => _shieldAbsorb.Value;

    static readonly Lazy<float> _bloodEfficiency = new(() => GetConfigValue<float>("BloodEfficiency"));
    public static float BloodEfficiency => _bloodEfficiency.Value;

    static readonly Lazy<bool> _professionSystem = new(() => GetConfigValue<bool>("ProfessionSystem"));
    public static bool ProfessionSystem => _professionSystem.Value;

    static readonly Lazy<float> _professionMultiplier = new(() => GetConfigValue<float>("ProfessionMultiplier"));
    public static float ProfessionMultiplier => _professionMultiplier.Value;

    static readonly Lazy<bool> _extraRecipes = new(() => GetConfigValue<bool>("ExtraRecipes"));
    public static bool ExtraRecipes => _extraRecipes.Value;

    static readonly Lazy<bool> _familiarSystem = new(() => GetConfigValue<bool>("FamiliarSystem"));
    public static bool FamiliarSystem => _familiarSystem.Value;

    static readonly Lazy<bool> _shareUnlocks = new(() => GetConfigValue<bool>("ShareUnlocks"));
    public static bool ShareUnlocks => _shareUnlocks.Value;

    static readonly Lazy<bool> _familiarCombat = new(() => GetConfigValue<bool>("FamiliarCombat"));
    public static bool FamiliarCombat => _familiarCombat.Value;

    static readonly Lazy<bool> _familiarPvP = new(() => GetConfigValue<bool>("FamiliarPvP"));
    public static bool FamiliarPvP => _familiarPvP.Value;

    static readonly Lazy<bool> _familiarBattles = new(() => GetConfigValue<bool>("FamiliarBattles"));
    public static bool FamiliarBattles => _familiarBattles.Value;

    static readonly Lazy<bool> _familiarPrestige = new(() => GetConfigValue<bool>("FamiliarPrestige"));
    public static bool FamiliarPrestige => _familiarPrestige.Value;

    static readonly Lazy<int> _maxFamiliarPrestiges = new(() => GetConfigValue<int>("MaxFamiliarPrestiges"));
    public static int MaxFamiliarPrestiges => _maxFamiliarPrestiges.Value;

    static readonly Lazy<float> _familiarPrestigeStatMultiplier = new(() => GetConfigValue<float>("FamiliarPrestigeStatMultiplier"));
    public static float FamiliarPrestigeStatMultiplier => _familiarPrestigeStatMultiplier.Value;

    static readonly Lazy<int> _maxFamiliarLevel = new(() => GetConfigValue<int>("MaxFamiliarLevel"));
    public static int MaxFamiliarLevel => _maxFamiliarLevel.Value;

    static readonly Lazy<bool> _allowVBloods = new(() => GetConfigValue<bool>("AllowVBloods"));
    public static bool AllowVBloods => _allowVBloods.Value;

    static readonly Lazy<bool> _allowMinions = new(() => GetConfigValue<bool>("AllowMinions"));
    public static bool AllowMinions => _allowMinions.Value;

    static readonly Lazy<string> _bannedUnits = new(() => GetConfigValue<string>("BannedUnits"));
    public static string BannedUnits => _bannedUnits.Value;

    static readonly Lazy<string> _bannedTypes = new(() => GetConfigValue<string>("BannedTypes"));
    public static string BannedTypes => _bannedTypes.Value;

    static readonly Lazy<float> _vBloodDamageMultiplier = new(() => GetConfigValue<float>("VBloodDamageMultiplier"));
    public static float VBloodDamageMultiplier => _vBloodDamageMultiplier.Value;

    static readonly Lazy<float> _unitFamiliarMultiplier = new(() => GetConfigValue<float>("UnitFamiliarMultiplier"));
    public static float UnitFamiliarMultiplier => _unitFamiliarMultiplier.Value;

    static readonly Lazy<float> _vBloodFamiliarMultiplier = new(() => GetConfigValue<float>("VBloodFamiliarMultiplier"));
    public static float VBloodFamiliarMultiplier => _vBloodFamiliarMultiplier.Value;

    static readonly Lazy<float> _unitUnlockChance = new(() => GetConfigValue<float>("UnitUnlockChance"));
    public static float UnitUnlockChance => _unitUnlockChance.Value;

    static readonly Lazy<float> _vBloodUnlockChance = new(() => GetConfigValue<float>("VBloodUnlockChance"));
    public static float VBloodUnlockChance => _vBloodUnlockChance.Value;

    static readonly Lazy<bool> _primalEchoes = new(() => GetConfigValue<bool>("PrimalEchoes"));
    public static bool PrimalEchoes => _primalEchoes.Value;

    static readonly Lazy<int> _echoesFactor = new(() => GetConfigValue<int>("EchoesFactor"));
    public static int EchoesFactor => _echoesFactor.Value;

    static readonly Lazy<float> _traitChance = new(() => GetConfigValue<float>("TraitChance"));
    public static float TraitChance => _traitChance.Value;

    static readonly Lazy<int> _traitRerollItemQuantity = new(() => GetConfigValue<int>("TraitRerollItemQuantity"));
    public static int TraitRerollItemQuantity => _traitRerollItemQuantity.Value;

    static readonly Lazy<float> _shinyChance = new(() => GetConfigValue<float>("ShinyChance"));
    public static float ShinyChance => _shinyChance.Value;

    static readonly Lazy<int> _shinyCostItemQuantity = new(() => GetConfigValue<int>("ShinyCostItemQuantity"));
    public static int ShinyCostItemQuantity => _shinyCostItemQuantity.Value;

    static readonly Lazy<int> _prestigeCostItemQuantity = new(() => GetConfigValue<int>("PrestigeCostItemQuantity"));
    public static int PrestigeCostItemQuantity => _prestigeCostItemQuantity.Value;

    static readonly Lazy<bool> _softSynergies = new(() => GetConfigValue<bool>("SoftSynergies"));
    public static bool SoftSynergies => _softSynergies.Value;

    static readonly Lazy<bool> _hardSynergies = new(() => GetConfigValue<bool>("HardSynergies"));
    public static bool HardSynergies => _hardSynergies.Value;

    static readonly Lazy<bool> _classSpellSchoolOnHitEffects = new(() => GetConfigValue<bool>("ClassSpellSchoolOnHitEffects"));
    public static bool ClassSpellSchoolOnHitEffects => _classSpellSchoolOnHitEffects.Value;

    static readonly Lazy<float> _onHitProcChance = new(() => GetConfigValue<float>("OnHitProcChance"));
    public static float OnHitProcChance => _onHitProcChance.Value;

    static readonly Lazy<float> _statSynergyMultiplier = new(() => GetConfigValue<float>("StatSynergyMultiplier"));
    public static float StatSynergyMultiplier => _statSynergyMultiplier.Value;

    static readonly Lazy<string> _bloodKnightWeapon = new(() => GetConfigValue<string>("BloodKnightWeapon"));
    public static string BloodKnightWeapon => _bloodKnightWeapon.Value;

    static readonly Lazy<string> _bloodKnightBlood = new(() => GetConfigValue<string>("BloodKnightBlood"));
    public static string BloodKnightBlood => _bloodKnightBlood.Value;

    static readonly Lazy<string> _demonHunterWeapon = new(() => GetConfigValue<string>("DemonHunterWeapon"));
    public static string DemonHunterWeapon => _demonHunterWeapon.Value;

    static readonly Lazy<string> _demonHunterBlood = new(() => GetConfigValue<string>("DemonHunterBlood"));
    public static string DemonHunterBlood => _demonHunterBlood.Value;

    static readonly Lazy<string> _vampireLordWeapon = new(() => GetConfigValue<string>("VampireLordWeapon"));
    public static string VampireLordWeapon => _vampireLordWeapon.Value;

    static readonly Lazy<string> _vampireLordBlood = new(() => GetConfigValue<string>("VampireLordBlood"));
    public static string VampireLordBlood => _vampireLordBlood.Value;

    static readonly Lazy<string> _shadowBladeWeapon = new(() => GetConfigValue<string>("ShadowBladeWeapon"));
    public static string ShadowBladeWeapon => _shadowBladeWeapon.Value;

    static readonly Lazy<string> _shadowBladeBlood = new(() => GetConfigValue<string>("ShadowBladeBlood"));
    public static string ShadowBladeBlood => _shadowBladeBlood.Value;

    static readonly Lazy<string> _arcaneSorcererWeapon = new(() => GetConfigValue<string>("ArcaneSorcererWeapon"));
    public static string ArcaneSorcererWeapon => _arcaneSorcererWeapon.Value;

    static readonly Lazy<string> _arcaneSorcererBlood = new(() => GetConfigValue<string>("ArcaneSorcererBlood"));
    public static string ArcaneSorcererBlood => _arcaneSorcererBlood.Value;

    static readonly Lazy<string> _deathMageWeapon = new(() => GetConfigValue<string>("DeathMageWeapon"));
    public static string DeathMageWeapon => _deathMageWeapon.Value;

    static readonly Lazy<string> _deathMageBlood = new(() => GetConfigValue<string>("DeathMageBlood"));
    public static string DeathMageBlood => _deathMageBlood.Value;

    static readonly Lazy<int> _defaultClassSpell = new(() => GetConfigValue<int>("DefaultClassSpell"));
    public static int DefaultClassSpell => _defaultClassSpell.Value;

    static readonly Lazy<string> _bloodKnightBuffs = new(() => GetConfigValue<string>("BloodKnightBuffs"));
    public static string BloodKnightBuffs => _bloodKnightBuffs.Value;

    static readonly Lazy<string> _bloodKnightSpells = new(() => GetConfigValue<string>("BloodKnightSpells"));
    public static string BloodKnightSpells => _bloodKnightSpells.Value;

    static readonly Lazy<string> _demonHunterBuffs = new(() => GetConfigValue<string>("DemonHunterBuffs"));
    public static string DemonHunterBuffs => _demonHunterBuffs.Value;

    static readonly Lazy<string> _demonHunterSpells = new(() => GetConfigValue<string>("DemonHunterSpells"));
    public static string DemonHunterSpells => _demonHunterSpells.Value;

    static readonly Lazy<string> _vampireLordBuffs = new(() => GetConfigValue<string>("VampireLordBuffs"));
    public static string VampireLordBuffs => _vampireLordBuffs.Value;

    static readonly Lazy<string> _vampireLordSpells = new(() => GetConfigValue<string>("VampireLordSpells"));
    public static string VampireLordSpells => _vampireLordSpells.Value;

    static readonly Lazy<string> _shadowBladeBuffs = new(() => GetConfigValue<string>("ShadowBladeBuffs"));
    public static string ShadowBladeBuffs => _shadowBladeBuffs.Value;

    static readonly Lazy<string> _shadowBladeSpells = new(() => GetConfigValue<string>("ShadowBladeSpells"));
    public static string ShadowBladeSpells => _shadowBladeSpells.Value;

    static readonly Lazy<string> _arcaneSorcererBuffs = new(() => GetConfigValue<string>("ArcaneSorcererBuffs"));
    public static string ArcaneSorcererBuffs => _arcaneSorcererBuffs.Value;

    static readonly Lazy<string> _arcaneSorcererSpells = new(() => GetConfigValue<string>("ArcaneSorcererSpells"));
    public static string ArcaneSorcererSpells => _arcaneSorcererSpells.Value;

    static readonly Lazy<string> _deathMageBuffs = new(() => GetConfigValue<string>("DeathMageBuffs"));
    public static string DeathMageBuffs => _deathMageBuffs.Value;

    static readonly Lazy<string> _deathMageSpells = new(() => GetConfigValue<string>("DeathMageSpells"));
    public static string DeathMageSpells => _deathMageSpells.Value;
    public static class ConfigInitialization
    {
        static readonly Regex _regex = new(@"^\[(.+)\]$");

        public static readonly Dictionary<string, object> FinalConfigValues = [];

        static readonly Lazy<List<string>> _directoryPaths = new(() =>
        {
            return
        [
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME),                                     // 0
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PlayerLeveling"),                   // 1
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Quests"),                           // 2
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "WeaponExpertise"),                  // 3
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "BloodLegacies"),                    // 4
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Professions"),                      // 5
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars"),                        // 6
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarLeveling"),    // 7
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarUnlocks"),     // 8
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PlayerBools"),                      // 9
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarEquipment"),   // 10
            // Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "Familiars", "FamiliarBattleGroups") // 10
        ];
        });
        public static List<string> DirectoryPaths => _directoryPaths.Value;

        public static readonly List<string> SectionOrder =
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
        public static readonly List<ConfigEntryDefinition> ConfigEntries =
        [
            new ConfigEntryDefinition("General", "LanguageLocalization", "English", "The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese"),
            new ConfigEntryDefinition("General", "ClientCompanion", false, "Enable if using the client companion mod, can configure what's displayed in the client config."),
            new ConfigEntryDefinition("General", "EliteShardBearers", false, "Enable or disable elite shard bearers."),
            new ConfigEntryDefinition("General", "ShardBearerLevel", 0, "Sets level of shard bearers if elite shard bearers is enabled. Leave at 0 for no effect."),
            new ConfigEntryDefinition("General", "PotionStacking", false, "Enable or disable potion stacking (can have t01 effects and t02 effects at the same time. also requires professions enabled)."),
            new ConfigEntryDefinition("General", "BearFormDash", false, "Enable or disable bear form dash."),
            new ConfigEntryDefinition("General", "PrimalJewelCost", -77477508, "If extra recipes is enabled with a valid item prefab here (default demon fragments), it can be refined via gemcutter for random enhanced tier 4 jewels (better rolls, more modifiers)."),
            // new ConfigEntryDefinition("General", "PerformanceAuditing", false, "(WIP) Enable or disable logging server RAM, CPU, and EntityQuery count. Probably doesn't belong here, fight me."),
            new ConfigEntryDefinition("StarterKit", "StarterKit", false, "Enable or disable the starter kit."),
            new ConfigEntryDefinition("StarterKit", "KitPrefabs", "862477668,-1531666018,-1593377811,1821405450", "Item prefabGuids for starting kit."),
            new ConfigEntryDefinition("StarterKit", "KitQuantities", "500,1000,1000,250", "The quantity of each item in the starter kit."),
            new ConfigEntryDefinition("Quests", "QuestSystem", false, "Enable or disable quests (kill, gather, and crafting)."),
            new ConfigEntryDefinition("Quests", "InfiniteDailies", false, "Enable or disable infinite dailies."),
            new ConfigEntryDefinition("Quests", "QuestRewards", "28358550,576389135,-257494203", "Item prefabs for quest reward pool."),
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
            new ConfigEntryDefinition("Leveling", "StartingLevel", 10, "Starting level for players if no data is found."),
            new ConfigEntryDefinition("Leveling", "UnitLevelingMultiplier", 7.5f, "The multiplier for experience gained from units."),
            new ConfigEntryDefinition("Leveling", "VBloodLevelingMultiplier", 15f, "The multiplier for experience gained from VBloods."),
            new ConfigEntryDefinition("Leveling", "DocileUnitMultiplier", 0.15f, "The multiplier for experience gained from docile units."),
            new ConfigEntryDefinition("Leveling", "WarEventMultiplier", 0.2f, "The multiplier for experience gained from war event trash spawns."),
            new ConfigEntryDefinition("Leveling", "UnitSpawnerMultiplier", 0f, "The multiplier for experience gained from unit spawners (vermin nests, tombs). Applies to familiar experience as well."),
            new ConfigEntryDefinition("Leveling", "GroupLevelingMultiplier", 1f, "The multiplier for experience gained from group kills."),
            new ConfigEntryDefinition("Leveling", "LevelScalingMultiplier", 0.05f, "Reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove."),
            new ConfigEntryDefinition("Leveling", "PlayerParties", false, "Enable or disable the ability to group with players not in your clan for experience/familiar unlock sharing."),
            new ConfigEntryDefinition("Leveling", "MaxPartySize", 5, "The maximum number of players that can share experience in a group."),
            new ConfigEntryDefinition("Leveling", "ExpShareDistance", 25f, "Default is ~5 floor tile lengths."),
            new ConfigEntryDefinition("Prestige", "PrestigeSystem", false, "Enable or disable the prestige system (requires leveling to be enabled as well)."),
            new ConfigEntryDefinition("Prestige", "PrestigeBuffs", "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884", "The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level."),
            new ConfigEntryDefinition("Prestige", "PrestigeLevelsToUnlockClassSpells", "0,1,2,3,4,5", "The prestige levels at which class spells are unlocked. This should match the number of spells per class +1 to account for the default class spell. Can leave at 0 each if you want them unlocked from the start."),
            new ConfigEntryDefinition("Prestige", "MaxLevelingPrestiges", 10, "The maximum number of prestiges a player can reach in leveling."),
            new ConfigEntryDefinition("Prestige", "LevelingPrestigeReducer", 0.05f, "Flat factor by which experience is reduced per increment of prestige in leveling."),
            new ConfigEntryDefinition("Prestige", "PrestigeRatesReducer", 0.10f, "Flat factor by which rates are reduced in expertise/legacy per increment of prestige in expertise/legacy."),
            new ConfigEntryDefinition("Prestige", "PrestigeStatMultiplier", 0.10f, "Flat factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy."),
            new ConfigEntryDefinition("Prestige", "PrestigeRateMultiplier", 0.10f, "Flat factor by which rates are increased in expertise/legacy per increment of prestige in leveling."),
            new ConfigEntryDefinition("Prestige", "ExoPrestiging", false, "Enable or disable exo prestiges (need to max normal prestiges first, 100 exo prestiges currently available)."),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeReward", 28358550, "The reward for exo prestiging (tier 3 nether shards by default)."),
            new ConfigEntryDefinition("Prestige", "ExoPrestigeRewardQuantity", 500, "The quantity of the reward for exo prestiging."),
            new ConfigEntryDefinition("Prestige", "TrueImmortal", false, "Enable or disable Immortal blood for the duration of exoform."),
            new ConfigEntryDefinition("Expertise", "ExpertiseSystem", false, "Enable or disable the expertise system."),
            new ConfigEntryDefinition("Expertise", "MaxExpertisePrestiges", 10, "The maximum number of prestiges a player can reach in expertise."),
            new ConfigEntryDefinition("Expertise", "UnarmedSlots", false, "Enable or disable the ability to use extra unarmed spell slots."),
            new ConfigEntryDefinition("Expertise", "ShiftSlot", false, "Enable or disable using class spell on shift."),
            new ConfigEntryDefinition("Expertise", "MaxExpertiseLevel", 100, "The maximum level a player can reach in weapon expertise."),
            new ConfigEntryDefinition("Expertise", "UnitExpertiseMultiplier", 2f, "The multiplier for expertise gained from units."),
            new ConfigEntryDefinition("Expertise", "VBloodExpertiseMultiplier", 5f, "The multiplier for expertise gained from VBloods."),
            new ConfigEntryDefinition("Expertise", "UnitSpawnerExpertiseFactor", 1f, "The multiplier for experience gained from unit spawners (vermin nests, tombs)."),
            new ConfigEntryDefinition("Expertise", "ExpertiseStatChoices", 2, "The maximum number of stat choices a player can pick for a weapon expertise. Max of 3 will be sent to client UI for display."),
            new ConfigEntryDefinition("Expertise", "ResetExpertiseItem", 576389135, "Item PrefabGUID cost for resetting weapon stats."),
            new ConfigEntryDefinition("Expertise", "ResetExpertiseItemQuantity", 500, "Quantity of item required for resetting stats."),
            new ConfigEntryDefinition("Expertise", "MaxHealth", 250f, "The base cap for maximum health."),
            new ConfigEntryDefinition("Expertise", "MovementSpeed", 0.25f, "The base cap for movement speed."),
            new ConfigEntryDefinition("Expertise", "PrimaryAttackSpeed", 0.10f, "The base cap for primary attack speed."),
            new ConfigEntryDefinition("Expertise", "PhysicalLifeLeech", 0.10f, "The base cap for physical life leech."),
            new ConfigEntryDefinition("Expertise", "SpellLifeLeech", 0.10f, "The base cap for spell life leech."),
            new ConfigEntryDefinition("Expertise", "PrimaryLifeLeech", 0.15f, "The base cap for primary life leech."),
            new ConfigEntryDefinition("Expertise", "PhysicalPower", 20f, "The base cap for physical power."),
            new ConfigEntryDefinition("Expertise", "SpellPower", 10f, "The base cap for spell power."),
            new ConfigEntryDefinition("Expertise", "PhysicalCritChance", 0.10f, "The base cap for physical critical strike chance."),
            new ConfigEntryDefinition("Expertise", "PhysicalCritDamage", 0.50f, "The base cap for physical critical strike damage."),
            new ConfigEntryDefinition("Expertise", "SpellCritChance", 0.10f, "The base cap for spell critical strike chance."),
            new ConfigEntryDefinition("Expertise", "SpellCritDamage", 0.50f, "The base cap for spell critical strike damage."),
            new ConfigEntryDefinition("Legacies", "BloodSystem", false, "Enable or disable the blood legacy system."),
            new ConfigEntryDefinition("Legacies", "MaxLegacyPrestiges", 10, "The maximum number of prestiges a player can reach in blood legacies."),
            new ConfigEntryDefinition("Legacies", "MaxBloodLevel", 100, "The maximum level a player can reach in blood legacies."),
            new ConfigEntryDefinition("Legacies", "UnitLegacyMultiplier", 1f, "The multiplier for lineage gained from units."),
            new ConfigEntryDefinition("Legacies", "VBloodLegacyMultiplier", 5f, "The multiplier for lineage gained from VBloods."),
            new ConfigEntryDefinition("Legacies", "LegacyStatChoices", 2, "The maximum number of stat choices a player can pick for a blood legacy. Max of 3 will be sent to client UI for display."),
            new ConfigEntryDefinition("Legacies", "ResetLegacyItem", 576389135, "Item PrefabGUID cost for resetting blood stats."),
            new ConfigEntryDefinition("Legacies", "ResetLegacyItemQuantity", 500, "Quantity of item required for resetting blood stats."),
            new ConfigEntryDefinition("Legacies", "HealingReceived", 0.15f, "The base cap for healing received."),
            new ConfigEntryDefinition("Legacies", "DamageReduction", 0.05f, "The base cap for damage reduction."),
            new ConfigEntryDefinition("Legacies", "PhysicalResistance", 0.10f, "The base cap for physical resistance."),
            new ConfigEntryDefinition("Legacies", "SpellResistance", 0.10f, "The base cap for spell resistance."),
            new ConfigEntryDefinition("Legacies", "ResourceYield", 0.25f, "The base cap for resource yield."),
            new ConfigEntryDefinition("Legacies", "BloodDrain", 0.5f, "The base cap for blood drain reduction."),
            new ConfigEntryDefinition("Legacies", "SpellCooldownRecoveryRate", 0.10f, "The base cap for spell cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "WeaponCooldownRecoveryRate", 0.10f, "The base cap for weapon cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "UltimateCooldownRecoveryRate", 0.20f, "The base cap for ultimate cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "MinionDamage", 0.25f, "The base cap for minion damage."),
            new ConfigEntryDefinition("Legacies", "ShieldAbsorb", 0.50f, "The base cap for shield absorb."),
            new ConfigEntryDefinition("Legacies", "BloodEfficiency", 0.10f, "The base cap for blood efficiency."),
            new ConfigEntryDefinition("Professions", "ProfessionSystem", false, "Enable or disable the profession system."),
            new ConfigEntryDefinition("Professions", "ProfessionMultiplier", 10f, "The multiplier for profession experience gained."),
            new ConfigEntryDefinition("Professions", "ExtraRecipes", false, "Enable or disable extra recipes. Players will not be able to add/change shiny buffs for familiars without this unless other means of obtaining vampiric dust are provided, salvage additions are controlled by this setting as well."), // maybe this should be in general >_>
            new ConfigEntryDefinition("Familiars", "FamiliarSystem", false, "Enable or disable the familiar system."),
            new ConfigEntryDefinition("Familiars", "ShareUnlocks", false, "Enable or disable sharing unlocks between players in clans or parties (uses exp share distance)."),
            new ConfigEntryDefinition("Familiars", "FamiliarCombat", true, "Enable or disable combat for familiars."),
            new ConfigEntryDefinition("Familiars", "FamiliarPvP", true, "Enable or disable PvP participation for familiars. (if set to false, familiars will be unbound when entering PvP combat)."),
            new ConfigEntryDefinition("Familiars", "FamiliarBattles", false, "Enable or disable familiar battle system."),
            new ConfigEntryDefinition("Familiars", "FamiliarPrestige", false, "Enable or disable the prestige system for familiars."),
            new ConfigEntryDefinition("Familiars", "MaxFamiliarPrestiges", 10, "The maximum number of prestiges a familiar can reach."),
            new ConfigEntryDefinition("Familiars", "FamiliarPrestigeStatMultiplier", 0.10f, "The multiplier for applicable stats gained per familiar prestige."),
            new ConfigEntryDefinition("Familiars", "MaxFamiliarLevel", 90, "The maximum level a familiar can reach."),
            new ConfigEntryDefinition("Familiars", "AllowVBloods", false, "Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list)."),
            new ConfigEntryDefinition("Familiars", "AllowMinions", false, "Allow Minions to be unlocked as familiars (leaving these excluded by default since some have undesirable behaviour and I am not sifting through them all to correct that, enable at own risk)."),
            new ConfigEntryDefinition("Familiars", "BannedUnits", "", "The PrefabGUID hashes for units that cannot be used as familiars. Same structure as the buff lists except unit prefabs."),
            new ConfigEntryDefinition("Familiars", "BannedTypes", "", "The types of units that cannot be used as familiars go here (Human, Undead, Demon, Mechanical, Beast)."),
            new ConfigEntryDefinition("Familiars", "VBloodDamageMultiplier", 1f, "Leave at 1 for no change (controls damage familiars do to VBloods)."),
            new ConfigEntryDefinition("Familiars", "UnitFamiliarMultiplier", 7.5f, "The multiplier for experience gained from units."),
            new ConfigEntryDefinition("Familiars", "VBloodFamiliarMultiplier", 15f, "The multiplier for experience gained from VBloods."),
            new ConfigEntryDefinition("Familiars", "UnitUnlockChance", 0.05f, "The chance for a unit unlock as a familiar."),
            new ConfigEntryDefinition("Familiars", "VBloodUnlockChance", 0.01f, "The chance for a VBlood unlock as a familiar."),
            new ConfigEntryDefinition("Familiars", "PrimalEchoes", false, "Enable or disable acquiring vBloods with configured item reward from exo prestiging (default primal shards) at cost scaling to unit tier using exo reward quantity as the base (highest tier are shard bearers which cost exo reward quantity times 25, or in other words after 25 exo prestiges a player would be able to purchase a shard bearer). Must enable exo prestiging (and therefore normal prestiging), checks for banned vBloods before allowing if applicable."),
            new ConfigEntryDefinition("Familiars", "EchoesFactor", 1, "Increase to multiply costs for vBlood purchases. Valid values are integers between 1-4, if outside that range in either direction it will be clamped."),
            // new ConfigEntryDefinition("Familiars", "TraitChance", 0.2f, "The chance for a trait when unlocking familiars. Guaranteed on second unlock of same unit."),
            // new ConfigEntryDefinition("Familiars", "TraitRerollItemQuantity", 1000, "Quantity of schematics required to reroll familiar trait. It's schematics, forever, because servers never provide sinks for schematics D:<"), // actually maybe vampiricDust
            new ConfigEntryDefinition("Familiars", "ShinyChance", 0.2f, "The chance for a shiny when unlocking familiars (6 total, 1 per familiar). Guaranteed on second unlock of same unit, chance on damage dealt (same as configured onHitEffect chance) to apply spell school debuff."),
            new ConfigEntryDefinition("Familiars", "ShinyCostItemQuantity", 250, "Quantity of vampiric dust required to make a familiar shiny. May also be spent to change shiny familiar's shiny buff at 25% cost. Enable ExtraRecipes to allow player refinement of this item from Advanced Grinders. Valid values are between 100-400, if outside that range in either direction it will be clamped."),
            new ConfigEntryDefinition("Familiars", "PrestigeCostItemQuantity", 1000, "Quantity of schematics required to immediately prestige familiar (gain total levels equal to max familiar level, extra levels remaining from the amount needed to prestige will be added to familiar after prestiging). Valid values are between 500-2000, if outside that range in either direction it will be clamped."),
            new ConfigEntryDefinition("Classes", "SoftSynergies", false, "Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard syergies at the same time)."),
            new ConfigEntryDefinition("Classes", "HardSynergies", false, "Enforce class synergies (turns on classes and restricts stat choices, do not use this and soft syergies at the same time)."),
            new ConfigEntryDefinition("Classes", "ChangeClassItem", 576389135, "Item PrefabGUID cost for changing class."),
            new ConfigEntryDefinition("Classes", "ChangeClassQuantity", 750, "Quantity of item required for changing class."),
            new ConfigEntryDefinition("Classes", "ClassSpellSchoolOnHitEffects", false, "Enable or disable class spell school on hit effects (respective debuff from spell school, leech chill condemn etc)."),
            new ConfigEntryDefinition("Classes", "OnHitProcChance", 0.075f, "The chance for a class effect to proc on hit."),
            new ConfigEntryDefinition("Classes", "StatSynergyMultiplier", 1.5f, "Multiplier for class stat synergies to base stat cap."),
            new ConfigEntryDefinition("Classes", "BloodKnightWeapon", "0,3,5,6", "Blood Knight weapon synergies."),
            new ConfigEntryDefinition("Classes", "BloodKnightBlood", "1,5,7,10", "Blood Knight blood synergies."),
            new ConfigEntryDefinition("Classes", "DemonHunterWeapon", "1,2,8,9", "Demon Hunter weapon synergies."),
            new ConfigEntryDefinition("Classes", "DemonHunterBlood", "2,5,7,9", "Demon Hunter blood synergies"),
            new ConfigEntryDefinition("Classes", "VampireLordWeapon", "0,4,6,7", "Vampire Lord weapon synergies."),
            new ConfigEntryDefinition("Classes", "VampireLordBlood", "1,3,8,11", "Vampire Lord blood synergies."),
            new ConfigEntryDefinition("Classes", "ShadowBladeWeapon", "1,2,6,9", "Shadow Blade weapon synergies."),
            new ConfigEntryDefinition("Classes", "ShadowBladeBlood", "3,5,7,10", "Shadow Blade blood synergies."),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererWeapon", "4,7,10,11", "Arcane Sorcerer weapon synergies."),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererBlood", "0,6,8,10", "Arcane Sorcerer blood synergies."),
            new ConfigEntryDefinition("Classes", "DeathMageWeapon", "0,4,7,11", "Death Mage weapon synergies."),
            new ConfigEntryDefinition("Classes", "DeathMageBlood", "2,3,6,9", "Death Mage blood synergies."),
            new ConfigEntryDefinition("Classes", "DefaultClassSpell", -433204738, "Default spell (veil of shadow) available to all classes."),
            new ConfigEntryDefinition("Classes", "BloodKnightBuffs", "1828387635,-534491790,-1055766373,-584203677", "The PrefabGUID hashes for blood knight leveling blood buffs. Granted every MaxLevel/(# of blood buffs)."),
            new ConfigEntryDefinition("Classes", "BloodKnightSpells", "-880131926,651613264,2067760264,189403977,375131842", "Blood Knight shift spells, granted at levels of prestige."),
            new ConfigEntryDefinition("Classes", "DemonHunterBuffs", "-154702686,-285745649,-1510965956,-397097531", "The PrefabGUID hashes for demon hunter leveling blood buffs."),
            new ConfigEntryDefinition("Classes", "DemonHunterSpells", "-356990326,-987810170,1071205195,1249925269,-914344112", "Demon Hunter shift spells, granted at levels of prestige."),
            new ConfigEntryDefinition("Classes", "VampireLordBuffs", "1558171501,997154800,-1413561088,1103099361", "The PrefabGUID hashes for vampire lord leveling blood buffs."),
            new ConfigEntryDefinition("Classes", "VampireLordSpells", "78384915,295045820,-1000260252,91249849,1966330719", "Vampire Lord shift spells, granted at levels of prestige."),
            new ConfigEntryDefinition("Classes", "ShadowBladeBuffs", "894725875,-1596803256,-993492354,210193036", "The PrefabGUID hashes for shadow blade leveling blood buffs."),
            new ConfigEntryDefinition("Classes", "ShadowBladeSpells", "1019568127,1575317901,1112116762,-358319417,1174831223", "Shadow Blade shift spells, granted at levels of prestige."),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererBuffs", "1614027598,884683323,-1576592687,-1859298707", "The PrefabGUID hashes for arcane leveling blood buffs."),
            new ConfigEntryDefinition("Classes", "ArcaneSorcererSpells", "247896794,268059675,-242769430,-2053450457,1650878435", "Arcane Sorcerer shift spells, granted at levels of prestige."),
            new ConfigEntryDefinition("Classes", "DeathMageBuffs", "-901503997,-804597757,1934870645,1201299233", "The PrefabGUID hashes for death mage leveling blood buffs."),
            new ConfigEntryDefinition("Classes", "DeathMageSpells", "-1204819086,481411985,1961570821,2138402840,-1781779733", "Death Mage shift spells, granted at levels of prestige.")
        ];
        public static void InitializeConfig()
        {
            foreach (string path in DirectoryPaths)
            {
                CreateDirectory(path);
            }

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
                        object convertedValue;

                        if (entryType == typeof(float))
                        {
                            convertedValue = float.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else if (entryType == typeof(double))
                        {
                            convertedValue = double.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else if (entryType == typeof(decimal))
                        {
                            convertedValue = decimal.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(oldValue, entryType);
                        }

                        var configEntry = generic.Invoke(null, [entry.Section, entry.Key, convertedValue, entry.Description]);
                        UpdateConfigProperty(entry.Key, configEntry);

                        object valueProp = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);
                        if (valueProp != null)
                        {
                            FinalConfigValues[entry.Key] = valueProp;
                        }
                        else
                        {
                            Core.Log.LogError($"Failed to get value property for {entry.Key}");
                        }
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

                    object valueProp = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);
                    if (valueProp != null)
                    {
                        FinalConfigValues[entry.Key] = valueProp;
                    }
                    else
                    {
                        Core.Log.LogError($"Failed to get value property for {entry.Key}");
                    }
                }
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
                                object convertedValue;

                                Type t = typeof(T);

                                if (t == typeof(float))
                                {
                                    convertedValue = float.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(double))
                                {
                                    convertedValue = double.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(decimal))
                                {
                                    convertedValue = decimal.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(int))
                                {
                                    convertedValue = int.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(uint))
                                {
                                    convertedValue = uint.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(long))
                                {
                                    convertedValue = long.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(ulong))
                                {
                                    convertedValue = ulong.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(short))
                                {
                                    convertedValue = short.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(ushort))
                                {
                                    convertedValue = ushort.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(bool))
                                {
                                    convertedValue = bool.Parse(configValue);
                                }
                                else if (t == typeof(string))
                                {
                                    convertedValue = configValue;
                                }
                                else
                                {
                                    // Handle other types or throw an exception
                                    throw new NotSupportedException($"Type {t} is not supported");
                                }

                                entry.Value = (T)convertedValue;
                            }
                            catch (Exception ex)
                            {
                                Plugin.LogInstance.LogError($"Failed to convert config value for {key}: {ex.Message}");
                            }

                            break;
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
                var match = _regex.Match(trimmedLine);

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
    static T GetConfigValue<T>(string key)
    {
        if (ConfigInitialization.FinalConfigValues.TryGetValue(key, out var val))
        {
            return (T)Convert.ChangeType(val, typeof(T));
        }
        else
        {
            Core.Log.LogInfo($"Using default config value...");
        }

        var entry = ConfigInitialization.ConfigEntries.FirstOrDefault(e => e.Key == key);
        return entry == null ? throw new InvalidOperationException($"Config entry for key '{key}' not found.") : (T)entry.DefaultValue;
    }
}
