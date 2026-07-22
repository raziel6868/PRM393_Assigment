[CmdletBinding()]
param([Parameter(Mandatory)][string]$UserId)

$ErrorActionPreference = 'Stop'
$parsedUserId = [Guid]::Empty
if (-not [Guid]::TryParse($UserId, [ref]$parsedUserId)) {
    throw 'Expiry fixture requires a valid user ID.'
}
$connectionString = [Environment]::GetEnvironmentVariable('ConnectionStrings__Default', 'Process')
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw 'Expiry fixture requires the run-scoped application connection.'
}
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ($builder.InitialCatalog -notmatch '^MyFSchool_QA_[0-9]{14}_[a-f0-9]{8}$') {
    throw 'Expiry fixture refused a non-QA database.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = @'
UPDATE Users
SET TemporaryPasswordExpiresAtUtc = DATEADD(minute, -1, SYSUTCDATETIME()),
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @UserId AND MustChangePassword = 1;
'@
        $null = $command.Parameters.Add('@UserId', [System.Data.SqlDbType]::UniqueIdentifier)
        $command.Parameters['@UserId'].Value = $parsedUserId
        if ($command.ExecuteNonQuery() -ne 1) {
            throw 'Expiry fixture expected exactly one restricted synthetic account.'
        }
    }
    finally { $command.Dispose() }
}
finally { $connection.Dispose() }
