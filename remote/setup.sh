#!/bin/bash
set -e

sudo apt update
sudo apt install -y ca-certificates curl

sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
sudo bash -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable" > /etc/apt/sources.list.d/docker.list'

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

/etc/loggle/install-certbot.sh

sudo docker compose -f /etc/loggle/docker-compose.yml --project-name loggle up -d

/etc/loggle/wait-es.sh

sudo apt update
sudo apt install -y wget
wget https://github.com/PowerShell/PowerShell/releases/download/v7.5.0/powershell_7.5.0-1.deb_amd64.deb
sudo dpkg -i powershell_7.5.0-1.deb_amd64.deb
sudo apt install -f
sudo rm powershell_7.5.0-1.deb_amd64.deb

es_output=$(pwsh /etc/loggle/es-init/batch-indexmanagement.ps1)
echo "$es_output"

# sudo systemctl daemon-reload
sudo systemctl enable loggle.service