using Bloodcraft.Resources;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;

internal static class ScalingMarkers
{
    static readonly PrefabGUID _primalRiftMarkerBuff = PrefabGUIDs.Item_EquipBuff_MagicSource_BloodKey_T01;

    const int PRIMAL_RIFT_MARKER_LEVEL = 731002;

    /// <summary>
    /// Returns whether the entity has the inert Primal Rift scaling marker buff.
    /// </summary>
    /// <param name="entity">The unit entity to inspect.</param>
    /// <returns>True when the Primal marker buff exists with the expected marker value.</returns>
    public static bool HasPrimalRiftScalingMarker(Entity entity)
    {
        return HasMarker(entity, _primalRiftMarkerBuff, PRIMAL_RIFT_MARKER_LEVEL);
    }

    /// <summary>
    /// Returns whether the entity has the inert Primal Rift scaling marker buff.
    /// </summary>
    /// <param name="entity">The unit entity to inspect.</param>
    /// <param name="serverGameManager">The server game manager to use for buff lookup.</param>
    /// <returns>True when the Primal marker buff exists with the expected marker value.</returns>
    public static bool HasPrimalRiftScalingMarker(Entity entity, ServerGameManager serverGameManager)
    {
        return HasMarker(entity, _primalRiftMarkerBuff, PRIMAL_RIFT_MARKER_LEVEL, serverGameManager);
    }

    /// <summary>
    /// Adds or stamps the inert Primal Rift scaling marker buff on the entity.
    /// </summary>
    /// <param name="entity">The unit entity to mark.</param>
    /// <returns>True when the marker was present or successfully stamped.</returns>
    public static bool TryAddPrimalRiftScalingMarker(Entity entity)
    {
        return TryAddMarker(entity, _primalRiftMarkerBuff, PRIMAL_RIFT_MARKER_LEVEL);
    }

    /// <summary>
    /// Adds or stamps the inert Primal Rift scaling marker buff on the entity.
    /// </summary>
    /// <param name="entity">The unit entity to mark.</param>
    /// <param name="serverGameManager">The server game manager to use for buff lookup and creation.</param>
    /// <returns>True when the marker was present or successfully stamped.</returns>
    public static bool TryAddPrimalRiftScalingMarker(Entity entity, ServerGameManager serverGameManager)
    {
        return TryAddMarker(entity, _primalRiftMarkerBuff, PRIMAL_RIFT_MARKER_LEVEL, serverGameManager);
    }

    static bool HasMarker(Entity entity, PrefabGUID markerBuff, int markerLevel)
    {
        return entity.TryGetBuff(markerBuff, out Entity buffEntity)
            && IsMarker(buffEntity, markerLevel);
    }

    static bool HasMarker(Entity entity, PrefabGUID markerBuff, int markerLevel, ServerGameManager serverGameManager)
    {
        return serverGameManager.TryGetBuff(entity, markerBuff.ToIdentifier(), out Entity buffEntity)
            && IsMarker(buffEntity, markerLevel);
    }

    static bool TryAddMarker(Entity entity, PrefabGUID markerBuff, int markerLevel)
    {
        if (HasMarker(entity, markerBuff, markerLevel))
        {
            return true;
        }

        if (!entity.TryGetBuff(markerBuff, out Entity buffEntity)
            && !entity.TryApplyAndGetBuff(markerBuff, out buffEntity))
        {
            return HasMarker(entity, markerBuff, markerLevel);
        }

        MakeInertMarkerBuff(buffEntity, markerLevel);
        return IsMarker(buffEntity, markerLevel);
    }

    static bool TryAddMarker(Entity entity, PrefabGUID markerBuff, int markerLevel, ServerGameManager serverGameManager)
    {
        if (HasMarker(entity, markerBuff, markerLevel, serverGameManager))
        {
            return true;
        }

        if (!serverGameManager.TryGetBuff(entity, markerBuff.ToIdentifier(), out Entity buffEntity)
            && !serverGameManager.TryInstantiateBuffEntityImmediate(entity, entity, markerBuff, out buffEntity))
        {
            return HasMarker(entity, markerBuff, markerLevel, serverGameManager);
        }

        MakeInertMarkerBuff(buffEntity, markerLevel);
        return IsMarker(buffEntity, markerLevel);
    }

    static bool IsMarker(Entity buffEntity, int markerLevel)
    {
        return buffEntity.TryGetComponent(out SpellLevel spellLevel)
            && spellLevel.Level == markerLevel;
    }

    static void MakeInertMarkerBuff(Entity buffEntity, int markerLevel)
    {
        buffEntity.AddWith((ref SpellLevel spellLevel) => spellLevel.Level = markerLevel);

        buffEntity.AddWith((ref LifeTime lifeTime) =>
        {
            lifeTime.Duration = 0f;
            lifeTime.EndAction = LifeTimeEndAction.None;
        });

        buffEntity.With((ref BuffCategory buffCategory) => buffCategory.Groups = BuffCategoryFlag.None);

        buffEntity.Remove<PersistenceV2.DontSaveEntity>();
        buffEntity.Remove<ScriptSpawn>();
        buffEntity.Remove<RemoveBuffOnGameplayEvent>();
        buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        buffEntity.Remove<CreateGameplayEventsOnSpawn>();
        buffEntity.Remove<GameplayEventListeners>();
        buffEntity.Remove<DestroyOnGameplayEvent>();
        buffEntity.Remove<DealDamageOnGameplayEvent>();
        buffEntity.Remove<HealOnGameplayEvent>();
        buffEntity.Remove<ModifyUnitStatBuff_DOTS>();
        buffEntity.Remove<ModifyMovementSpeedBuff>();
        buffEntity.Remove<ModifyMovementSpeedBuffModification>();
        buffEntity.Remove<ModifyTargetHUDBuff>();
        buffEntity.Remove<SyncToUserBuffer>();
        buffEntity.Remove<BuffModificationFlagData>();
        buffEntity.Remove<Buff_Persists_Through_Death>();
        buffEntity.Remove<UpdateLifeTimeWhenDisabled>();
        buffEntity.Remove<StackLifeTimeModifier>();
        buffEntity.Remove<SpawnRandomLifeTime>();
        buffEntity.Remove<AbilityProjectileFanOnGameplayEvent_DataServer>();
        buffEntity.Remove<SetOwnerRotateTowardsMouse>();
    }
}
