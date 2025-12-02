<#
.SYNOPSIS
  Builds the Loggle NuGet package and drops the .nupkg into the local nupkgs folder.

.DESCRIPTION
  This is a thin wrapper around `dotnet pack` that makes sure the correct project,
  configuration, and output folder are used. Supply the version explicitly so you
  control the package identity each time you run it.

.EXAMPLE
  pwsh ./build-nuget.ps1 -Version 1.0.0-rc1

.EXAMPLE
  pwsh ./build-nuget.ps1 -Version 1.0.0-rc1 -Configuration Debug -OutputDirectory artifacts
#>

[CmdletBinding()]
param(
    [string]$Project = 'src/Loggle/Loggle.csproj',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = 'nupkgs',
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        throw 'Path cannot be empty.'
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return (Join-Path -Path $PSScriptRoot -ChildPath $PathValue)
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK (dotnet) is required but was not found on PATH.'
}

$resolvedProjectPath = Resolve-FullPath -PathValue $Project
if (-not (Test-Path -LiteralPath $resolvedProjectPath)) {
    throw "Project file not found at '$resolvedProjectPath'."
}

$resolvedOutputPath = Resolve-FullPath -PathValue $OutputDirectory
if (-not (Test-Path -LiteralPath $resolvedOutputPath)) {
    New-Item -Path $resolvedOutputPath -ItemType Directory -Force | Out-Null
}

$packArgs = @(
    'pack', $resolvedProjectPath,
    '--configuration', $Configuration,
    '--output', $resolvedOutputPath,
    '/p:ContinuousIntegrationBuild=true',
    "/p:Version=$Version"
)

Write-Host "Building NuGet package for '$resolvedProjectPath'..." -ForegroundColor Cyan
Write-Host "Configuration : $Configuration"
Write-Host "Output folder : $resolvedOutputPath"
Write-Host "Version       : $Version"

$dotnetVersion = (& dotnet --version).Trim()
Write-Host "Using dotnet  : $dotnetVersion"

Push-Location -Path $PSScriptRoot
try {
    dotnet @packArgs
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'Done. The package can be pushed manually with:' -ForegroundColor Green
Write-Host "  dotnet nuget push `"$resolvedOutputPath\Loggle.$Version.nupkg`" --source nuget.org --api-key <key>"
