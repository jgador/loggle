targetScope = 'resourceGroup'

@description('Prefix applied to resource names (keep short and alphanumeric).')
param namePrefix string = 'loggle'

@description('Azure location / region for all resources in this deployment.')
param location string = resourceGroup().location

@description('VM SKU for the Loggle host.')
param vmSize string = 'Standard_D2s_v3'

@description('Admin username for SSH access.')
param adminUsername string = 'loggle'

@description('SSH public key in OpenSSH format (single line).')
param sshPublicKey string

@description('Public hostname served by the stack (also used for TLS issuance).')
param domainName string = 'kibana.loggle.co'

@description('Contact email used for Let\'s Encrypt requests.')
param certificateEmail string = 'certbot@loggle.co'

@allowed([
  'production'
  'staging'
])
@description('LetsEncrypt environment for certificate issuance. Leave at production for real deployments; switch to staging when repeatedly testing to avoid rate limits.')
param letsEncryptEnvironment string = 'production'

@description('IPv4 addresses or CIDR ranges allowed to reach Kibana / HTTPS. Default 0.0.0.0/0 leaves Kibana open to the entire internet - override this to restrict access.')
param kibanaAllowedIps array = [
  '0.0.0.0/0'
]

@description('Optional tags merged with the default workload tag.')
param extraTags object = {}

@description('Optional explicit names for resources to override prefix-based defaults. Keys: virtualNetwork, subnet, networkSecurityGroup, networkInterface, virtualMachine, userAssignedIdentity, keyVault, osDisk.')
param resourceNames object = {}

@description('IMPORTANT: Name of the pre-existing public IP address that already exists in this resource group. The template only attaches to it; it will not create a new public IP.')
param publicIpName string

@description('Optional explicit Key Vault name. Leave empty to use the default prefix+date naming pattern.')
param keyVaultName string = ''

@description('Git repository that hosts the VM bootstrap assets (setup.sh, docker-compose.yml, etc.).')
param assetRepoUrl string = 'https://github.com/jgador/loggle.git'

@description('Git branch or tag used to download the assetRepoPath contents (normally azure/vm-assets). Defaults to master; change only when testing assets from another ref.')
param assetRepoRef string = 'master'

@description('Path inside the repository that contains the VM bootstrap assets.')
param assetRepoPath string = 'azure/vm-assets'

@metadata({
  description: 'Internal date stamp appended to generated Key Vault names.'
  'x-ms-visibility': 'internal'
})
param keyVaultDateSuffix string = utcNow('yyyyMMdd')

var tags = union({
  workload: 'loggle'
}, extraTags)

var prefixSegment = empty(namePrefix) ? '' : '${namePrefix}-'
var sanitizedNamePrefix = toLower(replace(namePrefix, '-', ''))
var defaultKeyVaultBaseName = empty(sanitizedNamePrefix) ? 'kvstore' : '${sanitizedNamePrefix}kv'
var providedKeyVaultName = toLower(keyVaultName)
var vnetGeneratedName = '${prefixSegment}vnet'
var subnetGeneratedName = 'default'
var nsgGeneratedName = '${prefixSegment}nsg'
var nicGeneratedName = '${prefixSegment}nic'
var vmGeneratedName = '${prefixSegment}vm'
var identityGeneratedName = '${prefixSegment}id'
var keyVaultNameSeed = empty(keyVaultName) ? '${defaultKeyVaultBaseName}${keyVaultDateSuffix}' : providedKeyVaultName
var keyVaultGeneratedName = substring(keyVaultNameSeed, 0, min(24, length(keyVaultNameSeed)))
var osDiskGeneratedName = '${prefixSegment}osdisk'

