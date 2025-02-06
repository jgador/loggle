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

sudo mv /tmp/docker-compose.yml /etc/loggle/docker-compose.yml
sudo mv /tmp/otel-collector-config.yaml /etc/loggle/otel-collector-config.yaml

sudo docker compose -f /etc/loggle/docker-compose.yml --project-name loggle up -d

sudo tee /etc/systemd/system/loggle-docker-compose.service > /dev/null << 'EOF'
[Unit]
Description=Start Docker Compose for Loggle
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
ExecStart=/usr/bin/docker compose -f /etc/loggle/docker-compose.yml --project-name loggle up -d
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable loggle-docker-compose.service