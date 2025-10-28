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

Most production systems reach for Unity DOTS singletons during static initialization. The `UnityRuntimeScope` helper under `Support/` provisions a stub `World` named `Server`, injects lightweight stand-ins for `Core.SystemService` and `Core.ServerGameManager`, and ensures Unity's global world registry contains the shim before any static constructors run. Tests opt into the shim by decorating their classes with `[Collection(UnityRuntimeTestCollection.CollectionName)]` (or by instantiating `UnityRuntimeScope` directly) instead of maintaining bespoke Harmony prefixes for each type initializer.
