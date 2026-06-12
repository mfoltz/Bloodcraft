$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$sourcePath = Join-Path $repoRoot 'VExtensions.cs'
$source = Get-Content -Raw -Path $sourcePath

$signature = 'public static void Destroy(this Entity entity, bool immediate = false)'
$methodStart = $source.IndexOf($signature, [System.StringComparison]::Ordinal)
if ($methodStart -lt 0) {
    throw "Could not find shared Entity.Destroy helper."
}

$braceStart = $source.IndexOf('{', $methodStart)
if ($braceStart -lt 0) {
    throw "Could not find shared Entity.Destroy method body."
}

$depth = 0
$methodEnd = -1
for ($i = $braceStart; $i -lt $source.Length; $i++) {
    if ($source[$i] -eq '{') {
        $depth++
    }
    elseif ($source[$i] -eq '}') {
        $depth--
        if ($depth -eq 0) {
            $methodEnd = $i
            break
        }
    }
}

if ($methodEnd -lt 0) {
    throw "Could not parse shared Entity.Destroy method body."
}

$body = $source.Substring($braceStart, $methodEnd - $braceStart + 1)
$enableIndex = $body.IndexOf('entity.Enable();', [System.StringComparison]::Ordinal)
if ($enableIndex -lt 0) {
    throw "Could not find entity.Enable() in shared Entity.Destroy helper."
}

$beforeEnable = $body.Substring(0, $enableIndex)
if ($beforeEnable -notmatch 'Has<DestroyTag>\s*\(\)' -or $beforeEnable -notmatch '\breturn;') {
    throw "Entity.Destroy must return early for entities with DestroyTag before calling entity.Enable()."
}

Write-Host "Entity.Destroy has a DestroyTag guard before entity.Enable()."
