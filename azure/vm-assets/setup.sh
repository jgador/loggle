#!/bin/bash

# Exit on error, undefined variables, and pipe failures
set -euo pipefail

# Define constants (allow overrides for multi-tenant use)
readonly POWERSHELL_VERSION="7.5.0"
readonly LOGGLE_ROOT="${LOGGLE_INSTALL_ROOT:-/etc/loggle}"
readonly INFRA_ENV_PATH="$LOGGLE_ROOT/infra.env"
if [[ -f "$INFRA_ENV_PATH" ]]; then
    # shellcheck disable=SC1090
    source "$INFRA_ENV_PATH"
fi
readonly LOGGLE_PATH="$LOGGLE_ROOT"
readonly CERT_PATH="${LOGGLE_CERT_PATH:-$LOGGLE_PATH/certs}"
readonly DOMAIN="${LOGGLE_DOMAIN:-kibana.loggle.co}"
readonly EMAIL="${LOGGLE_CERT_EMAIL:-certbot@loggle.co}"
readonly DEFAULT_ASSET_REPO_URL="https://github.com/jgador/loggle.git"
readonly DEFAULT_ASSET_REPO_PATH="azure/vm-assets"
readonly ASSET_REPO_URL="${LOGGLE_ASSET_REPO_URL:-$DEFAULT_ASSET_REPO_URL}"
readonly ASSET_REPO_PATH="${LOGGLE_ASSET_REPO_PATH:-$DEFAULT_ASSET_REPO_PATH}"
readonly DEFAULT_ASSET_REPO_REF="master"
readonly ASSET_REPO_REF="${LOGGLE_ASSET_REPO_REF:-$DEFAULT_ASSET_REPO_REF}"
readonly ASSET_CLONE_DIR="${LOGGLE_ASSET_CLONE_DIR:-$LOGGLE_ROOT/repo}"
readonly ASSET_DIR="${LOGGLE_ASSET_DIR:-$LOGGLE_ROOT/assets}"
MANAGED_IDENTITY_CLIENT_ID="${LOGGLE_MANAGED_IDENTITY_CLIENT_ID:-}"
CERT_ENV="${LOGGLE_CERT_ENV:-production}"
KEY_VAULT_NAME="${LOGGLE_KEY_VAULT_NAME:-}"
readonly BOOTSTRAP_STATE_FILE="${LOGGLE_BOOTSTRAP_STATE_FILE:-}"
case "${CERT_ENV,,}" in
    staging|production) CERT_ENV="${CERT_ENV,,}" ;;
    *) CERT_ENV="production" ;;
esac
if [[ -z "$KEY_VAULT_NAME" ]]; then
    echo "Key Vault name missing. Ensure LOGGLE_KEY_VAULT_NAME is exported or persisted."
    exit 1
fi
export LOGGLE_KEY_VAULT_NAME="$KEY_VAULT_NAME"

# Function definitions
bootstrap_already_completed() {
    [[ -n "$BOOTSTRAP_STATE_FILE" && -f "$BOOTSTRAP_STATE_FILE" ]]
}

exit_if_bootstrap_completed() {
    if bootstrap_already_completed; then
        echo "Loggle bootstrap already completed per $BOOTSTRAP_STATE_FILE; exiting."
        exit 0
    fi
}

mark_bootstrap_completed() {
    if [[ -z "$BOOTSTRAP_STATE_FILE" ]]; then
        return
    fi

    mkdir -p "$(dirname "$BOOTSTRAP_STATE_FILE")"
    touch "$BOOTSTRAP_STATE_FILE"
    chmod 600 "$BOOTSTRAP_STATE_FILE"
}

setup_environment() {
    export DEBIAN_FRONTEND=noninteractive
    export NEEDRESTART_MODE=a
    export PIP_DISABLE_PIP_VERSION_CHECK=1
    export PIP_PROGRESS_BAR=off
    export APT_OPTIONS="-o Dpkg::Progress-Fancy=0 -o Dpkg::Use-Pty=0 -o APT::Color=0"
}

persist_infra_var() {
    local key="$1"
    local value="${2:-}"
    if [[ -z "$value" ]]; then
        return
    fi

    mkdir -p "$(dirname "$INFRA_ENV_PATH")"

    local tmp_file="${INFRA_ENV_PATH}.tmp"
    local sanitized="${value//\"/\\\"}"
    local current_value=""
    if [[ -f "$INFRA_ENV_PATH" ]]; then
        current_value="$(grep -m 1 "^${key}=" "$INFRA_ENV_PATH" || true)"
        if [[ "$current_value" == "${key}=\"${sanitized}\"" ]]; then
            return
        fi
    fi

    if [[ -f "$INFRA_ENV_PATH" ]]; then
        grep -v "^${key}=" "$INFRA_ENV_PATH" > "$tmp_file" || true
    else
        : > "$tmp_file"
    fi

    printf '%s="%s"\n' "$key" "$sanitized" >> "$tmp_file"
    mv "$tmp_file" "$INFRA_ENV_PATH"
    chmod 600 "$INFRA_ENV_PATH"
}

