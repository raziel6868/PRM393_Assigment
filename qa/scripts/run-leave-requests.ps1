[CmdletBinding()]
param(
    [int]$ApiPort = 5086,
    [switch]$KeepDatabase
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$backendDirectory = Join-Path $repositoryRoot 'backend'
$apiDirectory = Join-Path $repositoryRoot 'backend\src\MyFSchool.Api'
$apiAssembly = Join-Path $apiDirectory 'bin\Release\net10.0\MyFSchool.Api.dll'

# Ensure Release build is up to date before running scenario.
Write-Host "[leave-qa] building backend Release"
& dotnet build (Join-Path $backendDirectory 'MyFSchool.sln') --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Release build failed." }
if (-not (Test-Path -LiteralPath $apiAssembly)) {
    throw "Release build did not produce the API artifact: $apiAssembly"
}

$runId = '{0}_{1}' -f (Get-Date -Format 'yyyyMMddHHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
if ($runId -notmatch '^[0-9]{14}_[a-f0-9]{8}$') {
    throw "Unsafe run id format: $runId"
}

$databaseName = "MyFSchool_QA_$runId"
if ($databaseName -notmatch '^MyFSchool_QA_[0-9]{14}_[a-f0-9]{8}$') {
    throw "Unsafe database name: $databaseName"
}

$artifactDirectory = Join-Path $repositoryRoot "qa\artifacts\$runId"
$logsDirectory = Join-Path $artifactDirectory 'logs'
$null = New-Item -ItemType Directory -Force -Path $logsDirectory
$storageRoot = Join-Path ([System.IO.Path]::GetTempPath()) "MyFSchool-QA-leave-$runId"
$null = New-Item -ItemType Directory -Force -Path $storageRoot

$envFile = Join-Path $repositoryRoot '.env'
if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Repository-root .env not found at $envFile."
}
$parsed = @{}
foreach ($line in (Get-Content -LiteralPath $envFile)) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    $eq = $trimmed.IndexOf('=')
    if ($eq -le 0) { continue }
    $key = $trimmed.Substring(0, $eq).Trim()
    $value = $trimmed.Substring($eq + 1).Trim()
    $parsed[$key] = $value
}

$adminConnection = $parsed['QA_SQLSERVER_ADMIN_CONNECTION']
$jwtKey = $parsed['Auth__JwtSigningKey']
$issuer = $parsed['Auth__Issuer']
$audience = $parsed['Auth__Audience']
if (-not $adminConnection) { throw "QA_SQLSERVER_ADMIN_CONNECTION missing in .env" }
if (-not $jwtKey) { throw "Auth__JwtSigningKey missing in .env" }
if (-not $issuer) { throw "Auth__Issuer missing in .env" }
if (-not $audience) { throw "Auth__Audience missing in .env" }

$adminBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $adminConnection
$adminBuilder['Initial Catalog'] = 'master'
$applicationConnectionBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $adminConnection
$applicationConnectionBuilder['Initial Catalog'] = $databaseName
$applicationConnectionString = $applicationConnectionBuilder.ConnectionString

$adminPassword = "Qa!A7-$runId"
$adminUserName = "qa-leave-$runId"
$adminEmail = "qa-leave-$runId@example.invalid"
$bootstrapDisplayName = 'Quản trị viên QA'

$ownedProcesses = New-Object System.Collections.Generic.List[object]
$databaseCreated = $false
$databaseDropped = $true
$environmentRestored = $false
$previousEnvironment = @{}

function Save-CurrentEnv([string[]]$names) {
    foreach ($n in $names) {
        $previousEnvironment[$n] = [Environment]::GetEnvironmentVariable($n, 'Process')
    }
}

function Restore-Env {
    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item -LiteralPath "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
        }
    }
    $script:environmentRestored = $true
}

