using Bloodcraft.Resources;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.ShadowMatter.PrimalData.PrimalSettings;
using static Bloodcraft.Utilities.ShadowMatter.PrimalItem;
using static Bloodcraft.Utilities.ShadowMatter.PrimalItem.PrimalShared;

namespace Bloodcraft.Utilities;
internal static class ShadowMatter
{
    static PrimalData PrimalItems { get; } = new(PrimalData.PrimalItems);

    public readonly record struct PrimalData(ReadOnlyMemory<PrimalItem> Items)
    {
        internal static PrimalItem[] PrimalItems { get; } =
        [
            new(new(PrefabGUIDs.Item_Weapon_Spear_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability01),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_Spear_Primary_Attack_Group),
                AbilityInfo.Offensive(PrefabGUIDs.AB_Vampire_BloodKnight_SkeweringLeap_AbilityGroup),
                AbilityInfo.Defensive(PrefabGUIDs.AB_Vampire_BloodKnight_SpearTwirl_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_Vampire_BloodKnight_ThousandSpears_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_Vampire_BloodKnight_Dash_AbilityGroup)),
            new(new(PrefabGUIDs.Item_Weapon_GreatSword_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability02),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup)),
            /*
            new(new(PrefabGUIDs.Item_Weapon_TwinBlades_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability03),
                PowerInfo.Scholar,
                new AbilityInfo(PrefabGUIDs.AB_ArchMage_FlamingIce_AbilityGroup, 3f),
                AbilityInfo.Offensive(PrefabGUIDs.AB_ArchMage_LightningArc_AbilityGroup),
                new AbilityInfo(PrefabGUIDs.AB_ArchMage_ArcaneImprisonment_AbilityGroup, 12f),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_ArchMage_FlameSphere_Dash_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_ArchMage_Teleport_AbilityGroup)),
            */
            /*
            new(new(PrefabGUIDs.Item_Weapon_Whip_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_Whip_Ability01),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup)),
            new(new(PrefabGUIDs.Item_Weapon_Axe_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_DualHammers_Ability03),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup)),
            new(new(PrefabGUIDs.Item_Weapon_TwinBlades_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_GreatSword_Ability01),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup)),
            new(new(PrefabGUIDs.Item_Weapon_Daggers_T09_ShadowMatter, PrefabGUIDs.EquipBuff_Weapon_GreatSword_Ability02),
                PowerInfo.Default,
                AbilityInfo.Attack(PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup),
                AbilityInfo.Offensive(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup),
                AbilityInfo.Projectile(PrefabGUIDs.AB_HighLord_UnholySkill_AbilityGroup),
                AbilityInfo.Ultimate(PrefabGUIDs.AB_HighLord_CorpseStorm_AbilityGroup),
                AbilityInfo.Dash(PrefabGUIDs.AB_HighLord_UnholyWarp_AbilityGroup)),
            */
        ];

        public static PrimalSettings Settings { get; } = Default;

        public readonly record struct PrimalSettings(float WeaponLevel, float PhysicalPower, float SpellPower,
            float OffensiveCd, float DefensiveCd, float ProjectileCd, float UltimateCd, float DashCd)
        {
            public const float WEAPON_LVL = 100f;
            public const float PHYSICAL_PWR = 35f;
            public const float SPELL_PWR = 10f;

            public const float OFFENSIVE_CD = 8f;
            public const float DEFENSIVE_CD = 10f;
            public const float PROJECTILE_CD = 5f;

            public const float ULTIMATE_CD = 60f;
            public const float DASH_CD = 6f;

            public static PrimalSettings Default { get; } = new(WEAPON_LVL, PHYSICAL_PWR, SPELL_PWR, OFFENSIVE_CD, DEFENSIVE_CD, PROJECTILE_CD, ULTIMATE_CD, DASH_CD);
        }

        public ReadOnlySpan<PrimalItem>.Enumerator GetEnumerator()
            => Items.Span.GetEnumerator();
    }

    public readonly struct PrimalItem(PrimalBase weaponBase, PowerInfo weaponPower = default,
        AbilityInfo attackAbility = default, AbilityInfo primaryAbility = default, AbilityInfo secondaryAbility = default,
        AbilityInfo ultimateAbility = default, AbilityInfo dashAbility = default)
    {
        public PrefabGUID ItemWeapon
            => new(WeaponBase.WeaponGuid);
        public PrefabGUID EquipBuff
            => new(WeaponBase.BuffGuid);

        public readonly struct PrimalBase(PrefabGUID itemWeapon, PrefabGUID equipBuff)
        {
            public readonly int WeaponGuid = itemWeapon.GuidHash;
            public readonly int BuffGuid = equipBuff.GuidHash;
        }

        public PrimalBase WeaponBase { get; } = weaponBase;

        public PrefabGUID AttackGroup
            => WeaponShared.AttackSlot.AbilityGroup;
        public PrefabGUID PrimaryGroup
            => WeaponShared.PrimarySlot.AbilityGroup;
        public PrefabGUID SecondaryGroup
            => WeaponShared.SecondarySlot.AbilityGroup;
        public PrefabGUID UltimateGroup
            => WeaponShared.UltimateSlot.AbilityGroup;
        public PrefabGUID DashGroup
            => WeaponShared.DashSlot.AbilityGroup;

        public PrimalShared WeaponShared { get; } = new(weaponPower, attackAbility, primaryAbility, secondaryAbility, ultimateAbility, dashAbility);

        public readonly struct PrimalShared(PowerInfo weaponPower,
                AbilityInfo attackAbility, AbilityInfo primaryAbility, AbilityInfo secondaryAbility,
                AbilityInfo ultimateAbility, AbilityInfo dashAbility)
        {
            public PowerInfo WeaponPower { get; } = weaponPower;

            public readonly struct PowerInfo(float weaponLevel, float physicalPower, float spellPower)
            {
                public readonly float WeaponLevel = weaponLevel;
                public readonly float PhysicalPower = physicalPower;
                public readonly float SpellPower = spellPower;

                public static readonly PowerInfo Default = new(WEAPON_LVL, PHYSICAL_PWR, SPELL_PWR);
                public static readonly PowerInfo Scholar = new(WEAPON_LVL, SPELL_PWR, PHYSICAL_PWR);
            }

            public AbilityInfo AttackSlot
                => AbilityInfos[0];
            public AbilityInfo PrimarySlot
                => AbilityInfos[1];
            public AbilityInfo SecondarySlot
                => AbilityInfos[2];
            public AbilityInfo UltimateSlot
                => AbilityInfos[3];
            public AbilityInfo DashSlot
                => AbilityInfos[4];

            public readonly struct AbilityInfo(PrefabGUID abilityGroup, float cooldown = default)
            {
                public readonly PrefabGUID AbilityGroup = abilityGroup;
                public readonly float Cooldown = cooldown;
                public static AbilityInfo Attack(PrefabGUID abilityGroup)
                    => new(abilityGroup);
                public static AbilityInfo Offensive(PrefabGUID abilityGroup)
                    => new(abilityGroup, OFFENSIVE_CD);
                public static AbilityInfo Defensive(PrefabGUID abilityGroup)
                    => new(abilityGroup, DEFENSIVE_CD);
                public static AbilityInfo Projectile(PrefabGUID abilityGroup)
                    => new(abilityGroup, PROJECTILE_CD);
                public static AbilityInfo Ultimate(PrefabGUID abilityGroup)
                    => new(abilityGroup, ULTIMATE_CD);
                public static AbilityInfo Dash(PrefabGUID abilityGroup)
                    => new(abilityGroup, DASH_CD);
            }

            internal AbilityData AbilityInfos { get; } = new(attackAbility, primaryAbility, secondaryAbility, ultimateAbility, dashAbility);

            public readonly record struct AbilityData(ReadOnlyMemory<AbilityInfo> AbilityInfos)
            {
                public AbilityInfo this[int index]
                    => AbilityInfos.Span[index];

                public ReadOnlySpan<AbilityInfo>.Enumerator GetEnumerator()
                    => AbilityInfos.Span.GetEnumerator();

                public AbilityData(AbilityInfo attack, AbilityInfo primary, AbilityInfo secondary, AbilityInfo ultimate,
                    AbilityInfo dash) : this(new AbilityInfo[] { attack, primary, secondary, ultimate, dash }) { }
            }
        }
    }

    public static void GatherShadows()
    {
        foreach (var item in PrimalItems)
        {
            try
            {
                PrimalItem primalItem = item;
                Core.Log.LogWarning($"Forging Primal Weapon - {primalItem.ItemWeapon.GetPrefabName()}");

                RefineMateria(ref primalItem);
                Core.Log.LogWarning("Refining...");

                ImbueAbilities(ref primalItem);
                Core.Log.LogWarning("Imbuing...");

                EnforceCooldowns(ref primalItem);
                Core.Log.LogWarning("Complete!");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{ex}");
            }
        }
    }

    static void RefineMateria(ref PrimalItem primalItem)
    {
        Entity itemWeapon = primalItem.ItemWeapon.GetPrefabEntity();
        PowerInfo weaponPower = primalItem.WeaponShared.WeaponPower;

        itemWeapon.With((ref WeaponLevelSource weaponLevelSource)
            => weaponLevelSource.Level = weaponPower.WeaponLevel);
        itemWeapon.WithEdit(0, (ref ModifyUnitStatBuff_DOTS buff)
            => buff.Value = weaponPower.PhysicalPower);
        itemWeapon.WithInsert(1, new ModifyUnitStatBuff_DOTS()
        {
            StatType = UnitStatType.SpellPower,
            ModificationType = ModificationType.AddToBase,
            AttributeCapType = AttributeCapType.SoftCapped,
            Value = weaponPower.SpellPower,
            Modifier = 1
        });
    }

    static void ImbueAbilities(ref PrimalItem primalItem)
    {
        PrefabGUID itemWeapon = primalItem.ItemWeapon;
        PrefabGUID equipBuff = primalItem.EquipBuff;

        Entity weaponEntity = itemWeapon.GetPrefabEntity();
        weaponEntity.With((ref EquippableData equippableData) => equippableData.BuffGuid = equipBuff);

        Entity buffEntity = equipBuff.GetPrefabEntity();
        buffEntity.WithClear<ReplaceAbilityOnSlotBuff>();
        buffEntity.AddSlot(primalItem.AttackGroup, 0);
        buffEntity.AddSlot(primalItem.PrimaryGroup, 1);
        buffEntity.AddSlot(primalItem.SecondaryGroup, 4);
        buffEntity.AddSlot(primalItem.DashGroup, 2);
        buffEntity.AddSlot(primalItem.UltimateGroup, 7);
    }

    static void AddSlot(this Entity entity, PrefabGUID abilityGroup, int slot)
    {
        if (!abilityGroup.HasValue())
            return;

        entity.WithAdd(new ReplaceAbilityOnSlotBuff
        {
            Slot = slot,
            NewGroupId = abilityGroup,
            CopyCooldown = true
        });
    }

    static void EnforceCooldowns(ref PrimalItem primalItem)
    {
        PrimalShared primalShared = primalItem.WeaponShared;

        Entity primaryGroup = primalItem.PrimaryGroup.GetPrefabEntity();
        float primaryCooldown = primalShared.PrimarySlot.Cooldown;

        Entity secondaryGroup = primalItem.SecondaryGroup.GetPrefabEntity();
        float secondaryCooldown = primalShared.SecondarySlot.Cooldown;

        Entity ultimateGroup = primalItem.UltimateGroup.GetPrefabEntity();
        float ultimateCooldown = primalShared.UltimateSlot.Cooldown;

        Entity dashGroup = primalItem.DashGroup.GetPrefabEntity();
        float dashCooldown = primalShared.DashSlot.Cooldown;

        if (primaryGroup.TryGetBuffer<AbilityGroupStartAbilitiesBuffer>(out var buffer))
        {
            Entity primaryCast = buffer.IsIndexWithinRange(0) ? buffer[0].PrefabGUID.GetPrefabEntity() : Entity.Null;
            primaryCast.With((ref AbilityCooldownData cooldownData)
                => cooldownData.Cooldown._Value = primaryCooldown);

            PrefabGUID castGuid = primaryCast.GetPrefabGuid();
            VerifyCooldowns(castGuid, primaryCooldown);
        }

        if (secondaryGroup.TryGetBuffer(out buffer))
        {
            Entity secondaryCast = buffer.IsIndexWithinRange(0) ? buffer[0].PrefabGUID.GetPrefabEntity() : Entity.Null;
            secondaryCast.With((ref AbilityCooldownData cooldownData)
                => cooldownData.Cooldown._Value = secondaryCooldown);

            PrefabGUID castGuid = secondaryCast.GetPrefabGuid();
            VerifyCooldowns(castGuid, secondaryCooldown);
        }

        if (ultimateGroup.TryGetBuffer(out buffer))
        {
            Entity ultimateCast = buffer.IsIndexWithinRange(0) ? buffer[0].PrefabGUID.GetPrefabEntity() : Entity.Null;
            ultimateCast.With((ref AbilityCooldownData cooldownData)
                => cooldownData.Cooldown._Value = ultimateCooldown);

            PrefabGUID castGuid = ultimateCast.GetPrefabGuid();
            VerifyCooldowns(castGuid, ultimateCooldown);
        }

        if (dashGroup.TryGetBuffer(out buffer))
        {
            Entity dashCast = buffer.IsIndexWithinRange(0) ? buffer[0].PrefabGUID.GetPrefabEntity() : Entity.Null;
            dashCast.With((ref AbilityCooldownData cooldownData)
                => cooldownData.Cooldown._Value = dashCooldown);

            PrefabGUID castGuid = dashCast.GetPrefabGuid();
            VerifyCooldowns(castGuid, dashCooldown);
        }
    }

    static void VerifyCooldowns(PrefabGUID castGuid, float cooldown)
    {
        foreach (PlayerInfo playerInfo in SteamIdPlayerInfoCache.Values)
        {
            Entity playerCharacter = playerInfo.CharEntity;
            var attachedBuffer = playerCharacter.ReadBuffer<AttachedBuffer>();

            if (!playerCharacter.Exists())
                continue;

            Entity castEntity = ResolveCast(castGuid, ref attachedBuffer);
            castEntity.With((ref AbilityCooldownData cooldownData)
                => cooldownData.Cooldown._Value = cooldown);
        }
    }

    static Entity ResolveCast(PrefabGUID castGuid, ref DynamicBuffer<AttachedBuffer> buffer)
    {
        foreach (AttachedBuffer attached in buffer)
        {
            Entity entity = attached.Entity;
            PrefabGUID prefabGuid = attached.PrefabGuid;

            if (castGuid.Equals(prefabGuid))
                return entity;
        }

        return default;
    }
}
