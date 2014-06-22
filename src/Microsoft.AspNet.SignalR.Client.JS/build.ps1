Param(
    [string]$outputPath
)

# Files in the order they must be combined
$files = 
    "jquery.signalR.core.js",
    "jquery.signalR.transports.common.js",
    "jquery.signalR.transports.webSockets.js",
    "jquery.signalR.transports.serverSentEvents.js",
    "jquery.signalR.transports.foreverFrame.js",
    "jquery.signalR.transports.longPolling.js",
    "jquery.signalR.hubs.js",
    "jquery.signalR.version.js"

# Run JSHint against files
Write-Host "Running JSHint..." -ForegroundColor Yellow
foreach ($file in $files) {
    Write-Host "$file... " -NoNewline
    $output = "build-output.txt"
    & "cscript.exe" ..\..\tools\jshint\env\wsh.js "$file" > $output
    if (Select-String $output -SimpleMatch -Pattern "[$file]" -Quiet) {
        Write-Host
        (Get-Content $output) | Select -Skip 4 | Where { !$_.Contains("""use strict"";") } | Write-Host -ForegroundColor Red
        Remove-Item $output
        exit 1
    }
    Write-Host "no issues found" -ForegroundColor Green
}

# Combine all files into jquery.signalR.js
if (!(Test-Path -path "bin")) {
    New-Item "bin" -Type Directory | Out-Null
}

Write-Host "Building bin\jquery.signalR.js... " -NoNewline -ForegroundColor Yellow
$filePath = "$outputPath\jquery.signalR.js"
Remove-Item $filePath -Force -ErrorAction SilentlyContinue
foreach ($file in $files) {
    Add-Content -Path $filePath -Value "/* $file */"
    Get-Content -Path $file | Where { !$_.Contains("""use strict"";") } | Add-Content -Path $filePath
}
Write-Host "done" -ForegroundColor Green

# Minify to jquery.signalR.min.js
Write-Host "Building bin\jquery.signalR.min.js... " -NoNewline -ForegroundColor Yellow
& "..\..\tools\ajaxmin\AjaxMinifier.exe" $outputPath\jquery.signalR.js -out $outputPath\jquery.signalR.min.js -clobber > $output
(Get-Content $output)[6] | Write-Host -ForegroundColor Green

Remove-Item $output -Force