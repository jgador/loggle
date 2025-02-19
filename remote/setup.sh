#!/bin/bash
set -e

# Move configuration and setup files (copied from the repo via Terraform) from /tmp to /etc/loggle
mv /tmp/docker-compose.yml /tmp/otel-collector-config.yaml /tmp/install-certbot.sh /tmp/wait-es.sh /etc/loggle/
mv /tmp/loggle.service /etc/systemd/system/
mv /tmp/es-init /etc/loggle/
chmod +x /etc/loggle/wait-es.sh /etc/loggle/install-certbot.sh
chmod -R a+rw /etc/loggle/elasticsearch-data

export DEBIAN_FRONTEND=noninteractive
export NEEDRESTART_MODE=a
apt-get update && apt-get upgrade -y
apt-get install -y ca-certificates curl

install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
bash -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable" > /etc/apt/sources.list.d/docker.list'
apt-get update

apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf
sysctl -p

# Install Certbot
apt-get update
apt-get install -y python3 python3-venv libaugeas0
python3 -m venv /opt/certbot/
/opt/certbot/bin/pip install --upgrade pip
/opt/certbot/bin/pip install certbot
ln -s /opt/certbot/bin/certbot /usr/bin/certbot
certbot certonly --standalone -d kibana.loggle.co -m certbot@loggle.co --agree-tos --no-eff-email --preferred-challenges=http-01 --staging
# /etc/loggle/install-certbot.sh

systemctl daemon-reload
systemctl enable loggle.service
systemctl start loggle.service

# sudo docker compose -f /etc/loggle/docker-compose.yml --project-name loggle up -d
#/etc/loggle/wait-es.sh

es_ready=false
for es_attempt in {1..50}; do
  if curl --output /dev/null --silent --head --fail http://localhost:9200; then
    es_ready=true
    break
  else
    echo "Waiting for Elasticsearch to be ready"
    sleep 5
  fi
done

if [ "$es_ready" != true ]; then
  echo " Max attempts reached"
  exit 1
else
  echo "Elasticsearch is ready!"
fi

# Install PowerShell
apt-get update
apt-get install -y wget
wget https://github.com/PowerShell/PowerShell/releases/download/v7.5.0/powershell_7.5.0-1.deb_amd64.deb
dpkg -i powershell_7.5.0-1.deb_amd64.deb
apt-get install -f
rm powershell_7.5.0-1.deb_amd64.deb

es_output=$(pwsh /etc/loggle/es-init/batch-indexmanagement.ps1)
echo "$es_output"

# sudo systemctl daemon-reload
# sudo systemctl enable loggle.service

# Install kibana in VM
wget -qO - https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /usr/share/keyrings/elasticsearch-keyring.gpg
apt-get install -y apt-transport-https
echo "deb [signed-by=/usr/share/keyrings/elasticsearch-keyring.gpg] https://artifacts.elastic.co/packages/8.x/apt stable main" | sudo tee /etc/apt/sources.list.d/elastic-8.x.list
apt-get update
apt-get install -y kibana

mv /tmp/kibana.yml /etc/kibana/kibana.yml
sudo addgroup sslcert
sudo adduser kibana sslcert
sudo adduser root sslcert
sudo chgrp -R sslcert /etc/letsencrypt/live
sudo chgrp -R sslcert /etc/letsencrypt/archive
sudo chmod -R 750 /etc/letsencrypt/live
sudo chmod -R 750 /etc/letsencrypt/archive
sudo systemctl start kibana