using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Bloodcraft.Services;

namespace Bloodcraft.Tests.Services;

public sealed class QueryServiceLookupTests : IDisposable
{
    static readonly ConstructorInfo? CoreStaticConstructor = typeof(Core).TypeInitializer;
    static readonly ConstructorInfo? QueryServiceStaticConstructor = typeof(QueryService).TypeInitializer;
    static readonly HarmonyMethod SkipStaticConstructorMethod = new(typeof(QueryServiceLookupTests).GetMethod(nameof(SkipStaticConstructor), BindingFlags.Static | BindingFlags.NonPublic)!);

    readonly Harmony harmony = new($"Bloodcraft.Tests.QueryServiceLookupTests.{Guid.NewGuid():N}");
    readonly World world;
    readonly World? previousDefaultWorld;

    public QueryServiceLookupTests()
    {
        if (CoreStaticConstructor != null)
        {
            harmony.Patch(CoreStaticConstructor, prefix: SkipStaticConstructorMethod);
        }

        if (QueryServiceStaticConstructor != null)
        {
            harmony.Patch(QueryServiceStaticConstructor, prefix: SkipStaticConstructorMethod);
        }

        TestQueryInterceptors.EnsurePatched(harmony);

        world = new World("Server");
        previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld ??= world;

        SetCoreServer(world);
    }

    [Fact]
    public void ModifyEntityQuery_AppendsIncludeAndExcludeComponentsWithoutDuplicates()
    {
        ComponentType baselineAllA = TestQueryInterceptors.CreateComponentType(1);
        ComponentType baselineAllB = TestQueryInterceptors.CreateComponentType(2);
        ComponentType baselineAny = TestQueryInterceptors.CreateComponentType(3);
        ComponentType baselineNone = TestQueryInterceptors.CreateComponentType(4);
        ComponentType includeExtra = TestQueryInterceptors.CreateComponentType(5);
        ComponentType excludeExtra = TestQueryInterceptors.CreateComponentType(6);
        ComponentType excludeSupplemental = TestQueryInterceptors.CreateComponentType(7);

        EntityQueryDesc baselineDescriptor = new()
        {
            All = new[] { baselineAllA, baselineAllB },
            Any = new[] { baselineAny },
            None = new[] { baselineNone },
            Options = EntityQueryOptions.Default
        };

        var context = new EntityQueryTestContext(baselineDescriptor);
        context.ClearRecorded();
        TestQueryInterceptors.CurrentContext = context;

        ComponentType[] includeComponents = new[]
        {
            includeExtra,
            baselineAllA
        };

        ComponentType[] excludeComponents = new[]
        {
            excludeExtra,
            baselineNone,
            excludeSupplemental
        };

        EntityQuery modifiedQuery = QueryService.ModifyEntityQuery(context.BaselineQuery, includeComponents, excludeComponents);
        EntityQueryDesc modifiedDescriptor = modifiedQuery.GetEntityQueryDesc();

        AssertComponentSetsEqual(baselineDescriptor.All.Concat(includeComponents), modifiedDescriptor.All);
        AssertComponentSetsEqual(baselineDescriptor.Any, modifiedDescriptor.Any);
        AssertComponentSetsEqual(baselineDescriptor.None.Concat(excludeComponents), modifiedDescriptor.None);

        Assert.Equal(modifiedDescriptor.None.Length, modifiedDescriptor.None.Distinct().Count());

        TestQueryInterceptors.CurrentContext = null;
    }

    [Fact]
    public void ModifyEntityQuery_WithEmptyAllAndIncludes_AppendsIncludeComponents()
    {
        ComponentType includeAllA = TestQueryInterceptors.CreateComponentType(21);
        ComponentType includeAllB = TestQueryInterceptors.CreateComponentType(22);

        EntityQueryDesc baselineDescriptor = new()
        {
            All = Array.Empty<ComponentType>(),
            Any = Array.Empty<ComponentType>(),
            None = Array.Empty<ComponentType>(),
            Options = EntityQueryOptions.FilterWriteGroup
        };

        var context = new EntityQueryTestContext(baselineDescriptor);
        context.ClearRecorded();
        TestQueryInterceptors.CurrentContext = context;

        ComponentType[] includeComponents =
        {
            includeAllA,
            includeAllB,
            includeAllA
        };

        EntityQuery modifiedQuery = QueryService.ModifyEntityQuery(context.BaselineQuery, includeComponents, null);
        EntityQueryDesc modifiedDescriptor = modifiedQuery.GetEntityQueryDesc();

        AssertComponentSetsEqual(includeComponents, modifiedDescriptor.All);
        Assert.Empty(modifiedDescriptor.Any);
        Assert.Empty(modifiedDescriptor.None);
        Assert.Equal(baselineDescriptor.Options, modifiedDescriptor.Options);

        TestQueryInterceptors.CurrentContext = null;
    }

