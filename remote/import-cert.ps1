openssl pkcs12 -export `
  -in "/etc/letsencrypt/live/kibana.loggle.co/fullchain.pem" `
  -inkey "/etc/letsencrypt/live/kibana.loggle.co/privkey.pem" `
  -out "/etc/loggle/certs/kv-import-kibana.pfx" `
  -passout pass:

Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
Install-Module -Name Az.KeyVault -RequiredVersion 6.3.1 -AllowClobber -Scope AllUsers -Force
Install-Module -Name Az.Accounts -RequiredVersion 4.0.2 -AllowClobber -Scope AllUsers -Force
Connect-AzAccount -Identity

Import-AzKeyVaultCertificate -VaultName 'kv-loggle' -Name 'kibana' -FilePath '/etc/loggle/certs/kv-import-kibana.pfx'