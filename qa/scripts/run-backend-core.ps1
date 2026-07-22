[CmdletBinding()]
param([switch]$IncludeIdentity)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiDirectory = Join-Path $repositoryRoot 'backend\src\MyFSchool.Api'
$solutionPath = Join-Path $repositoryRoot 'backend\MyFSchool.sln'
$apiAssembly = Join-Path $apiDirectory 'bin\Release\net10.0\MyFSchool.Api.dll'
$runId = '{0}_{1}' -f (Get-Date -Format 'yyyyMMddHHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$databaseName = "MyFSchool_QA_$runId"
$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$runRoot = [System.IO.Path]::GetFullPath((Join-Path $temporaryRoot $databaseName))
$storageRoot = Join-Path $runRoot 'storage'
$missingStorageRoot = Join-Path $runRoot 'missing-storage'
$ownedProcesses = [System.Collections.Generic.List[object]]::new()
$cleanupFailures = [System.Collections.Generic.List[string]]::new()
$startedAt = Get-Date
$runFailure = $null
$databaseCreated = $false
$databaseDropped = $true
$runIdentityValidated = $false
$runRootRemoved = $true
$environmentRestored = $false
$currentPhase = 'setup'
$failureContext = [ordered]@{
    command = 'run-backend-core.ps1'
    scenario = 'backend-core'
    exitCodeOrTimeout = 'not-applicable'
    stableError = 'setup-failed'
    routeOrStep = 'setup'
}
$artifactDirectory = Join-Path $repositoryRoot "qa\artifacts\$runId"
$retainedLogs = [System.Collections.Generic.List[string]]::new()
$scenarioResults = [ordered]@{
    releaseBuild = 'pending'
    invalidSqlConfiguration = 'pending'
    invalidStorageConfiguration = 'pending'
    health = 'pending'
    apiContract = 'pending'
    ready = 'pending'
    missingStorage = 'pending'
}
if ($IncludeIdentity) {
    $scenarioResults.identityMigration = 'pending'
    $scenarioResults.identityAuth = 'pending'
    $scenarioResults.passwordAssistance = 'pending'
    $scenarioResults.identityPersistence = 'pending'
}
$importedEnvironmentNames = [System.Collections.Generic.List[string]]::new()
$managedEnvironmentNames = @(
    'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS', 'ConnectionStrings__Default', 'Storage__Provider', 'Storage__LocalRoot',
    'Auth__Issuer', 'Auth__Audience', 'Auth__JwtSigningKey', 'Auth__AccessTokenMinutes', 'Auth__RestrictedTokenMinutes',
    'Auth__RefreshTokenDays', 'Auth__TemporaryPasswordHours', 'Bootstrap__Enabled', 'Bootstrap__AdministratorUserName',
    'WebOrigins__AllowedOrigins__0',
    'Bootstrap__AdministratorEmail', 'Bootstrap__AdministratorDisplayName', 'Bootstrap__AdministratorPassword',
    'QA_ADMIN_USERNAME', 'QA_ADMIN_PASSWORD'
)
$previousEnvironment = @{}
foreach ($name in $managedEnvironmentNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
. (Join-Path $PSScriptRoot 'env.ps1')

function Assert-SafeRunIdentity {
    if ($databaseName -notmatch '^MyFSchool_QA_[0-9]{14}_[a-f0-9]{8}$') {
        throw 'Unsafe QA database name.'
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($runRoot)
    if (-not $resolvedPath.StartsWith($temporaryRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        [System.IO.Path]::GetFileName($resolvedPath) -ne $databaseName) {
        throw 'Unsafe QA run path.'
    }
}

function Assert-PortAvailable {
    param([Parameter(Mandatory)][int]$Port)

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() }
    catch { throw "Required QA port $Port is already in use." }
    finally { $listener.Stop() }
}

function Invoke-AdminSql {
    param([Parameter(Mandatory)][string]$CommandText)

    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
    $builder['Initial Catalog'] = 'master'
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $CommandText
            $command.CommandTimeout = 30
            [void]$command.ExecuteNonQuery()
        }
        finally { $command.Dispose() }
    }
    finally { $connection.Dispose() }
}

function Start-IsolatedApiProcess {
    param(
        [Parameter(Mandatory)][string]$StandardOutputPath,
        [Parameter(Mandatory)][string]$StandardErrorPath
    )

    $excludedNames = @('QA_SQLSERVER_ADMIN_CONNECTION', 'QA_GMAIL_RECIPIENT', 'QA_ADMIN_USERNAME', 'QA_ADMIN_PASSWORD', 'Smtp__UserName', 'Smtp__Password')
    $excludedValues = @{}
    foreach ($name in $excludedNames) {
        $excludedValues[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
    }

    try {
        return Start-Process -FilePath 'dotnet' -ArgumentList @($apiAssembly) `
            -WorkingDirectory $apiDirectory `
            -RedirectStandardOutput $StandardOutputPath `
            -RedirectStandardError $StandardErrorPath `
            -PassThru -WindowStyle Hidden
    }
    finally {
        foreach ($name in $excludedNames) {
            if ($null -eq $excludedValues[$name]) {
                Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
            }
            else {
                [Environment]::SetEnvironmentVariable($name, $excludedValues[$name], 'Process')
            }
        }
    }
}

function Start-ApiProcess {
    param(
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][string]$ConfiguredStorageRoot,
        [Parameter(Mandatory)][string]$LogName,
        [Parameter(Mandatory)][string]$ApplicationConnectionString
    )

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $env:ConnectionStrings__Default = $ApplicationConnectionString
    $env:Storage__Provider = 'Local'
    $env:Storage__LocalRoot = $ConfiguredStorageRoot

    $process = Start-IsolatedApiProcess `
        -StandardOutputPath (Join-Path $runRoot "$LogName.stdout.log") `
        -StandardErrorPath (Join-Path $runRoot "$LogName.stderr.log")

    $ownedProcesses.Add([pscustomobject]@{ Process = $process; StartTime = $process.StartTime })
    $deadline = (Get-Date).AddSeconds(20)
    $attempt = 0
    do {
        if ($process.HasExited) {
            $failureContext.exitCodeOrTimeout = "exit-code:$($process.ExitCode)"
            $failureContext.stableError = 'api-exited-before-health'
            throw "API on port $Port exited before readiness."
        }
        try { $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 2 }
        catch { $response = $null }
        if (-not $response) {
            $attempt++
            if ($attempt % 20 -eq 0) { Write-Host "[QA] waiting for API on port $Port..." }
            Start-Sleep -Milliseconds 250
        }
    } while (-not $response -and (Get-Date) -lt $deadline)

    if (-not $response) {
        $failureContext.exitCodeOrTimeout = 'timeout:20s'
        $failureContext.stableError = 'api-health-deadline'
        throw "API on port $Port did not become healthy before the deadline."
    }
}

function Test-TimeoutFailure {
    param([Parameter(Mandatory)][System.Management.Automation.ErrorRecord]$ErrorRecord)

    $exception = $ErrorRecord.Exception
    while ($exception) {
        if ($exception -is [System.TimeoutException] -or
            $exception -is [System.Threading.Tasks.TaskCanceledException] -or
            $exception -is [System.OperationCanceledException] -or
            $exception.Message -match '(?i)timed?\s*out|timeout|operation was canceled') {
            return $true
        }
        $exception = $exception.InnerException
    }
    return $false
}

function Assert-InvalidConfigurationFailsFast {
    param([Parameter(Mandatory)][string]$ApplicationConnectionString)

    $invalidConnections = @(
        'Encrypt=True',
        'Server=localhost;Database=master;Authentication=Sql Password;Encrypt=True',
        'Server=localhost;Database=master;Integrated Security=True;Authentication=Sql Password;Encrypt=True',
        'Server=localhost;Database=master;Integrated Security=True;User ID=unexpected;Encrypt=True',
        'Server=localhost;Database=master;Integrated Security=True;Password=unexpected;Encrypt=True'
    )

    for ($index = 0; $index -lt $invalidConnections.Count; $index++) {
        $failureContext.command = 'dotnet MyFSchool.Api.dll'
        $failureContext.scenario = 'invalid-configuration'
        $failureContext.exitCodeOrTimeout = 'pending'
        $failureContext.stableError = 'invalid-config-did-not-fail-fast'
        $failureContext.routeOrStep = "case:$index"
        $env:ASPNETCORE_URLS = 'http://127.0.0.1:5082'
        $env:ConnectionStrings__Default = $invalidConnections[$index]
        $env:Storage__Provider = 'Local'
        $env:Storage__LocalRoot = $storageRoot
        $stdoutPath = Join-Path $runRoot "invalid-config-$index.stdout.log"
        $stderrPath = Join-Path $runRoot "invalid-config-$index.stderr.log"
        $process = Start-IsolatedApiProcess `
            -StandardOutputPath $stdoutPath `
            -StandardErrorPath $stderrPath

        $ownedProcesses.Add([pscustomobject]@{ Process = $process; StartTime = $process.StartTime })
        if (-not $process.WaitForExit(10000)) {
            $failureContext.exitCodeOrTimeout = 'timeout:10s'
            throw 'Invalid SQL configuration did not fail before the 10-second deadline.'
        }

        $failureContext.exitCodeOrTimeout = "exit-code:$($process.ExitCode)"
        if ($process.ExitCode -eq 0) {
            $failureContext.stableError = 'invalid-config-unexpected-success'
            throw 'Invalid SQL configuration unexpectedly started the API.'
        }
        $combinedOutput = (Get-Content -Raw $stdoutPath) + (Get-Content -Raw $stderrPath)
        if (-not $combinedOutput.Contains('ConnectionStrings__Default must be a valid SQL Server connection string')) {
            $failureContext.stableError = 'validation-message-missing'
            $failureContext.routeOrStep = "case:${index}:startup-output"
            throw 'Invalid SQL configuration did not fail with the setting-named validation message.'
        }
    }
    $scenarioResults.invalidSqlConfiguration = 'passed'

    $invalidStorageCases = @(
        [ordered]@{ Provider = 'Cloud'; LocalRoot = $storageRoot; Expected = 'Storage__Provider must be Local'; Code = 'provider-not-local' },
        [ordered]@{ Provider = 'Local'; LocalRoot = 'relative-storage'; Expected = 'Storage__LocalRoot must be an absolute path'; Code = 'path-not-absolute' },
        [ordered]@{ Provider = 'Local'; LocalRoot = (Join-Path $repositoryRoot 'qa-storage'); Expected = 'Storage__LocalRoot must be outside the repository'; Code = 'path-inside-repository' }
    )

    for ($index = 0; $index -lt $invalidStorageCases.Count; $index++) {
        $case = $invalidStorageCases[$index]
        $failureContext.command = 'dotnet MyFSchool.Api.dll'
        $failureContext.scenario = 'invalid-storage-configuration'
        $failureContext.exitCodeOrTimeout = 'pending'
        $failureContext.stableError = $case.Code
        $failureContext.routeOrStep = "case:$index"
        $env:ASPNETCORE_URLS = 'http://127.0.0.1:5082'
        $env:ConnectionStrings__Default = $ApplicationConnectionString
        $env:Storage__Provider = $case.Provider
        $env:Storage__LocalRoot = $case.LocalRoot
        $stdoutPath = Join-Path $runRoot "invalid-storage-$index.stdout.log"
        $stderrPath = Join-Path $runRoot "invalid-storage-$index.stderr.log"
        $process = Start-IsolatedApiProcess `
            -StandardOutputPath $stdoutPath `
            -StandardErrorPath $stderrPath

        $ownedProcesses.Add([pscustomobject]@{ Process = $process; StartTime = $process.StartTime })
        if (-not $process.WaitForExit(10000)) {
            $failureContext.exitCodeOrTimeout = 'timeout:10s'
            throw 'Invalid storage configuration did not fail before the 10-second deadline.'
        }

        $failureContext.exitCodeOrTimeout = "exit-code:$($process.ExitCode)"
        if ($process.ExitCode -eq 0) { throw 'Invalid storage configuration unexpectedly started the API.' }
        $combinedOutput = (Get-Content -Raw $stdoutPath) + (Get-Content -Raw $stderrPath)
        if (-not $combinedOutput.Contains($case.Expected)) {
            $failureContext.stableError = 'storage-validation-message-missing'
            $failureContext.routeOrStep = "case:${index}:startup-output"
            throw 'Invalid storage configuration did not fail with the setting-named validation message.'
        }
    }
    $scenarioResults.invalidStorageConfiguration = 'passed'
}

function Stop-OwnedProcesses {
    foreach ($owned in $ownedProcesses) {
        try {
            $process = Get-Process -Id $owned.Process.Id -ErrorAction SilentlyContinue
            if (-not $process) { continue }
            if ($process.StartTime -ne $owned.StartTime) { throw 'PID start time changed.' }
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id
                if (-not $process.WaitForExit(5000)) {
                    Stop-Process -Id $process.Id -Force
                    if (-not $process.WaitForExit(5000)) { throw 'Process did not exit.' }
                }
            }
        }
        catch { $cleanupFailures.Add("Failed to stop owned API PID $($owned.Process.Id).") }
    }
}