var vnetName = resourceNames.?virtualNetwork ?? vnetGeneratedName
var subnetName = resourceNames.?subnet ?? subnetGeneratedName
var nsgName = resourceNames.?networkSecurityGroup ?? nsgGeneratedName
var nicName = resourceNames.?networkInterface ?? nicGeneratedName
var vmName = resourceNames.?virtualMachine ?? vmGeneratedName
var identityName = resourceNames.?userAssignedIdentity ?? identityGeneratedName
var keyVaultEffectiveName = resourceNames.?keyVault ?? keyVaultGeneratedName
var osDiskName = resourceNames.?osDisk ?? osDiskGeneratedName
// NOTE: Keeping the original infraEnvCommand around for potential future use.
/*
var infraEnvCommand = replace($'''
bash -c 'set -eo pipefail
LOGGLE_HOME="/etc/loggle"
INFRA_ENV_PATH="$LOGGLE_HOME/infra.env"

install -d -m 0755 "$LOGGLE_HOME"

cat <<'INFRAENV' >"$INFRA_ENV_PATH"
LOGGLE_DOMAIN="${domainName}"
LOGGLE_CERT_EMAIL="${certificateEmail}"
LOGGLE_CERT_ENV="${letsEncryptEnvironment}"
LOGGLE_KEY_VAULT_NAME="${keyVaultEffectiveName}"
LOGGLE_ASSET_REPO_URL="${assetRepoUrl}"
LOGGLE_ASSET_REPO_PATH="${assetRepoPath}"
LOGGLE_ASSET_REPO_REF="${assetRepoRef}"
LOGGLE_MANAGED_IDENTITY_CLIENT_ID="${userAssignedIdentity.properties.clientId}"
INFRAENV

chmod 600 "$INFRA_ENV_PATH"
'
''', '\r', '')
*/

var vmCustomData = base64(replace(format('''
LOGGLE_DOMAIN="{0}"
LOGGLE_CERT_EMAIL="{1}"
LOGGLE_CERT_ENV="{2}"
LOGGLE_KEY_VAULT_NAME="{3}"
LOGGLE_ASSET_REPO_URL="{4}"
LOGGLE_ASSET_REPO_PATH="{5}"
LOGGLE_ASSET_REPO_REF="{6}"
LOGGLE_MANAGED_IDENTITY_CLIENT_ID="{7}"
''', domainName, certificateEmail, letsEncryptEnvironment, keyVaultEffectiveName, assetRepoUrl, assetRepoPath, assetRepoRef, userAssignedIdentity.properties.clientId), '\r', ''))

var cloudFinalBootstrapCommand = replace(format('''
bash -c 'set -eo pipefail
LOGGLE_HOME="/etc/loggle"
BOOTSTRAP_SCRIPT="$LOGGLE_HOME/loggle-bootstrap.sh"
SERVICE_PATH="/etc/systemd/system/loggle-bootstrap.service"

install -d -m 0755 "$LOGGLE_HOME"
install -d -m 0755 "$(dirname "$BOOTSTRAP_SCRIPT")"

cat <<\LOGGLE_BOOTSTRAP >"$BOOTSTRAP_SCRIPT"
#!/bin/bash
set -eo pipefail

LOGGLE_HOME="/etc/loggle"
INFRA_ENV_PATH="$LOGGLE_HOME/infra.env"
CUSTOM_DATA_PATH="/var/lib/cloud/instance/user-data.txt"
SETUP_DEST="$LOGGLE_HOME/setup.sh"
DEFAULT_ASSET_REF="{0}"

copy_custom_data() {{
  install -d -m 0755 "$LOGGLE_HOME"
  if [[ -f "$CUSTOM_DATA_PATH" ]]; then
    cp "$CUSTOM_DATA_PATH" "$INFRA_ENV_PATH"
  else
    echo "Custom data file $CUSTOM_DATA_PATH not found; writing fallback env from template values." >&2
    cat <<\INFRAENV >"$INFRA_ENV_PATH"
LOGGLE_DOMAIN="{1}"
LOGGLE_CERT_EMAIL="{2}"
LOGGLE_CERT_ENV="{3}"
LOGGLE_KEY_VAULT_NAME="{4}"
LOGGLE_ASSET_REPO_URL="{5}"
LOGGLE_ASSET_REPO_PATH="{6}"
LOGGLE_ASSET_REPO_REF="{0}"
LOGGLE_MANAGED_IDENTITY_CLIENT_ID="{7}"
INFRAENV
  fi

  chmod 600 "$INFRA_ENV_PATH"
}}

resolve_asset_ref() {{
  local ref="$DEFAULT_ASSET_REF"
  if [[ -f "$INFRA_ENV_PATH" ]]; then
    # shellcheck disable=SC1091
    source "$INFRA_ENV_PATH"
  fi
  if [[ -n "$LOGGLE_ASSET_REPO_REF" ]]; then
    ref="$LOGGLE_ASSET_REPO_REF"
  fi
  printf "%s" "$ref"
}}

download_setup() {{
  local ref
  ref="$(resolve_asset_ref)"
  local setup_url="https://raw.githubusercontent.com/jgador/loggle/refs/heads/$ref/azure/vm-assets/setup.sh"
  local tmp_file
  tmp_file="$(mktemp)"
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$setup_url" -o "$tmp_file"
  elif command -v wget >/dev/null 2>&1; then
    wget -qO "$tmp_file" "$setup_url"
  else
    apt-get update
    apt-get install -y curl
    curl -fsSL "$setup_url" -o "$tmp_file"
  fi
  mv "$tmp_file" "$SETUP_DEST"
  chmod 755 "$SETUP_DEST"
}}

run_setup() {{
  if [[ ! -x "$SETUP_DEST" ]]; then
    echo "Setup script $SETUP_DEST not found or not executable; skipping launch." >&2
    return
  fi
  if [[ -z "${{HOME:-}}" ]]; then
    export HOME=/root
  fi
  local log_file="$LOGGLE_HOME/setup.log"
  echo "Launching $SETUP_DEST in background; follow logs via $log_file"
  nohup "$SETUP_DEST" >"$log_file" 2>&1 &
}}

main() {{
  copy_custom_data
  download_setup
  run_setup
}}

main "$@"
LOGGLE_BOOTSTRAP

chmod 755 "$BOOTSTRAP_SCRIPT"

cat <<\LOGGLE_SERVICE >"$SERVICE_PATH"
[Unit]
Description=Loggle bootstrap
After=cloud-final.service
Wants=cloud-final.service

[Service]
Type=oneshot
ExecStart=/etc/loggle/loggle-bootstrap.sh
ExecStartPost=/bin/systemctl disable loggle-bootstrap.service
RemainAfterExit=true

[Install]
WantedBy=multi-user.target
LOGGLE_SERVICE

systemctl daemon-reload
systemctl enable --now loggle-bootstrap.service
'
''', assetRepoRef, domainName, certificateEmail, letsEncryptEnvironment, keyVaultEffectiveName, assetRepoUrl, assetRepoPath, userAssignedIdentity.properties.clientId), '\r', '')
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultEffectiveName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    accessPolicies: []
  }
}

