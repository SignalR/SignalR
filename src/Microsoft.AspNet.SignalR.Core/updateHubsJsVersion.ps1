Param(
    [string]$outputPath,
    [string]$version
)

$outputFile = "$outputPath\hubs.js"

Remove-Item $outputFile -Force -ErrorAction SilentlyContinue

Get-Content -Path "Scripts\hubs.js" | 
	ForEach-Object { $_.Replace("[!VERSION!]", $version) } |
	Add-Content -Path $outputFile