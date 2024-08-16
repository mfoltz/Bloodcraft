namespace Bloodcraft.Services;

public class ConfigService
{
    readonly string _languageLocalization = Plugin.LanguageLocalization.Value;
    public string LanguageLocalization => _languageLocalization;

    readonly bool _clientCompanion = Plugin.ClientCompanion.Value;
    public bool ClientCompanion => _clientCompanion;

    readonly bool _eliteShardBearers = Plugin.EliteShardBearers.Value;
    public bool EliteShardBearers => _eliteShardBearers;

    readonly bool _potionStacking = Plugin.PotionStacking.Value;
    public bool PotionStacking => _potionStacking;

    readonly bool _starterKit = Plugin.StarterKit.Value;
    public bool StarterKit => _starterKit;

    readonly string _kitPrefabs = Plugin.KitPrefabs.Value;
    public string KitPrefabs => _kitPrefabs;

    readonly string _kitQuantities = Plugin.KitQuantities.Value;
    public string KitQuantities => _kitQuantities;

    readonly bool _questSystem = Plugin.QuestSystem.Value;
    public bool QuestSystem => _questSystem;

    readonly bool _infiniteDailies = Plugin.InfiniteDailies.Value;
    public bool InfiniteDailies => _infiniteDailies;

    readonly string _questRewards = Plugin.QuestRewards.Value;
    public string QuestRewards => _questRewards;

    readonly string _questRewardAmounts = Plugin.QuestRewardAmounts.Value;
    public string QuestRewardAmounts => _questRewardAmounts;

    readonly bool _levelingSystem = Plugin.LevelingSystem.Value;
    public bool LevelingSystem => _levelingSystem;

    readonly bool _restedXP = Plugin.RestedXP.Value;
    public bool RestedXP => _restedXP;

    readonly float _restedXPRate = Plugin.RestedXPRate.Value;
    public float RestedXPRate => _restedXPRate;

    readonly float _restedXPMaxMultiplier = Plugin.RestedXPMaxMultiplier.Value;
    public float RestedXPMaxMultiplier => _restedXPMaxMultiplier;

    readonly float _restedXPTickRate = Plugin.RestedXPTickRate.Value;
    public float RestedXPTickRate => _restedXPTickRate;

    readonly bool _prestigeSystem = Plugin.PrestigeSystem.Value;
    public bool PrestigeSystem => _prestigeSystem;

    readonly bool _exoPrestiging = Plugin.ExoPrestiging.Value;
    public bool ExoPrestiging => _exoPrestiging;

    readonly int _exoPrestiges = Plugin.ExoPrestiges.Value;
    public int ExoPrestiges => _exoPrestiges;

    readonly int _exoPrestigeReward = Plugin.ExoPrestigeReward.Value;
    public int ExoPrestigeReward => _exoPrestigeReward;

    readonly int _exoPrestigeRewardQuantity = Plugin.ExoPrestigeRewardQuantity.Value;
    public int ExoPrestigeRewardQuantity => _exoPrestigeRewardQuantity;

    readonly float _exoPrestigeDamageTakenMultiplier = Plugin.ExoPrestigeDamageTakenMultiplier.Value;
    public float ExoPrestigeDamageTakenMultiplier => _exoPrestigeDamageTakenMultiplier;

    readonly float _exoPrestigeDamageDealtMultiplier = Plugin.ExoPrestigeDamageDealtMultiplier.Value;
    public float ExoPrestigeDamageDealtMultiplier => _exoPrestigeDamageDealtMultiplier;

    readonly string _prestigeBuffs = Plugin.PrestigeBuffs.Value;
    public string PrestigeBuffs => _prestigeBuffs;

    readonly string _prestigeLevelsToUnlockClassSpells = Plugin.PrestigeLevelsToUnlockClassSpells.Value;
    public string PrestigeLevelsToUnlockClassSpells => _prestigeLevelsToUnlockClassSpells;

    readonly string _bloodKnightBuffs = Plugin.BloodKnightBuffs.Value;
    public string BloodKnightBuffs => _bloodKnightBuffs;

    readonly string _demonHunterBuffs = Plugin.DemonHunterBuffs.Value;
    public string DemonHunterBuffs => _demonHunterBuffs;

    readonly string _vampireLordBuffs = Plugin.VampireLordBuffs.Value;
    public string VampireLordBuffs => _vampireLordBuffs;

