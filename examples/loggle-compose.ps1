<#
    For local debugging:
    - Test app sends logs to the OTEL Collector.
    - OTEL Collector forwards Ingestion API.
    - The API is run locally in Visual Studio.
    Usage:
        .\loggle-compose.ps1 start   # Starts Docker Compose
        .\loggle-compose.ps1 stop    # Stops Docker Compose
#>

Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop")]
    [string]$action = "start"
)

$composeFile = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "docker-compose.yml"))
$initScript = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".." "azure" "vm-assets" "init-es" "init-es.ps1"))
$kibanaDataViewScript = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".." "azure" "vm-assets" "init-es" "kibana-dataview.ps1"))
$kibanaBaseUrl = "http://localhost:5601"

if (-not (Test-Path $composeFile)) {
    throw "docker-compose.yml not found at $composeFile"
}

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

function Wait-ForKibana {
  $kibanaReady = $false
  for ($i = 1; $i -le 60; $i++) {
      try {
          Invoke-WebRequest -Uri "$kibanaBaseUrl/api/status" -UseBasicParsing -ErrorAction Stop | Out-Null
          $kibanaReady = $true
          break
      } catch {
          Write-Host "Waiting for Kibana to be ready (attempt $i)..."
          Start-Sleep -Seconds 5
      }
  }

  if (-not $kibanaReady) {
      Write-Host "Max attempts reached. Kibana did not become ready."
      exit 1
  } else {
      Write-Host "Kibana is ready!"
  }
}

if ($action -eq "stop") {
  Write-Host "Stopping Loggle Docker Compose..."
  docker compose -f $composeFile --project-name loggle down
} else {
  Write-Host "Starting Loggle Docker Compose..."
  docker compose -f $composeFile --project-name loggle up --build -d

  # Wait for Elasticsearch to be ready.
  Wait-ForElasticsearch

  # Provision defaults by calling init-es if it exists.
  if (Test-Path $initScript) {
      Write-Host "Provisioning defaults with init-es.ps1..."
      & $initScript
  } else {
      Write-Host "Warning: init-es.ps1 not found."
  }

  # Ensure Kibana is ready before creating the data view.
  Wait-ForKibana

  if (Test-Path $kibanaDataViewScript) {
      Write-Host "Ensuring default Kibana data view exists..."
      & $kibanaDataViewScript -KibanaBaseUrl $kibanaBaseUrl
  } else {
      Write-Host "Warning: kibana-dataview.ps1 not found."
  }
}
