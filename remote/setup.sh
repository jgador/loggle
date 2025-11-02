#!/bin/bash

# Exit on error, undefined variables, and pipe failures
set -euo pipefail

# Define constants
readonly POWERSHELL_VERSION="7.5.0"
readonly DOMAIN="kibana.loggle.co"
readonly EMAIL="certbot@loggle.co"
readonly CERT_PATH="/etc/loggle/certs"
readonly LOGGLE_PATH="/etc/loggle"
readonly MANAGED_IDENTITY_CLIENT_ID="${LOGGLE_MANAGED_IDENTITY_CLIENT_ID:-}"

# Function definitions
setup_environment() {
    export DEBIAN_FRONTEND=noninteractive
    export NEEDRESTART_MODE=a
}

move_config_files() {
    local config_files=("docker-compose.yml" "otel-collector-config.yaml" "kibana.yml" "import-cert.ps1" "export-cert.ps1")
    for file in "${config_files[@]}"; do
        mv "/tmp/$file" "$LOGGLE_PATH/"
    done
    mv "/tmp/loggle.service" "/etc/systemd/system/"
    mv "/tmp/init-es" "$LOGGLE_PATH/"
}

set_permissions() {
    local dirs=("elasticsearch-data" "kibana-data" "certs")
    for dir in "${dirs[@]}"; do
        chmod -R a+rw "$LOGGLE_PATH/$dir"
    done
}

install_powershell() {
    local deb_file="powershell_${POWERSHELL_VERSION}-1.deb_amd64.deb"
    wget "https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/${deb_file}"
    dpkg -i "$deb_file"
    apt-get install -f
    rm "$deb_file"
}

install_docker() {
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc
    
    bash -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable" > /etc/apt/sources.list.d/docker.list'
    
    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
}

check_cert_status() {
    if [[ ! -f "$CERT_PATH/fullchain.pem" ]] || [[ ! -f "$CERT_PATH/privkey.pem" ]]; then
        echo "Certificate files missing"
        return 1
    fi
    
    if ! sudo openssl x509 -checkend 0 -noout -in "$CERT_PATH/fullchain.pem" >/dev/null 2>&1; then
        echo "Certificate expired"
        return 1
    fi
    
    return 0
}

wait_for_elasticsearch() {
    local max_attempts=50
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if curl --output /dev/null --silent --head --fail http://localhost:9200; then
            echo "Elasticsearch is ready!"
            return 0
        fi
        echo "Waiting for Elasticsearch (attempt $attempt/$max_attempts)"
        sleep 5
        ((attempt++))
    done
    
    echo "Max attempts reached waiting for Elasticsearch"
    return 1
}

# Main script execution
main() {
    setup_environment
    move_config_files
    set_permissions
    
    apt-get update && apt-get upgrade -y
    apt-get install -y ca-certificates curl wget python3 python3-venv libaugeas0
    
    install_powershell
    install_docker
    
    echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf
    sysctl -p
    
    # Install and configure Certbot
    python3 -m venv /opt/certbot/
    /opt/certbot/bin/pip install --upgrade pip certbot
    ln -sf /opt/certbot/bin/certbot /usr/bin/certbot
    
    # Certificate management
    echo "Attempting to export certificate from Key Vault..."
    if [[ -n "$MANAGED_IDENTITY_CLIENT_ID" ]]; then
        pwsh "$LOGGLE_PATH/export-cert.ps1" -ManagedIdentityClientId "$MANAGED_IDENTITY_CLIENT_ID"
    else
        pwsh "$LOGGLE_PATH/export-cert.ps1"
    fi
    
    if ! check_cert_status; then
        certbot certonly --standalone -d "$DOMAIN" -m "$EMAIL" --agree-tos --no-eff-email --preferred-challenges=http-01
    fi
    
    # Set certificate permissions
    for dir in /etc/letsencrypt/{live,archive}; do
        [[ -d "$dir" ]] && sudo chmod -R 750 "$dir"
    done
    
    echo "Importing certificate to Key Vault..."
    if [[ -n "$MANAGED_IDENTITY_CLIENT_ID" ]]; then
        pwsh "$LOGGLE_PATH/import-cert.ps1" -ManagedIdentityClientId "$MANAGED_IDENTITY_CLIENT_ID"
    else
        pwsh "$LOGGLE_PATH/import-cert.ps1"
    fi
    
    # Start services
    systemctl daemon-reload
    systemctl enable loggle.service
    systemctl start loggle.service
    
    # Initialize Elasticsearch
    if wait_for_elasticsearch; then
        pwsh "$LOGGLE_PATH/init-es/init-es.ps1"
    else
        exit 1
    fi
}

main "$@"