function Assert-IdentityPersistence {
    param([Parameter(Mandatory)][string]$ApplicationConnectionString)

    $connection = [System.Data.SqlClient.SqlConnection]::new($ApplicationConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = @'
SELECT
    (SELECT COUNT(*) FROM Users) AS UserCount,
    (SELECT COUNT(*) FROM Roles) AS RoleCount,
    (SELECT COUNT(*) FROM SecurityAuditEvents) AS AuditCount,
    (SELECT COUNT(*) FROM RefreshTokens) AS RefreshCount,
    (SELECT COUNT(*) FROM RefreshTokens WHERE LEN(TokenHash) <> 44) AS InvalidHashCount,
    (SELECT COUNT(*) FROM Users WHERE MustChangePassword = 1 AND TemporaryPasswordExpiresAtUtc IS NOT NULL) AS RestrictedUserCount,
    (SELECT COUNT(*) FROM PasswordHelpRequests WHERE Status = 0) AS PendingHelpCount,
    (SELECT COUNT(*) FROM PasswordHelpRequests WHERE Status = 1) AS ResolvedHelpCount,
    (SELECT COUNT(*) FROM PasswordHelpRequests WHERE Status = 2) AS RejectedHelpCount
'@
            $reader = $command.ExecuteReader()
            try {
                if (-not $reader.Read() -or
                    $reader.GetInt32(0) -lt 2 -or
                    $reader.GetInt32(1) -ne 4 -or
                    $reader.GetInt32(2) -lt 4 -or
                    $reader.GetInt32(3) -lt 3 -or
                    $reader.GetInt32(4) -ne 0 -or
                    $reader.GetInt32(5) -ne 4 -or
                    $reader.GetInt32(6) -ne 0 -or
                    $reader.GetInt32(7) -ne 1 -or
                    $reader.GetInt32(8) -ne 1) {
                    throw 'Identity persistence counts or token-hash invariants did not match the accepted flow.'
                }
            }
            finally { $reader.Dispose() }
        }
        finally { $command.Dispose() }
    }
    finally { $connection.Dispose() }
}

