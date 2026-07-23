# Run orchestrator with .env loaded and produce a captured log.
# Usage: pwsh scripts/run-qa.ps1
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Get-Content (Join-Path $root '.env') | ForEach-Object {
    if ($_ -match '^\s*#' -or $_ -match '^\s*$') { return }
    $name, $value = $_ -split '=', 2
    Set-Item -LiteralPath "Env:$name" -Value $value
}
& (Join-Path $root 'qa\scripts\run-backend-core.ps1') -IncludeIdentity -IncludeSchoolReference 2>&1 |
    Tee-Object -FilePath (Join-Path $root 'qa\.logs\backend-debug.log')
exit $LASTEXITCODE
