using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using static Bloodcraft.Utilities.ClassUtilities;

namespace Bloodcraft.Utilities;
internal static class BuffUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;
    
    public const float BaseDuration = 15f;
    public const float MaxDuration = 180f;

    static readonly AssetGuid AssetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly float3 Red = new(1f, 0f, 0f);

    static readonly WaitForSeconds SecondDelay = new(1f);

    public static readonly Dictionary<int, PrefabGUID> ExoFormAbilityMap = new()
    {
        { 0, new(-1473399128) }, // primary fast shockwaveslash
        { 1, new(841757706) }, // first weapon skill downswing detonate
        { 2, new(-1940289109) }, // dash skill teleport behind target on shift
        { 3, new(1270706044) }, // shift veil of bats
        { 4, new(532210332) }, // second weapon skill sword throw
        { 5, new(716346677) }, // batswarm?
        { 6, new(-1161896955) }, // etherial sword
        { 7, new(-7407393) } // ring of blood
    };

    static readonly Dictionary<int, int> ExoFormAbilityUnlockMap = new()
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

    //static readonly PrefabGUID ExoFormExitBuff = new(958508368);
    public static bool TryApplyBuff(Entity character, PrefabGUID buffPrefab)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab
        };

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character
        };

        if (!ServerGameManager.HasBuff(character, buffPrefab.ToIdentifier()))
        {
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            return true;
        }

        return false;
    }
    public static bool TryApplyBuffWithOwner(Entity target, Entity familiar, PrefabGUID buffPrefab)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab,
            Who = target.Read<NetworkId>()
        };

        FromCharacter fromCharacter = new() // fam should be entityOwner
        {
            Character = familiar,
            User = familiar
        };

        if (!ServerGameManager.HasBuff(target, buffPrefab.ToIdentifier()))
        {
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            return true;
        }

        return false;
    }
    public static void ModifyBloodBuff(Entity buff)
    {
        //Core.Log.LogInfo("ModifyBloodBuff: " + buff.Read<PrefabGUID>().LookupName());

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
            biteToMutant.MutantFaction = new(877850148); // slaves_rioters
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
    public static void ApplyPermanentBuff(Entity player, PrefabGUID buffPrefab)
    {
        //Core.Log.LogInfo("ApplyPermanentBuff: " + buffPrefab.LookupName());
        bool appliedBuff = TryApplyBuff(player, buffPrefab);

        if (appliedBuff && ServerGameManager.TryGetBuff(player, buffPrefab.ToIdentifier(), out Entity buffEntity))
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

        if (buffEntity.Has<LifeTime>()) // add LifeTime if doesn't have one to mark for checking the prestige buff list later? so can reference prestige buff list then see if the buff had an infinite lifetime to determine if should sync again or not
        {
            LifeTime lifeTime = buffEntity.Read<LifeTime>();
            lifeTime.Duration = -1;
            lifeTime.EndAction = LifeTimeEndAction.None;
            buffEntity.Write(lifeTime);
        }
    }
    public static void ApplyClassBuffs(Entity player, ulong steamId)
    {
        if (!HasClass(steamId)) return;

        if (!UpdateBuffsBufferDestroyPatch.ClassBuffs.TryGetValue(GetPlayerClass(steamId), out List<PrefabGUID> classBuffs)) return;
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
                ApplyPermanentBuff(player, classBuffs[i]);
            }
        }
    }
    public static void HandleVisual(Entity entity, PrefabGUID visual)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = visual,
        };

        FromCharacter fromCharacter = new()
        {
            Character = entity,
            User = entity
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
        {
            if (buff.Has<Buff>())
            {
                BuffCategory component = buff.Read<BuffCategory>();
                component.Groups = BuffCategoryFlag.None;
                buff.Write(component);
            }
            if (buff.Has<CreateGameplayEventsOnSpawn>())
            {
                buff.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (buff.Has<GameplayEventListeners>())
            {
                buff.Remove<GameplayEventListeners>();
            }
            if (buff.Has<LifeTime>())
            {
                LifeTime lifetime = buff.Read<LifeTime>();
                lifetime.Duration = -1;
                lifetime.EndAction = LifeTimeEndAction.None;
                buff.Write(lifetime);
            }
            if (buff.Has<RemoveBuffOnGameplayEvent>())
            {
                buff.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (buff.Has<RemoveBuffOnGameplayEventEntry>())
            {
                buff.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (buff.Has<DealDamageOnGameplayEvent>())
            {
                buff.Remove<DealDamageOnGameplayEvent>();
            }
            if (buff.Has<HealOnGameplayEvent>())
            {
                buff.Remove<HealOnGameplayEvent>();
            }
            if (buff.Has<BloodBuffScript_ChanceToResetCooldown>())
            {
                buff.Remove<BloodBuffScript_ChanceToResetCooldown>();
            }
            if (buff.Has<ModifyMovementSpeedBuff>())
            {
                buff.Remove<ModifyMovementSpeedBuff>();
            }
            if (buff.Has<ApplyBuffOnGameplayEvent>())
            {
                buff.Remove<ApplyBuffOnGameplayEvent>();
            }
            if (buff.Has<DestroyOnGameplayEvent>())
            {
                buff.Remove<DestroyOnGameplayEvent>();
            }
            if (buff.Has<WeakenBuff>())
            {
                buff.Remove<WeakenBuff>();
            }
            if (buff.Has<ReplaceAbilityOnSlotBuff>())
            {
                buff.Remove<ReplaceAbilityOnSlotBuff>();
            }
            if (buff.Has<AmplifyBuff>())
            {
                buff.Remove<AmplifyBuff>();
            }
        }
    }
    public static void PrestigeBuffs()
    {
        List<int> prestigeBuffs = ConfigUtilities.ParseConfigIntegerString(ConfigService.PrestigeBuffs);

        foreach (int buff in prestigeBuffs)
        {
            UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Add(new PrefabGUID(buff));
        }
    }
    public static void SanitizePrestigeBuffs(Entity character)
    {
        foreach (PrefabGUID buff in UpdateBuffsBufferDestroyPatch.PrestigeBuffs)
        {
            if (ServerGameManager.TryGetBuff(character, buff.ToIdentifier(), out Entity buffEntity) && buffEntity.Has<LifeTime>())
            {
                buffEntity.Remove<LifeTime>();
            }
        }
    }
    public static void HandleExoFormBuff(Entity buffEntity, Entity playerCharacter)
    {
        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.Read<User>();
        ulong steamId = user.PlatformId;

        int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int level) ? level : 0;

        float duration = CalculateFormDuration(exoLevel);
        //Core.Log.LogInfo("HandleExoFormBuff...");

        if (!buffEntity.Has<ReplaceAbilityOnSlotData>()) buffEntity.Add<ReplaceAbilityOnSlotData>();
        if (!buffEntity.Has<ScriptUpdate>()) buffEntity.Add<ScriptUpdate>();
        if (!buffEntity.Has<Script_Buff_Shapeshift_DataShared>()) buffEntity.Add<Script_Buff_Shapeshift_DataShared>();
        if (!buffEntity.Has<ModifyTargetHUDBuff>()) buffEntity.Add<ModifyTargetHUDBuff>();
        
        ModifyTargetHUDBuff modifyTargetHUDBuff = new()
        {
            Height = 1.25f,
            CharacterHUDHeightModId = ModificationId.Empty
        };

        buffEntity.Write(modifyTargetHUDBuff);

        // change to block buff to prevent other replaceAbilityOnSlotBuffers from overriding for the duration?
        buffEntity.With((ref Buff buff) =>
        {
            buff.BuffType = BuffType.Block;
        });

        buffEntity.With((ref BuffCategory buffCategory) =>
        {
            buffCategory.Groups = BuffCategoryFlag.Shapeshift | BuffCategoryFlag.RemoveOnDisconnect;
        });

        buffEntity.Add<LifeTime>();
        buffEntity.Write(new LifeTime
        {
            Duration = duration,
            EndAction = LifeTimeEndAction.Destroy
        });

        if (!buffEntity.Has<ReplaceAbilityOnSlotBuff>())
        {
            EntityManager.AddBuffer<ReplaceAbilityOnSlotBuff>(buffEntity);
        }

        var buffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        foreach (var keyValuePair in ExoFormAbilityMap)
        {
            ReplaceAbilityOnSlotBuff replaceAbilityOnSlotBuff = new()
            {
                Target = ReplaceAbilityTarget.BuffTarget,
                Slot = keyValuePair.Key,
                NewGroupId = ExoFormAbilityUnlockMap[keyValuePair.Key] <= exoLevel ? keyValuePair.Value : PrefabGUID.Empty,
                Priority = 99,
                CopyCooldown = true,
                CastBlockType = GroupSlotModificationCastBlockType.WholeCast
            };

            buffer.Add(replaceAbilityOnSlotBuff);
        }

        ReplaceAbilityOnSlotSystem.OnUpdate();

        string durationMessage = $"<color=red>Dracula's</color> latent power made manifest... (<color=white>{duration}</color>s)";
        LocalizationService.HandleServerReply(EntityManager, user, durationMessage);

        Core.StartCoroutine(ExoFormCountdown(buffEntity, playerCharacter, userEntity, duration - 5f)); // Start countdown messages 5 seconds before buff expires
    }
    public static float CalculateFormDuration(int prestigeLevel)
    {
        // Linear scaling from 15s to 120s over 1-100 prestige levels
        return BaseDuration + (MaxDuration / ConfigService.ExoPrestiges) * (prestigeLevel - 1);
    }
    static IEnumerator ExoFormCountdown(Entity buffEntity, Entity playerEntity, Entity userEntity, float countdownDelay)
    {
        yield return new WaitForSeconds(countdownDelay);

        float countdown = 5f;

        // Wait until there are 5 seconds left
        while (buffEntity.Exists() && countdown > 0f)
        {
            float3 targetPosition = playerEntity.Read<Translation>().Value;
            targetPosition = new float3(targetPosition.x, targetPosition.y + 1.5f, targetPosition.z);

            ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                AssetGuid,
                targetPosition,
                Red,
                playerEntity,
                countdown,
                default,
                userEntity
            );

            countdown--;
            yield return SecondDelay;
        }

        UpdateFullExoFormChargeUsed(playerEntity.GetSteamId());
    }
    public static void UpdateExoFormChargeStored(ulong steamId)
    {
        // add energy based on last time form was exited till now
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            DateTime now = DateTime.UtcNow;

            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
            float totalDuration = CalculateFormDuration(exoLevel);

            // energy earned based on total duration from exo level times fraction of time passed in seconds per day?
            float chargedEnergy = (float)(((now - exoFormData.Key).TotalSeconds / 86400) * totalDuration);
            float chargeStored = Mathf.Min(exoFormData.Value + chargedEnergy, MaxDuration);

            KeyValuePair<DateTime, float> timeEnergyPair = new(now, chargeStored);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdatePartialExoFormChargeUsed(Entity buffEntity, ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;

            float totalDuration = CalculateFormDuration(exoLevel);
            float remainingTime = buffEntity.Read<LifeTime>().Duration;
            float timeInForm = totalDuration - remainingTime;

            // set stamp to start 'charging' energy, subtract energy used based on duration and exo level
            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, exoFormData.Value - timeInForm);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdateFullExoFormChargeUsed(ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, 0f);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
}
