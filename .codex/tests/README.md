# Bloodcraft Test Harness

## Restoring dependencies
Run the provisioning script before invoking any `dotnet` commands:

```bash
bash .codex/install.sh
```

The script installs the pinned .NET SDK, restores both the main plugin and the test project, and produces a Release build artifact.

## Running tests
After the install script completes you can execute tests with the SDK it installs:

```bash
/root/.dotnet/dotnet test .codex/tests/Bloodcraft.Tests.csproj
```

This harness avoids loading the game's native `GameAssembly` dependency, so the tests can execute inside a headless CI container.
