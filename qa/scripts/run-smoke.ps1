[CmdletBinding()]
param(
    [string]$ApiOrigin = 'http://127.0.0.1:5080',
    [ValidateSet('smoke', 'core-contract', 'identity-auth', 'password-assistance', 'identity-relationships')]
    [string]$Scenario = 'smoke',
    [switch]$ThrowOnFailure
)

$ErrorActionPreference = 'Stop'
$startedAt = Get-Date
$status = 'failed'
$failureExitCode = $null
$previousApiOrigin = [Environment]::GetEnvironmentVariable('QA_API_ORIGIN', 'Process')
Write-Host "[QA] API smoke started at $($startedAt.ToString('o'))"

try {
    $env:QA_API_ORIGIN = $ApiOrigin
    Push-Location (Join-Path $PSScriptRoot '..\api')
    try {
        npm run $Scenario
        if ($LASTEXITCODE -ne 0) {
            $failureExitCode = $LASTEXITCODE
            if ($ThrowOnFailure) {
                $exception = [System.InvalidOperationException]::new("API smoke failed with exit code $failureExitCode.")
                $exception.Data['QaOutcome'] = "exit-code:$failureExitCode"
                throw $exception
            }
        }
        else { $status = 'passed' }
    }
    finally {
        Pop-Location
    }
}
finally {
    if ($null -eq $previousApiOrigin) {
        Remove-Item -LiteralPath 'Env:QA_API_ORIGIN' -ErrorAction SilentlyContinue
    }
    else {
        [Environment]::SetEnvironmentVariable('QA_API_ORIGIN', $previousApiOrigin, 'Process')
    }
    $duration = (Get-Date) - $startedAt
    Write-Host "[QA] API smoke $status in $([math]::Round($duration.TotalSeconds, 2))s"
}

if ($null -ne $failureExitCode) { exit $failureExitCode }
