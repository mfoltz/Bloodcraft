using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Bloodcraft.Services;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Services;

public sealed class QueryServiceLookupTests : IDisposable
{
    public QueryServiceLookupTests()
    {
        ClearOverrides();
        QueryService.ComponentTypeKeySelector = TestComponentFactory.GetComponentKey;
    }

    [Fact]
    public void BuildModifiedDescriptor_AppendsIncludeAndExcludeComponentsWithoutDuplicates()
    {
        ComponentType baselineAllA = TestComponentFactory.CreateComponentType(1);
        ComponentType baselineAllB = TestComponentFactory.CreateComponentType(2);
        ComponentType baselineAny = TestComponentFactory.CreateComponentType(3);
        ComponentType baselineNone = TestComponentFactory.CreateComponentType(4);
        ComponentType includeExtra = TestComponentFactory.CreateComponentType(5);
        ComponentType excludeExtra = TestComponentFactory.CreateComponentType(6);
        ComponentType excludeSupplemental = TestComponentFactory.CreateComponentType(7);

        QueryService.QueryDescriptorData baselineDescriptor = new(
            new[] { baselineAllA, baselineAllB },
            new[] { baselineAny },
            new[] { baselineNone },
            Array.Empty<ComponentType>(),
            Array.Empty<ComponentType>(),
            EntityQueryOptions.Default);

        ComponentType[] includeComponents =
        {
            includeExtra,
            baselineAllA
        };

        ComponentType[] excludeComponents =
        {
            excludeExtra,
            baselineNone,
            excludeSupplemental
        };

        QueryService.QueryDescriptorData modifiedDescriptor = QueryService.BuildModifiedDescriptorData(baselineDescriptor, includeComponents, excludeComponents);

        AssertComponentSetsEqual(baselineDescriptor.All.Concat(includeComponents), modifiedDescriptor.All);
        AssertComponentSequencesEqual(baselineDescriptor.Any, modifiedDescriptor.Any);
        AssertComponentSetsEqual(baselineDescriptor.None.Concat(excludeComponents), modifiedDescriptor.None);
        Assert.Equal(baselineDescriptor.Options, modifiedDescriptor.Options);

        AssertNoDuplicateComponents(modifiedDescriptor.All);
        AssertNoDuplicateComponents(modifiedDescriptor.None);
    }

    [Fact]
    public void BuildModifiedDescriptor_WithEmptyIncludeAndExclude_ReturnsEquivalentDescriptor()
    {
        ComponentType baselineAllA = TestComponentFactory.CreateComponentType(11);
        ComponentType baselineAllB = TestComponentFactory.CreateComponentType(12);
        ComponentType baselineAny = TestComponentFactory.CreateComponentType(13);
        ComponentType baselineNone = TestComponentFactory.CreateComponentType(14);

        QueryService.QueryDescriptorData baselineDescriptor = new(
            new[] { baselineAllA, baselineAllB },
            new[] { baselineAny },
            new[] { baselineNone },
            Array.Empty<ComponentType>(),
            Array.Empty<ComponentType>(),
            EntityQueryOptions.FilterWriteGroup);

        QueryService.QueryDescriptorData modifiedDescriptor = QueryService.BuildModifiedDescriptorData(
            baselineDescriptor,
            Array.Empty<ComponentType>(),
            Array.Empty<ComponentType>());

        AssertComponentSequencesEqual(baselineDescriptor.All, modifiedDescriptor.All);
        AssertComponentSequencesEqual(baselineDescriptor.Any, modifiedDescriptor.Any);
        AssertComponentSequencesEqual(baselineDescriptor.None, modifiedDescriptor.None);
        Assert.Equal(baselineDescriptor.Options, modifiedDescriptor.Options);
    }

