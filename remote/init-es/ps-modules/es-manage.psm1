<#
.SYNOPSIS
    Inserts new index management components into Elasticsearch using HTTP PUT with a JSON file as the request body.
.DESCRIPTION
    This function inserts new index management components, such as ILM policies, index templates, component templates, and index settings, into Elasticsearch using HTTP PUT with a JSON file as the request body.
.PARAMETER Endpoint
    The Elasticsearch endpoint to call, including the index and document ID if applicable.
.PARAMETER JsonFilePath
    The name of the JSON file to send as the HTTP request body. The file should be located in the same directory as the PowerShell script.
.EXAMPLE
    New-ElasticsearchIndexManagement -Endpoint "http://localhost:9200/_component_template/my-component-template" -JsonFilePath "my-component-template.json"
#>
function New-ElasticsearchIndexManagement {
    [CmdletBinding(SupportsShouldProcess)]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Endpoint,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$JsonFilePath
    )
    
    [string]$contentType = "application/json"
    
    # Check if the file exists
    if (-not (Test-Path $JsonFilePath)) {
        Write-Error "The file '$JsonFilePath' does not exist."
        return
    }
    
    # Set up HTTP request headers
    $headers = @{
        "Content-Type" = $contentType
    }
    
    # Send HTTP PUT request with JSON body
    if ($PSCmdlet.ShouldProcess($Endpoint, "Insert new index management component")) {
        try {
            $jsonBody = Get-Content -Path $JsonFilePath -Raw
            
            Invoke-RestMethod `
                -Method Put `
                -Uri $Endpoint `
                -Headers $headers `
                -Body $jsonBody `
                -ErrorAction Stop `
                -DisableKeepAlive `
                | Out-Null
        } catch {
            throw "Failed to complete request to `"$Endpoint`" for file `"$JsonFilePath`". $($_.Exception.Message)"
        }
    }
}
