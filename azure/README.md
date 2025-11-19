# Loggle Azure Template

This folder holds the Bicep template that mirrors the Terraform stack under `terraform/azure`. The template packages everything under `remote/` and executes `setup.sh` through the VM Custom Script Extension so the provisioning behavior matches Terraform.

## 1. Refresh the packaged remote assets

Whenever a file inside `remote/` (e.g., `setup.sh`, `docker-compose.yml`, `kibana.yml`, PowerShell helpers, or the `init-es` assets) changes, regenerate the tarball that the VM downloads during provisioning:

```pwsh
tar -czf azure/loggle-remote.tar.gz -C remote .
```

The archive is a straight copy of the `remote/` directory. Edit the files in-place under `remote/`, test them as needed, and then re-run the `tar` command above so the latest bits are bundled. Publish the tarball somewhere reachable (the default value uses the raw GitHub URL for `azure/loggle-remote.tar.gz`). Whenever you regenerate the archive, make sure the hosted copy is refreshed too, otherwise new deployments will keep downloading the stale bits.

## 2. Compile Bicep -> ARM JSON

Install/refresh the bundled CLI through Azure CLI (`az bicep install`). Then build the ARM template with:

```pwsh
az bicep build --file azure/loggle.bicep --outfile azure/loggle.json
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
| `remoteBundleUrl` | HTTPS URL pointing at the `loggle-remote.tar.gz` archive that `setup.sh` expects. | Raw GitHub URL for this repo/branch |

> Key Vault purge protection is always enabled in this template to satisfy Azure's irreversible requirement: once a vault has purge protection, redeployments must continue to request `enablePurgeProtection: true`.

Key Vault names are deterministic: the template lowercases the `namePrefix`, strips dashes, and appends `kv` (for example, `loggle` becomes `logglekv`). Provide `resourceNames.keyVault` if you need a different value or if your prefix would cause a collision.

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
  "publicIp": "corp-loggle-pip",
  "virtualMachine": "corp-loggle-vm",
  "keyVault": "corplogglekv001"
}
```

Any keys you omit continue to use the prefix-based defaults.