    readonly string _shadowBladeBuffs = Plugin.ShadowBladeBuffs.Value;
    public string ShadowBladeBuffs => _shadowBladeBuffs;

    readonly string _arcaneSorcererBuffs = Plugin.ArcaneSorcererBuffs.Value;
    public string ArcaneSorcererBuffs => _arcaneSorcererBuffs;

    readonly string _deathMageBuffs = Plugin.DeathMageBuffs.Value;
    public string DeathMageBuffs => _deathMageBuffs;

    readonly int _maxLevelingPrestiges = Plugin.MaxLevelingPrestiges.Value;
    public int MaxLevelingPrestiges => _maxLevelingPrestiges;

    readonly float _levelingPrestigeReducer = Plugin.LevelingPrestigeReducer.Value;
    public float LevelingPrestigeReducer => _levelingPrestigeReducer;

    readonly float _prestigeRatesReducer = Plugin.PrestigeRatesReducer.Value;
    public float PrestigeRatesReducer => _prestigeRatesReducer;

    readonly float _prestigeStatMultiplier = Plugin.PrestigeStatMultiplier.Value;
    public float PrestigeStatMultiplier => _prestigeStatMultiplier;

    readonly float _prestigeRatesMultiplier = Plugin.PrestigeRatesMultiplier.Value;
    public float PrestigeRatesMultiplier => _prestigeRatesMultiplier;

    readonly int _maxPlayerLevel = Plugin.MaxPlayerLevel.Value;
    public int MaxPlayerLevel => _maxPlayerLevel;

    readonly int _startingLevel = Plugin.StartingLevel.Value;
    public int StartingLevel => _startingLevel;

    readonly float _unitLevelingMultiplier = Plugin.UnitLevelingMultiplier.Value;
    public float UnitLevelingMultiplier => _unitLevelingMultiplier;

    readonly float _vBloodLevelingMultiplier = Plugin.VBloodLevelingMultiplier.Value;
    public float VBloodLevelingMultiplier => _vBloodLevelingMultiplier;

    readonly float _groupLevelingMultiplier = Plugin.GroupLevelingMultiplier.Value;
    public float GroupLevelingMultiplier => _groupLevelingMultiplier;

    readonly float _levelScalingMultiplier = Plugin.LevelScalingMultiplier.Value;
    public float LevelScalingMultiplier => _levelScalingMultiplier;

    readonly int _maxPartySize = Plugin.MaxPartySize.Value;
    public int MaxPartySize => _maxPartySize;

    readonly float _expShareDistance = Plugin.ExpShareDistance.Value;
    public float ExpShareDistance => _expShareDistance;

    readonly bool _parties = Plugin.Parties.Value;
    public bool Parties => _parties;

    readonly bool _preventFriendlyFire = Plugin.PreventFriendlyFire.Value;
    public bool PreventFriendlyFire => _preventFriendlyFire;

    readonly float _docileUnitMultiplier = Plugin.DocileUnitMultiplier.Value;
    public float DocileUnitMultiplier => _docileUnitMultiplier;

    readonly float _warEventMultiplier = Plugin.WarEventMultiplier.Value;
    public float WarEventMultiplier => _warEventMultiplier;

    readonly float _unitSpawnerMultiplier = Plugin.UnitSpawnerMultiplier.Value;
    public float UnitSpawnerMultiplier => _unitSpawnerMultiplier;

    readonly int _changeClassItem = Plugin.ChangeClassItem.Value;
    public int ChangeClassItem => _changeClassItem;

    readonly int _changeClassItemQuantity = Plugin.ChangeClassItemQuantity.Value;
    public int ChangeClassItemQuantity => _changeClassItemQuantity;

    readonly bool _expertiseSystem = Plugin.ExpertiseSystem.Value;
    public bool ExpertiseSystem => _expertiseSystem;

    readonly int _maxExpertisePrestiges = Plugin.MaxExpertisePrestiges.Value;
    public int MaxExpertisePrestiges => _maxExpertisePrestiges;

    readonly bool _unarmedSlots = Plugin.UnarmedSlots.Value;
    public bool UnarmedSlots => _unarmedSlots;

    readonly bool _shiftSlot = Plugin.ShiftSlot.Value;
    public bool ShiftSlot => _shiftSlot;

    readonly int _maxExpertiseLevel = Plugin.MaxExpertiseLevel.Value;
    public int MaxExpertiseLevel => _maxExpertiseLevel;

