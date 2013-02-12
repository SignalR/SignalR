# Files in the order they must be combined
$networkMockFiles = "../NetworkMock/"

$folderExclusions
$fileInclusionFilters =  "*.js"

# Files in the order they must be combined
$files = 
    "jquery.network.mock.core.js",
    "jquery.network.mock.ajax.js",
    "jquery.network.mock.websocket.js",
    "jquery.network.mock.eventsource.js",
    "jquery.network.mock.iframe.js"

if (!(Test-Path -path "bin")) {
	New-Item "bin" -Type Directory | Out-Null
}

Write-Host "Building bin\jquery.network.mock.js... " -NoNewline -ForegroundColor Yellow

$destinationFile = "../bin/jquery.network.mock.js"
Remove-Item $destinationFile -Force -ErrorAction SilentlyContinue

Foreach($file in $files)
{
    $filePath = $networkMockFiles + $file
    Add-Content -Path $destinationFile -Value "/* $file */"
	Get-Content -Path $filePath | Add-Content -Path $destinationFile
}

Write-Host "done" -ForegroundColor Green

# Minify to jquery.signalR.min.js
Write-Host "Building bin\jquery.network.mock.min.js... " -NoNewline -ForegroundColor Yellow
& "..\..\..\tools\ajaxmin\AjaxMin.exe" ..\bin\jquery.network.mock.js -out ..\bin\jquery.network.mock.min.js -clobber

Write-Host "done" -ForegroundColor Green