[CmdletBinding()]
param(
    [ValidateSet("preflight", "build", "deploy", "start", "wait-ready", "collect", "stop", "run")]
    [string]$Action = "run",
    [string]$Profile = "bloodcraft-smoke",
    [string]$ConfigPath,
    [switch]$DryRun
)

$LocalConfigPath = if ($ConfigPath) { $ConfigPath } else { (Join-Path $PSScriptRoot "harness.settings.json") }
$SharedHarnessPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\Emery\.codex\harness\Invoke-VRisingHarness.ps1"))

if (-not (Test-Path $SharedHarnessPath)) {
    throw "Shared harness not found: $SharedHarnessPath"
}

& $SharedHarnessPath -Action $Action -Profile $Profile -ConfigPath $LocalConfigPath -DryRun:$DryRun
