using namespace System
using namespace System.IO

function ExecuteBatch{
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Folder,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Uri]$BaseUrl
    )

    # folder parameter is relative, get the absolute path
    $absoluteFolderPath = Join-Path -Path $PSScriptRoot -ChildPath $Folder

    Get-ChildItem -Path $absoluteFolderPath -Filter "*.json" | ForEach-Object{
        # filename will be part of the url, as the name of index management resource
        $filename = [Path]::GetFileNameWithoutExtension($_.FullName)
        $uriBuilder = [UriBuilder]$BaseUrl
        $uriBuilder.Path = Join-Path $uriBuilder.Path $filename

        try {
            New-ElasticsearchIndexManagement `
                -JsonFilePath $_.FullName `
                -Endpoint $uriBuilder.Uri.AbsoluteUri `
                -ErrorAction Stop

            Write-Progress -Activity "Inserting/updating Elasticsearch component..." -Status $filename
        }
        catch {
            Write-Error $_.Exception.Message
        }
    }
}

# Get the path of the module
$modulePath = Join-Path -Path $PSScriptRoot -ChildPath 'ps-modules\indexmanagement.psm1'
$DebugPreference = 'Continue'
Import-Module $modulePath

$ilmBaseUrl = [Uri]"http://localhost:9200/_ilm/policy"
$componentTemplateBaseUrl = [Uri]"http://localhost:9200/_component_template"
$indexTemplateBaseUrl = [Uri]"http://localhost:9200/_index_template"

ExecuteBatch -Folder "ilm-policy" -BaseUrl $ilmBaseUrl
Write-Output "Default ILM policy: Done"

ExecuteBatch -Folder "component-template" -BaseUrl $componentTemplateBaseUrl
Write-Output "Default ILM Component templates: Done"

ExecuteBatch -Folder "index-template" -BaseUrl $indexTemplateBaseUrl
Write-Output "Default index template: Done"

# Create default data stream
$uri = "http://localhost:9200/_data_stream/logs-loggle-default"
$headers = @{
    "Content-Type" = "application/json"
}
try {
  Invoke-RestMethod -Uri $uri -Method Get -Headers $headers | Out-Null
  Write-Output "Data stream already exists, skipping creation."
} catch {
  Invoke-RestMethod -Uri $uri -Method Put -Headers $headers | Out-Null
  Write-Output "Default data stream created."
}

# Remove the module
Remove-Module -Name indexmanagement
