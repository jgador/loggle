<#
    For local debugging:
    - Test app sends logs to the OTEL Collector.
    - OTEL Collector forwards Ingestion API.
    - The API is run locally in Visual Studio.
    Usage:
        .\startdc.ps1 start   # Starts Docker Compose
        .\startdc.ps1 stop    # Stops Docker Compose
#>

Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop")]
    [string]$action = "start"
)

function Wait-ForElasticsearch {
  $esReady = $false
  for ($i = 1; $i -le 50; $i++) {
      try {
          Invoke-WebRequest -Uri "http://localhost:9200" -Method Head -UseBasicParsing -ErrorAction Stop | Out-Null
          $esReady = $true
          break
      } catch {
          Write-Host "Waiting for Elasticsearch to be ready (attempt $i)..."
          Start-Sleep -Seconds 5
      }
  }

  if (-not $esReady) {
      Write-Host "Max attempts reached. Elasticsearch did not become ready."
      exit 1
  } else {
      Write-Host "Elasticsearch is ready!"
  }
}

if ($action -eq "stop") {
  Write-Host "Stopping Loggle Docker Compose..."
  docker compose --project-name loggle down
} else {
  Write-Host "Starting Loggle Docker Compose..."
  docker compose -f .\docker-compose.yml --project-name loggle up -d

  # Wait for Elasticsearch to be ready.
  Wait-ForElasticsearch

  # Provision defaults by calling init-es if it exists.
  $batchScript = "..\..\remote\init-es\init-es.ps1"
  if (Test-Path $batchScript) {
      Write-Host "Provisioning defaults with init-es.ps1..."
      & $batchScript
  } else {
      Write-Host "Warning: init-es.ps1 not found."
  }
}