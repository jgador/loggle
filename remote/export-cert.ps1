<#
.SYNOPSIS
    Exports SSL/TLS certificates from Azure Key Vault and converts to PEM format.
.DESCRIPTION
    Retrieves a certificate from Azure Key Vault and converts it to PEM format 
    (both fullchain and private key) using OpenSSL.
.NOTES
    File Name      : export-cert.ps1
    Prerequisite   : PowerShell 7+, OpenSSL, Azure CLI
#>

[CmdletBinding()]
param (
    [string]$KeyVaultName = "kv-loggle",
    [string]$CertificateName = "kibana",
    [string]$CertPath = "/etc/loggle/certs",
    [string]$PfxPath = "$CertPath/kv-export-kibana.pfx",
    [string]$FullchainPath = "$CertPath/fullchain.pem",
    [string]$PrivkeyPath = "$CertPath/privkey.pem",
    [string]$ManagedIdentityClientId
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
                Install-Module @module -AllowClobber -Scope AllUsers -Force
            }
        }
        
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

function Export-CertificateFromKeyVault {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$VaultName,
        [Parameter(Mandatory)]
        [string]$CertName,
        [Parameter(Mandatory)]
        [string]$OutputPath,
        [string]$ManagedIdentityClientId
    )
    
    try {
        if ($ManagedIdentityClientId) {
            Write-Output "Connecting to Azure using managed identity $ManagedIdentityClientId."
            Connect-AzAccount -Identity -AccountId $ManagedIdentityClientId
        }
        else {
            Write-Output "Connecting to Azure using default managed identity context."
            Connect-AzAccount -Identity
        }

        $certBase64 = Get-AzKeyVaultSecret -VaultName $VaultName -Name $CertName -AsPlainText

        if ([string]::IsNullOrEmpty($certBase64)) {
            Write-Output "Certificate not found in Key Vault"
            return $false
        }

        $certBytes = [Convert]::FromBase64String($certBase64)
        Set-Content -Path $OutputPath -Value $certBytes -AsByteStream
        return $true
    }
    catch {
        Write-Output "ERROR: Failed to export certificate from Key Vault: $_"
        return $false
    }
}

function Convert-PfxToPem {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$PfxFile,
        [Parameter(Mandatory)]
        [string]$FullchainOutput,
        [Parameter(Mandatory)]
        [string]$PrivkeyOutput
    )
    
    try {
        # Extract public certificate
        & openssl pkcs12 -in $PfxFile -clcerts -nokeys -out $FullchainOutput -passin pass:
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to extract public certificate"
        }

        # Extract private key
        & openssl pkcs12 -in $PfxFile -nocerts -nodes -out $PrivkeyOutput -passin pass:
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to extract private key"
        }

        # Set permissions
        & chmod 750 $FullchainOutput
        & chmod 750 $PrivkeyOutput

        return $true
    }
    catch {
        Write-Output "ERROR: Failed to convert PFX to PEM: $_"
        return $false
    }
}

# Main execution
try {
    Initialize-AzureModules
    
    if (-not $ManagedIdentityClientId) {
        $ManagedIdentityClientId = $env:LOGGLE_MANAGED_IDENTITY_CLIENT_ID
    }

    if (-not $ManagedIdentityClientId) {
        $ManagedIdentityClientId = Get-ManagedIdentityClientId
    }

    if (-not (Export-CertificateFromKeyVault -VaultName $KeyVaultName -CertName $CertificateName -OutputPath $PfxPath -ManagedIdentityClientId $ManagedIdentityClientId)) {
        exit 1
    }
    
    if (-not (Convert-PfxToPem -PfxFile $PfxPath -FullchainOutput $FullchainPath -PrivkeyOutput $PrivkeyPath)) {
        exit 1
    }
    
    Write-Output "Certificate successfully exported and converted to PEM format"
}
catch {
    Write-Output "ERROR: Script execution failed: $_"
    exit 1
}
finally {
    if (Test-Path $PfxPath) {
        Remove-Item -Path $PfxPath -Force
    }
}