function Invoke-AdminSql([string]$commandText) {
    $connection = New-Object System.Data.SqlClient.SqlConnection $adminBuilder.ConnectionString
    try {
        $connection.Open()
        $cmd = $connection.CreateCommand()
        try {
            $cmd.CommandText = $commandText
            $cmd.CommandTimeout = 30
            [void]$cmd.ExecuteNonQuery()
        } finally { $cmd.Dispose() }
    } finally { $connection.Dispose() }
}

function Test-PortAvailable([int]$port) {
    $listener = New-Object System.Net.Sockets.TcpListener ([System.Net.IPAddress]::Loopback), $port
    try {
        $listener.Start()
    } catch {
        throw "Required port $port is already in use."
    } finally {
        $listener.Stop()
    }
}

function Start-IsolatedApiProcess {
    param(
        [int]$Port,
        [string]$LogPath,
        [string]$ApplicationConnectionString
    )

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $env:ConnectionStrings__Default = $ApplicationConnectionString
    $env:Storage__Provider = 'Local'
    $env:Storage__LocalRoot = $storageRoot
    $env:Auth__Issuer = $issuer
    $env:Auth__Audience = $audience
    $env:Auth__JwtSigningKey = $jwtKey
    $env:Auth__AccessTokenMinutes = '15'
    $env:Auth__RestrictedTokenMinutes = '5'
    $env:Auth__RefreshTokenDays = '7'
    $env:Auth__TemporaryPasswordHours = '24'
    $env:Bootstrap__Enabled = 'true'
    $env:Bootstrap__AdministratorUserName = $adminUserName
    $env:Bootstrap__AdministratorEmail = $adminEmail
    $env:Bootstrap__AdministratorDisplayName = $bootstrapDisplayName
    $env:Bootstrap__AdministratorPassword = $adminPassword
    $env:QA_ADMIN_USERNAME = $adminUserName
    $env:QA_ADMIN_PASSWORD = $adminPassword
    $env:Smtp__Enabled = 'false'
    $env:WebOrigins__AllowedOrigins__0 = 'http://localhost:5173'

    $stdoutLog = Join-Path $LogPath 'api.out.log'
    $stderrLog = Join-Path $LogPath 'api.err.log'

    $proc = Start-Process -FilePath 'dotnet' `
        -ArgumentList "`"$apiAssembly`"" `
        -WorkingDirectory $apiDirectory `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -WindowStyle Hidden `
        -PassThru
    $proc | Add-Member -NotePropertyName 'ApiStartedAt' -NotePropertyValue (Get-Date) -Force
    $ownedProcesses.Add($proc)

    $listeningDeadline = (Get-Date).AddSeconds(30)
    $listening = $false
    while ((Get-Date) -lt $listeningDeadline) {
        if ($proc.HasExited) { throw "API process exited with code $($proc.ExitCode) before becoming ready." }
        try {
            $null = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 3
            $listening = $true; break
        } catch [System.Net.WebException] {
            $code = $null
            try { $code = $_.Exception.Response.StatusCode.value__ } catch {}
            if ($code -and $code -gt 0) { $listening = $true; break }
            Start-Sleep -Milliseconds 300
        } catch {
            Start-Sleep -Milliseconds 300
        }
    }
    if (-not $listening) { throw "API did not start listening on port $Port within 30s." }

    $readyDeadline = (Get-Date).AddSeconds(45)
    $ready = $false
    while ((Get-Date) -lt $readyDeadline) {
        if ($proc.HasExited) { throw "API process exited with code $($proc.ExitCode) during readiness." }
        try {
            $r = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 300 }
    }
    if (-not $ready) { throw "API /health did not return 200 within 45s on port $Port." }
    return $proc
}

function Stop-OwnedApiProcesses {
    foreach ($p in $ownedProcesses) {
        if ($p.HasExited) { continue }
        try { $p.CloseMainWindow() | Out-Null } catch { }
        try { $p.Kill(); $p.WaitForExit(10) | Out-Null } catch { }
    }
}

