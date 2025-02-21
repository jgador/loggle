terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.0.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "rg-loggle"
  location = "Southeast Asia"
  lifecycle {
    prevent_destroy = true
  }
}

# Virtual Network
resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = ["10.0.0.0/16"]
  lifecycle {
    prevent_destroy = true
  }
}

# Subnet for VM
resource "azurerm_subnet" "subnet" {
  name                 = "default"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.1.0/24"]
  lifecycle {
    prevent_destroy = true
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
    destination_port_ranges    = [80, 443, 8080, 9200, 5601, 4318]
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
  lifecycle {
    prevent_destroy = true
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
    prevent_destroy = true
  }
}

resource "azurerm_network_interface_security_group_association" "nsg_assoc" {
  network_interface_id      = azurerm_network_interface.nic.id
  network_security_group_id = azurerm_network_security_group.nsg.id
  lifecycle {
    prevent_destroy = true
  }
}

# Virtual Machine
resource "azurerm_virtual_machine" "vm" {
  name                             = "vm-loggle"
  location                         = azurerm_resource_group.rg.location
  resource_group_name              = azurerm_resource_group.rg.name
  network_interface_ids            = [azurerm_network_interface.nic.id]
  vm_size                          = "Standard_D2s_v3"
  delete_os_disk_on_termination    = true
  delete_data_disks_on_termination = true

  storage_image_reference {
    publisher = "canonical"
    offer     = "0001-com-ubuntu-minimal-jammy"
    sku       = "minimal-22_04-lts-gen2"
    version   = "latest"
  }
  storage_os_disk {
    name              = "disk-loggle"
    caching           = "ReadWrite"
    create_option     = "FromImage"
    managed_disk_type = "Standard_LRS"
  }
  os_profile {
    computer_name  = "vm-loggle"
    admin_username = "loggle"
    admin_password = "L0gg|3K3y"
  }
  os_profile_linux_config {
    disable_password_authentication = true
    ssh_keys {
      path     = "/home/loggle/.ssh/authorized_keys"
      key_data = file("~/.ssh/loggle.pub")
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
      "sudo mkdir -p /etc/loggle/elasticsearch-data",
      "sudo mkdir -p /etc/loggle/kibana-data",
      "sudo mv /tmp/setup.sh /etc/loggle/",
      "sudo chmod +x /etc/loggle/setup.sh",
      "sudo /etc/loggle/setup.sh"
    ]
  }
  lifecycle {
    prevent_destroy = false
  }
}

output "vm_public_ip" {
  value = azurerm_public_ip.public_ip.ip_address
}