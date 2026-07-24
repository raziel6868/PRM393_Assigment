[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiProject = Join-Path $repositoryRoot 'backend\src\MyFSchool.Api\MyFSchool.Api.csproj'
$apiRoot = Split-Path -Parent $apiProject
$apiAssembly = Join-Path $repositoryRoot "backend\src\MyFSchool.Api\bin\$Configuration\net10.0\MyFSchool.Api.dll"
$webRoot = Join-Path $repositoryRoot 'frontend-web'
$viteEntryPoint = Join-Path $webRoot 'node_modules\vite\bin\vite.js'
$logRoot = Join-Path $repositoryRoot 'qa\.logs'
$apiErrorLog = Join-Path $logRoot 'demo-backend.err.log'
$importedEnvironmentNames = [System.Collections.Generic.List[string]]::new()
$ownedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
. (Join-Path $PSScriptRoot 'env.ps1')

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)

    if ($null -eq (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Không tìm thấy '$Name' trong PATH."
    }
}

function Assert-Setting {
    param([Parameter(Mandatory)][string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name, 'Process')
    if ([string]::IsNullOrWhiteSpace($value) -or $value.StartsWith('CHANGE_ME')) {
        throw "Thiếu cấu hình hợp lệ '$Name' trong file .env ở thư mục gốc."
    }
}

function Assert-PortAvailable {
    param([Parameter(Mandatory)][int]$Port)

    $listener = [System.Net.Sockets.TcpListener]::new(
        [System.Net.IPAddress]::Loopback,
        $Port
    )
    try {
        $listener.Start()
    }
    catch {
        throw "Cổng $Port đang được sử dụng. Hãy dừng process cũ trước khi chạy demo."
    }
    finally {
        $listener.Stop()
    }
}

function Wait-HttpReady {
    param(
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [string]$ErrorLogPath,
        [int]$TimeoutSeconds = 60
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) {
            $detail = if ($ErrorLogPath -and (Test-Path -LiteralPath $ErrorLogPath -PathType Leaf)) {
                (Get-Content -LiteralPath $ErrorLogPath -Tail 12) -join [Environment]::NewLine
            }
            else {
                'Không có stderr log.'
            }
            throw "$Name đã dừng trước khi sẵn sàng (exit code $($Process.ExitCode)).`n$detail"
        }

        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "$Name không sẵn sàng sau $TimeoutSeconds giây."
}

function Stop-OwnedProcess {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) { return }

    try {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id
            if (-not $Process.WaitForExit(5000)) {
                Stop-Process -Id $Process.Id -Force
            }
        }
    }
    catch {
        Write-Warning "Không thể dừng process $($Process.Id): $($_.Exception.Message)"
    }
}

try {
    Import-RootEnvironment -RepositoryRoot $repositoryRoot -ImportedNames $importedEnvironmentNames

    foreach ($settingName in @(
        'VITE_API_BASE_URL',
        'ASPNETCORE_URLS',
        'ConnectionStrings__Default',
        'Auth__JwtSigningKey',
        'Storage__Provider',
        'Storage__LocalRoot'
    )) {
        Assert-Setting -Name $settingName
    }

    Assert-Command -Name 'dotnet'
    Assert-Command -Name 'node'

    if (-not (Test-Path -LiteralPath $viteEntryPoint -PathType Leaf)) {
        throw "Thiếu package Web. Chạy 'npm ci --prefix frontend-web' một lần rồi thử lại."
    }

    $apiBinding = [Environment]::GetEnvironmentVariable('ASPNETCORE_URLS', 'Process').Split(';')[0]
    $apiUri = [Uri]$apiBinding
    $apiPort = $apiUri.Port
    $apiProbeHost = if ($apiUri.Host -in @('0.0.0.0', '+', '*')) { '127.0.0.1' } else { $apiUri.Host }
    $apiOrigin = '{0}://{1}:{2}' -f $apiUri.Scheme, $apiProbeHost, $apiPort
    $webOrigin = 'http://127.0.0.1:5173'

    Assert-PortAvailable -Port $apiPort
    Assert-PortAvailable -Port 5173

    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
    Write-Host "Đang build Backend ($Configuration)..."
    & dotnet build $apiProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Backend build thất bại với exit code $LASTEXITCODE."
    }
    if (-not (Test-Path -LiteralPath $apiAssembly -PathType Leaf)) {
        throw 'Không tìm thấy Backend assembly sau khi build.'
    }

    $apiProcess = Start-Process -FilePath 'dotnet' `
        -ArgumentList @($apiAssembly) `
        -WorkingDirectory $apiRoot `
        -RedirectStandardOutput (Join-Path $logRoot 'demo-backend.out.log') `
        -RedirectStandardError $apiErrorLog `
        -WindowStyle Hidden `
        -PassThru
    $ownedProcesses.Add($apiProcess)

    $webProcess = Start-Process -FilePath 'node' `
        -ArgumentList @($viteEntryPoint, '--host', '127.0.0.1', '--port', '5173', '--strictPort') `
        -WorkingDirectory $webRoot `
        -RedirectStandardOutput (Join-Path $logRoot 'demo-web.out.log') `
        -RedirectStandardError (Join-Path $logRoot 'demo-web.err.log') `
        -WindowStyle Hidden `
        -PassThru
    $ownedProcesses.Add($webProcess)

    Write-Host 'Đang chờ Backend và Web sẵn sàng...'
    Wait-HttpReady -Url "$apiOrigin/health" -Name 'Backend' -Process $apiProcess -ErrorLogPath $apiErrorLog
    Wait-HttpReady -Url $webOrigin -Name 'Frontend Web' -Process $webProcess

    Write-Host ''
    Write-Host 'MyFSchool đã sẵn sàng:'
    Write-Host "  Backend: $apiOrigin"
    Write-Host "  Web:     $webOrigin"
    Write-Host "  Log:     $logRoot"

    if ($CheckOnly) {
        Write-Host 'Kiểm tra khởi động thành công; đang dừng các process thử nghiệm.'
        return
    }

    Write-Host 'Nhấn Ctrl+C để dừng cả Backend và Web.'
    while ($true) {
        Start-Sleep -Seconds 1
        foreach ($process in $ownedProcesses) {
            $process.Refresh()
            if ($process.HasExited) {
                throw "Process $($process.Id) đã dừng ngoài dự kiến (exit code $($process.ExitCode))."
            }
        }
    }
}
finally {
    foreach ($process in $ownedProcesses) {
        Stop-OwnedProcess -Process $process
    }
    Remove-ImportedEnvironment -ImportedNames $importedEnvironmentNames
}
