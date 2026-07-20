[CmdletBinding()]
param(
    [string] $Version = (Get-Date).ToString("yy.MM.dd.HHmm")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d{2}\.\d{2}\.\d{2}\.\d{4}$') {
    throw "Version must use yy.MM.dd.HHmm format, for example 26.07.20.1430."
}

$scriptRoot = Split-Path -Parent $PSCommandPath
$projectRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $projectRoot "NZBlood.DirectedTransfer.Blazor.csproj"
$buildInfoPath = Join-Path $projectRoot "BuildInfo.cs"
$publishPath = Join-Path $projectRoot "publish"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

Push-Location $projectRoot
try {
    $buildInfo = [System.IO.File]::ReadAllText($buildInfoPath)
    $updatedBuildInfo = [regex]::Replace(
        $buildInfo,
        'public const string Version = "[^"]*";',
        "public const string Version = `"$Version`";"
    )

    if ($updatedBuildInfo -eq $buildInfo -and $buildInfo -notmatch [regex]::Escape("public const string Version = `"$Version`";")) {
        throw "Could not locate the BuildInfo.Version constant in $buildInfoPath."
    }

    [System.IO.File]::WriteAllText($buildInfoPath, $updatedBuildInfo, $utf8NoBom)
    Write-Host "Publishing Directed Transfer version $Version"

    if (Test-Path -LiteralPath $publishPath) {
        Remove-Item -LiteralPath $publishPath -Recurse -Force
    }

    dotnet publish $projectPath -c Release -o $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    [System.IO.File]::WriteAllText((Join-Path $publishPath "version.txt"), $Version, $utf8NoBom)

    $logsPath = Join-Path $publishPath "logs"
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null

    $webConfigPath = Join-Path $publishPath "web.config"
    $webConfig = [System.IO.File]::ReadAllText($webConfigPath)
    $webConfig = $webConfig.Replace('stdoutLogEnabled="false"', 'stdoutLogEnabled="true"')
    [System.IO.File]::WriteAllText($webConfigPath, $webConfig, $utf8NoBom)

    Write-Host "Published version $Version to $publishPath"
}
finally {
    Pop-Location
}
