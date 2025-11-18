targetScope = 'subscription'

@description('Name of the resource group that will host all Loggle resources.')
param resourceGroupName string = 'rg-loggle'

@description('Set to true to create the resource group; false to reuse an existing one.')
param createResourceGroup bool = true

@description('Azure region for the resource group and workload resources.')
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
param location string = 'southeastasia'

@description('Prefix applied to most Loggle resource names.')
param namePrefix string = 'loggle'

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

@description('IPv4 addresses or CIDR ranges allowed to reach Kibana / HTTPS.')
param kibanaAllowedIps array = [
  '34.126.86.243'
]

@description('Optional tags merged with the default workload tag.')
param extraTags object = {}

@description('Optional explicit names for workload resources; forwarded to loggle.bicep.')
param resourceNames object = {}

@description('Base64-encoded tar.gz produced from the remote/ folder.')
param remoteBundleBase64 string = loadFileAsBase64('loggle-remote.tar.gz')

var mergedTags = union({
  workload: 'loggle'
}, extraTags)

resource workloadRg 'Microsoft.Resources/resourceGroups@2023-07-01' = if (createResourceGroup) {
  name: resourceGroupName
  location: location
  tags: mergedTags
}

resource workloadRgExisting 'Microsoft.Resources/resourceGroups@2023-07-01' existing = {
  name: resourceGroupName
}

var effectiveLocation = createResourceGroup ? location : workloadRgExisting.location

module loggle 'loggle.bicep' = {
  name: 'loggleModule'
  scope: resourceGroup(resourceGroupName)
  params: {
    namePrefix: namePrefix
    location: effectiveLocation
    vmSize: vmSize
    adminUsername: adminUsername
    sshPublicKey: sshPublicKey
    domainName: domainName
    certificateEmail: certificateEmail
    kibanaAllowedIps: kibanaAllowedIps
    extraTags: extraTags
    resourceNames: resourceNames
    remoteBundleBase64: remoteBundleBase64
  }
  dependsOn: createResourceGroup ? [
    workloadRg
  ] : []
}

output resourceGroup string = workloadRg.name
output publicIpAddress string = loggle.outputs.publicIpAddress
output managedIdentityClientId string = loggle.outputs.managedIdentityClientId
output keyVaultResourceId string = loggle.outputs.keyVaultResourceId
