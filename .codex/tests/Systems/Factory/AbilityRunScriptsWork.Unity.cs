using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides Unity DOTS bindings for exercising <see cref="AbilityRunScriptsWork"/> inside a managed world.
/// </summary>
public partial class AbilityRunScriptsWork : Bloodcraft.Factory.ISystemWork
{
    EntityQuery runtimeCastStartedQuery;
    bool castStartedQueryCreated;
    bool runtimeRefreshExecuted;

    /// <summary>
    /// Gets a value indicating whether the runtime refresh delegate executed.
    /// </summary>
    internal bool RuntimeRefreshExecuted => runtimeRefreshExecuted;

    /// <inheritdoc />
    void Bloodcraft.Factory.IQuerySpec.Build(ref EntityQueryBuilder builder)
    {
        builder.AddAll(Unity.Entities.ComponentType.ReadOnly<AbilityPostCastEndedEvent>());
    }

    /// <inheritdoc />
    void Bloodcraft.Factory.ISystemWork.OnCreate(Bloodcraft.Factory.SystemContext context)
    {
        runtimeCastStartedQuery = context.EntityManager.CreateEntityQuery(Unity.Entities.ComponentType.ReadOnly<AbilityCastStartedEvent>());
        castStartedQueryCreated = true;
        context.Registrar.Register(RegisterRuntimeRefreshLookups);
    }

    /// <inheritdoc />
    void Bloodcraft.Factory.ISystemWork.OnStartRunning(Bloodcraft.Factory.SystemContext context)
    {
    }

    /// <inheritdoc />
    void Bloodcraft.Factory.ISystemWork.OnUpdate(Bloodcraft.Factory.SystemContext context)
    {
        context.ForEachEntity(context.Query, entity => ProcessPostCastEndedEvent(ToHandle(entity)));

        if (castStartedQueryCreated)
        {
            context.ForEachEntity(runtimeCastStartedQuery, entity => ProcessCastStartedEvent(ToHandle(entity)));
        }
    }

    /// <inheritdoc />
    void Bloodcraft.Factory.ISystemWork.OnStopRunning(Bloodcraft.Factory.SystemContext context)
    {
    }

    /// <inheritdoc />
    void Bloodcraft.Factory.ISystemWork.OnDestroy(Bloodcraft.Factory.SystemContext context)
    {
        if (castStartedQueryCreated)
        {
            runtimeCastStartedQuery.Dispose();
            castStartedQueryCreated = false;
        }
    }

    static EntityHandle ToHandle(Entity entity) => new(entity.Index);

    void RegisterRuntimeRefreshLookups(Unity.Entities.SystemBase system)
    {
        runtimeRefreshExecuted = true;
        _ = system.GetComponentLookup<AbilityPostCastEndedEvent>(isReadOnly: true);
        _ = system.GetComponentLookup<AbilityCastStartedEvent>(isReadOnly: true);
        _ = system.GetComponentLookup<User>(isReadOnly: true);
        _ = system.GetComponentLookup<FamiliarActivityChecker>(isReadOnly: true);
        _ = system.GetComponentLookup<FamiliarDismissalChecker>(isReadOnly: true);
        _ = system.GetComponentLookup<FamiliarResolver>(isReadOnly: true);
        _ = system.GetComponentLookup<FamiliarBuffChecker>(isReadOnly: true);
        _ = system.GetComponentLookup<FamiliarAutoCallRegistrar>();
        _ = system.GetComponentLookup<FamiliarDismissalDelegate>();
    }
}