Save-CurrentEnv @(
    'ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS','ConnectionStrings__Default',
    'Storage__Provider','Storage__LocalRoot','Auth__Issuer','Auth__Audience','Auth__JwtSigningKey',
    'Auth__AccessTokenMinutes','Auth__RestrictedTokenMinutes','Auth__RefreshTokenDays','Auth__TemporaryPasswordHours',
    'Bootstrap__Enabled','Bootstrap__AdministratorUserName','Bootstrap__AdministratorEmail',
    'Bootstrap__AdministratorDisplayName','Bootstrap__AdministratorPassword','QA_ADMIN_USERNAME','QA_ADMIN_PASSWORD',
    'Smtp__Enabled','WebOrigins__AllowedOrigins__0'
)

$startedAt = Get-Date
Write-Host "[leave-qa] run=$runId db=$databaseName port=$ApiPort started=$($startedAt.ToString('o'))"

try {
    Test-PortAvailable -Port $ApiPort

    Write-Host "[leave-qa] creating database $databaseName"
    Invoke-AdminSql "CREATE DATABASE [$databaseName]"
    $databaseCreated = $true
    $databaseDropped = $false

    Write-Host "[leave-qa] applying migrations"
    $env:ConnectionStrings__Default = $applicationConnectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet ef database update `
        --project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Infrastructure') `
        --startup-project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Api') `
        --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "Migration failed with exit code $LASTEXITCODE." }

    Write-Host "[leave-qa] starting API on port $ApiPort"
    $api = Start-IsolatedApiProcess -Port $ApiPort -LogPath $logsDirectory -ApplicationConnectionString $applicationConnectionString
    Write-Host "[leave-qa] API ready, pid=$($api.Id)"

    Write-Host "[leave-qa] running leave-requests scenario"
    Push-Location (Join-Path $repositoryRoot 'qa\api')
    try {
        $env:QA_API_ORIGIN = "http://127.0.0.1:$ApiPort"
        $npmLog = Join-Path $logsDirectory 'leave-requests.out.log'
        # Redirect stdout/stderr explicitly. PowerShell's *> redirect
        # collides with wildcard expansion, so use a Tee-Object fallback.
        $outputLines = & node.exe 'tests\leave-requests.mjs' 2>&1
        $testExit = $LASTEXITCODE
        Set-Content -LiteralPath $npmLog -Value $outputLines -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath $npmLog) {
            Get-Content -LiteralPath $npmLog -ErrorAction SilentlyContinue | Select-Object -Last 60 | ForEach-Object { Write-Host "[leave-qa]   $_" }
        }
    } finally {
        Pop-Location
    }

    if ($testExit -ne 0) { throw "leave-requests scenario failed with exit code $testExit." }
    Write-Host "[leave-qa] leave-requests passed in $([math]::Round(((Get-Date) - $startedAt).TotalSeconds, 2))s"
    $outcome = 'passed'
}
catch {
    Write-Host "[leave-qa] FAILED: $($_.Exception.Message)"
    $outcome = 'failed'
}
finally {
    Write-Host "[leave-qa] tearing down API processes"
    Stop-OwnedApiProcesses
    if ($databaseCreated -and -not $KeepDatabase) {
        try {
            Write-Host "[leave-qa] dropping database $databaseName"
            Invoke-AdminSql "ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]"
            $databaseDropped = $true
        } catch {
            Write-Host "[leave-qa] database drop failed: $($_.Exception.Message)"
        }
    }
    if (Test-Path -LiteralPath $storageRoot) {
        try {
            Remove-Item -LiteralPath $storageRoot -Recurse -Force -ErrorAction Stop
            Write-Host "[leave-qa] removed storage root $storageRoot"
        } catch {
            Write-Host "[leave-qa] storage root cleanup failed: $($_.Exception.Message)"
        }
    }
    Get-Job | Remove-Job -Force
    Restore-Env
    Write-Host "[leave-qa] outcome=$outcome artifacts=$artifactDirectory"
}

if ($outcome -ne 'passed') { exit 1 } else { exit 0 }