function Save-FailureEvidence {
    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    $sensitiveValues = @(
        [Environment]::GetEnvironmentVariable('Smtp__Password', 'Process'),
        [Environment]::GetEnvironmentVariable('Auth__JwtSigningKey', 'Process'),
        [Environment]::GetEnvironmentVariable('Bootstrap__AdministratorPassword', 'Process'),
        [Environment]::GetEnvironmentVariable('QA_ADMIN_PASSWORD', 'Process'),
        [Environment]::GetEnvironmentVariable('QA_SQLSERVER_ADMIN_CONNECTION', 'Process')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    if (Test-Path -LiteralPath $runRoot -PathType Container) {
        foreach ($logFile in Get-ChildItem -LiteralPath $runRoot -File -Filter '*.log') {
            $destination = Join-Path $artifactDirectory $logFile.Name
            $redactedLines = Get-Content -LiteralPath $logFile.FullName -Tail 200 | ForEach-Object {
                $line = [string]$_
                foreach ($value in $sensitiveValues) {
                    $line = $line.Replace($value, '<REDACTED>', [System.StringComparison]::Ordinal)
                }
                $line = $line.Replace($repositoryRoot, '<REPOSITORY>', [System.StringComparison]::OrdinalIgnoreCase)
                $line = $line.Replace($temporaryRoot, '<TEMP>', [System.StringComparison]::OrdinalIgnoreCase)
                $line = $line -replace '(?i)(Bearer\s+)[A-Za-z0-9._-]+', '$1REDACTED'
                $line = $line -replace '(?i)((?:password|pwd|token|secret|authorization|credential|api[-_]?key)\s*[:=]\s*)[^\s,;]+', '$1REDACTED'
                $line -replace '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}', 'REDACTED_EMAIL'
            }
            Set-Content -LiteralPath $destination -Value $redactedLines
            $retainedLogs.Add($logFile.Name)
        }
    }

    Write-Host "[QA] redacted failure evidence retained at $artifactDirectory"
}

function Test-OwnedProcessesExited {
    foreach ($owned in $ownedProcesses) {
        $process = Get-Process -Id $owned.Process.Id -ErrorAction SilentlyContinue
        if ($process -and $process.StartTime -eq $owned.StartTime -and -not $process.HasExited) { return $false }
    }
    return $true
}

function Write-ResultManifest {
    param([Parameter(Mandatory)][string]$Status)

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    $gitHead = (& git -C $repositoryRoot rev-parse HEAD 2>$null | Select-Object -First 1)
    $dotnetSdk = (& dotnet --version 2>$null | Select-Object -First 1)
    $nodeVersion = (& node --version 2>$null | Select-Object -First 1)
    $apiSha256 = if (Test-Path -LiteralPath $apiAssembly -PathType Leaf) {
        (Get-FileHash -LiteralPath $apiAssembly -Algorithm SHA256).Hash
    }
    else { $null }
    $inputFiles = @(
        Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'backend\src') -Recurse -File |
            Where-Object {
                $_.Extension -in @('.cs', '.csproj', '.json') -and
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            }
        Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'qa\api') -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/]node_modules[\\/]' }
        Get-Item -LiteralPath (Join-Path $repositoryRoot 'qa\scripts\run-backend-core.ps1')
        Get-Item -LiteralPath (Join-Path $repositoryRoot 'qa\scripts\run-smoke.ps1')
    ) | Sort-Object FullName -Unique
    $inputIdentity = $inputFiles | ForEach-Object {
        $relativePath = [System.IO.Path]::GetRelativePath($repositoryRoot, $_.FullName)
        "$relativePath`:$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
    }
    $inputSha256 = [Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData(
            [System.Text.Encoding]::UTF8.GetBytes(($inputIdentity -join "`n"))))
    $failure = if ($runFailure) {
        $exceptionType = $runFailure.Exception.GetType().Name
        $fingerprint = @(
            $failureContext.command,
            $failureContext.scenario,
            $failureContext.exitCodeOrTimeout,
            $failureContext.stableError,
            $failureContext.routeOrStep,
            $exceptionType
        ) -join '|'
        [ordered]@{
            phase = $currentPhase
            exceptionType = $exceptionType
            command = $failureContext.command
            scenario = $failureContext.scenario
            exitCodeOrTimeout = $failureContext.exitCodeOrTimeout
            stableError = $failureContext.stableError
            routeOrStep = $failureContext.routeOrStep
            fingerprint = $fingerprint
        }
    }
    elseif ($cleanupFailures.Count -gt 0) {
        [ordered]@{
            phase = 'teardown'
            exceptionType = 'CleanupFailure'
            command = 'run-backend-core.ps1 teardown'
            scenario = 'resource-cleanup'
            exitCodeOrTimeout = 'not-applicable'
            stableError = 'teardown-failed'
            routeOrStep = 'cleanup-resources'
            fingerprint = 'run-backend-core.ps1 teardown|resource-cleanup|not-applicable|teardown-failed|cleanup-resources|CleanupFailure'
        }
    }
    else { $null }
    $processes = @($ownedProcesses | ForEach-Object {
        [ordered]@{
            pid = $_.Process.Id
            startTimeUtc = $_.StartTime.ToUniversalTime().ToString('o')
        }
    })

    [ordered]@{
        runId = $runId
        status = $Status
        recordedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        failure = $failure
        artifact = [ordered]@{
            gitHead = [string]$gitHead
            apiSha256 = $apiSha256
            sourceAndQaInputSha256 = $inputSha256
            dotnetSdk = [string]$dotnetSdk
            nodeVersion = [string]$nodeVersion
        }
        resources = [ordered]@{
            databaseName = $databaseName
            storagePath = $storageRoot
            ports = @(5080, 5081, 5082)
            processes = $processes
        }
        scenarios = $scenarioResults
        teardown = [ordered]@{
            processesExited = (Test-OwnedProcessesExited)
            databaseDropped = $databaseDropped
            temporaryDirectoryRemoved = $runRootRemoved
            environmentRestored = $environmentRestored
            failures = @($cleanupFailures)
        }
        retainedLogs = @($retainedLogs)
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $artifactDirectory 'result-manifest.json')
}

