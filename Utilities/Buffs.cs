using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Buffs
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    public static readonly PrefabGUID HighlordDeadBuff = PrefabGUIDs.AB_HighLordSword_SelfStun_DeadBuff;
    public static readonly PrefabGUID CastleManCombatBuff = PrefabGUIDs.Buff_InCombat_Npc_CastleMan;
    public static readonly PrefabGUID StandardWerewolfBuff = PrefabGUIDs.Buff_General_Shapeshift_Werewolf_Standard;
    public static readonly PrefabGUID VBloodWerewolfBuff = PrefabGUIDs.Buff_General_Shapeshift_Werewolf_VBlood;
    public static readonly PrefabGUID DisableAggroBuff = PrefabGUIDs.Buff_Illusion_Mosquito_DisableAggro;
    public static readonly PrefabGUID InteractModeBuff = PrefabGUIDs.AB_Militia_HoundMaster_QuickShot_Buff;
    public static readonly PrefabGUID AdminInvulnerableBuff = PrefabGUIDs.Admin_Invulnerable_Buff;
    public static readonly PrefabGUID InvisibleAndImmaterialBuff = PrefabGUIDs.AB_InvisibilityAndImmaterial_Buff;
    public static readonly PrefabGUID DraculaBloodCurseBuff = PrefabGUIDs.Buff_Vampire_Dracula_BloodCurse; // BloodyPoint -89195359
    public static readonly PrefabGUID PvECombatBuff = PrefabGUIDs.Buff_InCombat;
    public static readonly PrefabGUID PvPCombatBuff = PrefabGUIDs.Buff_InCombat_PvPVampire;
    public static readonly PrefabGUID VBloodAbilityReplaceBuff = PrefabGUIDs.Buff_VBlood_Ability_Replace;
    public static readonly PrefabGUID VargulfBleedBuff = PrefabGUIDs.AB_Legion_Vargulf_SlicingArm_BleedBuff;
    public static readonly PrefabGUID DefaultEmoteBuff = PrefabGUIDs.AB_Emote_Buff_Default_NoAnimation;
    public static readonly PrefabGUID TauntEmoteBuff = PrefabGUIDs.AB_Emote_Vampire_Taunt_Buff;
    public static readonly PrefabGUID ShroudBuff = PrefabGUIDs.EquipBuff_ShroudOfTheForest;
    public static readonly PrefabGUID DominateBuff = PrefabGUIDs.AB_Shapeshift_DominatingPresence_PsychicForm_Buff;
    public static readonly PrefabGUID VanishBuff = PrefabGUIDs.AB_Bandit_Thief_Rush_Buff;
    public static readonly PrefabGUID HolyBubbleBuff = PrefabGUIDs.AB_ChurchOfLight_Paladin_HolyBubble_InvulnerableBuff;
    public static readonly PrefabGUID GateBossFeedCompleteBuff = PrefabGUIDs.AB_FeedGateBoss_04_Complete_AreaTriggerBuff;
    public static readonly PrefabGUID HolyBeamPowerBuff = PrefabGUIDs.AB_CastleMan_HolyBeam_PowerBuff_01;
    public static readonly PrefabGUID PvPProtectedBuff = PrefabGUIDs.Buff_General_PvPProtected;
    public static readonly PrefabGUID PhasingBuff = PrefabGUIDs.Buff_General_Phasing;
    public static readonly PrefabGUID WitchPigTransformationBuff = PrefabGUIDs.Witch_PigTransformation_Buff;
    public static readonly PrefabGUID WranglerPotionBuff = PrefabGUIDs.AB_Consumable_WranglerPotion_T01_Buff;
    public static readonly PrefabGUID HighlordGroundSwordBossBuff = PrefabGUIDs.AB_HighLord_GroundSword_PermaBuff_Boss;
    public static readonly PrefabGUID HighlordGroundSwordSpawnBuff = PrefabGUIDs.AB_HighLord_GroundSword_SilenceBuff_Boss;
    public static readonly PrefabGUID InkCrawlerDeathBuff = PrefabGUIDs.Buff_InkCrawler_Timer;
    public static readonly PrefabGUID TargetSwallowedBuff = PrefabGUIDs.AB_Cursed_ToadKing_Swallow_TargetSwallowedBuff;
    public static readonly PrefabGUID CombatStanceBuff = PrefabGUIDs.Buff_CombatStance;
    public static readonly PrefabGUID DraculaReturnHideBuff = PrefabGUIDs.Buff_Vampire_Dracula_ReturnHide;
    public static readonly PrefabGUID ActiveCharmedHumanBuff = PrefabGUIDs.AB_Charm_Active_Human_Buff;
    public static readonly PrefabGUID StormShieldTertiaryBuff = PrefabGUIDs.AB_Storm_Discharge_StormShield_Buff_03;
    public static readonly PrefabGUID StormShieldSecondaryBuff = PrefabGUIDs.AB_Storm_Discharge_StormShield_Buff_02;
    public static readonly PrefabGUID StormShieldPrimaryBuff = PrefabGUIDs.AB_Storm_Discharge_StormShield_Buff_01;
    public static readonly PrefabGUID TakeFlightBuff = PrefabGUIDs.AB_Shapeshift_Bat_TakeFlight_Buff;

    public static readonly PrefabGUID GarlicDebuff = PrefabGUIDs.Buff_General_Garlic_Area_Inside;
    public static readonly PrefabGUID SilverDebuff = PrefabGUIDs.Buff_General_Silver_Sickness_Burn_Debuff;
    public static readonly PrefabGUID HolyDebuff = PrefabGUIDs.Buff_General_Holy_Area_T01;
    public static readonly PrefabGUID DivineDebuff = PrefabGUIDs.Buff_General_Holy_Area_T02;

    public static readonly PrefabGUID VampireLeechDebuff = PrefabGUIDs.Blood_Vampire_Buff_Leech;
    public static readonly PrefabGUID VampireCondemnDebuff = PrefabGUIDs.Unholy_Vampire_Buff_Condemn;
    public static readonly PrefabGUID VampireIgniteDebuff = PrefabGUIDs.Chaos_Vampire_Buff_Ignite;
    public static readonly PrefabGUID VampireChillDebuff = PrefabGUIDs.Frost_Vampire_Buff_Chill;
    public static readonly PrefabGUID VampireWeakenDebuff = PrefabGUIDs.Illusion_Vampire_Buff_Weaken;
    public static readonly PrefabGUID VampireStaticDebuff = PrefabGUIDs.Storm_Vampire_Buff_Static;

    public static readonly PrefabGUID BloodCurseBuff = PrefabGUIDs.AB_Blood_VampiricCurse_Buff;
    public static readonly PrefabGUID StormChargeBuff = PrefabGUIDs.Storm_Vampire_Buff_Static_WeaponCharge;
    public static readonly PrefabGUID FrostWeaponBuff = PrefabGUIDs.AB_Frost_FrostWeapon_Buff;
    public static readonly PrefabGUID ChaosHeatedBuff = PrefabGUIDs.Chaos_Vampire_Buff_Heated;
    public static readonly PrefabGUID IllusionShieldBuff = PrefabGUIDs.Illusion_Vampire_SpellMod_Shield_Buff;
    public static readonly PrefabGUID UnholyAmplifyBuff = PrefabGUIDs.Unholy_Vampire_Buff_Amplify;

    public static readonly PrefabGUID EvolvedVampireBuff = PrefabGUIDs.Buff_Vampire_Dracula_SpellPhase;
    public static readonly PrefabGUID CorruptedSerpentBuff = PrefabGUIDs.AB_Blackfang_Morgana_Transformation_SnakePhaseBuff;
    public static readonly PrefabGUID AncientGuardianBuff = PrefabGUIDs.AB_Geomancer_Transform_ToGolem_Buff;

    public static readonly PrefabGUID BonusStatsBuff = PrefabGUIDs.SetBonus_AllLeech_T09;
    public static readonly PrefabGUID BonusPlayerStatsBuff = BonusStatsBuff;
    public static readonly PrefabGUID BonusFamiliarStatsBuff = BonusStatsBuff;

    static readonly Dictionary<PrefabGUID, int> _buffMaxStacks = [];
    public static bool TryApplyBuff(this Entity entity, PrefabGUID prefabGuid)
    {
        bool hasBuff = entity.HasBuff(prefabGuid);

        if (hasBuff && ShouldApplyStack(entity, prefabGuid, out Entity buffEntity, out byte stacks))
        {
            // ServerGameManager.CreateStacksIncreaseEvent(buffEntity, stacks, ++stacks);
            ServerGameManager.InstantiateBuffEntityImmediate(entity, entity, prefabGuid, null, stacks);
        }
        else if (!hasBuff)
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = prefabGuid,
                Who = entity.GetNetworkId(),
            };

            FromCharacter fromCharacter = new()
            {
                Character = entity,
                User = entity.IsPlayer() ? entity.GetUserEntity() : entity
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

            // ServerGameManager.InstantiateBuffEntityImmediate(entity, entity, prefabGuid, null, 0); better? worse? who knooooows maybe I'll check later
            return true;
        }

        return false;
    }
    static bool ShouldApplyStack(Entity entity, PrefabGUID prefabGuid, out Entity buffEntity, out byte stacks)
    {
        buffEntity = Entity.Null;
        stacks = 0;

        if (_buffMaxStacks.TryGetValue(prefabGuid, out int maxStacks)
            && entity.TryGetBuffStacks(prefabGuid, out buffEntity, out int buffStacks))
        {
            stacks = (byte)buffStacks;
            return stacks < maxStacks;
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
    public static bool TryGetBuffStacks(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity, out int stacks)
    {
        stacks = 0;

        if (ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity)
            && buffEntity.TryGetComponent(out Buff buff))
        {
            stacks = buff.Stacks;
            return true;
        }

        return false;
    }
    public static void TryRemoveBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (entity.TryGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            if (buffPrefabGuid.Equals(BonusStatsBuff))
            {
                // if (entity.IsPlayer()) ScriptSpawnServerPatch.RemovePlayerBonusStats(buffEntity, entity);
                // else if (entity.IsFamiliar()) Progression.RemoveFamiliarStats(buffEntity, entity);
            }

            buffEntity.Destroy();
        }
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
            buffEntity.With((ref EntityOwner entityOwner) => entityOwner.Owner = owner);

            return true;
        }

        return false;
    }
    public static void TryApplyBuffInteractMode(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            buffEntity.AddWith((ref BuffModificationFlagData buffFlagData) =>
            {
                buffFlagData.ModificationId = ModificationIDs.Create().NewModificationId();
                buffFlagData.ModificationTypes = (long)BuffModificationTypes.MovementImpair;
            });

            buffEntity.AddWith((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });
        }
    }
    public static void TryApplyBuffWithLifeTimeNone(this Entity entity, PrefabGUID buffPrefabGuid) // should rename to make permanent but not in a way that implies after death kept
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            buffEntity.AddWith((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });
        }
    }
    public static void TryApplyBuffWithLifeTimeDestroy(this Entity entity, PrefabGUID buffPrefabGuid, float duration)
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            buffEntity.AddWith((ref LifeTime lifeTime) =>
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
            buffEntity.With((ref EntityOwner entityOwner) => entityOwner.Owner = owner);

            return true;
        }

        return false;
    }
    public static void TryApplyPermanentBuff(Entity playerCharacter, PrefabGUID buffPrefab)
    {
        if (playerCharacter.TryApplyAndGetBuff(buffPrefab, out Entity buffEntity))
        {
            // Core.Log.LogInfo($"Applying permanent buff {buffPrefab.GetPrefabName()} to {playerCharacter.GetSteamId()}");
            ModifyPermanentBuff(buffEntity);
        }
    }
    static void ModifyPermanentBuff(Entity buffEntity)
    {
        buffEntity.Remove<RemoveBuffOnGameplayEvent>();
        buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        buffEntity.Remove<CreateGameplayEventsOnSpawn>();
        buffEntity.Remove<GameplayEventListeners>();
        buffEntity.Remove<Buff_Persists_Through_Death>(); // not sure why removing this here now that I'm thinking about it but can't afford to FAFO with what apparently works atm
        buffEntity.Remove<DestroyOnGameplayEvent>();

        if (buffEntity.Has<LifeTime>())
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });
        }
    }
    public static void HandleShinyBuff(Entity entity, PrefabGUID buffPrefabGuid)
    {
        if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
        {
            buffEntity.HasWith((ref Buff buff) =>
            {
                buff.MaxStacks = 3;
                buff.IncreaseStacks = true;
                buff.Stacks = 1;
            });

            buffEntity.HasWith((ref BuffCategory buffCategory) => buffCategory.Groups = BuffCategoryFlag.None);

            buffEntity.HasWith((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });

            buffEntity.Remove<CreateGameplayEventsOnSpawn>();
            buffEntity.Remove<GameplayEventListeners>();
            buffEntity.Remove<RemoveBuffOnGameplayEvent>();
            buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
            buffEntity.Remove<DealDamageOnGameplayEvent>();
            buffEntity.Remove<HealOnGameplayEvent>();
            buffEntity.Remove<BloodBuffScript_ChanceToResetCooldown>();
            buffEntity.Remove<ModifyMovementSpeedBuff>();
            buffEntity.Remove<ApplyBuffOnGameplayEvent>();
            buffEntity.Remove<DestroyOnGameplayEvent>();
            buffEntity.Remove<WeakenBuff>();
            buffEntity.Remove<ReplaceAbilityOnSlotBuff>();
            buffEntity.Remove<AmplifyBuff>();
        }
    }
    public static void GetPrestigeBuffs()
    {
        List<int> prestigeBuffs = Configuration.ParseIntegersFromString(ConfigService.PrestigeBuffs);

        foreach (int buff in prestigeBuffs)
        {
            UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Add(new PrefabGUID(buff));
        }
    }
    public static bool PlayerInCombat(this Entity entity)
    {
        if (entity.IsPlayer())
        {
            return entity.HasBuff(PvECombatBuff) || entity.HasBuff(PvPCombatBuff);
        }

        return false;
    }
    public static void GetStackableBuffs()
    {
        var prefabGuidsToEntities = PrefabCollectionSystem._PrefabGuidToEntityMap;

        var prefabGuids = prefabGuidsToEntities.GetKeyArray(Allocator.Temp);
        var entities = prefabGuidsToEntities.GetValueArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                PrefabGUID prefabGuid = prefabGuids[i];
                Entity entity = entities[i];

                if (entity.IsStackableBuff(out int maxStacks))
                {
                    _buffMaxStacks[prefabGuid] = maxStacks;
                }
            }
        }
        finally
        {
            prefabGuids.Dispose();
            entities.Dispose();
        }
    }

    static PrefabGUID _buffArenaActive = PrefabGUIDs.Buff_Arena_Active;
    static PrefabGUID _buffDuelActive = PrefabGUIDs.Buff_Duel_Active;
    public static bool IsDueling(this Entity playerCharacter)
    {
        if (playerCharacter.HasBuff(_buffArenaActive) || playerCharacter.HasBuff(_buffDuelActive))
        {
            return true;
        }

        return false;
    }
    public static void RefreshStats(Entity entity)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = BonusStatsBuff,
            Who = entity.GetNetworkId(),
        };

        FromCharacter fromCharacter = new()
        {
            Character = entity,
            User = entity.IsPlayer() ? entity.GetUserEntity() : entity
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
    }
}

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