move_config_files() {
    if [[ ! -d "$ASSET_DIR" ]]; then
        echo "Asset directory $ASSET_DIR not found; cannot copy configuration files."
        exit 1
    fi

    local config_files=("docker-compose.yml" "otel-collector-config.yaml" "kibana.yml" "import-cert.ps1" "export-cert.ps1")
    for file in "${config_files[@]}"; do
        local source="$ASSET_DIR/$file"
        if [[ -e "$source" ]]; then
            cp -f "$source" "$LOGGLE_PATH/"
        fi
    done

    if [[ -f "$ASSET_DIR/loggle.service" ]]; then
        cp -f "$ASSET_DIR/loggle.service" "/etc/systemd/system/loggle.service"
    fi

    if [[ -d "$ASSET_DIR/init-es" ]]; then
        local target="$LOGGLE_PATH/init-es"
        if [[ -d "$target" ]]; then
            rm -rf "$target"
        fi
        cp -R "$ASSET_DIR/init-es" "$LOGGLE_PATH/"
    fi
}

ensure_directories() {
    mkdir -p "$LOGGLE_PATH"
    mkdir -p "$CERT_PATH"
    mkdir -p "$LOGGLE_PATH/elasticsearch-data"
    mkdir -p "$LOGGLE_PATH/kibana-data"
    mkdir -p "$LOGGLE_PATH/certs"
    mkdir -p "$ASSET_DIR"
}

set_permissions() {
    local dirs=("elasticsearch-data" "kibana-data" "certs")
    for dir in "${dirs[@]}"; do
        chmod -R a+rw "$LOGGLE_PATH/$dir"
    done
}

ensure_certificate_placeholders() {
    for file in fullchain.pem privkey.pem; do
        local path="$CERT_PATH/$file"
        if [[ -d "$path" ]]; then
            rm -rf "$path"
        fi
        if [[ ! -f "$path" ]]; then
            touch "$path"
        fi
        chmod 644 "$path"
    done
}

load_or_cache_managed_identity() {
    if [[ -z "$MANAGED_IDENTITY_CLIENT_ID" && -n "${LOGGLE_MANAGED_IDENTITY_CLIENT_ID:-}" ]]; then
        MANAGED_IDENTITY_CLIENT_ID="$LOGGLE_MANAGED_IDENTITY_CLIENT_ID"
    fi

    if [[ -n "$MANAGED_IDENTITY_CLIENT_ID" ]]; then
        export LOGGLE_MANAGED_IDENTITY_CLIENT_ID="$MANAGED_IDENTITY_CLIENT_ID"
        persist_infra_var "LOGGLE_MANAGED_IDENTITY_CLIENT_ID" "$MANAGED_IDENTITY_CLIENT_ID"
    else
        echo "Managed identity client ID not provided and not cached."
    fi
}

assets_present_locally() {
    [[ -f "$ASSET_DIR/docker-compose.yml" && -f "$ASSET_DIR/import-cert.ps1" && -d "$ASSET_DIR/init-es" ]]
}

refresh_assets_from_repo() {
    if [[ -n "${LOGGLE_SKIP_ASSET_SYNC:-}" ]]; then
        echo "LOGGLE_SKIP_ASSET_SYNC is set; skipping asset synchronization."
        return
    fi

    if assets_present_locally; then
        echo "Asset payload already available in $ASSET_DIR; skipping clone."
        return
    fi

    if [[ -z "$ASSET_REPO_URL" ]]; then
        echo "Asset repository URL is not defined. Update LOGGLE_ASSET_REPO_URL or infra.env."
        exit 1
    fi

    install -d -m 0755 "$(dirname "$ASSET_CLONE_DIR")"
    rm -rf "$ASSET_CLONE_DIR"
    echo "Cloning assets from $ASSET_REPO_URL (ref: ${ASSET_REPO_REF:-default}) into $ASSET_CLONE_DIR..."
    local clone_args=(--depth 1)
    if [[ -n "$ASSET_REPO_REF" ]]; then
        clone_args+=(--branch "$ASSET_REPO_REF" --single-branch)
    fi

    if ! git clone "${clone_args[@]}" "$ASSET_REPO_URL" "$ASSET_CLONE_DIR"; then
        echo "Failed to clone $ASSET_REPO_URL"
        exit 1
    fi

    local repo_asset_dir="$ASSET_CLONE_DIR"
    if [[ -n "$ASSET_REPO_PATH" && "$ASSET_REPO_PATH" != "." ]]; then
        repo_asset_dir="$ASSET_CLONE_DIR/$ASSET_REPO_PATH"
    fi

    if [[ ! -d "$repo_asset_dir" ]]; then
        echo "Asset path $ASSET_REPO_PATH not found inside $ASSET_REPO_URL"
        exit 1
    fi

    echo "Staging assets into $ASSET_DIR..."
    rm -rf "$ASSET_DIR"
    install -d -m 0755 "$ASSET_DIR"
    cp -R "$repo_asset_dir"/. "$ASSET_DIR"/
}

