using Bloodcraft.Resources;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class Jewels
{
    static readonly Dictionary<PrefabGUID, IReadOnlyList<PrefabGUID>> _spellModSets = new()
    {
        // ─────────────────────────────────────── Blood
        [PrefabGUIDs.AB_Blood_BloodFountain_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_BloodFountain_FirstImpactApplyLeech,
            PrefabGUIDs.SpellMod_BloodFountain_FirstImpactDispell,
            PrefabGUIDs.SpellMod_BloodFountain_FirstImpactFadingSnare,
            PrefabGUIDs.SpellMod_BloodFountain_FirstImpactHealIncrease,
            PrefabGUIDs.SpellMod_BloodFountain_RecastLesser,
            PrefabGUIDs.SpellMod_BloodFountain_SecondImpactDamageIncrease,
            PrefabGUIDs.SpellMod_BloodFountain_SecondImpactHealIncrease,
            PrefabGUIDs.SpellMod_BloodFountain_SecondImpactKnockback,
            PrefabGUIDs.SpellMod_BloodFountain_SecondImpactSpeedBuff,
            PrefabGUIDs.SpellMod_BloodFountain_IncreaseArea
        ],
        [PrefabGUIDs.AB_Blood_BloodRage_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_BloodRage_DamageBoost,
            PrefabGUIDs.SpellMod_BloodRage_HealOnKill,
            PrefabGUIDs.SpellMod_BloodRage_IncreaseLifetime,
            PrefabGUIDs.SpellMod_BloodRage_IncreaseMoveSpeed,
            PrefabGUIDs.SpellMod_BloodRage_Shield,
            PrefabGUIDs.SpellMod_Shared_ApplyFadingSnare_Medium,
            PrefabGUIDs.SpellMod_Shared_DispellDebuffs,
            PrefabGUIDs.SpellMod_BloodRage_IncreaseArea
        ],
        [PrefabGUIDs.AB_Blood_BloodRite_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_BloodRite_ApplyFadingSnare,
            PrefabGUIDs.SpellMod_BloodRite_BonusDamage,
            PrefabGUIDs.SpellMod_BloodRite_DamageOnAttack,
            PrefabGUIDs.SpellMod_BloodRite_HealOnTrigger,
            PrefabGUIDs.SpellMod_BloodRite_IncreaseLifetime,
            PrefabGUIDs.SpellMod_BloodRite_Stealth,
            PrefabGUIDs.SpellMod_BloodRite_TossDaggers,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_High,
            PrefabGUIDs.SpellMod_BloodRite_DaggerBonusDamage,
            PrefabGUIDs.SpellMod_BloodRite_ConsumeLeechReduceCooldownXTimes,
            PrefabGUIDs.SpellMod_BloodRite_ConsumeLeechHealXTimes,
            PrefabGUIDs.SpellMod_BloodRite_BonusDaggers
        ],
        [PrefabGUIDs.AB_Blood_SanguineCoil_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_SanguineCoil_AddBounces,
            PrefabGUIDs.SpellMod_SanguineCoil_BonusDamage,
            PrefabGUIDs.SpellMod_SanguineCoil_BonusHealing,
            PrefabGUIDs.SpellMod_SanguineCoil_BonusLifeLeech,
            PrefabGUIDs.SpellMod_SanguineCoil_KillRecharge,
            PrefabGUIDs.SpellMod_SanguineCoil_LeechBonusDamage,
            PrefabGUIDs.SpellMod_Shared_AddCharges,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_Blood_Shadowbolt_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shadowbolt_ExplodeOnHit,
            PrefabGUIDs.SpellMod_Shadowbolt_ForkOnHit,
            PrefabGUIDs.SpellMod_Shadowbolt_LeechBonusDamage,
            PrefabGUIDs.SpellMod_Shadowbolt_VampiricCurse,
            PrefabGUIDs.SpellMod_Shared_Blood_ConsumeLeechSelfHeal_Small,
            PrefabGUIDs.SpellMod_Shared_CastRate,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_KnockbackOnHit_Medium,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfBlood_Group] =
        [
            PrefabGUIDs.SpellMod_VeilOfBlood_AttackInflictFadingSnare,
            PrefabGUIDs.SpellMod_VeilOfBlood_BloodNova,
            PrefabGUIDs.SpellMod_VeilOfBlood_BloodNovaArea,
            PrefabGUIDs.SpellMod_VeilOfBlood_DashInflictLeech,
            PrefabGUIDs.SpellMod_VeilOfBlood_Empower,
            PrefabGUIDs.SpellMod_VeilOfBlood_SelfHealing,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration
        ],
        [PrefabGUIDs.AB_Blood_CarrionSwarm_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_CarrionSwam_BonusDamage,
            PrefabGUIDs.SpellMod_CarrionSwam_Explode,
            PrefabGUIDs.SpellMod_CarrionSwam_Leech,
            PrefabGUIDs.SpellMod_CarrionSwam_StunOnHit,
            PrefabGUIDs.SpellMod_CarrionSwam_VampiricCurse,
            PrefabGUIDs.SpellMod_Shared_ApplyFadingSnare_Long
        ],
        // ─────────────────────────────────────── Chaos
        [PrefabGUIDs.AB_Chaos_Aftershock_Group] =
        [
            PrefabGUIDs.SpellMod_Chaos_Aftershock_BonusDamage,
            PrefabGUIDs.SpellMod_Chaos_Aftershock_InflictSlowOnProjectile,
            PrefabGUIDs.SpellMod_Chaos_Aftershock_KnockbackArea,
            PrefabGUIDs.SpellMod_Shared_Chaos_ConsumeIgniteAgonizingFlames,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_Projectile_IncreaseRange_Medium
        ],
        [PrefabGUIDs.AB_Chaos_Barrier_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Chaos_Barrier_BonusDamage,
            PrefabGUIDs.SpellMod_Chaos_Barrier_ConsumeAttackReduceCooldownXTimes,
            PrefabGUIDs.SpellMod_Chaos_Barrier_ExplodeOnHit,
            PrefabGUIDs.SpellMod_Chaos_Barrier_LesserPowerSurge,
            PrefabGUIDs.SpellMod_Chaos_Barrier_StunOnHit,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_Low
        ],
        [PrefabGUIDs.AB_Chaos_PowerSurge_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_PowerSurge_AttackSpeed,
            PrefabGUIDs.SpellMod_PowerSurge_EmpowerPhysical,
            PrefabGUIDs.SpellMod_PowerSurge_Haste,
            PrefabGUIDs.SpellMod_PowerSurge_IncreaseDurationOnKill,
            PrefabGUIDs.SpellMod_PowerSurge_Lifetime,
            PrefabGUIDs.SpellMod_PowerSurge_RecastDestonate,
            PrefabGUIDs.SpellMod_PowerSurge_Shield,
            PrefabGUIDs.SpellMod_Shared_DispellDebuffs
        ],
        [PrefabGUIDs.AB_Chaos_Void_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Chaos_Void_BonusDamage,
            PrefabGUIDs.SpellMod_Chaos_Void_BurnArea,
            PrefabGUIDs.SpellMod_Chaos_Void_FragBomb,
            PrefabGUIDs.SpellMod_Chaos_Void_ReduceChargeCD,
            PrefabGUIDs.SpellMod_Shared_Chaos_ConsumeIgniteAgonizingFlames,
            PrefabGUIDs.SpellMod_Shared_TargetAoE_IncreaseRange_Medium
        ],
        [PrefabGUIDs.AB_Chaos_Volley_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Chaos_Volley_BonusDamage,
            PrefabGUIDs.SpellMod_Chaos_Volley_SecondProjectileBonusDamage,
            PrefabGUIDs.SpellMod_Shared_Chaos_ConsumeIgniteAgonizingFlames,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_KnockbackOnHit_Light,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfChaos_Group] =
        [
            PrefabGUIDs.SpellMod_VeilOfChaos_ApplySnareOnExplode,
            PrefabGUIDs.SpellMod_VeilOfChaos_BonusDamageOnExplode,
            PrefabGUIDs.SpellMod_VeilOfChaos_BonusIllusion,
            PrefabGUIDs.SpellMod_Shared_Chaos_ConsumeIgniteAgonizingFlames_OnAttack,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration
        ],
        [PrefabGUIDs.AB_Chaos_RainOfChaos_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_RainOfChaos_BonusMeteor,
            PrefabGUIDs.SpellMod_RainOfChaos_BurnArea,
            PrefabGUIDs.SpellMod_RainOfChaos_MegaMeteor,
            PrefabGUIDs.SpellMod_Shared_Chaos_ConsumeIgniteAgonizingFlames,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_ApplyFadingSnare_Short
        ],
        // ─────────────────────────────────────── Frost
        [PrefabGUIDs.AB_Frost_ColdSnap_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_ColdSnap_BonusAbsorb,
            PrefabGUIDs.SpellMod_ColdSnap_BonusDamage,
            PrefabGUIDs.SpellMod_ColdSnap_HasteWhileShielded,
            PrefabGUIDs.SpellMod_ColdSnap_Immaterial,
            PrefabGUIDs.SpellMod_Shared_Frost_IncreaseFreezeWhenChill,
            PrefabGUIDs.SpellMod_Shared_FrostWeapon,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_High
        ],
        [PrefabGUIDs.AB_Frost_CrystalLance_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_CrystalLance_BonusDamageToFrosty,
            PrefabGUIDs.SpellMod_CrystalLance_PierceEnemies,
            PrefabGUIDs.SpellMod_Shared_CastRate,
            PrefabGUIDs.SpellMod_Shared_Frost_IncreaseFreezeWhenChill,
            PrefabGUIDs.SpellMod_Shared_Frost_ShieldOnFrosty,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_FrostBarrier_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_FrostBarrier_BonusDamage,
            PrefabGUIDs.SpellMod_FrostBarrier_BonusSpellPowerOnAbsorb,
            PrefabGUIDs.SpellMod_FrostBarrier_ConsumeAttackReduceCooldownXTimes,
            PrefabGUIDs.SpellMod_FrostBarrier_KnockbackOnRecast,
            PrefabGUIDs.SpellMod_FrostBarrier_ShieldOnFrostyRecast,
            PrefabGUIDs.SpellMod_Shared_Frost_ConsumeChillIntoFreeze_Recast,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_Low
        ],
        [PrefabGUIDs.AB_Frost_FrostBat_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_FrostBat_AreaDamage,
            PrefabGUIDs.SpellMod_FrostBat_BonusDamageToFrosty,
            PrefabGUIDs.SpellMod_Shared_CastRate,
            PrefabGUIDs.SpellMod_Shared_Frost_SplinterNovaOnFrosty,
            PrefabGUIDs.SpellMod_Shared_Frost_ShieldOnFrosty,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_Frost_IceNova_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_IceNova_ApplyShield,
            PrefabGUIDs.SpellMod_IceNova_BonusDamageToFrosty,
            PrefabGUIDs.SpellMod_IceNova_IncreaseRadius,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_TargetAoE_IncreaseRange_Medium,
            PrefabGUIDs.SpellMod_IceNova_RecastLesserNova
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfFrost_Group] =
        [
            PrefabGUIDs.SpellMod_VeilOfFrost_BonusDamage,
            PrefabGUIDs.SpellMod_VeilOfFrost_FrostNova,
            PrefabGUIDs.SpellMod_VeilOfFrost_IllusionFrostBlast,
            PrefabGUIDs.SpellMod_VeilOfFrost_ShieldBonus,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration
        ],
        [PrefabGUIDs.AB_FrostCone_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_FrostCone_BonusDamage,
            PrefabGUIDs.SpellMod_FrostCone_BonusSpeed,
            PrefabGUIDs.SpellMod_FrostCone_FrostWave,
            PrefabGUIDs.SpellMod_FrostCone_IncreaseFreeze,
            PrefabGUIDs.SpellMod_FrostCone_KnockbackOnEnd,
            PrefabGUIDs.SpellMod_FrostCone_Leech,
            PrefabGUIDs.SpellMod_FrostCone_Shield
        ],
        // ─────────────────────────────────────── Illusion
        [PrefabGUIDs.AB_Illusion_MistTrance_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_MIstTrance_DamageOnAttack,
            PrefabGUIDs.SpellMod_MistTrance_FearOnTrigger,
            PrefabGUIDs.SpellMod_MistTrance_HasteOnTrigger,
            PrefabGUIDs.SpellMod_MistTrance_PhantasmOnTrigger,
            PrefabGUIDs.SpellMod_MistTrance_ReduceSecondaryWeaponCD,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_High,
            PrefabGUIDs.SpellMod_Shared_KnockbackOnHit_Medium,
            PrefabGUIDs.SpellMod_Shared_TravelBuff_IncreaseRange_Medium
        ],
        [PrefabGUIDs.AB_Illusion_Mosquito_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Mosquito_BonusDamage,
            PrefabGUIDs.SpellMod_Mosquito_BonusFearDuration,
            PrefabGUIDs.SpellMod_Mosquito_BonusHealthAndSpeed,
            PrefabGUIDs.SpellMod_Mosquito_ShieldOnSpawn,
            PrefabGUIDs.SpellMod_Mosquito_WispsOnDestroy
        ],
        [PrefabGUIDs.AB_Illusion_PhantomAegis_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_PhantomAegis_ConsumeShieldAndPullAlly,
            PrefabGUIDs.SpellMod_PhantomAegis_ExplodeOnDestroy,
            PrefabGUIDs.SpellMod_PhantomAegis_IncreaseLifetime,
            PrefabGUIDs.SpellMod_PhantomAegis_IncreaseSpellPower,
            PrefabGUIDs.SpellMod_PhantomAegis_ConsumeWeakenIntoFear,
            PrefabGUIDs.SpellMod_Shared_DispellDebuffs,
            PrefabGUIDs.SpellMod_Shared_KnockbackOnHit_Medium,
            PrefabGUIDs.SpellMod_Shared_MovementSpeed_Normal
        ],
        [PrefabGUIDs.AB_Illusion_SpectralWolf_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shared_Illusion_ConsumeWeakenSpawnWisp,
            PrefabGUIDs.SpellMod_Shared_Illusion_WeakenShield,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity,
            PrefabGUIDs.SpellMod_SpectralWolf_AddBounces,
            PrefabGUIDs.SpellMod_SpectralWolf_DecreaseBounceDamageReduction,
            PrefabGUIDs.SpellMod_SpectralWolf_FirstBounceInflictFadingSnare,
            PrefabGUIDs.SpellMod_SpectralWolf_ReturnToOwner,
            PrefabGUIDs.SpellMod_SpectralWolf_WeakenApplyXPhantasm
        ],
        [PrefabGUIDs.AB_Illusion_WraithSpear_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shared_ApplyFadingSnare_Medium,
            PrefabGUIDs.SpellMod_Shared_Illusion_ConsumeWeakenSpawnWisp,
            PrefabGUIDs.SpellMod_Shared_Illusion_WeakenShield,
            PrefabGUIDs.SpellMod_Shared_Projectile_IncreaseRange_Medium,
            PrefabGUIDs.SpellMod_WraithSpear_BonusDamage,
            PrefabGUIDs.SpellMod_WraithSpear_ReducedDamageReduction,
            PrefabGUIDs.SpellMod_WraithSpear_ShieldAlly
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfIllusion_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_VeilOfIllusion_AttackInflictFadingSnare,
            PrefabGUIDs.SpellMod_VeilOfIllusion_IllusionFireProjectiles,
            PrefabGUIDs.SpellMod_VeilOfIllusion_IllusionProjectileDamage,
            PrefabGUIDs.SpellMod_VeilOfIllusion_PhantasmOnHit,
            PrefabGUIDs.SpellMod_VeilOfIllusion_RecastDetonate,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration,
            PrefabGUIDs.SpellMod_Shared_Illusion_WeakenShield_OnAttack
        ],
        [PrefabGUIDs.AB_Illusion_Curse_Group] =
        [
            PrefabGUIDs.SpellMod_Curse_DamageFactor,
            PrefabGUIDs.SpellMod_Curse_DamageOnHit,
            PrefabGUIDs.SpellMod_Curse_IncreaseDuration,
            PrefabGUIDs.SpellMod_Curse_ReapplyWeaken,
            PrefabGUIDs.SpellMod_Curse_SpawnWispOnDeath,
            PrefabGUIDs.SpellMod_Shared_Illusion_ConsumeWeakenSpawnWisp,
            PrefabGUIDs.SpellMod_Shared_Illusion_WeakenShield
        ],
        // ─────────────────────────────────────── Storm
        [PrefabGUIDs.AB_Storm_BallLightning_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_BallLightning_BonusDamage,
            PrefabGUIDs.SpellMod_BallLightning_DetonateOnRecast,
            PrefabGUIDs.SpellMod_BallLightning_Haste,
            PrefabGUIDs.SpellMod_BallLightning_KnockbackOnExplode,
            PrefabGUIDs.SpellMod_Shared_Projectile_IncreaseRange_Medium,
            PrefabGUIDs.SpellMod_Shared_Storm_ConsumeStaticIntoStun_Explode
        ],
        [PrefabGUIDs.AB_Storm_Cyclone_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Cyclone_BonusDamage,
            PrefabGUIDs.SpellMod_Cyclone_BonusDamageStormShield,
            PrefabGUIDs.SpellMod_Cyclone_BonusShield,
            PrefabGUIDs.SpellMod_Cyclone_IncreaseLifetime,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity,
            PrefabGUIDs.SpellMod_Shared_Storm_ConsumeStaticIntoStun,
            PrefabGUIDs.SpellMod_Shared_Storm_ConsumeStaticIntoWeaponCharge,
            PrefabGUIDs.SpellMod_Shared_CastRate,
            PrefabGUIDs.SpellMod_Cyclone_SpellLeechStormShield,
            PrefabGUIDs.SpellMod_Cyclone_ReducedDamageReduction
        ],
        [PrefabGUIDs.AB_Storm_Discharge_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Discharge_BonusDamage,
            PrefabGUIDs.SpellMod_Discharge_Immaterial,
            PrefabGUIDs.SpellMod_Discharge_IncreaseStormShieldDuration,
            PrefabGUIDs.SpellMod_Discharge_IncreaseStunDuration,
            PrefabGUIDs.SpellMod_Discharge_RecastDetonate,
            PrefabGUIDs.SpellMod_Discharge_SpellLeech,
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_High,
            PrefabGUIDs.SpellMod_Shared_Storm_GrantWeaponCharge
        ],
        [PrefabGUIDs.AB_Storm_LightningWall_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_LightningWall_ApplyShield,
            PrefabGUIDs.SpellMod_LightningWall_BonusDamage,
            PrefabGUIDs.SpellMod_LightningWall_ConsumeProjectileWeaponCharge,
            PrefabGUIDs.SpellMod_LightningWall_FadingSnare,
            PrefabGUIDs.SpellMod_LightningWall_IncreaseMovementSpeed
        ],
        [PrefabGUIDs.AB_Storm_PolarityShift_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shared_ApplyFadingSnare_Medium,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity,
            PrefabGUIDs.SpellMod_Shared_Storm_ConsumeStaticIntoWeaponCharge,
            PrefabGUIDs.SpellMod_Storm_PolarityShift_AreaImpactDestination,
            PrefabGUIDs.SpellMod_Storm_PolarityShift_AreaImpactOrigin
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfStorm_Group] =
        [
            PrefabGUIDs.SpellMod_Shared_Storm_ConsumeStaticIntoStun,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration,
            PrefabGUIDs.SpellMod_VeilOfStorm_AttackInflictFadingSnare,
            PrefabGUIDs.SpellMod_VeilOfStorm_DashInflictStatic,
            PrefabGUIDs.SpellMod_VeilOfStorm_SparklingIllusion,
            PrefabGUIDs.SpellMod_VeilOfStorm_RecastIllusionDash
        ],
        [PrefabGUIDs.AB_Storm_LightningTendrils_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_LightningTendrils_BonusDamage,
            PrefabGUIDs.SpellMod_LightningTendrils_BonusProjectile,
            PrefabGUIDs.SpellMod_LightningTendrils_ChainLightning,
            PrefabGUIDs.SpellMod_LightningTendrils_SpeedChanneling,
            PrefabGUIDs.SpellMod_LightningTendrils_StunOnHit,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity,
            PrefabGUIDs.SpellMod_Shared_CastRate
        ],
        // ─────────────────────────────────────── Unholy
        [PrefabGUIDs.AB_Unholy_CorpseExplosion_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_CorpseExplosion_BonusDamage,
            PrefabGUIDs.SpellMod_CorpseExplosion_DoubleImpact,
            PrefabGUIDs.SpellMod_CorpseExplosion_HealMinions,
            PrefabGUIDs.SpellMod_CorpseExplosion_KillingBlow,
            PrefabGUIDs.SpellMod_CorpseExplosion_SkullNova,
            PrefabGUIDs.SpellMod_CorpseExplosion_SnareBonus,
            PrefabGUIDs.SpellMod_Shared_Cooldown_Medium,
            PrefabGUIDs.SpellMod_Shared_TargetAoE_IncreaseRange_Medium,
            PrefabGUIDs.SpellMod_Shared_Unholy_SkeletonBomb
        ],
        [PrefabGUIDs.AB_Unholy_CorruptedSkull_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_CorruptedSkull_BoneSpirit,
            PrefabGUIDs.SpellMod_CorruptedSkull_BonusDamage,
            PrefabGUIDs.SpellMod_CorruptedSkull_DetonateSkeleton,
            PrefabGUIDs.SpellMod_CorruptedSkull_LesserProjectiles,
            PrefabGUIDs.SpellMod_Shared_KnockbackOnHit_Medium,
            PrefabGUIDs.SpellMod_Shared_Projectile_RangeAndVelocity
        ],
        [PrefabGUIDs.AB_Unholy_DeathKnight_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_DeathKnight_BonusDamage,
            PrefabGUIDs.SpellMod_DeathKnight_BonusDamageBelowTreshhold,
            PrefabGUIDs.SpellMod_DeathKnight_IncreaseLifetime,
            PrefabGUIDs.SpellMod_DeathKnight_LifeLeech,
            PrefabGUIDs.SpellMod_DeathKnight_MaxHealth,
            PrefabGUIDs.SpellMod_DeathKnight_SkeletonMageOnDeath,
            PrefabGUIDs.SpellMod_DeathKnight_SkeletonMageOnLifetimeEnded,
            PrefabGUIDs.SpellMod_DeathKnight_SnareEnemiesOnSummon
        ],
        [PrefabGUIDs.AB_Unholy_Soulburn_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shared_DispellDebuffs_Self,
            PrefabGUIDs.SpellMod_Soulburn_BonusDamage,
            PrefabGUIDs.SpellMod_Soulburn_BonusLifeDrain,
            PrefabGUIDs.SpellMod_Soulburn_ConsumeSkeletonEmpower,
            PrefabGUIDs.SpellMod_Soulburn_ConsumeSkeletonHeal,
            PrefabGUIDs.SpellMod_Soulburn_IncreasedSilenceDuration,
            PrefabGUIDs.SpellMod_Soulburn_IncreaseTriggerCount,
            PrefabGUIDs.SpellMod_Soulburn_ReduceCooldownOnSilence,
            PrefabGUIDs.SpellMod_Soulburn_Shield,
            PrefabGUIDs.SpellMod_Soulburn_SpawnSkeleton
        ],
        [PrefabGUIDs.AB_Unholy_WardOfTheDamned_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_Shared_IncreaseMoveSpeedDuringChannel_Low,
            PrefabGUIDs.SpellMod_WardOfTheDamned_BonusDamageOnRecast,
            PrefabGUIDs.SpellMod_WardOfTheDamned_DamageMeleeAttackers,
            PrefabGUIDs.SpellMod_WardOfTheDamned_EmpowerSkeletonsOnRecast,
            PrefabGUIDs.SpellMod_WardOfTheDamned_HealOnAbsorbProjectile,
            PrefabGUIDs.SpellMod_WardOfTheDamned_KnockbackOnRecast,
            PrefabGUIDs.SpellMod_WardOfTheDamned_MightSpawnMageSkeleton,
            PrefabGUIDs.SpellMod_WardOfTheDamned_ShieldSkeletonsOnRecast,
            PrefabGUIDs.SpellMod_Shared_Unholy_SkeletonBomb
        ],
        [PrefabGUIDs.AB_Vampire_VeilOfBones_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_VeilOfBones_BonusDamageBelowTreshhold,
            PrefabGUIDs.SpellMod_VeilOfBones_DashHealMinions,
            PrefabGUIDs.SpellMod_VeilOfBones_DashInflictCondemn,
            PrefabGUIDs.SpellMod_VeilOfBones_SpawnSkeletonMage,
            PrefabGUIDs.SpellMod_VeilOfBones_SpawnSkeleton,
            PrefabGUIDs.SpellMod_Shared_Veil_BuffAndIllusionDuration,
            PrefabGUIDs.SpellMod_VeilOfBones_SkeletonBomb,
            PrefabGUIDs.SpellMod_Shared_Veil_BonusDamageOnPrimary
        ],
        [PrefabGUIDs.AB_Unholy_ChainsOfDeath_AbilityGroup] =
        [
            PrefabGUIDs.SpellMod_ChainsOfDeath_BoneSpirit,
            PrefabGUIDs.SpellMod_ChainsOfDeath_Dot,
            PrefabGUIDs.SpellMod_ChainsOfDeath_DurationAndDamage,
            PrefabGUIDs.SpellMod_ChainsOfDeath_Explosion,
            PrefabGUIDs.SpellMod_ChainsOfDeath_FadingSnare,
            PrefabGUIDs.SpellMod_ChainsOfDeath_Haste,
            PrefabGUIDs.SpellMod_ChainsOfDeath_Leech,
            PrefabGUIDs.SpellMod_ChainsOfDeath_ReducedDamage,
            PrefabGUIDs.SpellMod_ChainsOfDeath_SkullNova,
            PrefabGUIDs.SpellMod_ChainsOfDeath_Slow
        ]
};
    public static bool TryGetSpellMods(PrefabGUID spell, out IReadOnlyList<PrefabGUID> spellMods) => _spellModSets.TryGetValue(spell, out spellMods);
}