    [Fact]
    public void ModifyEntityQuery_WithEmptyIncludeAndExclude_ReturnsEquivalentDescriptor()
    {
        ComponentType baselineAllA = TestQueryInterceptors.CreateComponentType(11);
        ComponentType baselineAllB = TestQueryInterceptors.CreateComponentType(12);
        ComponentType baselineAny = TestQueryInterceptors.CreateComponentType(13);
        ComponentType baselineNone = TestQueryInterceptors.CreateComponentType(14);

        EntityQueryDesc baselineDescriptor = new()
        {
            All = new[] { baselineAllA, baselineAllB },
            Any = new[] { baselineAny },
            None = new[] { baselineNone },
            Options = EntityQueryOptions.FilterWriteGroup
        };

        var context = new EntityQueryTestContext(baselineDescriptor);
        context.ClearRecorded();
        TestQueryInterceptors.CurrentContext = context;

        EntityQuery modifiedQuery = QueryService.ModifyEntityQuery(context.BaselineQuery, Array.Empty<ComponentType>(), Array.Empty<ComponentType>());
        EntityQueryDesc modifiedDescriptor = modifiedQuery.GetEntityQueryDesc();

        Assert.True(baselineDescriptor.All.SequenceEqual(modifiedDescriptor.All));
        Assert.True(baselineDescriptor.Any.SequenceEqual(modifiedDescriptor.Any));
        Assert.True(baselineDescriptor.None.SequenceEqual(modifiedDescriptor.None));
        Assert.Equal(baselineDescriptor.Options, modifiedDescriptor.Options);

        TestQueryInterceptors.CurrentContext = null;
    }

    public void Dispose()
    {
        SetCoreServer(null);
        TestQueryInterceptors.CurrentContext = null;

        if (CoreStaticConstructor != null)
        {
            harmony.Unpatch(CoreStaticConstructor, HarmonyPatchType.Prefix, harmony.Id);
        }

        if (QueryServiceStaticConstructor != null)
        {
            harmony.Unpatch(QueryServiceStaticConstructor, HarmonyPatchType.Prefix, harmony.Id);
        }

        harmony.UnpatchSelf();

        if (World.DefaultGameObjectInjectionWorld == world)
        {
            World.DefaultGameObjectInjectionWorld = previousDefaultWorld;
        }

        // Disposing the synthetic world can destabilize the IL2CPP-backed runtime in this test harness,
        // so we intentionally leave it undisposed to avoid crashing the test host.
    }

    static bool SkipStaticConstructor()
    {
        return false;
    }

