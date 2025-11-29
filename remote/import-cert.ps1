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
    [string]$KeyVaultName,
    [string]$CertificateName = "kibana",
    [string]$Domain = "kibana.example.com",
    [string]$FullchainPath = "/etc/letsencrypt/live/$Domain/fullchain.pem",
    [string]$PrivkeyPath = "/etc/letsencrypt/live/$Domain/privkey.pem",
    [string]$TempPfxPath = "/etc/loggle/certs/kv-import-kibana.pfx",
    [string]$ManagedIdentityClientId,
    [string]$InfraEnvPath = "/etc/loggle/infra.env"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

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
                Install-Module -Name $module.Name -RequiredVersion $module.RequiredVersion -AllowClobber -Scope AllUsers -Force
            }
            else {
                Write-Output "Module $($module.Name) version $($module.RequiredVersion) is already installed."
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

function Get-ManagedIdentityClientId {
    try {
        $headers = @{ Metadata = 'true' }
        $metadataUri = 'http://169.254.169.254/metadata/instance?api-version=2021-02-01'
        $response = Invoke-RestMethod -Method Get -Uri $metadataUri -Headers $headers -ErrorAction Stop

        $userAssigned = $response.compute.identity.userAssignedIdentities
        if ($null -ne $userAssigned) {
            $firstIdentity = $userAssigned.PSObject.Properties | Select-Object -First 1
            if ($null -ne $firstIdentity) {
                return $firstIdentity.Value.clientId
            }
        }

        if ($response.compute.identity.type -like '*SystemAssigned*' -and $null -ne $response.compute.identity.clientId) {
            return $response.compute.identity.clientId
        }
    }
    catch {
        Write-Output "WARN: Unable to query Azure Instance Metadata Service for managed identity: $_"
    }

    return $null
}

function Get-InfraEnvValue {
    param(
        [Parameter(Mandatory)]
        [string]$Key,
        [string]$Path = "/etc/loggle/infra.env"
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        return $null
    }

    try {
        foreach ($line in Get-Content -Path $Path) {
            if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
                continue
            }

            $parts = $line.Split('=', 2)
            if ($parts.Count -eq 2 -and $parts[0].Trim() -eq $Key) {
                return $parts[1].Trim()
            }
        }
    }
    catch {
        Write-Output "WARN: Unable to read infra environment file ${Path}: $_"
    }

    return $null
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
    
    if (-not $ManagedIdentityClientId) {
        $ManagedIdentityClientId = $env:LOGGLE_MANAGED_IDENTITY_CLIENT_ID
    }

    if (-not $ManagedIdentityClientId) {
        $ManagedIdentityClientId = Get-InfraEnvValue -Key "LOGGLE_MANAGED_IDENTITY_CLIENT_ID" -Path $InfraEnvPath
    }

    if (-not $ManagedIdentityClientId) {
        $ManagedIdentityClientId = Get-ManagedIdentityClientId
    }

    if (-not $KeyVaultName) {
        $KeyVaultName = Get-InfraEnvValue -Key "LOGGLE_KEY_VAULT_NAME" -Path $InfraEnvPath
    }

    if (-not $KeyVaultName) {
        Write-Output "ERROR: Key Vault name not provided. Pass -KeyVaultName or ensure LOGGLE_KEY_VAULT_NAME is set."
        exit 1
    }

    if ($ManagedIdentityClientId) {
        Write-Output "Connecting to Azure using managed identity $ManagedIdentityClientId."
        Connect-AzAccount -Identity -AccountId $ManagedIdentityClientId
    }
    else {
        Write-Output "Connecting to Azure using default managed identity context."
        Connect-AzAccount -Identity
    }

    try {
        $existingCert = Get-AzKeyVaultCertificate -VaultName $KeyVaultName -Name $CertificateName -ErrorAction SilentlyContinue
        if ($existingCert) {
            Write-Output "Removing existing certificate $CertificateName from Key Vault to store fresh version."
            Remove-AzKeyVaultCertificate -VaultName $KeyVaultName -Name $CertificateName -Force
        }
    }
    catch {
        Write-Output "WARN: Failed to remove existing certificate (it may not exist): $_"
    }

    # Connect and import certificate
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
