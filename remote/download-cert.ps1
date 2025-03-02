function Download-Cert {
  param(
      [Parameter(Mandatory = $true)]
      [ValidateNotNullOrEmpty()]
      [string]$VaultName,

      [Parameter(Mandatory = $true)]
      [ValidateNotNullOrEmpty()]
      [string]$CertName,

      [Parameter(Mandatory = $true)]
      [ValidateNotNullOrEmpty()]
      [string]$OutputFile
  )

  $CertBase64 = Get-AzKeyVaultSecret -VaultName $vaultName -Name $certName -AsPlainText
  $CertBytes = [Convert]::FromBase64String($CertBase64)
  Set-Content -Path $OutputFile -Value $CertBytes -AsByteStream
}

Connect-AzAccount -Identity
Download-Cert -VaultName "kv-loggle" -CertName "kv-loggle-cert" -OutputFile "/etc/loggle/kv-kibana.pfx"