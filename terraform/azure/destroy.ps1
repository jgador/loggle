<#
.SYNOPSIS
    Destroys all Terraform-managed Azure resources except the protected resource group and public IP.
.DESCRIPTION
    Lists the Terraform state, removes the protected resources (resource group and static public IP)
    from the target list, and runs `terraform destroy` with the resulting targets.
    Run this script from the same directory that holds your Terraform configuration (terraform/azure).
.PARAMETER AutoApprove
    Pass -AutoApprove to forward the flag to terraform destroy and skip the confirmation prompt.
#>

[CmdletBinding()]
param (
    [bool]$AutoApprove = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Terraform {
    param (
        [string[]]$Arguments
    )

    & terraform @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command 'terraform $($Arguments -join ' ')'' failed with exit code $LASTEXITCODE."
    }
}

$protectedResources = @(
    'azurerm_resource_group.rg',
    'azurerm_public_ip.public_ip',
    'azurerm_key_vault.kv'
)

Write-Host 'Building target list from Terraform state...' -ForegroundColor Cyan
$stateResources = & terraform state list 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error $stateResources
    throw 'Unable to obtain Terraform state. Run this script after `terraform init` and ensure state exists.'
}

$targets = $stateResources |
    Where-Object { $_ -and ($_ -notin $protectedResources) -and ($_ -notlike 'data.*') } |
    ForEach-Object { "-target=$_" }

if (-not $targets) {
    Write-Host 'No resources to destroy (state only contains protected resources).' -ForegroundColor Yellow
    exit 0
}

Write-Host 'Destroying targeted resources while keeping protected infrastructure...' -ForegroundColor Cyan
$destroyArgs = @('destroy') + $targets
if ($AutoApprove) {
    $destroyArgs += '-auto-approve'
}

Invoke-Terraform -Arguments $destroyArgs
Write-Host 'Destroy operation completed. Protected resources remained in place.' -ForegroundColor Green
