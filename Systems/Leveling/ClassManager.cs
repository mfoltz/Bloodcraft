using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;

namespace Bloodcraft.Systems.Leveling;
internal static class ClassManager
{
    public enum PlayerClass
    {
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }
    public enum ClassStatType : int
    {
        PhysicalPower = 0,
        ResourcePower = 1,
        SiegePower = 2,
        ResourceYield = 3,
        MaxHealth = 4,
        MovementSpeed = 5,
        CooldownRecoveryRate = 7,
        PhysicalResistance = 8,
        FireResistance = 9,
        HolyResistance = 10,
        SilverResistance = 11,
        SunChargeTime = 12,
        SunResistance = 19,
        GarlicResistance = 20,
        Vision = 22,
        SpellResistance = 23,
        Radial_SpellResistance = 24,
        SpellPower = 25,
        PassiveHealthRegen = 26,
        PhysicalLifeLeech = 27,
        SpellLifeLeech = 28,
        PhysicalCriticalStrikeChance = 29,
        PhysicalCriticalStrikeDamage = 30,
        SpellCriticalStrikeChance = 31,
        SpellCriticalStrikeDamage = 32,
        AbilityAttackSpeed = 33,
        DamageVsUndeads = 38,
        DamageVsHumans = 39,
        DamageVsDemons = 40,
        DamageVsMechanical = 41,
        DamageVsBeasts = 42,
        DamageVsCastleObjects = 43,
        DamageVsVampires = 44,
        ResistVsUndeads = 45,
        ResistVsHumans = 46,
        ResistVsDemons = 47,
        ResistVsMechanical = 48,
        ResistVsBeasts = 49,
        ResistVsCastleObjects = 50,
        ResistVsVampires = 51,
        DamageVsWood = 52,
        DamageVsMineral = 53,
        DamageVsVegetation = 54,
        DamageVsLightArmor = 55,
        DamageVsVBloods = 56,
        DamageVsMagic = 57,
        ReducedResourceDurabilityLoss = 58,
        PrimaryAttackSpeed = 59,
        ImmuneToHazards = 60,
        PrimaryLifeLeech = 61,
        HealthRecovery = 62,
        PrimaryCooldownModifier = 63,
        FallGravity = 64,
        PvPResilience = 65,
        BloodDrain = 66,
        BonusPhysicalPower = 67,
        BonusSpellPower = 68,
        CCReduction = 69,
        SpellCooldownRecoveryRate = 70,
        WeaponCooldownRecoveryRate = 71,
        UltimateCooldownRecoveryRate = 72,
        MinionDamage = 73,
        DamageReduction = 74,
        HealingReceived = 75,
        IncreasedShieldEfficiency = 76,
        BloodEfficiency = 77,
        InventorySlots = 78,
        SilverCoinResistance = 79,
        TravelCooldownRecoveryRate = 80,
        ReducedBloodDrain = 81,
        BonusMaxHealth = 82,
        BonusMovementSpeed = 83,
        BonusShapeshiftMovementSpeed = 84,
        BonusMountMovementSpeed = 85,
        UltimateEfficiency = 86,
        SpellFreeCast = 88,
        WeaponFreeCast = 89,
        WeaponSkillPower = 90,
        FeedCooldownRecoveryRate = 91,
        BloodMendHealEfficiency = 92,
        DemountProtection = 93,
        BloodDrainMultiplier = 94,
        CorruptionDamageReduction = 95
    }
    public class ClassOnHitSettings
    {
        public class OnHitEffects(PrefabGUID primary, PrefabGUID secondary, bool isDebuff)
        {
            public PrefabGUID Primary { get; } = primary;
            public PrefabGUID Secondary { get; } = secondary;
            public bool IsDebuff { get; } = isDebuff;
            public void ApplyEffect(Entity source, Entity target)
            {
                bool hasPrimary = target.HasBuff(Primary);

                if (!hasPrimary) target.TryApplyBuffWithOwner(source, Primary);
                else if (IsDebuff) target.TryApplyBuffWithOwner(source, Secondary);
                else source.TryApplyBuff(Secondary);
            }
        }
        public static IReadOnlyDictionary<PlayerClass, OnHitEffects> ClassOnDamageEffects => _classOnDamageEffects;
        static readonly Dictionary<PlayerClass, OnHitEffects> _classOnDamageEffects = new()
        {
            { PlayerClass.BloodKnight, new(Buffs.VampireLeechDebuff, Buffs.BloodCurseBuff, true) },
            { PlayerClass.DemonHunter, new(Buffs.VampireStaticDebuff, Buffs.StormChargeBuff, false) },
            { PlayerClass.VampireLord, new(Buffs.VampireChillDebuff, Buffs.FrostWeaponBuff, false) },
            { PlayerClass.ShadowBlade, new(Buffs.VampireIgniteDebuff, Buffs.ChaosHeatedBuff, false) },
            { PlayerClass.ArcaneSorcerer, new(Buffs.VampireWeakenDebuff, Buffs.IllusionShieldBuff, false) },
            { PlayerClass.DeathMage, new(Buffs.VampireCondemnDebuff, Buffs.UnholyAmplifyBuff, true) },
        };
    }
    public static IReadOnlyDictionary<ClassStatType, UnitStatType> ClassStatTypes => _classStatTypes;
    static readonly Dictionary<ClassStatType, UnitStatType> _classStatTypes = new()
    {
        { ClassStatType.PhysicalPower, UnitStatType.PhysicalPower },
        { ClassStatType.ResourcePower, UnitStatType.ResourcePower },
        { ClassStatType.SiegePower, UnitStatType.SiegePower },
        { ClassStatType.ResourceYield, UnitStatType.ResourceYield },
        { ClassStatType.MaxHealth, UnitStatType.MaxHealth },
        { ClassStatType.MovementSpeed, UnitStatType.MovementSpeed },
        { ClassStatType.CooldownRecoveryRate, UnitStatType.CooldownRecoveryRate },
        { ClassStatType.PhysicalResistance, UnitStatType.PhysicalResistance },
        { ClassStatType.FireResistance, UnitStatType.FireResistance },
        { ClassStatType.HolyResistance, UnitStatType.HolyResistance },
        { ClassStatType.SilverResistance, UnitStatType.SilverResistance },
        { ClassStatType.SpellResistance, UnitStatType.SpellResistance },
        { ClassStatType.Radial_SpellResistance, UnitStatType.Radial_SpellResistance },
        { ClassStatType.SpellPower, UnitStatType.SpellPower },
        { ClassStatType.PassiveHealthRegen, UnitStatType.PassiveHealthRegen },
        { ClassStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech },
        { ClassStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech },
        { ClassStatType.PhysicalCriticalStrikeChance, UnitStatType.PhysicalCriticalStrikeChance },
        { ClassStatType.PhysicalCriticalStrikeDamage, UnitStatType.PhysicalCriticalStrikeDamage },
        { ClassStatType.SpellCriticalStrikeChance, UnitStatType.SpellCriticalStrikeChance },
        { ClassStatType.SpellCriticalStrikeDamage, UnitStatType.SpellCriticalStrikeDamage },
        { ClassStatType.AbilityAttackSpeed, UnitStatType.AbilityAttackSpeed },
        { ClassStatType.DamageVsUndeads, UnitStatType.DamageVsUndeads },
        { ClassStatType.DamageVsHumans, UnitStatType.DamageVsHumans },
        { ClassStatType.DamageVsDemons, UnitStatType.DamageVsDemons },
        { ClassStatType.DamageVsMechanical, UnitStatType.DamageVsMechanical },
        { ClassStatType.DamageVsBeasts, UnitStatType.DamageVsBeasts },
        { ClassStatType.DamageVsCastleObjects, UnitStatType.DamageVsCastleObjects },
        { ClassStatType.DamageVsVampires, UnitStatType.DamageVsVampires },
        { ClassStatType.ResistVsUndeads, UnitStatType.ResistVsUndeads },
        { ClassStatType.ResistVsHumans, UnitStatType.ResistVsHumans },
        { ClassStatType.ResistVsDemons, UnitStatType.ResistVsDemons },
        { ClassStatType.ResistVsMechanical, UnitStatType.ResistVsMechanical },
        { ClassStatType.ResistVsBeasts, UnitStatType.ResistVsBeasts },
        { ClassStatType.ResistVsCastleObjects, UnitStatType.ResistVsCastleObjects },
        { ClassStatType.ResistVsVampires, UnitStatType.ResistVsVampires },
        { ClassStatType.DamageVsWood, UnitStatType.DamageVsWood },
        { ClassStatType.DamageVsMineral, UnitStatType.DamageVsMineral },
        { ClassStatType.DamageVsVegetation, UnitStatType.DamageVsVegetation },
        { ClassStatType.DamageVsLightArmor, UnitStatType.DamageVsLightArmor },
        { ClassStatType.DamageVsVBloods, UnitStatType.DamageVsVBloods },
        { ClassStatType.DamageVsMagic, UnitStatType.DamageVsMagic },
        { ClassStatType.ReducedResourceDurabilityLoss, UnitStatType.ReducedResourceDurabilityLoss },
        { ClassStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed },
        { ClassStatType.ImmuneToHazards, UnitStatType.ImmuneToHazards },
        { ClassStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech },
        { ClassStatType.HealthRecovery, UnitStatType.HealthRecovery },
        { ClassStatType.PrimaryCooldownModifier, UnitStatType.PrimaryCooldownModifier },
        { ClassStatType.FallGravity, UnitStatType.FallGravity },
        { ClassStatType.PvPResilience, UnitStatType.PvPResilience },
        { ClassStatType.BloodDrain, UnitStatType.BloodDrain },
        { ClassStatType.BonusPhysicalPower, UnitStatType.BonusPhysicalPower },
        { ClassStatType.BonusSpellPower, UnitStatType.BonusSpellPower },
        { ClassStatType.CCReduction, UnitStatType.CCReduction },
        { ClassStatType.SpellCooldownRecoveryRate, UnitStatType.SpellCooldownRecoveryRate },
        { ClassStatType.WeaponCooldownRecoveryRate, UnitStatType.WeaponCooldownRecoveryRate },
        { ClassStatType.UltimateCooldownRecoveryRate, UnitStatType.UltimateCooldownRecoveryRate },
        { ClassStatType.MinionDamage, UnitStatType.MinionDamage },
        { ClassStatType.DamageReduction, UnitStatType.DamageReduction },
        { ClassStatType.HealingReceived, UnitStatType.HealingReceived },
        { ClassStatType.IncreasedShieldEfficiency, UnitStatType.IncreasedShieldEfficiency },
        { ClassStatType.BloodEfficiency, UnitStatType.BloodEfficiency },
        { ClassStatType.InventorySlots, UnitStatType.InventorySlots },
        { ClassStatType.SilverCoinResistance, UnitStatType.SilverCoinResistance },
        { ClassStatType.TravelCooldownRecoveryRate, UnitStatType.TravelCooldownRecoveryRate },
        { ClassStatType.ReducedBloodDrain, UnitStatType.ReducedBloodDrain },
        { ClassStatType.BonusMaxHealth, UnitStatType.BonusMaxHealth },
        { ClassStatType.BonusMovementSpeed, UnitStatType.BonusMovementSpeed },
        { ClassStatType.BonusShapeshiftMovementSpeed, UnitStatType.BonusShapeshiftMovementSpeed },
        { ClassStatType.BonusMountMovementSpeed, UnitStatType.BonusMountMovementSpeed },
        { ClassStatType.UltimateEfficiency, UnitStatType.UltimateEfficiency },
        { ClassStatType.SpellFreeCast, UnitStatType.SpellFreeCast },
        { ClassStatType.WeaponFreeCast, UnitStatType.WeaponFreeCast },
        { ClassStatType.WeaponSkillPower, UnitStatType.WeaponSkillPower },
        { ClassStatType.FeedCooldownRecoveryRate, UnitStatType.FeedCooldownRecoveryRate },
        { ClassStatType.BloodMendHealEfficiency, UnitStatType.BloodMendHealEfficiency },
        { ClassStatType.DemountProtection, UnitStatType.DemountProtection },
        { ClassStatType.BloodDrainMultiplier, UnitStatType.BloodDrainMultiplier },
        { ClassStatType.CorruptionDamageReduction, UnitStatType.CorruptionDamageReduction }
    };
    public static IReadOnlyDictionary<ClassStatType, float> ClassStatBaseCaps => _classStatBaseCaps;
    static readonly Dictionary<ClassStatType, float> _classStatBaseCaps = [];
    public static IReadOnlyDictionary<PlayerClass, List<WeaponStatType>> ClassWeaponStatSynergies => _classWeaponStatSynergies;
    static readonly Dictionary<PlayerClass, List<WeaponStatType>> _classWeaponStatSynergies = new()
    {
        { PlayerClass.BloodKnight, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.BloodKnightWeaponSynergies) },
        { PlayerClass.DemonHunter, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.DemonHunterWeaponSynergies) },
        { PlayerClass.VampireLord, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.VampireLordWeaponSynergies) },
        { PlayerClass.ShadowBlade, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.ShadowBladeWeaponSynergies) },
        { PlayerClass.ArcaneSorcerer, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.ArcaneSorcererWeaponSynergies) },
        { PlayerClass.DeathMage, Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.DeathMageWeaponSynergies) }
    };
    public static IReadOnlyDictionary<PlayerClass, List<BloodStatType>> ClassBloodStatSynergies => _classBloodStatSynergies;
    static readonly Dictionary<PlayerClass, List<BloodStatType>> _classBloodStatSynergies = new()
    {
        { PlayerClass.BloodKnight, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.BloodKnightBloodSynergies) },
        { PlayerClass.DemonHunter, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.DemonHunterBloodSynergies) },
        { PlayerClass.VampireLord, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.VampireLordBloodSynergies) },
        { PlayerClass.ShadowBlade, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.ShadowBladeBloodSynergies) },
        { PlayerClass.ArcaneSorcerer, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.ArcaneSorcererBloodSynergies) },
        { PlayerClass.DeathMage, Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.DeathMageBloodSynergies) }
    };
}