    static void SetCoreServer(World? testWorld)
    {
        FieldInfo? serverField = typeof(Core).GetField("<Server>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        if (serverField == null)
        {
            throw new InvalidOperationException("Unable to locate Core.Server backing field for test initialization.");
        }

        serverField.SetValue(null, testWorld);
    }

    static void AssertComponentSetsEqual(IEnumerable<ComponentType> expected, IReadOnlyList<ComponentType> actual)
    {
        HashSet<ComponentType> expectedSet = expected.Distinct().ToHashSet();
        HashSet<ComponentType> actualSet = actual.ToHashSet();

        Assert.Equal(expectedSet, actualSet);
    }

    private sealed class EntityQueryTestContext
    {
        const ulong BaselineSeqnoValue = 1;
        const ulong ModifiedSeqnoValue = 2;

        EntityQuery? modifiedQuery;

        public EntityQueryTestContext(EntityQueryDesc baselineDescriptor)
        {
            BaselineDescriptor = baselineDescriptor;
            BaselineQuery = TestQueryInterceptors.CreateQuery(BaselineSeqnoValue);
            Options = baselineDescriptor.Options;
        }

        public EntityQueryDesc BaselineDescriptor { get; }
        public EntityQuery BaselineQuery { get; }
        public ulong BaselineSeqno => BaselineSeqnoValue;
        public ulong ModifiedSeqno => ModifiedSeqnoValue;
        public List<ComponentType> All { get; } = new();
        public List<ComponentType> Any { get; } = new();
        public List<ComponentType> None { get; } = new();
        public EntityQueryOptions Options { get; private set; }
        public EntityQueryDesc? ModifiedDescriptor { get; private set; }

        public EntityQuery ModifiedQuery
        {
            get
            {
                if (modifiedQuery is not { } query)
                {
                    throw new InvalidOperationException("Modified query has not been created.");
                }

                return query;
            }
        }

        public void ClearRecorded()
        {
            All.Clear();
            Any.Clear();
            None.Clear();
            Options = BaselineDescriptor.Options;
            ModifiedDescriptor = null;
            modifiedQuery = null;
        }

        public void RecordAll(ComponentType component) => All.Add(component);
        public void RecordAny(ComponentType component) => Any.Add(component);
        public void RecordNone(ComponentType component) => None.Add(component);
        public void RecordOptions(EntityQueryOptions options) => Options = options;

        public EntityQuery BuildModifiedQuery()
        {
            ComponentType[] all = DistinctOrEmpty(All);
            ComponentType[] any = DistinctOrEmpty(Any);
            ComponentType[] none = DistinctOrEmpty(None);

            ModifiedDescriptor = new EntityQueryDesc
            {
                All = all,
                Any = any,
                None = none,
                Options = Options
            };

            modifiedQuery = TestQueryInterceptors.CreateQuery(ModifiedSeqno);
            return modifiedQuery.Value;
        }

        static ComponentType[] DistinctOrEmpty(List<ComponentType> components)
        {
            if (components.Count == 0)
            {
                return Array.Empty<ComponentType>();
            }

            return components.Distinct().ToArray();
        }
    }

    private static class TestQueryInterceptors
    {
        static readonly FieldInfo SeqnoField = typeof(EntityQuery).GetField("__seqno", BindingFlags.Instance | BindingFlags.NonPublic)!;
        static readonly FieldInfo TypeIndexField = typeof(ComponentType).GetField("TypeIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
        static readonly FieldInfo AccessModeField = typeof(ComponentType).GetField("AccessModeType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        static readonly MethodInfo TypeIndexFromIntMethod = typeof(TypeIndex).GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(int) })!;

        static bool patched;

        public static EntityQueryTestContext? CurrentContext { get; set; }

        public static void EnsurePatched(Harmony harmony)
        {
            if (patched)
            {
                return;
            }

            harmony.Patch(
                AccessTools.Method(typeof(EntityQuery), nameof(EntityQuery.GetEntityQueryDesc)),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(GetEntityQueryDescPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Method(typeof(EntityManager), nameof(EntityManager.CreateEntityQuery), new[] { typeof(EntityQueryBuilder).MakeByRefType() }),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(CreateEntityQueryPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Method(typeof(EntityQueryBuilder), nameof(EntityQueryBuilder.AddAll)),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(AddAllPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Method(typeof(EntityQueryBuilder), nameof(EntityQueryBuilder.AddAny)),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(AddAnyPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Method(typeof(EntityQueryBuilder), nameof(EntityQueryBuilder.AddNone)),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(AddNonePrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Method(typeof(EntityQueryBuilder), nameof(EntityQueryBuilder.WithOptions)),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(WithOptionsPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            harmony.Patch(
                AccessTools.Constructor(typeof(EntityQueryBuilder), new[] { typeof(AllocatorManager.AllocatorHandle) }),
                prefix: new HarmonyMethod(typeof(TestQueryInterceptors).GetMethod(nameof(BuilderConstructorPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            patched = true;
        }

        public static ComponentType CreateComponentType(int id, ComponentType.AccessMode mode = ComponentType.AccessMode.ReadOnly)
        {
            object boxed = FormatterServices.GetUninitializedObject(typeof(ComponentType));
            object typeIndex = TypeIndexFromIntMethod.Invoke(null, new object[] { id })!;
            TypeIndexField.SetValue(boxed, typeIndex);
            AccessModeField.SetValue(boxed, mode);
            return (ComponentType)boxed;
        }

        public static EntityQuery CreateQuery(ulong seqno)
        {
            object boxed = FormatterServices.GetUninitializedObject(typeof(EntityQuery));
            SeqnoField.SetValue(boxed, seqno);
            return (EntityQuery)boxed;
        }

        static bool BuilderConstructorPrefix(ref EntityQueryBuilder __instance, AllocatorManager.AllocatorHandle allocator)
        {
            __instance = default;
            return false;
        }

        static bool GetEntityQueryDescPrefix(ref EntityQuery __instance, ref EntityQueryDesc __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            ulong seqno = GetSeqno(__instance);
            if (seqno == context.BaselineSeqno)
            {
                __result = context.BaselineDescriptor;
                return false;
            }

            if (seqno == context.ModifiedSeqno && context.ModifiedDescriptor is { } descriptor)
            {
                __result = descriptor;
                return false;
            }

            return true;
        }

        static bool CreateEntityQueryPrefix(ref EntityManager __instance, ref EntityQueryBuilder builder, ref EntityQuery __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            __result = context.BuildModifiedQuery();
            return false;
        }

        static bool AddAllPrefix(ref EntityQueryBuilder __instance, ComponentType t, ref EntityQueryBuilder __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            context.RecordAll(t);
            __result = __instance;
            return false;
        }

        static bool AddAnyPrefix(ref EntityQueryBuilder __instance, ComponentType t, ref EntityQueryBuilder __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            context.RecordAny(t);
            __result = __instance;
            return false;
        }

        static bool AddNonePrefix(ref EntityQueryBuilder __instance, ComponentType t, ref EntityQueryBuilder __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            context.RecordNone(t);
            __result = __instance;
            return false;
        }

        static bool WithOptionsPrefix(ref EntityQueryBuilder __instance, EntityQueryOptions options, ref EntityQueryBuilder __result)
        {
            if (CurrentContext is not { } context)
            {
                return true;
            }

            context.RecordOptions(options);
            __result = __instance;
            return false;
        }

        static ulong GetSeqno(EntityQuery query)
        {
            object boxed = query;
            return (ulong)SeqnoField.GetValue(boxed)!;
        }
    }
}
