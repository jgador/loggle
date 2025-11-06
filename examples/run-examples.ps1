[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Language
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

$languageHandlers = @{
    "csharp" = {
        Require-Command -Name "dotnet"
        $dir = Join-Path $PSScriptRoot "dotnet-otel-logger"
        Invoke-Process -Command "dotnet" -Arguments @("run") -WorkingDirectory $dir
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
        Invoke-Process -Command "python" -Arguments @("main.py") -WorkingDirectory $dir
    }
    "javascript" = {
        Require-Command -Name "npm"
        $dir = Join-Path $PSScriptRoot "javascript-otel-logger"
        Invoke-Process -Command "npm" -Arguments @("install", "--no-fund", "--no-audit", "--legacy-peer-deps") -WorkingDirectory $dir
        Invoke-Process -Command "npm" -Arguments @("run", "start", "--silent") -WorkingDirectory $dir
    }
    "typescript" = {
        Require-Command -Name "npx"
        $dir = Join-Path $PSScriptRoot "typescript-otel-logger"
        Invoke-Process -Command "npm" -Arguments @("install", "--no-fund", "--no-audit", "--legacy-peer-deps") -WorkingDirectory $dir
        Invoke-Process -Command "npm" -Arguments @("run", "start", "--silent") -WorkingDirectory $dir
    }
    "go" = {
        Require-Command -Name "go"
        $dir = Join-Path $PSScriptRoot "go-otel-logger"
        Invoke-Process -Command "go" -Arguments @("run", ".") -WorkingDirectory $dir
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

Write-Host "Continuous mode enabled. Press Ctrl+C to stop."
while ($true) {
    & $execute
}
