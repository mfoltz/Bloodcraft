Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ServerInstallPath = $env:VRISING_HARNESS_SERVER_INSTALL
if ([string]::IsNullOrWhiteSpace($ServerInstallPath)) {
    throw "VRISING_HARNESS_SERVER_INSTALL is required."
}

$RconPort = $env:VRISING_HARNESS_RCON_PORT
$RconPassword = $env:VRISING_HARNESS_RCON_PASSWORD
if (-not [string]::IsNullOrWhiteSpace($RconPort)) {
    $HostSettingsPath = Join-Path $ServerInstallPath "VRisingServer_Data\StreamingAssets\Settings\ServerHostSettings.json"
    if (-not (Test-Path -LiteralPath $HostSettingsPath)) {
        throw "Server host settings not found: $HostSettingsPath"
    }

    $HostSettings = Get-Content -Raw -Path $HostSettingsPath | ConvertFrom-Json
    if ($null -eq $HostSettings.Rcon) {
        $HostSettings | Add-Member -MemberType NoteProperty -Name Rcon -Value ([pscustomobject]@{})
    }

    $HostSettings.Rcon.Enabled = $true
    $HostSettings.Rcon.Port = [int]$RconPort
    $HostSettings.Rcon.Password = if ($null -eq $RconPassword) { "" } else { $RconPassword }

    $HostSettings | ConvertTo-Json -Depth 8 | Set-Content -Path $HostSettingsPath -Encoding UTF8
    Write-Host "[bloodcraft-scenario] Enabled V Rising RCON on port $RconPort in $HostSettingsPath"
}

$PersistentDataPath = $env:VRISING_HARNESS_PERSISTENT_DATA
$AdminSteamIds = $env:VRISING_HARNESS_ADMIN_STEAM_IDS
if (-not [string]::IsNullOrWhiteSpace($PersistentDataPath) -and -not [string]::IsNullOrWhiteSpace($AdminSteamIds)) {
    $SettingsPath = Join-Path $PersistentDataPath "Settings"
    $AdminListPath = Join-Path $SettingsPath "adminlist.txt"

    New-Item -ItemType Directory -Path $SettingsPath -Force | Out-Null

    $ExistingAdminIds = @()
    if (Test-Path -LiteralPath $AdminListPath) {
        $ExistingAdminIds = @(Get-Content -Path $AdminListPath | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }

    $ConfiguredAdminIds = @($AdminSteamIds -split "[,;\s]+" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    foreach ($AdminSteamId in $ConfiguredAdminIds) {
        if ($AdminSteamId -notmatch "^\d{17}$") {
            throw "Invalid admin SteamID '$AdminSteamId'. Expected a 17-digit SteamID64."
        }
    }

    $MergedAdminIds = @($ExistingAdminIds + $ConfiguredAdminIds | Select-Object -Unique)
    Set-Content -Path $AdminListPath -Value $MergedAdminIds -Encoding UTF8
    Write-Host "[bloodcraft-scenario] Seeded $($ConfiguredAdminIds.Count) admin SteamID(s) in $AdminListPath"
}

$ConfigPath = Join-Path $ServerInstallPath "BepInEx\config\io.zfolmt.Bloodcraft.cfg"
if (-not (Test-Path -LiteralPath $ConfigPath)) {
    New-Item -ItemType Directory -Path (Split-Path -Path $ConfigPath -Parent) -Force | Out-Null
    Set-Content -Path $ConfigPath -Value "[General]`r`n" -Encoding UTF8
}

$ConfigText = Get-Content -Raw -Path $ConfigPath
$Replacements = [ordered]@{
    "ElitePrimalRifts" = "true"
    "RiftFrequency" = "6"
    "NightmareMode" = "true"
}

foreach ($Entry in $Replacements.GetEnumerator()) {
    $Pattern = "(?m)^$([regex]::Escape($Entry.Key))\s*=.*$"
    $Replacement = "$($Entry.Key) = $($Entry.Value)"

    if ([regex]::IsMatch($ConfigText, $Pattern)) {
        $ConfigText = [regex]::Replace($ConfigText, $Pattern, $Replacement)
    }
    else {
        $ConfigText = $ConfigText.TrimEnd() + [Environment]::NewLine + $Replacement + [Environment]::NewLine
    }
}

Set-Content -Path $ConfigPath -Value $ConfigText -Encoding UTF8
Write-Host "[bloodcraft-scenario] Enabled Nightmare Mode and Primal Rifts in $ConfigPath"
