targetScope = 'resourceGroup'

@description('Prefix applied to resource names (keep short and alphanumeric). Used to derive the default VM name.')
param namePrefix string = 'loggle'

@description('Azure location / region for the VM extension deployment.')
param location string = resourceGroup().location

@description('Optional explicit VM name. Leave empty to use the default prefix-based value.')
param virtualMachineName string = ''

@description('Git repository that hosts the VM bootstrap assets (setup.sh, docker-compose.yml, etc.).')
param assetRepoUrl string = 'https://github.com/jgador/loggle.git'

@description('Git branch or tag used to download the assetRepoPath contents (normally azure/vm-assets). Defaults to master; change only when testing assets from another ref.')
param assetRepoRef string = 'master'

@description('Path inside the repository that contains the VM bootstrap assets.')
param assetRepoPath string = 'azure/vm-assets'

@description('HTTPS endpoint that hosts setup.sh (fetched onto the VM before execution).')
param setupScriptUrl string = 'https://stloggleprod.blob.${environment().suffixes.storage}/download/setup.sh'

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

@description('Client ID of the user-assigned managed identity created by the infrastructure deployment.')
param managedIdentityClientId string

@description('Name of the Key Vault created by the infrastructure deployment.')
param keyVaultName string

var prefixSegment = empty(namePrefix) ? '' : '${namePrefix}-'
var defaultVmName = '${prefixSegment}vm'
var vmName = empty(virtualMachineName) ? defaultVmName : virtualMachineName

var commandToExecute = format('''
bash -c 'set -euo pipefail
SETUP_URL="{0}"
REPO_URL="{1}"
REPO_REF="{2}"
REPO_PATH="{3}"
SRC_DIR="/tmp/loggle-src"

rm -rf "$SRC_DIR"
rm -f /tmp/setup.sh /tmp/docker-compose.yml /tmp/otel-collector-config.yaml /tmp/kibana.yml /tmp/import-cert.ps1 /tmp/export-cert.ps1 /tmp/loggle.service
rm -rf /tmp/init-es

if ! command -v git >/dev/null 2>&1 || ! command -v curl >/dev/null 2>&1; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -y
  apt-get install -y git curl
fi

git clone --depth 1 --filter=blob:none --branch "$REPO_REF" "$REPO_URL" "$SRC_DIR"
if [ -n "$REPO_PATH" ] && [ "$REPO_PATH" != "." ]; then
  git -C "$SRC_DIR" sparse-checkout init --cone
  git -C "$SRC_DIR" sparse-checkout set "$REPO_PATH"
else
  REPO_PATH="."
fi
ASSETS_DIR="$SRC_DIR/$REPO_PATH"
if [ ! -d "$ASSETS_DIR" ]; then
  echo "Assets path $REPO_PATH not found in repository $REPO_URL"
  exit 1
fi
shopt -s dotglob
cp -R "$ASSETS_DIR"/* /tmp/
shopt -u dotglob
rm -rf "$SRC_DIR"

curl -fsSL "$SETUP_URL" -o /tmp/setup.sh
chmod +x /tmp/setup.sh

export LOGGLE_DOMAIN="{4}"
export LOGGLE_CERT_EMAIL="{5}"
export LOGGLE_MANAGED_IDENTITY_CLIENT_ID="{6}"
export LOGGLE_CERT_ENV="{7}"
export LOGGLE_KEY_VAULT_NAME="{8}"

/tmp/setup.sh
'
''', setupScriptUrl, assetRepoUrl, assetRepoRef, assetRepoPath, domainName, certificateEmail, managedIdentityClientId, letsEncryptEnvironment, keyVaultName)

resource virtualMachine 'Microsoft.Compute/virtualMachines@2023-09-01' existing = {
  name: vmName
}

resource bootstrapExtension 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = {
  name: 'loggleProvisioning'
  parent: virtualMachine
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
}
