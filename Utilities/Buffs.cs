using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Utilities;
internal static class Buffs
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    const float EXO_COUNTDOWN = 5f;

    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _vBloodBloodBuff = new(20081801);

    public static readonly Dictionary<int, PrefabGUID> ExoFormAbilityMap = new()
    {
        { 0, new(-1473399128) }, // primary attack fast shockwaveslash
        { 1, new(841757706) }, // first weapon skill downswing detonate
        { 2, new(-1940289109) }, // space dash skill teleport behind target
        { 3, new(1270706044) }, // shift dash skill veil of bats
        { 4, new(-2146217789) }, // second weapon skill sword thrust
        { 5, new(-1161896955) }, // first spell skill etherial sword
        { 6, new(-7407393) }, // second spell skill ring of blood
        { 7, new(797450963) } //  ultimate spell blood... lasers?
    };

    public static readonly Dictionary<int, float> ExoFormCooldownMap = new()
    {
        // { 0, 8f },
        // { 1, 8f },
        // { 2, 8f },
        // { 3, 8f },
        // { 4, 8f },
        { 5, 15f },
        { 6, 30f }
        //{ 7, 35f }
    };

    public static readonly Dictionary<int, int> ExoFormUnlockMap = new()
    {
        { 0, 0 },
        { 1, 15 },
        { 2, 0 },
        { 3, 0 },
        { 4, 30 },
        { 5, 45 },
        { 6, 60 },
        { 7, 75 }
    };
    public static bool TryApplyBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (!entity.HasBuff(buffPrefabGuid))
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = buffPrefabGuid
            };

            FromCharacter fromCharacter = new()
            {
                Character = entity,
                User = entity
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

            return true;
        }

        return false;
    }
    public static void ModifyBloodBuff(Entity buff)
    {
        if (buff.Has<BloodBuffScript_Rogue_MountDamageBonus>())
        {
            var mountDamageBonus = buff.Read<BloodBuffScript_Rogue_MountDamageBonus>();

            mountDamageBonus.RequiredBloodPercentage = 0f;
            mountDamageBonus.MinMountDamageIncrease = 0;
            mountDamageBonus.MaxMountDamageIncrease = 0;
            buff.Write(mountDamageBonus);

            return;
        }

        if (buff.Has<BloodBuff_HealReceivedProc_DataShared>())
        {
            var healReceivedProc = buff.Read<BloodBuff_HealReceivedProc_DataShared>();

            healReceivedProc.RequiredBloodPercentage = 0;
            buff.Write(healReceivedProc);

            return;
        }

        if (buff.Has<BloodBuffScript_Brute_HealthRegenBonus>())
        {
            var bruteHealthRegenBonus = buff.Read<BloodBuffScript_Brute_HealthRegenBonus>();

            bruteHealthRegenBonus.RequiredBloodPercentage = 0;
            bruteHealthRegenBonus.MinHealthRegenIncrease = bruteHealthRegenBonus.MaxHealthRegenIncrease;
            buff.Write(bruteHealthRegenBonus);

            return;
        }

        if (buff.Has<BloodBuffScript_Brute_NulifyAndEmpower>())
        {
            var bruteNulifyAndEmpower = buff.Read<BloodBuffScript_Brute_NulifyAndEmpower>();

            bruteNulifyAndEmpower.RequiredBloodPercentage = 0;
            buff.Write(bruteNulifyAndEmpower);

            return;
        }

        if (buff.Has<BloodBuff_Brute_PhysLifeLeech_DataShared>())
        {
            var brutePhysLifeLeech = buff.Read<BloodBuff_Brute_PhysLifeLeech_DataShared>();

            brutePhysLifeLeech.RequiredBloodPercentage = 0;
            brutePhysLifeLeech.MinIncreasedPhysicalLifeLeech = brutePhysLifeLeech.MaxIncreasedPhysicalLifeLeech;
            buff.Write(brutePhysLifeLeech);

            return;
        }

        if (buff.Has<BloodBuff_Brute_RecoverOnKill_DataShared>())
        {
            var bruteRecoverOnKill = buff.Read<BloodBuff_Brute_RecoverOnKill_DataShared>();

            bruteRecoverOnKill.RequiredBloodPercentage = 0;
            bruteRecoverOnKill.MinHealingReceivedValue = bruteRecoverOnKill.MaxHealingReceivedValue;
            buff.Write(bruteRecoverOnKill);

            return;
        }

        if (buff.Has<BloodBuff_Creature_SpeedBonus_DataShared>())
        {
            var creatureSpeedBonus = buff.Read<BloodBuff_Creature_SpeedBonus_DataShared>();

            creatureSpeedBonus.RequiredBloodPercentage = 0;
            creatureSpeedBonus.MinMovementSpeedIncrease = creatureSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(creatureSpeedBonus);

            return;
        }

        if (buff.Has<BloodBuff_SunResistance_DataShared>())
        {
            var sunResistance = buff.Read<BloodBuff_SunResistance_DataShared>();

            sunResistance.RequiredBloodPercentage = 0;
            sunResistance.MinBonus = sunResistance.MaxBonus;
            buff.Write(sunResistance);

            return;
        }

        if (buff.Has<BloodBuffScript_Draculin_BloodMendBonus>())
        {
            var draculinBloodMendBonus = buff.Read<BloodBuffScript_Draculin_BloodMendBonus>();

            draculinBloodMendBonus.RequiredBloodPercentage = 0;
            draculinBloodMendBonus.MinBonusHealing = draculinBloodMendBonus.MaxBonusHealing;
            buff.Write(draculinBloodMendBonus);

            return;
        }

        if (buff.Has<Script_BloodBuff_CCReduction_DataShared>())
        {
            var bloodBuffCCReduction = buff.Read<Script_BloodBuff_CCReduction_DataShared>();

            bloodBuffCCReduction.RequiredBloodPercentage = 0;
            bloodBuffCCReduction.MinBonus = bloodBuffCCReduction.MaxBonus;
            buff.Write(bloodBuffCCReduction);

            return;
        }

        if (buff.Has<Script_BloodBuff_Draculin_ImprovedBite_DataShared>())
        {
            var draculinImprovedBite = buff.Read<Script_BloodBuff_Draculin_ImprovedBite_DataShared>();

            draculinImprovedBite.RequiredBloodPercentage = 0;
            buff.Write(draculinImprovedBite);

            return;
        }

        if (buff.Has<BloodBuffScript_LastStrike>())
        {
            var lastStrike = buff.Read<BloodBuffScript_LastStrike>();

            lastStrike.RequiredBloodQuality = 0;
            lastStrike.LastStrikeBonus_Min = lastStrike.LastStrikeBonus_Max;
            buff.Write(lastStrike);

            return;
        }

        if (buff.Has<BloodBuff_Draculin_SpeedBonus_DataShared>())
        {
            var draculinSpeedBonus = buff.Read<BloodBuff_Draculin_SpeedBonus_DataShared>();

            draculinSpeedBonus.RequiredBloodPercentage = 0;
            draculinSpeedBonus.MinMovementSpeedIncrease = draculinSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(draculinSpeedBonus);

            return;
        }

        if (buff.Has<BloodBuff_AllResistance_DataShared>())
        {
            var allResistance = buff.Read<BloodBuff_AllResistance_DataShared>();

            allResistance.RequiredBloodPercentage = 0;
            allResistance.MinBonus = allResistance.MaxBonus;
            buff.Write(allResistance);

            return;
        }

        if (buff.Has<BloodBuff_BiteToMutant_DataShared>())
        {
            var biteToMutant = buff.Read<BloodBuff_BiteToMutant_DataShared>();

            biteToMutant.RequiredBloodPercentage = 0;
            buff.Write(biteToMutant);

            return;
        }

        if (buff.Has<BloodBuff_BloodConsumption_DataShared>())
        {
            var bloodConsumption = buff.Read<BloodBuff_BloodConsumption_DataShared>();

            bloodConsumption.RequiredBloodPercentage = 0;
            bloodConsumption.MinBonus = bloodConsumption.MaxBonus;
            buff.Write(bloodConsumption);

            return;
        }

        if (buff.Has<BloodBuff_HealthRegeneration_DataShared>())
        {
            var healthRegeneration = buff.Read<BloodBuff_HealthRegeneration_DataShared>();

            healthRegeneration.RequiredBloodPercentage = 0;
            healthRegeneration.MinBonus = healthRegeneration.MaxBonus;
            buff.Write(healthRegeneration);

            return;
        }

        if (buff.Has<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>())
        {
            var applyMovementSpeedOnShapeshift = buff.Read<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>();

            applyMovementSpeedOnShapeshift.RequiredBloodPercentage = 0;
            applyMovementSpeedOnShapeshift.MinBonus = applyMovementSpeedOnShapeshift.MaxBonus;
            buff.Write(applyMovementSpeedOnShapeshift);

            return;
        }

        if (buff.Has<BloodBuff_PrimaryAttackLifeLeech_DataShared>())
        {
            var primaryAttackLifeLeech = buff.Read<BloodBuff_PrimaryAttackLifeLeech_DataShared>();

            primaryAttackLifeLeech.RequiredBloodPercentage = 0;
            primaryAttackLifeLeech.MinBonus = primaryAttackLifeLeech.MaxBonus;
            buff.Write(primaryAttackLifeLeech);

            return;
        }

        if (buff.Has<BloodBuff_PrimaryProc_FreeCast_DataShared>())
        {
            var primaryProcFreeCast = buff.Read<BloodBuff_PrimaryProc_FreeCast_DataShared>();

            primaryProcFreeCast.RequiredBloodPercentage = 0;
            primaryProcFreeCast.MinBonus = primaryProcFreeCast.MaxBonus;
            buff.Write(primaryProcFreeCast);

            return;
        }

        if (buff.Has<BloodBuff_Rogue_AttackSpeedBonus_DataShared>())
        {
            var rogueAttackSpeedBonus = buff.Read<BloodBuff_Rogue_AttackSpeedBonus_DataShared>();

            rogueAttackSpeedBonus.RequiredBloodPercentage = 0;
            buff.Write(rogueAttackSpeedBonus);

            if (buff.Has<BloodBuff_Rogue_SpeedBonus_DataShared>()) // dracula blood
            {
                var rogueSpeedBonus = buff.Read<BloodBuff_Rogue_SpeedBonus_DataShared>();

                rogueSpeedBonus.RequiredBloodPercentage = 0;
                buff.Write(rogueSpeedBonus);

                return;
            }

            return;
        }

        if (buff.Has<BloodBuff_CritAmplifyProc_DataShared>())
        {
            var critAmplifyProc = buff.Read<BloodBuff_CritAmplifyProc_DataShared>();

            critAmplifyProc.RequiredBloodPercentage = 0;
            critAmplifyProc.MinBonus = critAmplifyProc.MaxBonus;
            buff.Write(critAmplifyProc);

            return;
        }

        if (buff.Has<BloodBuff_PhysCritChanceBonus_DataShared>())
        {
            var physCritChanceBonus = buff.Read<BloodBuff_PhysCritChanceBonus_DataShared>();

            physCritChanceBonus.RequiredBloodPercentage = 0;
            physCritChanceBonus.MinPhysicalCriticalStrikeChance = physCritChanceBonus.MaxPhysicalCriticalStrikeChance;
            buff.Write(physCritChanceBonus);

            return;
        }

        if (buff.Has<BloodBuff_Rogue_SpeedBonus_DataShared>())
        {
            var rogueSpeedBonus = buff.Read<BloodBuff_Rogue_SpeedBonus_DataShared>();

            rogueSpeedBonus.RequiredBloodPercentage = 0;
            rogueSpeedBonus.MinMovementSpeedIncrease = rogueSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(rogueSpeedBonus);

            return;
        }

        if (buff.Has<BloodBuff_ReducedTravelCooldown_DataShared>())
        {
            var reducedTravelCooldown = buff.Read<BloodBuff_ReducedTravelCooldown_DataShared>();

            reducedTravelCooldown.RequiredBloodPercentage = 0;
            reducedTravelCooldown.MinBonus = reducedTravelCooldown.MaxBonus;
            buff.Write(reducedTravelCooldown);

            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellCooldown_DataShared>())
        {
            var scholarSpellCooldown = buff.Read<BloodBuff_Scholar_SpellCooldown_DataShared>();

            scholarSpellCooldown.RequiredBloodPercentage = 0;
            scholarSpellCooldown.MinCooldownReduction = scholarSpellCooldown.MaxCooldownReduction;
            buff.Write(scholarSpellCooldown);

            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>())
        {
            var scholarSpellCritChanceBonus = buff.Read<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>();

            scholarSpellCritChanceBonus.RequiredBloodPercentage = 0;
            scholarSpellCritChanceBonus.MinSpellCriticalStrikeChance = scholarSpellCritChanceBonus.MaxSpellCriticalStrikeChance;
            buff.Write(scholarSpellCritChanceBonus);

            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellPowerBonus_DataShared>())
        {
            var scholarSpellPowerBonus = buff.Read<BloodBuff_Scholar_SpellPowerBonus_DataShared>();

            scholarSpellPowerBonus.RequiredBloodPercentage = 0;
            scholarSpellPowerBonus.MinSpellPowerIncrease = scholarSpellPowerBonus.MaxSpellPowerIncrease;
            buff.Write(scholarSpellPowerBonus);

            if (buff.Has<BloodBuff_Warrior_PhysDamageBonus_DataShared>()) // blood of the immortal
            {
                var warriorPhysDamageBonus = buff.Read<BloodBuff_Warrior_PhysDamageBonus_DataShared>();

                warriorPhysDamageBonus.RequiredBloodPercentage = 0;
                warriorPhysDamageBonus.MinPhysDamageIncrease = warriorPhysDamageBonus.MaxPhysDamageIncrease;
                buff.Write(warriorPhysDamageBonus);

                return;
            }

            return;
        }

        if (buff.Has<BloodBuff_SpellLifeLeech_DataShared>())
        {
            var spellLifeLeech = buff.Read<BloodBuff_SpellLifeLeech_DataShared>();

            spellLifeLeech.RequiredBloodPercentage = 0;
            spellLifeLeech.MinBonus = spellLifeLeech.MaxBonus;
            buff.Write(spellLifeLeech);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_DamageReduction_DataShared>())
        {
            var warriorDamageReduction = buff.Read<BloodBuff_Warrior_DamageReduction_DataShared>();

            warriorDamageReduction.RequiredBloodPercentage = 0;
            warriorDamageReduction.MinDamageReduction = warriorDamageReduction.MaxDamageReduction;
            buff.Write(warriorDamageReduction);

            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>())
        {
            var warriorPhysCritDamageBonus = buff.Read<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>();

            warriorPhysCritDamageBonus.RequiredBloodPercentage = 0;
            warriorPhysCritDamageBonus.MinWeaponCriticalStrikeDamageIncrease = warriorPhysCritDamageBonus.MaxWeaponCriticalStrikeDamageIncrease;
            buff.Write(warriorPhysCritDamageBonus);

            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysDamageBonus_DataShared>())
        {
            var warriorPhysDamageBonus = buff.Read<BloodBuff_Warrior_PhysDamageBonus_DataShared>();

            warriorPhysDamageBonus.RequiredBloodPercentage = 0;
            warriorPhysDamageBonus.MinPhysDamageIncrease = warriorPhysDamageBonus.MaxPhysDamageIncrease;
            buff.Write(warriorPhysDamageBonus);

            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysicalBonus_DataShared>())
        {
            var warriorPhysicalBonus = buff.Read<BloodBuff_Warrior_PhysicalBonus_DataShared>();

            warriorPhysicalBonus.RequiredBloodPercentage = 0;
            warriorPhysicalBonus.MinWeaponPowerIncrease = warriorPhysicalBonus.MaxWeaponPowerIncrease;
            buff.Write(warriorPhysicalBonus);

            return;
        }

        if (buff.Has<BloodBuff_Warrior_WeaponCooldown_DataShared>())
        {
            var warriorWeaponCooldown = buff.Read<BloodBuff_Warrior_WeaponCooldown_DataShared>();

            warriorWeaponCooldown.RequiredBloodPercentage = 0;
            warriorWeaponCooldown.MinCooldownReduction = warriorWeaponCooldown.MaxCooldownReduction;
            buff.Write(warriorWeaponCooldown);

            return;
        }

        if (buff.Has<BloodBuff_Brute_100_DataShared>())
        {
            var bruteEffect = buff.Read<BloodBuff_Brute_100_DataShared>();

            bruteEffect.RequiredBloodPercentage = 0;
            bruteEffect.MinHealthRegainPercentage = bruteEffect.MaxHealthRegainPercentage;
            buff.Write(bruteEffect);

            return;
        }

        if (buff.Has<BloodBuff_Rogue_100_DataShared>())
        {
            var rogueEffect = buff.Read<BloodBuff_Rogue_100_DataShared>();

            rogueEffect.RequiredBloodPercentage = 0;
            buff.Write(rogueEffect);

            return;
        }

        if (buff.Has<BloodBuff_Warrior_100_DataShared>())
        {
            var warriorEffect = buff.Read<BloodBuff_Warrior_100_DataShared>();

            warriorEffect.RequiredBloodPercentage = 0;
            buff.Write(warriorEffect);

            return;
        }

        if (buff.Has<BloodBuffScript_Scholar_MovementSpeedOnCast>())
        {
            var scholarEffect = buff.Read<BloodBuffScript_Scholar_MovementSpeedOnCast>();

            scholarEffect.RequiredBloodPercentage = 0;
            scholarEffect.ChanceToGainMovementOnCast_Min = scholarEffect.ChanceToGainMovementOnCast_Max;
            buff.Write(scholarEffect);

            return;
        }

        if (buff.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
        {
            var bruteAttackSpeedBonus = buff.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();

            bruteAttackSpeedBonus.MinValue = bruteAttackSpeedBonus.MaxValue;
            bruteAttackSpeedBonus.RequiredBloodPercentage = 0;
            bruteAttackSpeedBonus.GearLevel = 0f;
            buff.Write(bruteAttackSpeedBonus);

            return;
        }
    }
    public static void TryApplyPermanentBuff(Entity player, PrefabGUID buffPrefab)
    {
        if (player.TryApplyAndGetBuff(buffPrefab, out Entity buffEntity))
        {
            ModifyPermanentBuff(buffEntity);
        }
    }
    static void ModifyPermanentBuff(Entity buffEntity)
    {
        if (buffEntity.Has<BloodBuff>()) ModifyBloodBuff(buffEntity);

        if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
        {
            buffEntity.Remove<RemoveBuffOnGameplayEvent>();
        }

        if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
        {
            buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        }

        if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
        {
            buffEntity.Remove<CreateGameplayEventsOnSpawn>();
        }

        if (buffEntity.Has<GameplayEventListeners>())
        {
            buffEntity.Remove<GameplayEventListeners>();
        }

        if (!buffEntity.Has<Buff_Persists_Through_Death>())
        {
            buffEntity.Add<Buff_Persists_Through_Death>();
        }

        if (buffEntity.Has<DestroyOnGameplayEvent>())
        {
            buffEntity.Remove<DestroyOnGameplayEvent>();
        }

        if (buffEntity.Has<LifeTime>())
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });
        }
    }
    public static void HandleClassBuffs(Entity player, ulong steamId)
    {
        if (!HasClass(steamId)) return;

        if (!UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.TryGetValue(GetPlayerClass(steamId), out List<PrefabGUID> classBuffs)) return;
        else if (classBuffs.Count == 0) return;

        int levelStep = ConfigService.MaxLevel / classBuffs.Count;
        int playerLevel;

        if (ConfigService.LevelingSystem)
        {
            playerLevel = GetLevel(steamId);
        }
        else
        {
            Equipment equipment = player.Read<Equipment>();
            playerLevel = (int)equipment.GetFullLevel();
        }

        if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData[PrestigeType.Experience] > 0)
        {
            playerLevel = ConfigService.MaxLevel;
        }

        if (levelStep <= 0) return;

        int numBuffsToApply = playerLevel / levelStep;

        if (numBuffsToApply > 0 && numBuffsToApply <= classBuffs.Count)
        {
            numBuffsToApply = Math.Min(numBuffsToApply, classBuffs.Count); // Limit to available buffs

            for (int i = 0; i < numBuffsToApply; i++)
            {
                TryApplyPermanentBuff(player, classBuffs[i]);
            }
        }
    }
    public static void ModifyShinyBuff(Entity entity, PrefabGUID buffPrefabGuid) // using this for shardbearer visuals as well but prefer this method name
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            if (buffEntity.Has<Buff>())
            {
                BuffCategory component = buffEntity.Read<BuffCategory>();
                component.Groups = BuffCategoryFlag.None;
                buffEntity.Write(component);
            }
            if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
            {
                buffEntity.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (buffEntity.Has<GameplayEventListeners>())
            {
                buffEntity.Remove<GameplayEventListeners>();
            }
            if (buffEntity.Has<LifeTime>())
            {
                buffEntity.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });
            }
            if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
            {
                buffEntity.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
            {
                buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (buffEntity.Has<DealDamageOnGameplayEvent>())
            {
                buffEntity.Remove<DealDamageOnGameplayEvent>();
            }
            if (buffEntity.Has<HealOnGameplayEvent>())
            {
                buffEntity.Remove<HealOnGameplayEvent>();
            }
            if (buffEntity.Has<BloodBuffScript_ChanceToResetCooldown>())
            {
                buffEntity.Remove<BloodBuffScript_ChanceToResetCooldown>();
            }
            if (buffEntity.Has<ModifyMovementSpeedBuff>())
            {
                buffEntity.Remove<ModifyMovementSpeedBuff>();
            }
            if (buffEntity.Has<ApplyBuffOnGameplayEvent>())
            {
                buffEntity.Remove<ApplyBuffOnGameplayEvent>();
            }
            if (buffEntity.Has<DestroyOnGameplayEvent>())
            {
                buffEntity.Remove<DestroyOnGameplayEvent>();
            }
            if (buffEntity.Has<WeakenBuff>())
            {
                buffEntity.Remove<WeakenBuff>();
            }
            if (buffEntity.Has<ReplaceAbilityOnSlotBuff>())
            {
                buffEntity.Remove<ReplaceAbilityOnSlotBuff>();
            }
            if (buffEntity.Has<AmplifyBuff>())
            {
                buffEntity.Remove<AmplifyBuff>();
            }
        }    
    }
    public static void PrestigeBuffs()
    {
        List<int> prestigeBuffs = Configuration.ParseConfigIntegerString(ConfigService.PrestigeBuffs);

        foreach (int buff in prestigeBuffs)
        {
            UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Add(new PrefabGUID(buff));
        }
    }
    public static void ModifyExoFormBuff(Entity buffEntity, Entity playerCharacter)
    {
        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;

        int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int level) ? level : 0;
        float duration = steamId.TryGetPlayerExoFormData(out var exoFormData) ? exoFormData.Value : 0f;
        float bonusPhysicalPower = playerCharacter.TryGetComponent(out UnitStats unitStats) ? unitStats.SpellPower._Value : 0f;

        if (!buffEntity.Has<ReplaceAbilityOnSlotData>()) buffEntity.Add<ReplaceAbilityOnSlotData>();
        if (!buffEntity.Has<ScriptUpdate>()) buffEntity.Add<ScriptUpdate>();
        if (!buffEntity.Has<Script_Buff_Shapeshift_DataShared>()) buffEntity.Add<Script_Buff_Shapeshift_DataShared>();
        //if (!buffEntity.Has<ModifyTargetHUDBuff>()) buffEntity.Add<ModifyTargetHUDBuff>();
        if (!buffEntity.Has<AmplifyBuff>()) buffEntity.Add<AmplifyBuff>();
        if (!buffEntity.Has<ChangeKnockbackResistanceBuff>())
        {
            buffEntity.AddWith((ref ChangeKnockbackResistanceBuff changeKnockbackResistance) =>
            {
                changeKnockbackResistance.KnockbackResistanceIndex = 6;
            });
        }
        if (!buffEntity.Has<ModifyUnitStatBuff_DOTS>())
        {
            EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
        }

        var buffer = buffEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();

        ModifyUnitStatBuff_DOTS extraPhysicalPower = new()
        {
            StatType = UnitStatType.PhysicalPower,
            ModificationType = ModificationType.AddToBase,
            Value = bonusPhysicalPower,
            Modifier = 1,
            IncreaseByStacks = false,
            ValueByStacks = 0,
            Priority = 0,
            Id = ModificationIDs.Create().NewModificationId()
        };

        buffer.Add(extraPhysicalPower);

        ModifyUnitStatBuffSystemSpawn.OnUpdate();

        AmplifyBuff amplifyBuff = new()
        {
            AmplifyModifier = -0.25f
        };

        buffEntity.Write(amplifyBuff);

        /*
        if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
        {
            CreateGameplayEventsOnSpawn createGameplayEventsOnSpawn = buffEntity.ReadBuffer<CreateGameplayEventsOnSpawn>()[0];
            GameplayEventId eventId = createGameplayEventsOnSpawn.EventId;
            GameplayEventTarget eventTarget = createGameplayEventsOnSpawn.Target;

            buffEntity.Remove<CreateGameplayEventsOnSpawn>();
            if (!buffEntity.Has<CreateGameplayEventsOnDestroy>())
            {
                var createGameplayEventsOnDestroyBuffer = EntityManager.AddBuffer<CreateGameplayEventsOnDestroy>(buffEntity);

                CreateGameplayEventsOnDestroy createGameplayEventsOnDestroy = new()
                {
                    EventId = eventId,
                    Target = eventTarget,
                    SpecificDestroyReason = false,
                    DestroyReason = DestroyReason.Default
                };

                createGameplayEventsOnDestroyBuffer.Add(createGameplayEventsOnDestroy);

                var spawnPrefabOnGameplayEventBuffer = buffEntity.ReadBuffer<SpawnPrefabOnGameplayEvent>();

                SpawnPrefabOnGameplayEvent spawnPrefabOnGameplayEvent = spawnPrefabOnGameplayEventBuffer[0];
                spawnPrefabOnGameplayEvent.SpawnPrefab = ExoFormExitBuff;

                spawnPrefabOnGameplayEventBuffer[0] = spawnPrefabOnGameplayEvent;
            }
        }

        ModifyTargetHUDBuff modifyTargetHUDBuff = new()
        {
            Height = 1.25f,
            CharacterHUDHeightModId = ModificationId.Empty
        };

        buffEntity.Write(modifyTargetHUDBuff);
        */

        // change to block buff to prevent other replaceAbilityOnSlotBuffers from overriding for the duration? 
        buffEntity.With((ref Buff buff) =>
        {
            buff.BuffType = BuffType.Block;
        });

        buffEntity.With((ref BuffCategory buffCategory) =>
        {
            buffCategory.Groups = BuffCategoryFlag.Shapeshift | BuffCategoryFlag.RemoveOnDisconnect;
        });

        if (!buffEntity.Has<LifeTime>()) buffEntity.Add<LifeTime>();

        buffEntity.Write(new LifeTime
        {
            Duration = duration,
            EndAction = LifeTimeEndAction.Destroy
        });

        if (!buffEntity.Has<ReplaceAbilityOnSlotBuff>())
        {
            EntityManager.AddBuffer<ReplaceAbilityOnSlotBuff>(buffEntity);
        }

        var replaceAbilityOnSlotBuffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        foreach (var keyValuePair in ExoFormAbilityMap)
        {
            ReplaceAbilityOnSlotBuff replaceAbilityOnSlotBuff = new()
            {
                Target = ReplaceAbilityTarget.BuffTarget,
                Slot = keyValuePair.Key,
                NewGroupId = ExoFormUnlockMap[keyValuePair.Key] <= exoLevel ? keyValuePair.Value : PrefabGUID.Empty,
                Priority = 99,
                CopyCooldown = true,
                CastBlockType = GroupSlotModificationCastBlockType.WholeCast
            };

            replaceAbilityOnSlotBuffer.Add(replaceAbilityOnSlotBuff);
        }

        ReplaceAbilityOnSlotSystem.OnUpdate();

        string durationMessage = $"<color=red>Dracula's</color> latent power made manifest... (<color=white>{(int)duration}</color>s)";
        LocalizationService.HandleServerReply(EntityManager, user, durationMessage);

        ExoForm.ExoFormCountdown(buffEntity, playerCharacter, userEntity, duration - EXO_COUNTDOWN).Start();
    }
    public static bool IsPlayerInCombat(this Entity entity)
    {
        if (entity.IsPlayer())
        {
            return entity.HasBuff(_pveCombatBuff) || entity.HasBuff(_pvpCombatBuff);
        }

        return false;
    }
    public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity)
    {
        if (ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity))
        {
            return true;
        }

        return false;
    }
    public static bool TryRemoveBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (entity.TryGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);

            return true;
        }

        return false;
    }
    public static bool TryApplyAndGetBuff(this Entity entity, PrefabGUID buffPrefabGuid, out Entity buffEntity)
    {
        buffEntity = Entity.Null;

        if (entity.TryApplyBuff(buffPrefabGuid) && entity.TryGetBuff(buffPrefabGuid, out buffEntity))
        {
            return true;
        }

        return false;
    }
    public static bool TryApplyBuffWithOwner(this Entity target, Entity owner, PrefabGUID buffPrefabGuid)
    {
        if (target.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity) && buffEntity.Has<EntityOwner>())
        {
            buffEntity.With((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = owner;
            });

            return true;
        }

        return false;
    }
    public static void TryApplyBuffWithLifeTime(this Entity entity, PrefabGUID buffPrefabGuid, float duration)
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            if (!buffEntity.Has<LifeTime>()) buffEntity.Add<LifeTime>();

            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = duration;
                lifeTime.EndAction = LifeTimeEndAction.Destroy;
            });
        }
    }
    public static bool TryApplyAndGetBuffWithOwner(this Entity target, Entity owner, PrefabGUID buffPrefabGUID, out Entity buffEntity)
    {
        buffEntity = Entity.Null;

        if (target.TryApplyAndGetBuff(buffPrefabGUID, out buffEntity))
        {
            buffEntity.With((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = owner;
            });

            return true;
        }

        return false;
    }
    public static void RefreshStats(Entity playerCharacter)
    {
        if (playerCharacter.HasBuff(_vBloodBloodBuff)) playerCharacter.TryRemoveBuff(_vBloodBloodBuff);
    }
    static void Testing(Entity buffEntity)
    {    
        Buff_ApplyBuffOnDamageTypeDealt_DataShared buff_ApplyBuffOnDamageTypeDealt = new()
        {
            OnDamageDealtListener = ServerGameManager.AddEventListener<DealDamageEvent>(buffEntity, null),
        };

        // NativeArray<Entity> entitiesAsync = __instance.__query_401358720_0.ToEntityArrayAsync(Allocator.TempJob, out JobHandle job);

        // while (!job.IsCompleted)
        {

        }
    }
}
