[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$ApplicationArguments
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiProject = Join-Path $repositoryRoot 'backend\src\MyFSchool.Api\MyFSchool.Api.csproj'
$importedEnvironmentNames = [System.Collections.Generic.List[string]]::new()
$exitCode = 0
. (Join-Path $PSScriptRoot 'env.ps1')

try {
    Import-RootEnvironment -RepositoryRoot $repositoryRoot -ImportedNames $importedEnvironmentNames
    Push-Location $repositoryRoot
    try {
        dotnet run --project $apiProject --configuration $Configuration --no-launch-profile -- @ApplicationArguments
        $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
}
finally {
    Remove-ImportedEnvironment -ImportedNames $importedEnvironmentNames
}

if ($exitCode -ne 0) { exit $exitCode }