    [Fact]
    public void ModifyEntityQuery_UsesOverridesToCreateQueryFromModifiedDescriptor()
    {
        ComponentType baselineAllA = TestComponentFactory.CreateComponentType(21);
        ComponentType baselineAllB = TestComponentFactory.CreateComponentType(22);
        ComponentType baselineAny = TestComponentFactory.CreateComponentType(23);
        ComponentType baselineNone = TestComponentFactory.CreateComponentType(24);
        ComponentType includeExtra = TestComponentFactory.CreateComponentType(25);
        ComponentType excludeExtra = TestComponentFactory.CreateComponentType(26);

        QueryService.QueryDescriptorData baselineDescriptor = new(
            new[] { baselineAllA, baselineAllB },
            new[] { baselineAny },
            new[] { baselineNone },
            Array.Empty<ComponentType>(),
            Array.Empty<ComponentType>(),
            EntityQueryOptions.IncludePrefab | EntityQueryOptions.FilterWriteGroup);

        ComponentType[] includeComponents = { includeExtra, baselineAllA };
        ComponentType[] excludeComponents = { excludeExtra, baselineNone };

        QueryService.QueryDescriptorData? capturedDescriptor = null;

        try
        {
            QueryService.QueryDescriptorProviderOverride = _ => baselineDescriptor;
            QueryService.EntityQueryFactoryOverride = descriptor =>
            {
                capturedDescriptor = descriptor;
                return TestComponentFactory.CreateQuery(42);
            };

            _ = QueryService.ModifyEntityQuery(default, includeComponents, excludeComponents);
            Assert.NotNull(capturedDescriptor);

            QueryService.QueryDescriptorData descriptor = Assert.IsType<QueryService.QueryDescriptorData>(capturedDescriptor);
            AssertComponentSetsEqual(baselineDescriptor.All.Concat(includeComponents), descriptor.All);
            AssertComponentSequencesEqual(baselineDescriptor.Any, descriptor.Any);
            AssertComponentSetsEqual(baselineDescriptor.None.Concat(excludeComponents), descriptor.None);
            Assert.Equal(baselineDescriptor.Options, descriptor.Options);
        }
        finally
        {
            ClearOverrides();
        }
    }

    public void Dispose()
    {
        ClearOverrides();
    }

    static void ClearOverrides()
    {
        QueryService.QueryDescriptorProviderOverride = null;
        QueryService.EntityQueryFactoryOverride = null;
        QueryService.ComponentTypeKeySelector = component => component.GetHashCode();
    }

    static void AssertComponentSetsEqual(IEnumerable<ComponentType> expected, IReadOnlyList<ComponentType> actual)
    {
        HashSet<int> expectedSet = new();
        foreach (ComponentType component in expected)
        {
            expectedSet.Add(TestComponentFactory.GetComponentKey(component));
        }

        HashSet<int> actualSet = new();
        foreach (ComponentType component in actual)
        {
            actualSet.Add(TestComponentFactory.GetComponentKey(component));
        }

        Assert.Equal(expectedSet, actualSet);
    }

    static void AssertComponentSequencesEqual(IEnumerable<ComponentType> expected, IEnumerable<ComponentType> actual)
    {
        Assert.Equal(
            expected.Select(TestComponentFactory.GetComponentKey),
            actual.Select(TestComponentFactory.GetComponentKey));
    }

    static void AssertNoDuplicateComponents(IEnumerable<ComponentType> components)
    {
        HashSet<int> seen = new();
        foreach (ComponentType component in components)
        {
            int key = TestComponentFactory.GetComponentKey(component);
            Assert.True(seen.Add(key), "Duplicate component detected in collection.");
        }
    }

    static class TestComponentFactory
    {
        public static ComponentType CreateComponentType(int id)
        {
            ComponentType component = default;
            Unsafe.As<ComponentType, int>(ref component) = id;
            return component;
        }

        public static int GetComponentKey(ComponentType component)
        {
            return Unsafe.As<ComponentType, int>(ref component);
        }

        public static EntityQuery CreateQuery(ulong seqno)
        {
            return default;
        }

    }

}