resource kvCertificatesRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, 'cert-officer')
  scope: keyVault
  properties: {
    principalId: userAssignedIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a4417e6f-fecd-4de8-b567-7b0420556985')
    principalType: 'ServicePrincipal'
  }
}

resource kvSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, 'secret-user')
  scope: keyVault
  properties: {
    principalId: userAssignedIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-02-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
  }
}

resource subnet 'Microsoft.Network/virtualNetworks/subnets@2023-02-01' = {
  name: subnetName
  parent: virtualNetwork
  properties: {
    addressPrefix: '10.0.1.0/24'
  }
}

resource networkSecurityGroup 'Microsoft.Network/networkSecurityGroups@2023-02-01' = {
  name: nsgName
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'SSH'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'Loggle'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRanges: [
            '80'
            '443'
            '4318'
          ]
          sourceAddressPrefixes: [for ip in kibanaAllowedIps: ip]
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource networkInterface 'Microsoft.Network/networkInterfaces@2023-02-01' = {
  name: nicName
  location: location
  tags: tags
  properties: {
    networkSecurityGroup: {
      id: networkSecurityGroup.id
    }
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: subnet.id
          }
          publicIPAddress: {
            id: resourceId('Microsoft.Network/publicIPAddresses', publicIpName)
          }
        }
      }
    ]
  }
}

resource virtualMachine 'Microsoft.Compute/virtualMachines@2023-09-01' = {
  name: vmName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: vmName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
      customData: vmCustomData
    }
    storageProfile: {
      imageReference: {
        publisher: 'canonical'
        offer: '0001-com-ubuntu-minimal-jammy'
        sku: 'minimal-22_04-lts-gen2'
        version: 'latest'
      }
      osDisk: {
        name: osDiskName
        createOption: 'FromImage'
        caching: 'ReadWrite'
        deleteOption: 'Delete'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterface.id
        }
      ]
    }
  }
}

resource bootstrapExtension 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = {
  name: 'loggleBootstrap'
  parent: virtualMachine
  location: location
  properties: {
    publisher: 'Microsoft.Azure.Extensions'
    type: 'CustomScript'
    typeHandlerVersion: '2.1'
    autoUpgradeMinorVersion: true
    protectedSettings: {
      commandToExecute: cloudFinalBootstrapCommand
    }
    settings: {}
  }
}

output publicIpAddress string = reference(resourceId('Microsoft.Network/publicIPAddresses', publicIpName), '2023-02-01').ipAddress
output managedIdentityClientId string = userAssignedIdentity.properties.clientId
output keyVaultResourceId string = keyVault.id
