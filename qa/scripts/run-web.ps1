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

function Assert-WebOriginFree {
    param([int]$Port)
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() }
    catch { throw "Web port $Port is already in use." }
    finally { $listener.Stop() }
}

function Start-ApiForWeb {
    $apiDirectory = Join-Path $repositoryRoot 'backend\src\MyFSchool.Api'
    $apiAssembly = Join-Path $apiDirectory 'bin\Release\net10.0\MyFSchool.Api.dll'

    if (-not $env:QA_SQLSERVER_ADMIN_CONNECTION) {
        throw 'QA_SQLSERVER_ADMIN_CONNECTION is required. Define it in the repository-root .env.'
    }

    $databaseName = "MyFSchool_QA_$runId"
    $runRoot = Join-Path ([System.IO.Path]::GetTempPath()) $databaseName
    $storageRoot = Join-Path $runRoot 'storage'
    $logDirectory = Join-Path $runRoot 'logs'
    New-Item -ItemType Directory -Path $runRoot, $storageRoot, $logDirectory -Force | Out-Null

    Write-Host "[WEB-QA] Provisioning QA database $databaseName..."
    $adminBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
    $adminBuilder['Initial Catalog'] = 'master'
    $adminConnection = [System.Data.SqlClient.SqlConnection]::new($adminBuilder.ConnectionString)
    $adminConnection.Open()
    try {
        $cmd = $adminConnection.CreateCommand()
        try {
            $cmd.CommandText = "IF DB_ID('$databaseName') IS NULL CREATE DATABASE [$databaseName];"
            [void]$cmd.ExecuteNonQuery()
        } finally { $cmd.Dispose() }
    } finally { $adminConnection.Dispose() }

    $appBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
    $appBuilder['Initial Catalog'] = $databaseName
    $appBuilder['TrustServerCertificate'] = $true
    $applicationConnectionString = $appBuilder.ConnectionString

    Write-Host "[WEB-QA] Applying EF Core migrations..."
    $env:ConnectionStrings__Default = $applicationConnectionString
    & dotnet ef database update `
        --project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Infrastructure') `
        --startup-project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Api') `
        --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'EF Core migration update failed.' }

    $adminUserName = [Guid]::NewGuid().ToString('N').Substring(0, 12)
    # Identity policy: 12+ chars, digit, lower+upper, non-alphanumeric, no spaces/unicode disallowed
    $baseLower = ([Guid]::NewGuid().ToString('N') + ([Guid]::NewGuid().ToString('N'))).ToLower()
    $baseUpper = ([Guid]::NewGuid().ToString('N') + ([Guid]::NewGuid().ToString('N'))).ToUpper()
    $adminPassword = ('Pa1!' + $baseLower.Substring(0, 8) + $baseUpper.Substring(0, 4) + '!Aa1')
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = $apiOrigin
    $env:ConnectionStrings__Default = $applicationConnectionString
    $env:Storage__Provider = 'Local'
    $env:Storage__LocalRoot = $storageRoot
    $env:Bootstrap__Enabled = 'true'
    $env:Bootstrap__AdministratorUserName = $adminUserName
    $env:Bootstrap__AdministratorEmail = "$adminUserName@myfschool.local"
    $env:Bootstrap__AdministratorDisplayName = 'QA Administrator'
    $env:Bootstrap__AdministratorPassword = $adminPassword

    Write-Host "[WEB-QA] Starting API on $apiOrigin (logs: $logDirectory)..."
    $stdoutPath = Join-Path $logDirectory 'api.stdout.log'
    $stderrPath = Join-Path $logDirectory 'api.stderr.log'
    $proc = Start-Process -FilePath 'dotnet' `
        -ArgumentList @($apiAssembly) `
        -WorkingDirectory $apiDirectory `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru -WindowStyle Hidden
    $script:webApiProc = $proc
    $script:webApiDatabaseName = $databaseName
    $script:webApiRunRoot = $runRoot
    $script:webApiAdminUserName = $adminUserName
    $script:webApiAdminPassword = $adminPassword

    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if ($proc.HasExited) {
            Write-Host "[WEB-QA] API stderr: $(Get-Content $stderrPath -Raw -ErrorAction SilentlyContinue)"
            throw "API exited before becoming healthy on $apiOrigin (exit $($proc.ExitCode))."
        }
        try {
            $probe = Invoke-WebRequest -Uri "$apiOrigin/health" -UseBasicParsing -TimeoutSec 2
            if ($probe.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $stderrPath) {
        Write-Host "[WEB-QA] API stderr (last 200 lines):"
        Get-Content -LiteralPath $stderrPath -Tail 200
    }
    if (Test-Path -LiteralPath $stdoutPath) {
        Write-Host "[WEB-QA] API stdout (last 50 lines):"
        Get-Content -LiteralPath $stdoutPath -Tail 50
    }
    throw "API did not become healthy on $apiOrigin within 30s."
}

function Stop-ApiForWeb {
    param([Parameter(Mandatory=$false)][System.Diagnostics.Process]$Process)
    if ($Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
    if ($script:webApiDatabaseName) {
        try {
            $adminBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
            $adminBuilder['Initial Catalog'] = 'master'
            $adminConnection = [System.Data.SqlClient.SqlConnection]::new($adminBuilder.ConnectionString)
            $adminConnection.Open()
            try {
                $cmd = $adminConnection.CreateCommand()
                try {
                    $cmd.CommandText = "IF DB_ID('$($script:webApiDatabaseName)') IS NOT NULL BEGIN ALTER DATABASE [$($script:webApiDatabaseName)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($script:webApiDatabaseName)]; END"
                    $cmd.CommandTimeout = 30
                    [void]$cmd.ExecuteNonQuery()
                } finally { $cmd.Dispose() }
            } finally { $adminConnection.Dispose() }
        } catch { Write-Host "[WEB-QA] drop database failed: $_" }
    }
    if ($script:webApiRunRoot -and (Test-Path -LiteralPath $script:webApiRunRoot)) {
        Remove-Item -LiteralPath $script:webApiRunRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Install-Web {
    if ($SkipInstall) { return }
    Write-Host "[WEB-QA] Installing web deps ($runId)..."
    Push-Location $webDirectory
    try {
        $output = & npm.cmd install --no-audit --no-fund 2>&1
        $global:LASTEXITCODE = $LASTEXITCODE
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[WEB-QA] npm install stderr: $output"
            throw "npm install failed."
        }
    }
    finally { Pop-Location }
}

function Build-Web {
    Write-Host "[WEB-QA] Building production bundle ($runId)..."
    # Strip any shell-leaked VITE_API_BASE_URL so the build reads from .env.
    Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
    Push-Location $webDirectory
    try {
        & npm.cmd run build
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

    $deadline = (Get-Date).AddSeconds(20)
    while ((Get-Date) -lt $deadline) {
        if ($proc.HasExited) {
            throw "vite preview exited before becoming reachable on port $Port (exit $($proc.ExitCode))."
        }
        try {
            $probe = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/" -UseBasicParsing -TimeoutSec 3
            if ($probe.StatusCode -eq 200) { return $proc }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    throw "vite preview did not become reachable on port $Port within 20s."
}

function Stop-ProcessSafely {
    param([Parameter(Mandatory=$false)][System.Diagnostics.Process]$Process)
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

try {
    Install-Web
    Build-Web
    Start-ApiForWeb
    $webProcess = Start-WebPreview -Port $webPort
    Write-Host "[WEB-QA] API at $apiOrigin / Web at http://localhost:$webPort"

    Push-Location (Join-Path $repositoryRoot 'qa\web')
    try {
        & npx.cmd playwright install chromium
        if ($LASTEXITCODE -ne 0) { throw "playwright install failed." }

        $env:QA_API_ORIGIN = $apiOrigin
        $env:QA_WEB_ORIGIN = "http://localhost:$webPort"
        $env:QA_ADMIN_USERNAME = $script:webApiAdminUserName
        $env:QA_ADMIN_PASSWORD = $script:webApiAdminPassword

        & npx.cmd playwright test
        $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
}
finally {
    Stop-ProcessSafely -Process $webProcess
    if ($webStdout -and (Test-Path -LiteralPath $webStdout)) { Remove-Item -LiteralPath $webStdout -Force -ErrorAction SilentlyContinue }
    if ($webStderr -and (Test-Path -LiteralPath $webStderr)) { Remove-Item -LiteralPath $webStderr -Force -ErrorAction SilentlyContinue }
    Stop-ApiForWeb -Process $webApiProc
}

if ($exitCode -ne 0) { throw "Playwright run failed (exit $exitCode)." }