try {
    $currentPhase = 'release-build'
    $failureContext.command = 'dotnet build backend/MyFSchool.sln -c Release --no-restore'
    $failureContext.scenario = 'release-build'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'release-build-failed'
    $failureContext.routeOrStep = 'compile current source before QA'
    & dotnet build $solutionPath --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        $failureContext.exitCodeOrTimeout = "exit-code:$LASTEXITCODE"
        throw 'Release build failed.'
    }
    if (-not (Test-Path -LiteralPath $apiAssembly -PathType Leaf)) {
        throw "Release build did not produce the API artifact: $apiAssembly"
    }
    $scenarioResults.releaseBuild = 'passed'

    $currentPhase = 'configuration'
    $failureContext.command = 'Import-RootEnvironment'
    $failureContext.scenario = 'configuration'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'configuration-invalid'
    $failureContext.routeOrStep = 'repository-root-env'
    Import-RootEnvironment -RepositoryRoot $repositoryRoot -ImportedNames $importedEnvironmentNames
    if ([string]::IsNullOrWhiteSpace($env:QA_SQLSERVER_ADMIN_CONNECTION)) {
        throw 'Missing required setting: QA_SQLSERVER_ADMIN_CONNECTION'
    }
    $env:Auth__Issuer = 'MyFSchool.QA'
    $env:Auth__Audience = 'MyFSchool.QA.Clients'
    $env:Auth__JwtSigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $env:Auth__AccessTokenMinutes = '15'
    $env:Auth__RestrictedTokenMinutes = '5'
    $env:Auth__RefreshTokenDays = '7'
    $env:Auth__TemporaryPasswordHours = '24'
    $env:WebOrigins__AllowedOrigins__0 = 'http://localhost:5173'
    $env:Bootstrap__Enabled = 'false'

    Assert-SafeRunIdentity
    $runIdentityValidated = $true
    $currentPhase = 'port-validation'
    foreach ($port in 5080..5082) {
        $failureContext.command = 'Assert-PortAvailable'
        $failureContext.scenario = 'port-validation'
        $failureContext.exitCodeOrTimeout = 'not-applicable'
        $failureContext.stableError = 'port-in-use'
        $failureContext.routeOrStep = "port:$port"
        Assert-PortAvailable -Port $port
    }
    $currentPhase = 'temporary-storage'
    $failureContext.command = 'New-Item'
    $failureContext.scenario = 'temporary-storage'
    $failureContext.stableError = 'storage-create-failed'
    $failureContext.routeOrStep = 'create-run-storage'
    New-Item -ItemType Directory -Path $storageRoot -Force | Out-Null
    $runRootRemoved = $false

    Write-Host "[QA] backend-core run $runId started at $($startedAt.ToString('o'))"
    $currentPhase = 'database-create'
    $failureContext.command = 'Invoke-AdminSql'
    $failureContext.scenario = 'database-lifecycle'
    $failureContext.stableError = 'database-create-failed'
    $failureContext.routeOrStep = 'create-database'
    Invoke-AdminSql -CommandText "CREATE DATABASE [$databaseName]"
    $databaseCreated = $true
    $databaseDropped = $false
    $applicationBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
    $applicationBuilder['Initial Catalog'] = $databaseName
    $applicationConnectionString = $applicationBuilder.ConnectionString

    if ($IncludeIdentity) {
        $currentPhase = 'identity-migration'
        $failureContext.command = 'dotnet ef database update'
        $failureContext.scenario = 'identity-migration'
        $failureContext.exitCodeOrTimeout = 'not-applicable'
        $failureContext.stableError = 'identity-migration-failed'
        $failureContext.routeOrStep = 'apply current migration to fresh database'
        $env:ConnectionStrings__Default = $applicationConnectionString
        & dotnet ef database update `
            --project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Infrastructure') `
            --startup-project (Join-Path $repositoryRoot 'backend\src\MyFSchool.Api') `
            --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) {
            $failureContext.exitCodeOrTimeout = "exit-code:$LASTEXITCODE"
            throw 'Identity migration failed.'
        }
        $scenarioResults.identityMigration = 'passed'
    }

    $currentPhase = 'invalid-configuration'
    Assert-InvalidConfigurationFailsFast -ApplicationConnectionString $applicationConnectionString
    if ($IncludeIdentity) {
        $env:Bootstrap__Enabled = 'true'
        $env:Bootstrap__AdministratorUserName = "qa-admin-$runId"
        $env:Bootstrap__AdministratorEmail = "qa-admin-$runId@example.invalid"
        $env:Bootstrap__AdministratorDisplayName = 'Quản trị viên QA'
        $env:Bootstrap__AdministratorPassword = "Qa!A7-$([Guid]::NewGuid().ToString('N'))"
        $env:QA_ADMIN_USERNAME = $env:Bootstrap__AdministratorUserName
        $env:QA_ADMIN_PASSWORD = $env:Bootstrap__AdministratorPassword
    }
    $currentPhase = 'health'
    $failureContext.command = 'dotnet MyFSchool.Api.dll'
    $failureContext.scenario = 'health'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'api-startup-failed'
    $failureContext.routeOrStep = 'GET /health startup'
    Start-ApiProcess -Port 5080 -ConfiguredStorageRoot $storageRoot -LogName 'ready' -ApplicationConnectionString $applicationConnectionString
    $failureContext.command = 'npm run smoke'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'health-contract-failed'
    $failureContext.routeOrStep = 'GET /health'
    & (Join-Path $PSScriptRoot 'run-smoke.ps1') -ApiOrigin 'http://127.0.0.1:5080' -ThrowOnFailure
    $scenarioResults.health = 'passed'

    $currentPhase = 'api-contract'
    $failureContext.command = 'npm run core-contract'
    $failureContext.scenario = 'api-contract'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'api-contract-failed'
    $failureContext.routeOrStep = 'security headers, OpenAPI, ProblemDetails'
    & (Join-Path $PSScriptRoot 'run-smoke.ps1') -ApiOrigin 'http://127.0.0.1:5080' -Scenario 'core-contract' -ThrowOnFailure
    $scenarioResults.apiContract = 'passed'

    if ($IncludeIdentity) {
        $currentPhase = 'identity-auth'
        $failureContext.command = 'npm run identity-auth'
        $failureContext.scenario = 'identity-auth'
        $failureContext.exitCodeOrTimeout = 'not-applicable'
        $failureContext.stableError = 'identity-auth-contract-failed'
        $failureContext.routeOrStep = 'sign-in, session, provision, refresh rotation/reuse, logout'
        & (Join-Path $PSScriptRoot 'run-smoke.ps1') -ApiOrigin 'http://127.0.0.1:5080' -Scenario 'identity-auth' -ThrowOnFailure
        $scenarioResults.identityAuth = 'passed'
        $currentPhase = 'password-assistance'
        $failureContext.command = 'npm run password-assistance'
        $failureContext.scenario = 'password-assistance'
        $failureContext.exitCodeOrTimeout = 'not-applicable'
        $failureContext.stableError = 'password-assistance-contract-failed'
        $failureContext.routeOrStep = 'generic request, Admin decision, temporary password, forced change, revocation'
        Start-ApiProcess -Port 5082 -ConfiguredStorageRoot $storageRoot -LogName 'password-assistance' -ApplicationConnectionString $applicationConnectionString
        & (Join-Path $PSScriptRoot 'run-smoke.ps1') -ApiOrigin 'http://127.0.0.1:5082' -Scenario 'password-assistance' -ThrowOnFailure
        $scenarioResults.passwordAssistance = 'passed'
        $currentPhase = 'identity-persistence'
        $failureContext.command = 'Assert-IdentityPersistence'
        $failureContext.scenario = 'identity-persistence'
        $failureContext.exitCodeOrTimeout = 'not-applicable'
        $failureContext.stableError = 'identity-persistence-invalid'
        $failureContext.routeOrStep = 'users, roles, audits, hashed refresh tokens, restricted user'
        Assert-IdentityPersistence -ApplicationConnectionString $applicationConnectionString
        $scenarioResults.identityPersistence = 'passed'
    }

    $currentPhase = 'ready'
    $failureContext.command = 'Invoke-WebRequest'
    $failureContext.scenario = 'ready'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'ready-request-failed'
    $failureContext.routeOrStep = 'GET /ready'
    try {
        $readyHttpResponse = Invoke-WebRequest -Uri 'http://127.0.0.1:5080/ready' -UseBasicParsing -SkipHttpErrorCheck -TimeoutSec 5
    }
    catch {
        $failureContext.exitCodeOrTimeout = if (Test-TimeoutFailure -ErrorRecord $_) { 'timeout:5s' } else { 'request-error' }
        throw
    }
    $failureContext.exitCodeOrTimeout = "http-status:$([int]$readyHttpResponse.StatusCode)"
    if ([int]$readyHttpResponse.StatusCode -ne 200) {
        $failureContext.stableError = 'ready-http-status'
        throw 'Expected HTTP 200 for readiness.'
    }
    $failureContext.stableError = 'ready-payload-invalid'
    $readyResponse = $readyHttpResponse.Content | ConvertFrom-Json
    $components = @($readyResponse.components)
    $databaseComponents = @($components | Where-Object name -eq 'database')
    $storageComponents = @($components | Where-Object name -eq 'storage')
    if ($readyResponse.status -ne 'ready' -or $components.Count -ne 2 -or
        $databaseComponents.Count -ne 1 -or $databaseComponents[0].status -ne 'ready' -or
        $storageComponents.Count -ne 1 -or $storageComponents[0].status -ne 'ready') {
        throw 'Expected exactly one ready database and storage component.'
    }
    $scenarioResults.ready = 'passed'

    $currentPhase = 'missing-storage'
    $failureContext.command = 'dotnet MyFSchool.Api.dll'
    $failureContext.scenario = 'missing-storage'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'api-startup-failed'
    $failureContext.routeOrStep = 'GET /health startup on port 5081'
    Start-ApiProcess -Port 5081 -ConfiguredStorageRoot $missingStorageRoot -LogName 'not-ready' -ApplicationConnectionString $applicationConnectionString
    $failureContext.command = 'Invoke-WebRequest'
    $failureContext.exitCodeOrTimeout = 'not-applicable'
    $failureContext.stableError = 'missing-storage-request-failed'
    $failureContext.routeOrStep = 'GET /ready expected 503'
    try {
        $missingStorageResponse = Invoke-WebRequest -Uri 'http://127.0.0.1:5081/ready' -UseBasicParsing -SkipHttpErrorCheck -TimeoutSec 5
    }
    catch {
        $failureContext.exitCodeOrTimeout = if (Test-TimeoutFailure -ErrorRecord $_) { 'timeout:5s' } else { 'request-error' }
        throw
    }
    $failureContext.exitCodeOrTimeout = "http-status:$([int]$missingStorageResponse.StatusCode)"
    if ([int]$missingStorageResponse.StatusCode -ne 503) {
        $failureContext.stableError = 'missing-storage-http-status'
        throw 'Expected HTTP 503 for missing storage.'
    }
    $failureContext.stableError = 'missing-storage-payload-invalid'
    $payload = $missingStorageResponse.Content | ConvertFrom-Json
    $failureComponents = @($payload.components)
    $failureDatabase = @($failureComponents | Where-Object name -eq 'database')
    $failureStorage = @($failureComponents | Where-Object name -eq 'storage')
    if ($payload.status -ne 'notReady' -or $failureComponents.Count -ne 2 -or
        $failureDatabase.Count -ne 1 -or $failureDatabase[0].status -ne 'ready' -or
        $failureStorage.Count -ne 1 -or $failureStorage[0].status -ne 'notReady') {
        throw 'Readiness did not identify missing storage safely.'
    }
    $scenarioResults.missingStorage = 'passed'

    $currentPhase = 'completed'
    Write-Host '[QA] backend-core scenarios passed.'
}
catch {
    $runFailure = $_
    if ($_.Exception.Data -and $_.Exception.Data.Contains('QaOutcome')) {
        $failureContext.exitCodeOrTimeout = [string]$_.Exception.Data['QaOutcome']
    }
}
finally {
    $currentPhaseAtFailure = $currentPhase
    Stop-OwnedProcesses

    if ($databaseCreated) {
        try {
            Assert-SafeRunIdentity
            Invoke-AdminSql -CommandText "ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]"
            $databaseDropped = $true
        }
        catch { $cleanupFailures.Add("Failed to drop owned QA database $databaseName.") }
    }

    if ($runFailure -or $cleanupFailures.Count -gt 0) {
        try { Save-FailureEvidence }
        catch { $cleanupFailures.Add('Failed to retain redacted QA failure evidence.') }
    }

    if ($runIdentityValidated) {
        try {
            Assert-SafeRunIdentity
            if (Test-Path -LiteralPath $runRoot) { Remove-Item -LiteralPath $runRoot -Recurse -Force }
            $runRootRemoved = -not (Test-Path -LiteralPath $runRoot)
        }
        catch { $cleanupFailures.Add('Failed to remove owned QA temporary directory.') }
    }

    try {
        foreach ($name in $managedEnvironmentNames) {
            if ($null -eq $previousEnvironment[$name]) {
                Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
            }
            else {
                [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
            }
        }
        Remove-ImportedEnvironment -ImportedNames $importedEnvironmentNames
        $environmentRestored = $true
    }
    catch { $cleanupFailures.Add('Failed to restore caller environment.') }

    $duration = (Get-Date) - $startedAt
    $status = if ($runFailure -or $cleanupFailures.Count -gt 0) { 'failed' } else { 'passed' }
    $currentPhase = $currentPhaseAtFailure
    try { Write-ResultManifest -Status $status }
    catch { $cleanupFailures.Add('Failed to write the QA result manifest.') }
    Write-Host "[QA] backend-core $status; teardown attempted for all resources in $([math]::Round($duration.TotalSeconds, 2))s"
}

if ($cleanupFailures.Count -gt 0) { throw ($cleanupFailures -join ' ') }
if ($runFailure) { throw "Backend-core QA failed: $($runFailure.Exception.Message)" }
