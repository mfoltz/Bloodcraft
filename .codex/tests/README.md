# Bloodcraft Test Harness

## Restoring dependencies
Run the provisioning script before invoking any `dotnet` commands. It restores the NuGet packages required by both the plugin and the test harness, so contributors are expected to execute it before running tests:

```bash
bash .codex/install.sh
```

The script installs the pinned .NET SDK, restores both the main plugin and the test project, and produces a Release build artifact. NuGet restore is the supported way to hydrate every dependency required by the tests—no manual downloads or native shims are necessary.

For day-to-day development, use the helper script that performs the install/restore flow automatically before executing tests:

```bash
.codex/tests/run-tests.sh --filter FamiliarLevelingTests
```

The wrapper guarantees that package restore runs on every invocation, even when contributors forget to provision the workspace manually.

## Running tests
After the install script completes you can execute tests with the SDK it installs:

```bash
dotnet test .codex/tests/Bloodcraft.Tests.csproj
```

## Fixture guidelines

Service and system fixtures must inherit from [`TestHost`](TestHost.cs) so the module initializer and configuration sandbox stay active for the lifetime of the test case. The base host wires the module bootstrap defined in [`Support/TestModuleInitializers.cs`](Support/TestModuleInitializers.cs), keeping the Unity shims and content overrides engaged without each fixture needing to repeat the setup logic.

When exercising shard reset behavior, use the seam provided by [`EliteShardBearerBootstrapper`](Services/EliteShardBearerBootstrapperTests.cs) and [`ShardBearerResetService`](Services/ShardBearerResetServiceTests.cs). Supply fakes or scoped contexts to those entry points instead of calling `Core.Initialize()`, which still spins up the live DOTS runtime and bypasses the harness' safety rails.

### Native bootstrap strategy

The harness patches the relevant type initializers at runtime so the managed assemblies bootstrap without touching Unity's native binaries. This lets the suite execute inside a headless CI container while keeping coverage over the same startup paths the plugin exercises in-game.