    readonly float _unitExpertiseMultiplier = Plugin.UnitExpertiseMultiplier.Value;
    public float UnitExpertiseMultiplier => _unitExpertiseMultiplier;

    readonly float _vBloodExpertiseMultiplier = Plugin.VBloodExpertiseMultiplier.Value;
    public float VBloodExpertiseMultiplier => _vBloodExpertiseMultiplier;

    readonly int _expertiseStatChoices = Plugin.ExpertiseStatChoices.Value;
    public int ExpertiseStatChoices => _expertiseStatChoices;

    readonly int _resetExpertiseItem = Plugin.ResetExpertiseItem.Value;
    public int ResetExpertiseItem => _resetExpertiseItem;

    readonly int _resetExpertiseItemQuantity = Plugin.ResetExpertiseItemQuantity.Value;
    public int ResetExpertiseItemQuantity => _resetExpertiseItemQuantity;

    readonly float _maxHealth = Plugin.MaxHealth.Value;
    public float MaxHealth => _maxHealth;

    readonly float _movementSpeed = Plugin.MovementSpeed.Value;
    public float MovementSpeed => _movementSpeed;

    readonly float _primaryAttackSpeed = Plugin.PrimaryAttackSpeed.Value;
    public float PrimaryAttackSpeed => _primaryAttackSpeed;

    readonly float _physicalLifeLeech = Plugin.PhysicalLifeLeech.Value;
    public float PhysicalLifeLeech => _physicalLifeLeech;

    readonly float _spellLifeLeech = Plugin.SpellLifeLeech.Value;
    public float SpellLifeLeech => _spellLifeLeech;

    readonly float _primaryLifeLeech = Plugin.PrimaryLifeLeech.Value;
    public float PrimaryLifeLeech => _primaryLifeLeech;

    readonly float _physicalPower = Plugin.PhysicalPower.Value;
    public float PhysicalPower => _physicalPower;

    readonly float _spellPower = Plugin.SpellPower.Value;
    public float SpellPower => _spellPower;

    readonly float _physicalCritChance = Plugin.PhysicalCritChance.Value;
    public float PhysicalCritChance => _physicalCritChance;

    readonly float _physicalCritDamage = Plugin.PhysicalCritDamage.Value;
    public float PhysicalCritDamage => _physicalCritDamage;

    readonly float _spellCritChance = Plugin.SpellCritChance.Value;
    public float SpellCritChance => _spellCritChance;

    readonly float _spellCritDamage = Plugin.SpellCritDamage.Value;
    public float SpellCritDamage => _spellCritDamage;

    readonly bool _bloodSystem = Plugin.BloodSystem.Value;
    public bool BloodSystem => _bloodSystem;

    readonly int _maxLegacyPrestiges = Plugin.MaxLegacyPrestiges.Value;
    public int MaxLegacyPrestiges => _maxLegacyPrestiges;

    readonly bool _bloodQualityBonus = Plugin.BloodQualityBonus.Value;
    public bool BloodQualityBonus => _bloodQualityBonus;

    readonly float _prestigeBloodQuality = Plugin.PrestigeBloodQuality.Value;
    public float PrestigeBloodQuality => _prestigeBloodQuality;

    readonly int _maxBloodLevel = Plugin.MaxBloodLevel.Value;
    public int MaxBloodLevel => _maxBloodLevel;

    readonly float _unitLegacyMultiplier = Plugin.UnitLegacyMultiplier.Value;
    public float UnitLegacyMultiplier => _unitLegacyMultiplier;

    readonly float _vBloodLegacyMultiplier = Plugin.VBloodLegacyMultipler.Value;
    public float VBloodLegacyMultiplier => _vBloodLegacyMultiplier;

    readonly int _legacyStatChoices = Plugin.LegacyStatChoices.Value;
    public int LegacyStatChoices => _legacyStatChoices;

    readonly int _resetLegacyItem = Plugin.ResetLegacyItem.Value;
    public int ResetLegacyItem => _resetLegacyItem;

    readonly int _resetLegacyItemQuantity = Plugin.ResetLegacyItemQuantity.Value;
    public int ResetLegacyItemQuantity => _resetLegacyItemQuantity;

    readonly float _healingReceived = Plugin.HealingReceived.Value;
    public float HealingReceived => _healingReceived;

    readonly float _damageReduction = Plugin.DamageReduction.Value;
    public float DamageReduction => _damageReduction;

