#requires -Version 7.0
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$KibanaBaseUrl = "http://localhost:5601",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$DataViewId = "loggle",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$DataViewName = "loggle",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$IndexPattern = "logs-loggle*",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$TimeField = "@timestamp",

    [Parameter()]
    [ValidateRange(30, 1800)]
    [int]$WaitTimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
Set-StrictMode -Version Latest

function New-KibanaUri {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Path
    )

    $base = $KibanaBaseUrl.TrimEnd('/')
    if ($Path.StartsWith("/")) {
        return "$base$Path"
    }
    return "$base/$Path"
}

function Invoke-KibanaRequest {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Path,

        [Parameter()]
        $Body
    )

    $uri = New-KibanaUri -Path $Path
    $params = @{
        Uri                  = $uri
        Method               = $Method
        Headers              = @{ "kbn-xsrf" = "loggle-init" }
        SkipCertificateCheck = $true
    }

    if ($PSBoundParameters.ContainsKey("Body")) {
        $params.ContentType = "application/json"
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    return Invoke-RestMethod @params
}

function Wait-ForKibana {
    param(
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-KibanaRequest -Method Get -Path "/api/status" | Out-Null
            return $true
        }
        catch {
            Start-Sleep -Seconds 5
        }
    }

    return $false
}

if (-not (Wait-ForKibana -TimeoutSeconds $WaitTimeoutSeconds)) {
    throw "Kibana did not become ready within $WaitTimeoutSeconds seconds."
}

$payload = @{
    data_view = @{
        id             = $DataViewId
        name           = $DataViewName
        title          = $IndexPattern
        timeFieldName  = $TimeField
    }
}

try {
    Invoke-KibanaRequest -Method Post -Path "/api/data_views/data_view" -Body $payload | Out-Null
    Write-Host "Kibana data view '$DataViewName' created."
}
catch {
    $response = $_.Exception.Response
    $statusCode = $null
    if ($response -and $response.StatusCode) {
        $statusCode = [int]$response.StatusCode
    }

    if ($statusCode -eq 409) {
        Write-Host "Kibana data view '$DataViewName' already exists; ensuring it matches desired settings."
        $encodedId = [Uri]::EscapeDataString($DataViewId)
        Invoke-KibanaRequest -Method Put -Path "/api/data_views/data_view/$encodedId" -Body $payload | Out-Null
        Write-Host "Kibana data view '$DataViewName' updated."
    }
    else {
        throw
    }
}
