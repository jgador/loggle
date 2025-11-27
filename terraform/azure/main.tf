terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.117.1"
    }
  }
}

data "azurerm_client_config" "client" {}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = false
    }
  }
}

# Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "rg-loggle"
  location = "Southeast Asia"
  lifecycle {
    prevent_destroy = true
  }
}

# Managed Identity
resource "azurerm_user_assigned_identity" "auth_id" {
  name                = "id-loggle"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
}

# Key Vault
resource "azurerm_key_vault" "kv" {
  name                       = "kv-loggle"
  resource_group_name        = azurerm_resource_group.rg.name
  location                   = azurerm_resource_group.rg.location
  tenant_id                  = data.azurerm_client_config.client.tenant_id
  soft_delete_retention_days = 7
  purge_protection_enabled   = false
  sku_name                   = "standard"
  enable_rbac_authorization  = true
  lifecycle {
    prevent_destroy = true
  }
}

# Set RBAC for Key Vault
resource "azurerm_role_assignment" "auth_kv_cert" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Certificates Officer"
  principal_id         = azurerm_user_assigned_identity.auth_id.principal_id
}
resource "azurerm_role_assignment" "auth_kv_secret" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.auth_id.principal_id
}

# Virtual Network
resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = ["10.0.0.0/16"]
  lifecycle {
    prevent_destroy = false
  }
}

# Subnet for VM
resource "azurerm_subnet" "subnet" {
  name                 = "default"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.1.0/24"]
  lifecycle {
    prevent_destroy = false
  }
}

# Static Public IP
resource "azurerm_public_ip" "public_ip" {
  name                = "ip-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  allocation_method   = "Static"
  sku                 = "Basic"
  lifecycle {
    prevent_destroy = true
  }
}

# Network Security Group
resource "azurerm_network_security_group" "nsg" {
  name                = "nsg-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  security_rule {
    name                       = "SSH"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
  security_rule {
    name                       = "Loggle"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    source_address_prefixes    = var.kibana_allowed_ips
    destination_port_ranges    = [80, 443, 4318]
    destination_address_prefix = "*"
  }
  lifecycle {
    prevent_destroy = false
  }
}

# Network Interface with NSG
resource "azurerm_network_interface" "nic" {
  name                = "nic-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.subnet.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.public_ip.id # Associate static public IP
  }
  lifecycle {
    prevent_destroy = false
  }
}

resource "azurerm_network_interface_security_group_association" "nsg_assoc" {
  network_interface_id      = azurerm_network_interface.nic.id
  network_security_group_id = azurerm_network_security_group.nsg.id
  lifecycle {
    prevent_destroy = false
  }
}

# Virtual Machine
resource "azurerm_virtual_machine" "vm" {
  name                             = "vm-loggle"
  location                         = azurerm_resource_group.rg.location
  resource_group_name              = azurerm_resource_group.rg.name
  network_interface_ids            = [azurerm_network_interface.nic.id]
  vm_size                          = var.vm_size
  delete_os_disk_on_termination    = true
  delete_data_disks_on_termination = true

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.auth_id.id]
  }
  storage_image_reference {
    publisher = "canonical"
    offer     = "0001-com-ubuntu-minimal-jammy"
    sku       = "minimal-22_04-lts-gen2"
    version   = "latest"
  }
  os_profile {
    computer_name  = "vm-loggle"
    admin_username = "loggle"
  }
  storage_os_disk {
    name              = "disk-loggle"
    caching           = "ReadWrite"
    create_option     = "FromImage"
    managed_disk_type = "Standard_LRS"
  }
  os_profile_linux_config {
    disable_password_authentication = true
    ssh_keys {
      path     = var.ssh_key_path
      key_data = file(var.ssh_key_data)
    }
  }
  connection {
    type        = "ssh"
    host        = azurerm_public_ip.public_ip.ip_address
    user        = "loggle"
    private_key = file("~/.ssh/loggle")
  }
  provisioner "file" {
    source      = "../../remote/"
    destination = "/tmp"
  }
  provisioner "remote-exec" {
    inline = [
      "sudo mkdir -p /etc/loggle",
      "sudo mkdir -p /etc/loggle/certs",
      "sudo mkdir -p /etc/loggle/elasticsearch-data",
      "sudo mkdir -p /etc/loggle/kibana-data",
      "sudo mv /tmp/install.sh /etc/loggle/",
      "sudo chmod +x /etc/loggle/install.sh",
      "sudo LOGGLE_MANAGED_IDENTITY_CLIENT_ID=${azurerm_user_assigned_identity.auth_id.client_id} /etc/loggle/install.sh"
    ]
  }
  depends_on = [azurerm_user_assigned_identity.auth_id, azurerm_key_vault.kv]
  lifecycle {
    prevent_destroy = false
  }
}

output "vm_public_ip" {
  value = azurerm_public_ip.public_ip.ip_address
}
