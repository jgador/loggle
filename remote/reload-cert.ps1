Connect-AzAccount -Identity
$CertBase64 = Get-AzKeyVaultSecret -VaultName "kv-loggle" -Name "kibana" -AsPlainText
$CertBytes = [Convert]::FromBase64String($CertBase64)
Set-Content -Path "/etc/loggle/certs/kv-export-kibana.pfx" -Value $CertBytes -AsByteStream

openssl pkcs12 `
  -in /etc/loggle/certs/kv-export-kibana.pfx `
  -clcerts `
  -nokeys `
  -out /etc/loggle/certs/fullchain.pem `
  -passin pass:

openssl pkcs12 `
  -in /etc/loggle/certs/kv-export-kibana.pfx `
  -nocerts `
  -nodes `
  -out /etc/loggle/certs/privkey.pem `
  -passin pass:

# Restart kibana to apply the new certificate
docker compose -f /etc/loggle/docker-compose.yml restart kibana

chmod 750 /etc/loggle/certs/fullchain.pem
chmod 750 /etc/loggle/certs/privkey.pem