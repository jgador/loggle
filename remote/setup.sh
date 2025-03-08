#!/bin/bash
set -e

export DEBIAN_FRONTEND=noninteractive
export NEEDRESTART_MODE=a

# Move configuration and setup files (copied from the repo via Terraform) from /tmp to /etc/loggle
mv /tmp/docker-compose.yml /tmp/otel-collector-config.yaml /tmp/kibana.yml /tmp/import-cert.ps1 /tmp/export-cert.ps1 /etc/loggle/
mv /tmp/loggle.service /etc/systemd/system/
mv /tmp/es-init /etc/loggle/
chmod -R a+rw /etc/loggle/elasticsearch-data
chmod -R a+rw /etc/loggle/kibana-data
chmod -R a+rw /etc/loggle/certs

apt-get update && apt-get upgrade -y
apt-get install -y ca-certificates curl

# Install PowerShell
apt-get update
apt-get install -y wget
wget https://github.com/PowerShell/PowerShell/releases/download/v7.5.0/powershell_7.5.0-1.deb_amd64.deb
dpkg -i powershell_7.5.0-1.deb_amd64.deb
apt-get install -f
rm powershell_7.5.0-1.deb_amd64.deb

# Install Docker
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
bash -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable" > /etc/apt/sources.list.d/docker.list'
apt-get update

apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf
sysctl -p

# Try exporting cert from Key Vault
kv_export=$(pwsh /etc/loggle/export-cert.ps1)
echo "$kv_export"

# Install Certbot
apt-get update
apt-get install -y python3 python3-venv libaugeas0
python3 -m venv /opt/certbot/
/opt/certbot/bin/pip install --upgrade pip
/opt/certbot/bin/pip install certbot
ln -s /opt/certbot/bin/certbot /usr/bin/certbot

if [ ! -f "/etc/loggle/certs/fullchain.pem" ] || [ ! -f "/etc/loggle/certs/privkey.pem" ]; then
  echo "One or both certificate files are missing, running certbot..."
  certbot certonly --standalone -d kibana.loggle.co -m certbot@loggle.co --agree-tos --no-eff-email --preferred-challenges=http-01 --staging
else
  cert_exit_code=$(sudo openssl x509 -checkend 0 -noout -in /etc/loggle/certs/fullchain.pem >/dev/null 2>&1
  echo $?
  )

  if [ "$cert_exit_code" -eq 1 ]; then
    echo "Certificate is expired, running certbot..."
    certbot certonly --standalone -d kibana.loggle.co -m certbot@loggle.co --agree-tos --no-eff-email --preferred-challenges=http-01 --staging
  fi
fi

if [ - d "/etc/letsencrypt/live" ]; then
  sudo chmod -R 750 /etc/letsencrypt/live
fi

if [ - d "/etc/letsencrypt/archive" ]; then
  sudo chmod -R 750 /etc/letsencrypt/archive
fi

kv_import=$(pwsh /etc/loggle/import-cert.ps1)
echo "$kv_import"

# sudo chmod +x /etc/letsencrypt/renewal-hooks/post/export-cert.sh
# sudo /etc/letsencrypt/renewal-hooks/post/export-cert.sh

sudo systemctl daemon-reload
sudo systemctl enable loggle.service
sudo systemctl start loggle.service

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

es_output=$(pwsh /etc/loggle/es-init/batch-indexmanagement.ps1)
echo "$es_output"