apt_get() {
    apt-get $APT_OPTIONS "$@"
}

enable_universe_repo() {
    if ! grep -Rq "^[[:space:]]*deb .*universe" /etc/apt/sources.list /etc/apt/sources.list.d 2>/dev/null; then
        apt_get update
        apt_get install -y software-properties-common
        add-apt-repository -y universe
    fi
}

sync_certificates() {
    local source_dir="/etc/letsencrypt/live/$DOMAIN"
    if [[ -f "$source_dir/fullchain.pem" && -f "$source_dir/privkey.pem" ]]; then
        if [[ -d "$CERT_PATH/fullchain.pem" ]]; then
            rm -rf "$CERT_PATH/fullchain.pem"
        fi
        if [[ -d "$CERT_PATH/privkey.pem" ]]; then
            rm -rf "$CERT_PATH/privkey.pem"
        fi
        cp "$source_dir/fullchain.pem" "$CERT_PATH/fullchain.pem"
        cp "$source_dir/privkey.pem" "$CERT_PATH/privkey.pem"
        chmod 644 "$CERT_PATH/fullchain.pem" "$CERT_PATH/privkey.pem"
    fi
}

install_powershell() {
    if command -v pwsh >/dev/null 2>&1; then
        local installed_version
        installed_version="$(pwsh --version 2>/dev/null | awk '{print $NF}')"
        if [[ "$installed_version" == "$POWERSHELL_VERSION" ]]; then
            echo "PowerShell $POWERSHELL_VERSION already installed; skipping installation."
            return
        fi
    fi

    local deb_file="powershell_${POWERSHELL_VERSION}-1.deb_amd64.deb"
    wget -q "https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/${deb_file}"
    dpkg -i "$deb_file"
    apt_get install -f -y
    rm "$deb_file"
}

install_docker() {
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc
    
    bash -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo \"${UBUNTU_CODENAME:-$VERSION_CODENAME}\") stable" > /etc/apt/sources.list.d/docker.list'
    
    apt_get update
    apt_get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
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
    ensure_directories
    load_or_cache_managed_identity

    enable_universe_repo
    apt_get update
    apt_get upgrade -y
    apt_get install -y ca-certificates curl wget python3 python3-venv libaugeas0 git

    install_powershell
    install_docker

    refresh_assets_from_repo
    move_config_files
    set_permissions
    ensure_certificate_placeholders

    if [[ ! -f /etc/sysctl.conf ]] || ! grep -q '^vm.max_map_count=262144$' /etc/sysctl.conf; then
        echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf >/dev/null
    fi
    sysctl -p
    
    # Install and configure Certbot
    python3 -m venv /opt/certbot/
    /opt/certbot/bin/pip install --no-cache-dir --disable-pip-version-check --upgrade pip certbot
    ln -sf /opt/certbot/bin/certbot /usr/bin/certbot
    
    # Certificate management
    echo "Attempting to export certificate from Key Vault..."
    if [[ -n "$MANAGED_IDENTITY_CLIENT_ID" ]]; then
        if ! pwsh "$LOGGLE_PATH/export-cert.ps1" -KeyVaultName "$KEY_VAULT_NAME" -ManagedIdentityClientId "$MANAGED_IDENTITY_CLIENT_ID"; then
            echo "Key Vault certificate export failed or no certificate found; proceeding to Certbot fallback."
        fi
    else
        if ! pwsh "$LOGGLE_PATH/export-cert.ps1" -KeyVaultName "$KEY_VAULT_NAME"; then
            echo "Key Vault certificate export failed or no certificate found; proceeding to Certbot fallback."
        fi
    fi

    if check_cert_status; then
        echo "Valid certificate available in $CERT_PATH; skipping Let's Encrypt request."
    else
        echo "Requesting fresh certificate from Let's Encrypt..."
        local certbot_cmd=(certbot certonly --standalone -d "$DOMAIN" -m "$EMAIL" --agree-tos --no-eff-email --preferred-challenges=http-01)
        if [[ "$CERT_ENV" == "staging" ]]; then
            certbot_cmd+=(--staging)
        fi
        "${certbot_cmd[@]}"
        sync_certificates
    fi

    ensure_certificate_placeholders
    
    # Set certificate permissions
    for dir in /etc/letsencrypt/{live,archive}; do
        [[ -d "$dir" ]] && sudo chmod -R 750 "$dir"
    done
    
    echo "Importing certificate to Key Vault..."
    if [[ -n "$MANAGED_IDENTITY_CLIENT_ID" ]]; then
        pwsh "$LOGGLE_PATH/import-cert.ps1" -KeyVaultName "$KEY_VAULT_NAME" -ManagedIdentityClientId "$MANAGED_IDENTITY_CLIENT_ID"
    else
        pwsh "$LOGGLE_PATH/import-cert.ps1" -KeyVaultName "$KEY_VAULT_NAME"
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

exit_if_bootstrap_completed
main "$@"
mark_bootstrap_completed
