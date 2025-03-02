Connect-AzAccount -Identity
$CertBase64 = Get-AzKeyVaultSecret -VaultName "kv-loggle" -Name "kibana" -AsPlainText
$CertBytes = [Convert]::FromBase64String($CertBase64)
Set-Content -Path "/etc/loggle/certs/kv-export-kibana.pfx" -Value $CertBytes -AsByteStream