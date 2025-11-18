targetScope = 'resourceGroup'

@description('Prefix applied to resource names (keep short and alphanumeric).')
param namePrefix string = 'loggle'

@description('Azure location / region for all resources in this deployment.')
@allowed([
  'asia'
  'asiapacific'
  'australia'
  'australiacentral'
  'australiacentral2'
  'australiaeast'
  'australiasoutheast'
  'austriaeast'
  'belgiumcentral'
  'brazil'
  'brazilsouth'
  'brazilsoutheast'
  'canada'
  'canadacentral'
  'canadaeast'
  'centralindia'
  'centralus'
  'centraluseuap'
  'centralusstage'
  'chilecentral'
  'eastasia'
  'eastasiastage'
  'eastus'
  'eastus2'
  'eastus2euap'
  'eastus2stage'
  'eastusstage'
  'eastusstg'
  'europe'
  'france'
  'francecentral'
  'francesouth'
  'germany'
  'germanynorth'
  'germanywestcentral'
  'global'
  'india'
  'indonesia'
  'indonesiacentral'
  'israel'
  'israelcentral'
  'italy'
  'italynorth'
  'japan'
  'japaneast'
  'japanwest'
  'jioindiacentral'
  'jioindiawest'
  'korea'
  'koreacentral'
  'koreasouth'
  'malaysia'
  'malaysiawest'
  'mexico'
  'mexicocentral'
  'newzealand'
  'newzealandnorth'
  'northcentralus'
  'northcentralusstage'
  'northeurope'
  'norway'
  'norwayeast'
  'norwaywest'
  'poland'
  'polandcentral'
  'qatar'
  'qatarcentral'
  'singapore'
  'southafrica'
  'southafricanorth'
  'southafricawest'
  'southcentralus'
  'southcentralusstage'
  'southcentralusstg'
  'southeastasia'
  'southeastasiastage'
  'southindia'
  'spain'
  'spaincentral'
  'sweden'
  'swedencentral'
  'switzerland'
  'switzerlandnorth'
  'switzerlandwest'
  'taiwan'
  'uae'
  'uaecentral'
  'uaenorth'
  'uk'
  'uksouth'
  'ukwest'
  'unitedstates'
  'unitedstateseuap'
  'westcentralus'
  'westeurope'
  'westindia'
  'westus'
  'westus2'
  'westus2stage'
  'westus3'
  'westusstage'
])
param location string = resourceGroup().location

@description('VM SKU for the Loggle host.')
param vmSize string = 'Standard_D2s_v3'

@description('Admin username for SSH access.')
param adminUsername string = 'loggle'

@description('SSH public key in OpenSSH format (single line).')
param sshPublicKey string

@description('Public hostname served by the stack (also used for TLS issuance).')
param domainName string = 'kibana.loggle.co'

@description('Contact email used for Let’s Encrypt requests.')
param certificateEmail string = 'certbot@loggle.co'

@description('IPv4 addresses or CIDR ranges allowed to reach Kibana / HTTPS. Default 0.0.0.0/0 leaves Kibana open to the entire internet—override this to restrict access.')
param kibanaAllowedIps array = [
  '0.0.0.0/0'
]

@description('Optional tags merged with the default workload tag.')
param extraTags object = {}

@description('Optional explicit names for resources to override prefix-based defaults. Keys: virtualNetwork, subnet, networkSecurityGroup, publicIp, networkInterface, virtualMachine, userAssignedIdentity, keyVault, osDisk.')
param resourceNames object = {}

@description('Base64-encoded tar.gz produced from the remote/ folder.')
param remoteBundleBase64 string = loadFileAsBase64('loggle-remote.tar.gz')

var tags = union({
  workload: 'loggle'
}, extraTags)

var prefixSegment = empty(namePrefix) ? '' : '${namePrefix}-'
var vnetGeneratedName = '${prefixSegment}vnet'
var subnetGeneratedName = 'default'
var publicIpGeneratedName = '${prefixSegment}pip'
var nsgGeneratedName = '${prefixSegment}nsg'
var nicGeneratedName = '${prefixSegment}nic'
var vmGeneratedName = '${prefixSegment}vm'
var identityGeneratedName = '${prefixSegment}id'
var keyVaultUnique = uniqueString(resourceGroup().id, namePrefix)
var keyVaultGeneratedName = toLower(substring(replace('${empty(namePrefix) ? "" : namePrefix}kv${keyVaultUnique}', '-', ''), 0, 24))
var osDiskGeneratedName = '${prefixSegment}osdisk'

var vnetName = contains(resourceNames, 'virtualNetwork') ? resourceNames.virtualNetwork : vnetGeneratedName
var subnetName = contains(resourceNames, 'subnet') ? resourceNames.subnet : subnetGeneratedName
var publicIpName = contains(resourceNames, 'publicIp') ? resourceNames.publicIp : publicIpGeneratedName
var nsgName = contains(resourceNames, 'networkSecurityGroup') ? resourceNames.networkSecurityGroup : nsgGeneratedName
var nicName = contains(resourceNames, 'networkInterface') ? resourceNames.networkInterface : nicGeneratedName
var vmName = contains(resourceNames, 'virtualMachine') ? resourceNames.virtualMachine : vmGeneratedName
var identityName = contains(resourceNames, 'userAssignedIdentity') ? resourceNames.userAssignedIdentity : identityGeneratedName
var keyVaultName = contains(resourceNames, 'keyVault') ? resourceNames.keyVault : keyVaultGeneratedName
var osDiskName = contains(resourceNames, 'osDisk') ? resourceNames.osDisk : osDiskGeneratedName

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

var commandToExecute = '''
bash -c "set -euo pipefail
TMP_DIR=/tmp/loggle-deploy
rm -rf \"$TMP_DIR\"
mkdir -p \"$TMP_DIR\"
cat <<'ARCHIVE' | base64 -d > \"$TMP_DIR/bundle.tgz\"
${remoteBundleBase64}
ARCHIVE
tar -xzf \"$TMP_DIR/bundle.tgz\" -C \"$TMP_DIR\"
chmod +x \"$TMP_DIR/setup.sh\"
export LOGGLE_DOMAIN=\"${domainName}\" LOGGLE_CERT_EMAIL=\"${certificateEmail}\" LOGGLE_MANAGED_IDENTITY_CLIENT_ID=\"${userAssignedIdentity.properties.clientId}\"
\"$TMP_DIR/setup.sh\"
"
'''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
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
  name: '${vnetName}/${subnetName}'
  properties: {
    addressPrefix: '10.0.1.0/24'
  }
  dependsOn: [
    virtualNetwork
  ]
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

resource publicIp 'Microsoft.Network/publicIPAddresses@2023-02-01' = {
  name: publicIpName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
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
            id: publicIp.id
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

resource customScript 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = {
  name: '${virtualMachine.name}/loggleProvisioning'
  location: location
  properties: {
    publisher: 'Microsoft.Azure.Extensions'
    type: 'CustomScript'
    typeHandlerVersion: '2.1'
    autoUpgradeMinorVersion: true
    protectedSettings: {
      commandToExecute: commandToExecute
    }
    settings: {}
  }
  dependsOn: [
    virtualMachine
  ]
}

output publicIpAddress string = publicIp.properties.ipAddress
output managedIdentityClientId string = userAssignedIdentity.properties.clientId
output keyVaultResourceId string = keyVault.id
