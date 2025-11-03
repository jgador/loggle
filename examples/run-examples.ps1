[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Language,
    [string]$OtlpEndpoint = "http://localhost:4318/v1/logs",
    [string]$BearerToken = "REPLACE_WITH_YOUR_OWN_SECRET",
    [string]$ServiceVersion = "0.1.0",
    [string]$Environment = "sample",
    [switch]$Continuous
)

function Require-Command {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Command '$Name' is required but was not found on PATH."
    }
}

function Invoke-Process {
    param(
        [Parameter(Mandatory)]
        [string]$Command,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory = $PSScriptRoot
    )

    Write-Verbose ("`n> {0} {1}" -f $Command, ($Arguments -join ' '))

    $resolvedCommand = Get-Command $Command -ErrorAction SilentlyContinue
    if (-not $resolvedCommand) {
        throw "Command '$Command' not found. Install it or adjust PATH."
    }

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $resolvedCommand.Path
    foreach ($arg in $Arguments) {
        [void]$psi.ArgumentList.Add($arg)
    }
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false

    $process = [System.Diagnostics.Process]::Start($psi)
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw "Command '$Command' exited with code $($process.ExitCode)."
    }
}

function With-Environment {
    param(
        [Parameter(Mandatory)]
        [hashtable]$Values,
        [Parameter(Mandatory)]
        [ScriptBlock]$Script
    )

    $backup = @{}
    foreach ($key in $Values.Keys) {
        $backup[$key] = [System.Environment]::GetEnvironmentVariable($key, "Process")
        $newValue = $Values[$key]
        if ($null -eq $newValue -or ($newValue -is [string] -and [string]::IsNullOrWhiteSpace($newValue))) {
            Remove-Item -Path ("Env:" + $key) -ErrorAction SilentlyContinue
        }
        else {
            [System.Environment]::SetEnvironmentVariable($key, [string]$newValue)
        }
    }

    try {
        & $Script
    }
    finally {
        foreach ($key in $backup.Keys) {
            $original = $backup[$key]
            if ([string]::IsNullOrEmpty($original)) {
                Remove-Item -Path ("Env:" + $key) -ErrorAction SilentlyContinue
            }
            else {
                [System.Environment]::SetEnvironmentVariable($key, $original)
            }
        }
    }
}

$envValues = @{
    "LOGGLE_OTLP_ENDPOINT" = $OtlpEndpoint
    "LOGGLE_BEARER_TOKEN"  = $BearerToken
    "LOGGLE_SERVICE_VERSION" = $ServiceVersion
    "LOGGLE_ENVIRONMENT" = $Environment
}

function New-EnvBlock {
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName
    )

    $clone = [hashtable]::new()
    foreach ($key in $envValues.Keys) {
        $clone[$key] = $envValues[$key]
    }
    $clone["LOGGLE_SERVICE_NAME"] = $ServiceName
    return $clone
}

$languageHandlers = @{
    "csharp" = {
        Require-Command -Name "dotnet"
        $dir = Join-Path $PSScriptRoot "Examples.Loggle.Console"
        With-Environment (New-EnvBlock -ServiceName "loggle-dotnet-example") {
            Invoke-Process -Command "dotnet" -Arguments @("run") -WorkingDirectory $dir
        }
    }
    "python" = {
        Require-Command -Name "python"
        $dir = Join-Path $PSScriptRoot "python-otel-logger"
        try {
            Invoke-Process -Command "python" -Arguments @("-m", "pip", "--version") -WorkingDirectory $dir
        }
        catch {
            Write-Verbose "pip not detected; attempting to bootstrap via ensurepip."
            Invoke-Process -Command "python" -Arguments @("-m", "ensurepip", "--upgrade") -WorkingDirectory $dir
        }
        Invoke-Process -Command "python" -Arguments @("-m", "pip", "install", "-q", "-r", "requirements.txt") -WorkingDirectory $dir
        With-Environment (New-EnvBlock -ServiceName "loggle-python-example") {
            Invoke-Process -Command "python" -Arguments @("main.py") -WorkingDirectory $dir
        }
    }
    "javascript" = {
        Require-Command -Name "npm"
        $dir = Join-Path $PSScriptRoot "javascript-otel-logger"
        Invoke-Process -Command "npm" -Arguments @("install", "--no-fund", "--no-audit", "--legacy-peer-deps") -WorkingDirectory $dir
        With-Environment (New-EnvBlock -ServiceName "loggle-javascript-example") {
            Invoke-Process -Command "npm" -Arguments @("run", "start", "--silent") -WorkingDirectory $dir
        }
    }
    "typescript" = {
        Require-Command -Name "npx"
        $dir = Join-Path $PSScriptRoot "typescript-otel-logger"
        Invoke-Process -Command "npm" -Arguments @("install", "--no-fund", "--no-audit", "--legacy-peer-deps") -WorkingDirectory $dir
        With-Environment (New-EnvBlock -ServiceName "loggle-typescript-example") {
            Invoke-Process -Command "npm" -Arguments @("run", "start", "--silent") -WorkingDirectory $dir
        }
    }
    "go" = {
        Require-Command -Name "go"
        $dir = Join-Path $PSScriptRoot "go-otel-logger"
        With-Environment (New-EnvBlock -ServiceName "loggle-go-example") {
            Invoke-Process -Command "go" -Arguments @("run", ".") -WorkingDirectory $dir
        }
    }
}

$execute = {
    $key = $Language.ToLowerInvariant()
    if (-not $languageHandlers.ContainsKey($key)) {
        throw "Unsupported language '$Language'. Valid options: $($languageHandlers.Keys -join ', ')"
    }

    Write-Host "=== Running $key example ==="
    & $languageHandlers[$key]
}

if ($Continuous) {
    Write-Host "Continuous mode enabled. Press Ctrl+C to stop."
    while ($true) {
        & $execute
    }
}
else {
    & $execute
}
