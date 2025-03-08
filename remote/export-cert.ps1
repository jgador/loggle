Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
Install-Module -Name Az.KeyVault -RequiredVersion 6.3.1 -AllowClobber -Scope AllUsers -Force
Install-Module -Name Az.Accounts -RequiredVersion 4.0.2 -AllowClobber -Scope AllUsers -Force
Connect-AzAccount -Identity

$CertBase64 = Get-AzKeyVaultSecret -VaultName "kv-loggle" -Name "kibana" -AsPlainText

if (-not [string]::IsNullOrEmpty($CertBase64)) {
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
  
  chmod 750 /etc/loggle/certs/fullchain.pem
  chmod 750 /etc/loggle/certs/privkey.pem
}