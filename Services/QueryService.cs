using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Services;
internal static class QueryService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly Lazy<EntityQuery> UpdateBuffsBufferDestroyQueryLazy = new(() =>
        ModifyEntityQuery(
            SystemService.UpdateBuffsBuffer_Destroy.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        ));

    static readonly Lazy<EntityQuery> BuffSpawnServerQueryLazy = new(() =>
        ModifyEntityQuery(
            SystemService.BuffSystem_Spawn_Server.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        ));

    static readonly Lazy<EntityQuery> ScriptSpawnServerQueryLazy = new(() =>
        ModifyEntityQuery(
            SystemService.ScriptSpawnServer.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<Buff>()), ComponentType.ReadOnly(Il2CppType.Of<EntityOwner>())]
        ));

    internal static Func<EntityQuery, QueryDescriptorData> QueryDescriptorProviderOverride { get; set; }
    internal static Func<QueryDescriptorData, EntityQuery> EntityQueryFactoryOverride { get; set; }
    internal static Func<ComponentType, int> ComponentTypeKeySelector { get; set; } = component => component.GetHashCode();

    public static EntityQuery UpdateBuffsBufferDestroyQuery => UpdateBuffsBufferDestroyQueryLazy.Value;
    public static EntityQuery BuffSpawnServerQuery => BuffSpawnServerQueryLazy.Value;
    public static EntityQuery ScriptSpawnServerQuery => ScriptSpawnServerQueryLazy.Value;

    internal static EntityQueryDesc BuildModifiedDescriptor(
        EntityQueryDesc baseline,
        ComponentType[] includeComponents,
        ComponentType[] excludeComponents)
    {
        QueryDescriptorData baselineData = ExtractDescriptorData(baseline);
        QueryDescriptorData modifiedData = BuildModifiedDescriptorData(baselineData, includeComponents, excludeComponents);
        return CreateEntityQueryDesc(modifiedData);
    }

    internal static QueryDescriptorData BuildModifiedDescriptorData(
        QueryDescriptorData baseline,
        ComponentType[] includeComponents,
        ComponentType[] excludeComponents)
    {
        ComponentType[] allComponents = MergeComponentSets(baseline.All, includeComponents);
        ComponentType[] anyComponents = CloneDistinct(baseline.Any);
        ComponentType[] noneComponents = MergeComponentSets(baseline.None, excludeComponents);
        ComponentType[] disabledComponents = CloneDistinct(baseline.Disabled);
        ComponentType[] absentComponents = CloneDistinct(baseline.Absent);

        return new QueryDescriptorData(allComponents, anyComponents, noneComponents, disabledComponents, absentComponents, baseline.Options);
    }

    public static EntityQuery ModifyEntityQuery(EntityQuery originalQuery, ComponentType[] includeComponents = null, ComponentType[] excludeComponents = null)
    {
        QueryDescriptorData baselineData = QueryDescriptorProviderOverride?.Invoke(originalQuery) ?? ExtractDescriptorData(originalQuery.GetEntityQueryDesc());
        QueryDescriptorData modifiedData = BuildModifiedDescriptorData(baselineData, includeComponents, excludeComponents);

        if (EntityQueryFactoryOverride is { } factory)
        {
            return factory(modifiedData);
        }

        EntityQueryDesc modifiedDescriptor = CreateEntityQueryDesc(modifiedData);
        Il2CppReferenceArray<EntityQueryDesc> queryDescArray = new(1);
        queryDescArray[0] = modifiedDescriptor;

        return EntityManager.CreateEntityQuery(queryDescArray);
    }

    static ComponentType[] MergeComponentSets(IReadOnlyList<ComponentType> baseline, ComponentType[] supplemental)
    {
        if ((baseline == null || baseline.Count == 0) && (supplemental == null || supplemental.Length == 0))
        {
            return Array.Empty<ComponentType>();
        }

        HashSet<int> seen = new();
        List<ComponentType> merged = new();

        if (baseline != null)
        {
            foreach (ComponentType component in baseline)
            {
                if (seen.Add(ComponentTypeKeySelector(component)))
                {
                    merged.Add(component);
                }
            }
        }

        if (supplemental != null)
        {
            foreach (ComponentType component in supplemental)
            {
                if (seen.Add(ComponentTypeKeySelector(component)))
                {
                    merged.Add(component);
                }
            }
        }

        return merged.ToArray();
    }

    static ComponentType[] CloneDistinct(IReadOnlyList<ComponentType> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<ComponentType>();
        }

        HashSet<int> seen = new();
        List<ComponentType> components = new(source.Count);

        foreach (ComponentType component in source)
        {
            if (seen.Add(ComponentTypeKeySelector(component)))
            {
                components.Add(component);
            }
        }

        return components.ToArray();
    }

    static QueryDescriptorData ExtractDescriptorData(EntityQueryDesc descriptor)
    {
        ComponentType[] all = CloneDistinct(descriptor.All);
        ComponentType[] any = CloneDistinct(descriptor.Any);
        ComponentType[] none = CloneDistinct(descriptor.None);
        ComponentType[] disabled = CloneDistinct(descriptor.Disabled);
        ComponentType[] absent = CloneDistinct(descriptor.Absent);

        return new QueryDescriptorData(all, any, none, disabled, absent, descriptor.Options);
    }

    static EntityQueryDesc CreateEntityQueryDesc(QueryDescriptorData data)
    {
        EntityQueryDesc descriptor = new()
        {
            All = data.All,
            Any = data.Any,
            None = data.None,
            Disabled = data.Disabled,
            Absent = data.Absent,
            Options = data.Options
        };

        return descriptor;
    }

    internal readonly record struct QueryDescriptorData(
        ComponentType[] All,
        ComponentType[] Any,
        ComponentType[] None,
        ComponentType[] Disabled,
        ComponentType[] Absent,
        EntityQueryOptions Options);
}