    readonly float _physicalResistance = Plugin.PhysicalResistance.Value;
    public float PhysicalResistance => _physicalResistance;

    readonly float _spellResistance = Plugin.SpellResistance.Value;
    public float SpellResistance => _spellResistance;

    readonly float _resourceYield = Plugin.ResourceYield.Value;
    public float ResourceYield => _resourceYield;

    readonly float _ccReduction = Plugin.CCReduction.Value;
    public float CCReduction => _ccReduction;

    readonly float _spellCooldownRecoveryRate = Plugin.SpellCooldownRecoveryRate.Value;
    public float SpellCooldownRecoveryRate => _spellCooldownRecoveryRate;

    readonly float _weaponCooldownRecoveryRate = Plugin.WeaponCooldownRecoveryRate.Value;
    public float WeaponCooldownRecoveryRate => _weaponCooldownRecoveryRate;

    readonly float _ultimateCooldownRecoveryRate = Plugin.UltimateCooldownRecoveryRate.Value;
    public float UltimateCooldownRecoveryRate => _ultimateCooldownRecoveryRate;

    readonly float _minionDamage = Plugin.MinionDamage.Value;
    public float MinionDamage => _minionDamage;

    readonly float _shieldAbsorb = Plugin.ShieldAbsorb.Value;
    public float ShieldAbsorb => _shieldAbsorb;

    readonly float _bloodEfficiency = Plugin.BloodEfficiency.Value;
    public float BloodEfficiency => _bloodEfficiency;

    readonly bool _professionSystem = Plugin.ProfessionSystem.Value;
    public bool ProfessionSystem => _professionSystem;

    readonly int _maxProfessionLevel = Plugin.MaxProfessionLevel.Value;
    public int MaxProfessionLevel => _maxProfessionLevel;

    readonly float _professionMultiplier = Plugin.ProfessionMultiplier.Value;
    public float ProfessionMultiplier => _professionMultiplier;

    readonly bool _extraRecipes = Plugin.ExtraRecipes.Value;
    public bool ExtraRecipes => _extraRecipes;

    readonly bool _familiarSystem = Plugin.FamiliarSystem.Value;
    public bool FamiliarSystem => _familiarSystem;

    readonly bool _shareUnlocks = Plugin.ShareUnlocks.Value;
    public bool ShareUnlocks => _shareUnlocks;

    readonly bool _familiarCombat = Plugin.FamiliarCombat.Value;
    public bool FamiliarCombat => _familiarCombat;

    readonly bool _familiarPrestige = Plugin.FamiliarPrestige.Value;
    public bool FamiliarPrestige => _familiarPrestige;

    readonly int _maxFamiliarPrestiges = Plugin.MaxFamiliarPrestiges.Value;
    public int MaxFamiliarPrestiges => _maxFamiliarPrestiges;

    readonly float _familiarPrestigeStatMultiplier = Plugin.FamiliarPrestigeStatMultiplier.Value;
    public float FamiliarPrestigeStatMultiplier => _familiarPrestigeStatMultiplier;

    readonly int _maxFamiliarLevel = Plugin.MaxFamiliarLevel.Value;
    public int MaxFamiliarLevel => _maxFamiliarLevel;

    readonly bool _allowVBloods = Plugin.AllowVBloods.Value;
    public bool AllowVBloods => _allowVBloods;

    readonly string _bannedUnits = Plugin.BannedUnits.Value;
    public string BannedUnits => _bannedUnits;

    readonly string _bannedTypes = Plugin.BannedTypes.Value;
    public string BannedTypes => _bannedTypes;

    readonly float _unitFamiliarMultiplier = Plugin.UnitFamiliarMultiplier.Value;
    public float UnitFamiliarMultiplier => _unitFamiliarMultiplier;

    readonly float _vBloodFamiliarMultiplier = Plugin.VBloodFamiliarMultiplier.Value;
    public float VBloodFamiliarMultiplier => _vBloodFamiliarMultiplier;

    readonly float _unitUnlockChance = Plugin.UnitUnlockChance.Value;
    public float UnitUnlockChance => _unitUnlockChance;

    readonly float _vBloodUnlockChance = Plugin.VBloodUnlockChance.Value;
    public float VBloodUnlockChance => _vBloodUnlockChance;

    readonly float _shinyChance = Plugin.ShinyChance.Value;
    public float ShinyChance => _shinyChance;

