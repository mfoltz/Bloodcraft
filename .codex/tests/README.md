# Bloodcraft Test Harness

## Restoring dependencies
Run the provisioning script before invoking any `dotnet` commands. It restores the NuGet packages required by both the plugin and the test harness, so contributors are expected to execute it before running tests:

```bash
bash .codex/install.sh
```

The script installs the pinned .NET SDK, restores both the main plugin and the test project, and produces a Release build artifact.

## Running tests
After the install script completes you can execute tests with the SDK it installs:

```bash
/root/.dotnet/dotnet test .codex/tests/Bloodcraft.Tests.csproj
```

### GameAssembly loading strategy

The harness no longer ships a mocked `GameAssembly` binary. Instead, the tests patch the relevant type initializers at runtime so the managed assemblies bootstrap without touching the native dependency. This lets the suite execute inside a headless CI container while keeping coverage over the same startup paths the plugin exercises in-game.
