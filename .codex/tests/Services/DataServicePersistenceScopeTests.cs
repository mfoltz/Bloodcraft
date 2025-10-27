using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bloodcraft.Services;
using Xunit;

namespace Bloodcraft.Tests.Services;

public sealed class DataServicePersistenceScopeTests
{
    [Fact]
    public void SuppressPersistence_SingleScope_TogglesSuppression()
    {
        DataServiceTestAccessor.ResetPersistenceSuppression();

        try
        {
            IDisposable scope = DataService.SuppressPersistence();

            try
            {
                Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
                Assert.Equal(1, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
            }
            finally
            {
                scope.Dispose();
            }

            Assert.False(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(0, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
        }
        finally
        {
            DataServiceTestAccessor.ResetPersistenceSuppression();
        }
    }

    [Fact]
    public void SuppressPersistence_NestedScopes_IncrementDepth()
    {
        DataServiceTestAccessor.ResetPersistenceSuppression();

        try
        {
            IDisposable outerScope = DataService.SuppressPersistence();

            try
            {
                Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
                Assert.Equal(1, DataServiceTestAccessor.GetPersistenceSuppressionDepth());

                IDisposable innerScope = DataService.SuppressPersistence();

                try
                {
                    Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
                    Assert.Equal(2, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
                }
                finally
                {
                    innerScope.Dispose();
                }

                Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
                Assert.Equal(1, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
            }
            finally
            {
                outerScope.Dispose();
            }

            Assert.False(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(0, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
        }
        finally
        {
            DataServiceTestAccessor.ResetPersistenceSuppression();
        }
    }

    [Fact]
    public void SuppressPersistence_DoubleDispose_DoesNotUnderflow()
    {
        DataServiceTestAccessor.ResetPersistenceSuppression();

        try
        {
            IDisposable scope = DataService.SuppressPersistence();
            Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(1, DataServiceTestAccessor.GetPersistenceSuppressionDepth());

            scope.Dispose();
            Assert.False(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(0, DataServiceTestAccessor.GetPersistenceSuppressionDepth());

            scope.Dispose();
            Assert.False(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(0, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
        }
        finally
        {
            DataServiceTestAccessor.ResetPersistenceSuppression();
        }
    }

    [Fact]
    public async Task SuppressPersistence_IsThreadSafeAcrossThreads()
    {
        DataServiceTestAccessor.ResetPersistenceSuppression();

        try
        {
            const int taskCount = 8;
            using var startBarrier = new Barrier(taskCount + 1);
            using var readyBarrier = new Barrier(taskCount + 1);

            Task[] tasks = Enumerable.Range(0, taskCount)
                .Select(_ => Task.Run(() => RunSuppressionScopeOnWorker(startBarrier, readyBarrier)))
                .ToArray();

            startBarrier.SignalAndWait();
            readyBarrier.SignalAndWait();

            Assert.True(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(taskCount, DataServiceTestAccessor.GetPersistenceSuppressionDepth());

            readyBarrier.SignalAndWait();

            await Task.WhenAll(tasks);

            Assert.False(DataServiceTestAccessor.GetIsPersistenceSuppressed());
            Assert.Equal(0, DataServiceTestAccessor.GetPersistenceSuppressionDepth());
        }
        finally
        {
            DataServiceTestAccessor.ResetPersistenceSuppression();
        }
    }

    private static void RunSuppressionScopeOnWorker(Barrier startBarrier, Barrier readyBarrier)
    {
        startBarrier.SignalAndWait();

        IDisposable scope = DataService.SuppressPersistence();

        try
        {
            readyBarrier.SignalAndWait();
            readyBarrier.SignalAndWait();
        }
        finally
        {
            scope.Dispose();
        }
    }

    private static class DataServiceTestAccessor
    {
        private static readonly Type DataServiceType = typeof(DataService);
        private static readonly PropertyInfo IsPersistenceSuppressedProperty = DataServiceType
            .GetProperty("IsPersistenceSuppressed", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to locate DataService.IsPersistenceSuppressed property.");
        private static readonly FieldInfo PersistenceSuppressionDepthField = DataServiceType
            .GetField("persistenceSuppressionDepth", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to locate DataService.persistenceSuppressionDepth field.");
        private static readonly object PersistenceSuppressionLock = DataServiceType
            .GetField("PersistenceSuppressionLock", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null)
            ?? throw new InvalidOperationException("Unable to locate DataService.PersistenceSuppressionLock field.");

        public static bool GetIsPersistenceSuppressed()
        {
            return (bool)IsPersistenceSuppressedProperty.GetValue(null)!;
        }

        public static int GetPersistenceSuppressionDepth()
        {
            lock (PersistenceSuppressionLock)
            {
                return (int)PersistenceSuppressionDepthField.GetValue(null)!;
            }
        }

        public static void ResetPersistenceSuppression()
        {
            lock (PersistenceSuppressionLock)
            {
                PersistenceSuppressionDepthField.SetValue(null, 0);
            }
        }
    }
}
