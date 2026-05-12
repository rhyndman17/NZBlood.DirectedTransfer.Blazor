[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $RemoteUrl = "https://github.com/rhyndman17/NZBlood.DirectedTransfer.Blazor.git",

    [string] $RemoteName = "NZBlood.DirectedTransfer.Blazor",

    [string] $Branch = "main",

    [string] $Message = "_2026.5.1",

    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$projectRoot = Split-Path -Parent $scriptRoot
$repoRoot = $projectRoot

function Assert-GitAvailable {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw "Git was not found on PATH. Run this script from a Git-enabled terminal, or install Git for Windows."
    }
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)] [string[]] $Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & git @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Get-GitOutput {
    param([Parameter(Mandatory = $true)] [string[]] $Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }

        return $output
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Test-GitWorkTree {
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git rev-parse --is-inside-work-tree 2>$null
        return ($LASTEXITCODE -eq 0 -and $output -eq "true")
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

Push-Location $repoRoot
try {
    Assert-GitAvailable

    if (-not (Test-GitWorkTree)) {
        if ($PSCmdlet.ShouldProcess($repoRoot, "Initialize Git repository")) {
            Invoke-Git @("init")
        }
        else {
            return
        }
    }

    $currentBranch = (& git branch --show-current 2>$null)
    if ([string]::IsNullOrWhiteSpace($currentBranch)) {
        if ($PSCmdlet.ShouldProcess($repoRoot, "Create or switch to branch $Branch")) {
            Invoke-Git @("checkout", "-B", $Branch)
        }
    }
    elseif ($currentBranch -ne $Branch) {
        if ($PSCmdlet.ShouldProcess($repoRoot, "Switch from $currentBranch to $Branch")) {
            Invoke-Git @("checkout", "-B", $Branch)
        }
    }

    $remoteNames = @(Get-GitOutput @("remote"))
    if ($remoteNames -notcontains $RemoteName) {
        if ([string]::IsNullOrWhiteSpace($RemoteUrl)) {
            throw "Remote '$RemoteName' is not configured. Rerun with -RemoteUrl https://github.com/<owner>/<repo>.git"
        }

        if ($PSCmdlet.ShouldProcess($repoRoot, "Add remote $RemoteName")) {
            Invoke-Git @("remote", "add", $RemoteName, $RemoteUrl)
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($RemoteUrl)) {
        if ($PSCmdlet.ShouldProcess($repoRoot, "Update remote $RemoteName URL")) {
            Invoke-Git @("remote", "set-url", $RemoteName, $RemoteUrl)
        }
    }

    if (-not $SkipBuild) {
        if ($PSCmdlet.ShouldProcess($projectRoot, "Build Blazor project")) {
            Push-Location $projectRoot
            try {
                dotnet build "NZBlood.DirectedTransfer.Blazor.csproj" -o ".verify-build"
                if ($LASTEXITCODE -ne 0) {
                    throw "dotnet build failed with exit code $LASTEXITCODE."
                }
            }
            finally {
                Pop-Location
            }
        }
    }

    $status = @(Get-GitOutput @("status", "--short"))
    if ($status.Count -eq 0) {
        Write-Host "No changes to commit."
    }
    else {
        if ($PSCmdlet.ShouldProcess($repoRoot, "Stage changes")) {
            Invoke-Git @("add", "--all")
        }

        if ($PSCmdlet.ShouldProcess($repoRoot, "Create commit")) {
            Invoke-Git @("commit", "-m", $Message)
        }
    }

    if ($PSCmdlet.ShouldProcess("$RemoteName/$Branch", "Push branch")) {
        Invoke-Git @("push", "-u", $RemoteName, $Branch)
    }
}
finally {
    Pop-Location
}
