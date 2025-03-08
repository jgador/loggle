<#
.SYNOPSIS
    Imports SSL/TLS certificates into Azure Key Vault.
.DESCRIPTION
    Converts PEM certificates to PFX format and imports them into Azure Key Vault.
.NOTES
    File Name      : import-cert.ps1
    Prerequisite   : PowerShell 7+, OpenSSL
#>

[CmdletBinding()]
param (
    [string]$KeyVaultName = "kv-loggle",
    [string]$CertificateName = "kibana",
    [string]$Domain = "kibana.loggle.co",
    [string]$FullchainPath = "/etc/letsencrypt/live/$Domain/fullchain.pem",
    [string]$PrivkeyPath = "/etc/letsencrypt/live/$Domain/privkey.pem",
    [string]$TempPfxPath = "/etc/loggle/certs/kv-import-kibana.pfx"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Initialize-AzureModules {
    [CmdletBinding()]
    param (
        [hashtable[]]$RequiredModules = @(
            @{ Name = 'Az.KeyVault'; RequiredVersion = '6.3.1' },
            @{ Name = 'Az.Accounts'; RequiredVersion = '4.0.2' }
        )
    )
    
    try {
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
        
        foreach ($module in $RequiredModules) {
            $existingModule = Get-Module -ListAvailable -Name $module.Name | 
                Where-Object { $_.Version -eq $module.RequiredVersion }
            
            if (-not $existingModule) {
                Write-Output "Installing module $($module.Name) version $($module.RequiredVersion)..."
                Install-Module -Name $module.Name -RequiredVersion $module.Version -AllowClobber -Scope AllUsers -Force
            }
            else {
                Write-Output "Module $($module.Name) version $($module.Version) is already installed."
            }
        }
        
        # Import the modules
        foreach ($module in $RequiredModules) {
            Import-Module $module.Name -ErrorAction Stop
        }
    }
    catch {
        Write-Output "ERROR: Failed to initialize Azure modules: $_"
        throw
    }
}

# Check certificate files first
if (-not (Test-Path $FullchainPath) -or -not (Test-Path $PrivkeyPath)) {
    Write-Output "Certificate files not found. Skipping import."
    exit 0
}

try {
    # Convert PEM to PFX
    & openssl pkcs12 -export `
        -in $FullchainPath `
        -inkey $PrivkeyPath `
        -out $TempPfxPath `
        -passout pass:

    if ($LASTEXITCODE -ne 0) {
        Write-Output "Failed to convert certificate to PFX format."
        exit 1
    }

    # Initialize Azure modules
    Initialize-AzureModules
    
    # Connect and import certificate
    Connect-AzAccount -Identity
    Import-AzKeyVaultCertificate -VaultName $KeyVaultName -Name $CertificateName -FilePath $TempPfxPath
    
    Write-Output "Certificate successfully imported to Key Vault."
}
catch {
    Write-Output "ERROR: $_"
    exit 1
}
finally {
    # Cleanup temporary PFX file
    if (Test-Path $TempPfxPath) {
        Remove-Item -Path $TempPfxPath -Force
    }
}