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

### Native bootstrap strategy

The harness patches the relevant type initializers at runtime so the managed assemblies bootstrap without touching Unity's native binaries. This lets the suite execute inside a headless CI container while keeping coverage over the same startup paths the plugin exercises in-game.
