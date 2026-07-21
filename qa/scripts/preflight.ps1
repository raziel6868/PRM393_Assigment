[CmdletBinding()]
param(
    [ValidateSet('Backend', 'Web', 'Flutter')]
    [string[]]$Products = @('Backend'),
    [switch]$RequireEmail
)

$ErrorActionPreference = 'Stop'
$startedAt = Get-Date
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$failures = [System.Collections.Generic.List[string]]::new()
$importedEnvironmentNames = [System.Collections.Generic.List[string]]::new()
. (Join-Path $PSScriptRoot 'env.ps1')

function Test-Command {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        $failures.Add("Missing required command: $Name")
    }
}

Write-Host "[QA] preflight started at $($startedAt.ToString('o')) for $($Products -join ', ')"

try {
    Import-RootEnvironment -RepositoryRoot $repositoryRoot -ImportedNames $importedEnvironmentNames
}
catch {
    $failures.Add($_.Exception.Message)
}

if ($Products -contains 'Backend') {
    Test-Command -Name 'dotnet'
    Test-Command -Name 'node'
    Test-Command -Name 'npm'

    $dotnetVersion = if (Get-Command dotnet -ErrorAction SilentlyContinue) { dotnet --version } else { $null }
    if ($dotnetVersion -and -not $dotnetVersion.StartsWith('10.')) {
        $failures.Add(".NET 10 SDK is required; found $dotnetVersion")
    }

    if ([string]::IsNullOrWhiteSpace($env:QA_SQLSERVER_ADMIN_CONNECTION)) {
        $failures.Add('Missing required setting: QA_SQLSERVER_ADMIN_CONNECTION')
    }
    else {
        try {
            $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($env:QA_SQLSERVER_ADMIN_CONNECTION)
            $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
            try {
                $connection.Open()
                $command = $connection.CreateCommand()
                $command.CommandText = 'SELECT 1'
                $command.CommandTimeout = 5
                [void]$command.ExecuteNonQuery()
                $command.Dispose()
            }
            finally {
                $connection.Dispose()
            }
        }
        catch {
            $failures.Add('SQL Server administrator connectivity check failed.')
        }
    }
}

if ($Products -contains 'Web') {
    Test-Command -Name 'node'
    Test-Command -Name 'npm'
}

if ($Products -contains 'Flutter') {
    Test-Command -Name 'flutter'
    Test-Command -Name 'adb'
    Test-Command -Name 'maestro'
}

if ($RequireEmail) {
    foreach ($settingName in @('Smtp__UserName', 'Smtp__Password', 'QA_GMAIL_RECIPIENT')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($settingName, 'Process'))) {
            $failures.Add("Missing required email QA setting: $settingName")
        }
    }
}

$duration = (Get-Date) - $startedAt
if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    Remove-ImportedEnvironment -ImportedNames $importedEnvironmentNames
    Write-Host "[QA] preflight failed in $([math]::Round($duration.TotalSeconds, 2))s"
    exit 1
}

Remove-ImportedEnvironment -ImportedNames $importedEnvironmentNames
Write-Host "[QA] preflight passed in $([math]::Round($duration.TotalSeconds, 2))s"
