Set-Location "C:\Users\RobertHyndman\OneDrive - Altara Limited\Customers\NZ Blood\Projects\Directed Transfer Blazor\"
$publishPath = Join-Path (Get-Location) "publish"
if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}
dotnet publish .\NZBlood.DirectedTransfer.Blazor.csproj -c Release -o .\publish
$logsPath = Join-Path $publishPath "logs"
New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
$webConfigPath = Join-Path $publishPath "web.config"
$webConfig = Get-Content -Raw -LiteralPath $webConfigPath
$webConfig = $webConfig.Replace('stdoutLogEnabled="false"', 'stdoutLogEnabled="true"')
Set-Content -LiteralPath $webConfigPath -Value $webConfig
