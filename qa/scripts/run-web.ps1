[CmdletBinding()]
param(
    [switch]$SkipInstall,
    [switch]$IncludeImports
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$webDirectory = Join-Path $repositoryRoot 'frontend-web'
$distDirectory = Join-Path $webDirectory 'dist'
$webPort = 5173
$apiOrigin = 'http://127.0.0.1:5080'
$runId = '{0}_{1}' -f (Get-Date -Format 'yyyyMMddHHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$webStdout = Join-Path $env:TEMP "myfschool-web-$runId.stdout.log"
$webStderr = Join-Path $env:TEMP "myfschool-web-$runId.stderr.log"
$apiRunManifest = Join-Path $repositoryRoot "qa\artifacts\$runId\result-manifest.json"

function Assert-WebOriginFree {
    param([int]$Port)
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() }
    catch { throw "Web port $Port is already in use." }
    finally { $listener.Stop() }
}

function Start-ApiForWeb {
    $script = Join-Path $repositoryRoot 'qa\scripts\run-backend-core.ps1'
    if ($IncludeImports) {
        Write-Host '[WEB-QA] Starting API with identity + school reference + imports...'
        & $script -IncludeIdentity -IncludeSchoolReference -IncludeImports
    }
    else {
        Write-Host '[WEB-QA] Starting API with identity only...'
        & $script -IncludeIdentity
    }
    if ($LASTEXITCODE -ne 0) { throw "Backend-core QA failed (exit $LASTEXITCODE)." }
}

function Install-Web {
    if ($SkipInstall) { return }
    Write-Host "[WEB-QA] Installing web deps ($runId)..."
    Push-Location $webDirectory
    try {
        & npm install --no-audit --no-fund | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "npm install failed." }
    }
    finally { Pop-Location }
}

function Build-Web {
    Write-Host "[WEB-QA] Building production bundle ($runId)..."
    Push-Location $webDirectory
    try {
        & npm run build | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "vite build failed." }
    }
    finally { Pop-Location }
}

function Start-WebPreview {
    param([int]$Port)
    Assert-WebOriginFree -Port $Port
    Write-Host "[WEB-QA] Starting vite preview on port $Port (logs: $webStdout)"
    $proc = Start-Process -FilePath 'npx.cmd' `
        -ArgumentList @('vite', 'preview', '--port', "$Port", '--strictPort', '--host', '127.0.0.1') `
        -WorkingDirectory $webDirectory `
        -RedirectStandardOutput $webStdout `
        -RedirectStandardError $webStderr `
        -PassThru -WindowStyle Hidden

    $deadline = (Get-Date).AddSeconds(15)
    do {
        if ($proc.HasExited) {
            throw "vite preview exited before becoming reachable on port $Port."
        }
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return $proc }
        }
        catch { $response = $null }
        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $deadline)

    throw "vite preview did not become reachable on port $Port."
}

function Stop-ProcessSafely {
    param([Parameter(Mandatory)][System.Diagnostics.Process]$Process)
    if (-not $Process) { return }
    try {
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id
            $Process.WaitForExit(5000) | Out-Null
        }
    }
    catch { Write-Host "[WEB-QA] WARN: failed to stop process $($Process.Id)" }
}

$webProcess = $null
$apiOrchestratorRan = $false

try {
    Install-Web
    Build-Web
    Start-ApiForWeb
    $apiOrchestratorRan = $true
    $webProcess = Start-WebPreview -Port $webPort
    Write-Host "[WEB-QA] API at $apiOrigin / Web at http://localhost:$webPort"

    Push-Location (Join-Path $repositoryRoot 'qa\web')
    try {
        & npx.cmd playwright install chromium | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "playwright install failed." }

        $env:QA_API_ORIGIN = $apiOrigin
        $env:QA_WEB_ORIGIN = "http://localhost:$webPort"
        if (Test-Path -LiteralPath $apiRunManifest) {
            $manifest = Get-Content -LiteralPath $apiRunManifest -Raw | ConvertFrom-Json
            $env:QA_ADMIN_USERNAME = $manifest.resources.psObject.Properties['adminUserName']?.Value
            $env:QA_ADMIN_PASSWORD = $manifest.resources.psObject.Properties['adminPassword']?.Value
        }

        & npx.cmd playwright test
        $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
}
finally {
    Stop-ProcessSafely -Process $webProcess
    if ($webStdout -and (Test-Path -LiteralPath $webStdout)) { Remove-Item -LiteralPath $webStdout -Force -ErrorAction SilentlyContinue }
    if ($webStderr -and (Test-Path -LiteralPath $webStderr)) { Remove-Item -LiteralPath $webStderr -Force -ErrorAction SilentlyContinue }
}

if ($exitCode -ne 0) { throw "Playwright run failed (exit $exitCode)." }
