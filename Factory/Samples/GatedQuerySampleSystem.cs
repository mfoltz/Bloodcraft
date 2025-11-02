#nullable enable

using System;
using Unity.Entities;
using Unity.Transforms;

namespace Bloodcraft.Factory.Samples;

/// <summary>
/// Demonstrates how to throttle expensive query work using the builder's gating helpers.
/// </summary>
internal sealed class GatedQuerySampleSystem : VSystemBase<GatedQuerySampleSystem.Work>
{
    public new sealed class Work : ISystemWork
    {
        readonly ISystemWork _implementation;
        readonly SystemWorkBuilder.QueryHandleHolder _expensiveEntities;

        int _executionCount;
        double _lastServerExecutionTime;

        public Work()
        {
            var descriptor = QueryDescriptor.Create()
                .WithAll<LocalToWorld>()
                .IncludeSystems();

            var builder = new SystemWorkBuilder()
                .WithQuery(descriptor)
                .WithServerTimeInterval(TimeSpan.FromSeconds(5))
                .WithFixedServerTickInterval(10);

            _expensiveEntities = builder.WithPrimaryQuery(requireForUpdate: true);

            builder.OnUpdate(context =>
            {
                var handle = _expensiveEntities.Handle;
                if (handle == null || handle.IsDisposed)
                {
                    return;
                }

                _executionCount++;
                _lastServerExecutionTime = Core.ServerTime;

                SystemWorkBuilder.ForEachEntity(context, handle, _ =>
                {
                    // Heavy logic would be placed here.
                });
            });

            _implementation = builder.Build();
        }

        /// <summary>
        /// Gets the number of times the gated update has executed.
        /// </summary>
        public int ExecutionCount => _executionCount;

        /// <summary>
        /// Gets the server time recorded during the last execution.
        /// </summary>
        public double LastServerExecutionTime => _lastServerExecutionTime;

        public void Build(ref EntityQueryBuilder builder) =>
            _implementation.Build(ref builder);

        public void OnCreate(SystemContext context) =>
            _implementation.OnCreate(context);

        public void OnStartRunning(SystemContext context) =>
            _implementation.OnStartRunning(context);

        public void OnUpdate(SystemContext context) =>
            _implementation.OnUpdate(context);

        public void OnStopRunning(SystemContext context) =>
            _implementation.OnStopRunning(context);

        public void OnDestroy(SystemContext context) =>
            _implementation.OnDestroy(context);
    }

}
