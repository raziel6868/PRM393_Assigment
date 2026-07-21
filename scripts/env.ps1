function Import-RootEnvironment {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [System.Collections.Generic.List[string]]$ImportedNames
    )

    $environmentPath = Join-Path $RepositoryRoot '.env'
    if (-not (Test-Path -LiteralPath $environmentPath -PathType Leaf)) {
        throw 'Missing repository-root .env (copy .env.example and provide local values).'
    }

    foreach ($rawLine in Get-Content -LiteralPath $environmentPath) {
        $line = $rawLine.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { continue }

        $separatorIndex = $line.IndexOf('=')
        if ($separatorIndex -le 0) {
            throw 'The repository-root .env contains an invalid entry.'
        }

        $name = $line.Substring(0, $separatorIndex).Trim()
        $value = $line.Substring($separatorIndex + 1).Trim()
        if ($name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
            throw 'The repository-root .env contains an invalid setting name.'
        }

        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        if ($null -eq [Environment]::GetEnvironmentVariable($name, 'Process')) {
            [Environment]::SetEnvironmentVariable($name, $value, 'Process')
            if ($null -ne $ImportedNames) { $ImportedNames.Add($name) }
        }
    }
}

function Remove-ImportedEnvironment {
    param([Parameter(Mandatory)][System.Collections.Generic.List[string]]$ImportedNames)

    foreach ($name in $ImportedNames | Select-Object -Unique) {
        Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
    }
}
