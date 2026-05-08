[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "Bloodcraft.csproj"
$testProjectPath = Join-Path $repoRoot ".codex\tests\Bloodcraft.Tests.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found at $projectPath"
}

Write-Host "Restoring Bloodcraft project dependencies..."
dotnet restore $projectPath

if (Test-Path -LiteralPath $testProjectPath) {
    Write-Host "Restoring test project dependencies..."
    dotnet restore $testProjectPath
}

Write-Host "Building Bloodcraft project..."
dotnet build $projectPath --configuration Release --no-restore -p:RunGenerateREADME=false

$dllPath = Join-Path $repoRoot "bin\Release\net6.0\Bloodcraft.dll"
if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Build failed: $dllPath not found."
}

Write-Host "Build succeeded: $dllPath"

$pluginDir = $env:BEPINEX_PLUGIN_DIR
if (-not [string]::IsNullOrWhiteSpace($pluginDir)) {
    if (-not (Test-Path -LiteralPath $pluginDir)) {
        throw "BEPINEX_PLUGIN_DIR does not exist: $pluginDir"
    }

    Copy-Item -LiteralPath $dllPath -Destination (Join-Path $pluginDir "Bloodcraft.dll") -Force
    Write-Host "Copied Bloodcraft.dll to $pluginDir"
}
else {
    Write-Host "Set BEPINEX_PLUGIN_DIR to copy the built DLL into your BepInEx plugins directory."
}