    readonly int _shinyCostItemPrefab = Plugin.ShinyCostItemPrefab.Value;
    public int ShinyCostItemPrefab => _shinyCostItemPrefab;

    readonly int _shinyCostItemQuantity = Plugin.ShinyCostItemQuantity.Value;
    public int ShinyCostItemQuantity => _shinyCostItemQuantity;

    readonly float _vBloodDamageMultiplier = Plugin.VBloodDamageMultiplier.Value;
    public float VBloodDamageMultiplier => _vBloodDamageMultiplier;

    readonly float _playerVampireDamageMultiplier = Plugin.PlayerVampireDamageMultiplier.Value;
    public float PlayerVampireDamageMultiplier => _playerVampireDamageMultiplier;

    readonly bool _softSynergies = Plugin.SoftSynergies.Value;
    public bool SoftSynergies => _softSynergies;

    readonly bool _hardSynergies = Plugin.HardSynergies.Value;
    public bool HardSynergies => _hardSynergies;

    readonly bool _classSpellSchoolOnHitEffects = Plugin.ClassSpellSchoolOnHitEffects.Value;
    public bool ClassSpellSchoolOnHitEffects => _classSpellSchoolOnHitEffects;

    readonly float _onHitProcChance = Plugin.OnHitProcChance.Value;
    public float OnHitProcChance => _onHitProcChance;

    readonly float _statSynergyMultiplier = Plugin.StatSynergyMultiplier.Value;
    public float StatSynergyMultiplier => _statSynergyMultiplier;

    readonly string _bloodKnightWeapon = Plugin.BloodKnightWeapon.Value;
    public string BloodKnightWeapon => _bloodKnightWeapon;

    readonly string _bloodKnightBlood = Plugin.BloodKnightBlood.Value;
    public string BloodKnightBlood => _bloodKnightBlood;

    readonly string _bloodKnightSpells = Plugin.BloodKnightSpells.Value;
    public string BloodKnightSpells => _bloodKnightSpells;

    readonly string _demonHunterWeapon = Plugin.DemonHunterWeapon.Value;
    public string DemonHunterWeapon => _demonHunterWeapon;

    readonly string _demonHunterBlood = Plugin.DemonHunterBlood.Value;
    public string DemonHunterBlood => _demonHunterBlood;

    readonly string _demonHunterSpells = Plugin.DemonHunterSpells.Value;
    public string DemonHunterSpells => _demonHunterSpells;

    readonly string _vampireLordWeapon = Plugin.VampireLordWeapon.Value;
    public string VampireLordWeapon => _vampireLordWeapon;

    readonly string _vampireLordBlood = Plugin.VampireLordBlood.Value;
    public string VampireLordBlood => _vampireLordBlood;

    readonly string _vampireLordSpells = Plugin.VampireLordSpells.Value;
    public string VampireLordSpells => _vampireLordSpells;

    readonly string _shadowBladeWeapon = Plugin.ShadowBladeWeapon.Value;
    public string ShadowBladeWeapon => _shadowBladeWeapon;

    readonly string _shadowBladeBlood = Plugin.ShadowBladeBlood.Value;
    public string ShadowBladeBlood => _shadowBladeBlood;

    readonly string _shadowBladeSpells = Plugin.ShadowBladeSpells.Value;
    public string ShadowBladeSpells => _shadowBladeSpells;

    readonly string _arcaneSorcererWeapon = Plugin.ArcaneSorcererWeapon.Value;
    public string ArcaneSorcererWeapon => _arcaneSorcererWeapon;

    readonly string _arcaneSorcererBlood = Plugin.ArcaneSorcererBlood.Value;
    public string ArcaneSorcererBlood => _arcaneSorcererBlood;

    readonly string _arcaneSorcererSpells = Plugin.ArcaneSorcererSpells.Value;
    public string ArcaneSorcererSpells => _arcaneSorcererSpells;

    readonly string _deathMageWeapon = Plugin.DeathMageWeapon.Value;
    public string DeathMageWeapon => _deathMageWeapon;

    readonly string _deathMageBlood = Plugin.DeathMageBlood.Value;
    public string DeathMageBlood => _deathMageBlood;

    readonly string _deathMageSpells = Plugin.DeathMageSpells.Value;
    public string DeathMageSpells => _deathMageSpells;

    // Few bool combinations that are handy to have in one place
    public bool ClassesInactive => !_softSynergies && !_hardSynergies;
    public bool Classes => _softSynergies || _hardSynergies;
}
