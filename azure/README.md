# Loggle Azure Template

This folder holds two Bicep templates that mirror the Terraform stack under `terraform/azure`. Both templates package everything under `remote/` and execute `setup.sh` through the VM Custom Script Extension so the provisioning behavior matches Terraform.

- `loggle.bicep` – resource-group scoped template. Use this when the resource group already exists (same scope as the original Terraform deployment).
- `loggle-subscription.bicep` – subscription-scoped wrapper that first creates the resource group and then invokes `loggle.bicep`. This is the easiest artifact to distribute because it provisions “start to finish” in a single upload.

## 1. Refresh the packaged remote assets

Whenever a file inside `remote/` (e.g., `setup.sh`, `docker-compose.yml`, `kibana.yml`, PowerShell helpers, or the `init-es` assets) changes, regenerate the tarball that gets embedded inside the template:

```pwsh
tar -czf azure/loggle-remote.tar.gz -C remote .
```

The archive is a straight copy of the `remote/` directory. Edit the files in-place under `remote/`, test them as needed, and then re-run the `tar` command above so the latest bits are bundled. Both Bicep templates load `loggle-remote.tar.gz` via `loadFileAsBase64(...)`, so rebuilding the tarball is required before recompiling either template.

## 2. Compile Bicep -> ARM JSON

Install the Bicep CLI once (`az bicep install` or `winget install --id Microsoft.Bicep`) and then run:

```pwsh
# Resource-group scope version (expects target RG to exist)
bicep build azure/loggle.bicep --outfile azure/loggle.json

# Subscription-scope wrapper (creates the RG + workload)
bicep build azure/loggle-subscription.bicep --outfile azure/loggle-subscription.json
```

This produces an Azure Resource Manager template (`loggle.json`) that you can distribute to consumers. The template exposes the following key parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `namePrefix` | Short prefix applied to every resource (affects VM, NIC, NSG, etc.). | `loggle` |
| `location` | Region for all resources. | Current RG location |
| `vmSize` | VM SKU. | `Standard_D2s_v3` |
| `adminUsername` | SSH admin user. | `loggle` |
| `sshPublicKey` | **Required** OpenSSH public key. | *(none)* |
| `domainName` | Hostname served by the stack and used for TLS. | `kibana.loggle.co` |
| `certificateEmail` | Let's Encrypt contact email. | `certbot@loggle.co` |
| `kibanaAllowedIps` | Array of CIDR ranges allowed through the NSG for HTTP/S. | `["34.126.86.243"]` |
| `extraTags` | Additional resource tags merged with `{ workload = "loggle" }`. | `{}` |
| `resourceNames` | Object that overrides auto-generated names (keys: `virtualNetwork`, `subnet`, `networkSecurityGroup`, `publicIp`, `networkInterface`, `virtualMachine`, `userAssignedIdentity`, `keyVault`, `osDisk`). | `{}` |
| `createResourceGroup` (subscription template only) | `true` to create the RG, `false` to target an existing RG. | `true` |

## 3. Azure Portal deployment workflow

**Resource-group scoped (`loggle.json`)**
- Make sure the target resource group already exists.
- In the portal, go to **Create a resource** -> search for **Template deployment (deploy using custom templates)**.
- Keep the scope set to the existing resource group, choose **Build your own template in the editor**, load `azure/loggle.json`, then fill in the parameters (the SSH public key is mandatory).

**Subscription scoped (`loggle-subscription.json`)**
- In the same **Template deployment** experience, switch the scope selector to your subscription (not a specific RG).
- Load `azure/loggle-subscription.json`. This template prompts for the resource-group name/location plus a `createResourceGroup` flag. Keep it `true` to provision a new RG in-line, or set it to `false` to reuse an existing group (in which case the template reads the RG's location automatically).

In both cases the deployment outputs the VM public IP, the managed identity client ID, and the Key Vault resource ID. Share whichever ARM JSON fits your scenario (most consumers will prefer the subscription-scoped template since it provisions everything end-to-end).

### Naming flexibility

By default, every resource name is derived from the `namePrefix` parameter (e.g., `loggle-vnet`, `loggle-nsg`). If you set `namePrefix` to an empty string, the template falls back to simple names like `vnet` and `nsg`. For complete control, supply the `resourceNames` object with your preferred naming convention:

```jsonc
"resourceNames": {
  "virtualNetwork": "corp-core-vnet",
  "networkSecurityGroup": "corp-loggle-nsg",
  "publicIp": "corp-loggle-pip",
  "virtualMachine": "corp-loggle-vm",
  "keyVault": "corplogglekv001"
}
```

Any keys you omit continue to use the prefix-based defaults.
