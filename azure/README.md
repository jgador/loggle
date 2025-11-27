# Loggle Azure Template

This folder holds the Bicep template that mirrors the Terraform stack under `terraform/azure`. The VM Custom Script extension clones this repository, stages the contents of `azure/vm-assets/` into `/var/cache/loggle-assets/`, downloads `setup.sh` from Azure Storage (via the `setupScriptUrl` parameter), and drops a `loggle-bootstrap.service` unit that runs after `cloud-final.service` to execute the script. Operators can still inspect every asset on-disk before the installer runs.

## 1. Keep the VM assets in sync

`azure/vm-assets/` is a straight copy of the root `remote/` directory (the same payload Terraform uploads with its `file` provisioner). Whenever you touch anything under `remote/`, refresh the Azure copy before committing:

```pwsh
Remove-Item -Path azure/vm-assets -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path azure/vm-assets | Out-Null
Copy-Item -Path remote/* -Destination azure/vm-assets -Recurse -Force
```

This keeps the Azure artifacts readable in-tree and guarantees the deployment bundle always matches the Terraform provisioning logic.

## 2. Compile Bicep -> ARM JSON

Install/refresh the bundled CLI through Azure CLI (`az bicep install`). Then build the ARM template with:

```pwsh
az bicep build --file azure/loggle.bicep --outfile azure/loggle.json
```

This produces an Azure Resource Manager template (`loggle.json`) that you can distribute to consumers. **Prerequisites:** create (or select) the resource group up front and provision a public IP inside that group; the deployment only attaches to an existing IP and will not create one for you. The template exposes the following key parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `namePrefix` | Short prefix applied to every resource (affects VM, NIC, NSG, etc.). | `loggle` |
| `location` | Region for all resources. | Current RG location |
| `vmSize` | VM SKU. | `Standard_D2s_v3` |
| `adminUsername` | SSH admin user. | `loggle` |
| `sshPublicKey` | **Required** OpenSSH public key. | *(none)* |
| `domainName` | Hostname served by the stack and used for TLS. | `kibana.loggle.co` |
| `certificateEmail` | Let's Encrypt contact email. | `certbot@loggle.co` |
| `letsEncryptEnvironment` | Choose `production` for real certs or `staging` when testing repeatedly (avoids rate limits with test certificates). | `production` |
| `kibanaAllowedIps` | Array of CIDR ranges allowed through the NSG for HTTP/S. | `["34.126.86.243"]` |
| `extraTags` | Additional resource tags merged with `{ workload = "loggle" }`. | `{}` |
| `resourceNames` | Object that overrides auto-generated names (keys: `virtualNetwork`, `subnet`, `networkSecurityGroup`, `networkInterface`, `virtualMachine`, `userAssignedIdentity`, `keyVault`, `osDisk`). | `{}` |
| `keyVaultName` | Optional explicit Key Vault name. Leave empty to use the prefix + date pattern. | `""` |
| `repositoryUrl` | Git repository that hosts the `vm-assets` folder. | `https://github.com/jgador/loggle.git` |
| `setupScriptUrl` | Raw HTTPS URL for `setup.sh` (point it at another branch/ref when testing). | `https://raw.githubusercontent.com/jgador/loggle/refs/heads/master/azure/vm-assets/setup.sh` |
| `publicIpName` | **Required** name of the pre-created public IP that already lives in the chosen resource group. The template only attaches to this IP. | *(none)* |

> Purge protection is disabled by default so the Key Vault can be deleted (and purged) during environment teardown. Toggle it manually if your compliance posture requires it.  
> **Important:** The `publicIpName` you provide must reference an existing public IP resource inside the same resource group you deploy to; the template will fail if it cannot find that IP.  
> **Testing tip:** switch `letsEncryptEnvironment` to `staging` while iterating, then back to `production` before go-live.

Key Vault names are deterministic by default: the template lowercases the `namePrefix`, strips dashes, appends `kv`, and then adds the current UTC date suffix (e.g., `loggle` on 2025‑03‑20 becomes `logglekv20250320`). If you prefer a fixed name, set the `keyVaultName` parameter (or `resourceNames.keyVault`) and the template will use it verbatim.

## 3. Azure Portal deployment workflow

**Resource-group scoped (`loggle.json`)**
- In the portal, go to **Create a resource** -> search for **Template deployment (deploy using custom templates)**.
- Select your subscription, then pick an existing resource group or use the **Create new** button in the scope picker. The portal handles both flows so the template itself only needs to target the chosen RG.
- Choose **Build your own template in the editor**, load `azure/loggle.json`, then fill in the parameters (the SSH public key is mandatory).

The deployment outputs the VM public IP, the managed identity client ID, and the Key Vault resource ID.

### Naming flexibility

By default, every resource name is derived from the `namePrefix` parameter (e.g., `loggle-vnet`, `loggle-nsg`). If you set `namePrefix` to an empty string, the template falls back to simple names like `vnet` and `nsg`. For complete control, supply the `resourceNames` object with your preferred naming convention:

```jsonc
"resourceNames": {
  "virtualNetwork": "corp-core-vnet",
  "networkSecurityGroup": "corp-loggle-nsg",
  "virtualMachine": "corp-loggle-vm",
  "keyVault": "corplogglekv001"
}
```

Any keys you omit continue to use the prefix-based defaults.
