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
}

# Virtual Network
resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  address_space       = ["10.0.0.0/16"]
}

# Subnet for VM
resource "azurerm_subnet" "subnet" {
  name                 = "default"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Static Public IP
resource "azurerm_public_ip" "public_ip" {
  name                = "ip-loggle"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  allocation_method   = "Static"
  sku                 = "Basic"
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
    name                       = "Elasticsearch"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "9200"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
  security_rule {
    name                       = "Kibana"
    priority                   = 120
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "5601"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
  security_rule {
    name                       = "HTTPS"
    priority                   = 130
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "443"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
  security_rule {
    name                       = "HTTP"
    priority                   = 140
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "80"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
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
}

resource "azurerm_network_interface_security_group_association" "nsg_assoc" {
  network_interface_id      = azurerm_network_interface.nic.id
  network_security_group_id = azurerm_network_security_group.nsg.id
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
    disable_password_authentication = false
  }
  connection {
    type     = "ssh"
    user     = "loggle"
    password = "L0gg|3K3y"
    host     = azurerm_public_ip.public_ip.ip_address
  }
  provisioner "file" {
    source = "../../docker/docker-compose.yml"
    destination = "/tmp/docker-compose.yml"
  }
  provisioner "file" {
    source = "../../docker/otel-collector-config.yaml"
    destination = "/tmp/otel-collector-config.yaml"
  }
  provisioner "remote-exec" {
    inline = [
      "sudo apt update",
      "sudo apt install -y ca-certificates curl",
      "sudo install -m 0755 -d /etc/apt/keyrings",
      "sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc",
      "sudo chmod a+r /etc/apt/keyrings/docker.asc",
      "sudo echo \"deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"$${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable\" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null",
      "sudo apt update",
      "sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin",
      "echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf",
      "sudo sysctl -p",
      "sudo mkdir -p /etc/loggle",
      "sudo mv /tmp/docker-compose.yml /etc/loggle/docker-compose.yml",
      "sudo mv /tmp/otel-collector-config.yaml /etc/loggle/otel-collector-config.yaml",
      "sudo docker compose -f /etc/loggle/docker-compose.yml --project-name loggle up -d"
    ]
  }
}

output "vm_public_ip" {
  value = azurerm_public_ip.public_ip.ip_address
}