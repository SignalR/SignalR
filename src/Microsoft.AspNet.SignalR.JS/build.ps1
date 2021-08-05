Param(
    [string]$outputPath,
    [string]$version
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
    $output = Join-Path $outputPath "build-output.txt"
    & "cscript.exe" ..\..\tools\jshint\env\wsh.js "$file" > $output
    if (Select-String $output -SimpleMatch -Pattern "[$file]" -Quiet) {
        Write-Host
        (Get-Content $output) | Select-Object -Skip 4 | Where { !$_.Contains("""use strict"";") } | Write-Host -ForegroundColor Red
        Remove-Item $output
        exit 1
    }
    Write-Host "no issues found" -ForegroundColor Green
}

$hasError = $false;
Write-Host "Running ESLint..." -ForegroundColor Yellow
foreach ($file in $files) {
    Write-Host "$file..."
    & "./node_modules/.bin/eslint" "$file"
    if ($LASTEXITCODE -ne 0) {
        $hasError = $true;
    }
}

if ($hasError) {
    Write-Host "Error with ESLint"
    exit 1;
}

# Combine all files into jquery.signalR.js
if (!(Test-Path -path "$outputPath")) {
    New-Item "$outputPath" -Type Directory | Out-Null
}

Write-Host "Building $outputPath\jquery.signalR-$version.js... " -NoNewline -ForegroundColor Yellow
$filePath = "$outputPath\jquery.signalR-$version.js"
Remove-Item $filePath -Force -ErrorAction SilentlyContinue

foreach ($file in $files) {
    Add-Content -Path $filePath -Value "/* $file */"
    Get-Content -Path $file | 
        Where-Object { !$_.Contains("""use strict"";") } | 
        ForEach-Object { $_.Replace("[!VERSION!]", $version) } |
        Add-Content -Path $filePath
}
Write-Host "done" -ForegroundColor Green

# Minify to jquery.signalR.min.js
Write-Host "Building $outputPath\jquery.signalR-$version.min.js... " -NoNewline -ForegroundColor Yellow
& "..\..\tools\ajaxmin\AjaxMinifier.exe" "$outputPath\jquery.signalR-$version.js" -out "$outputPath\jquery.signalR-$version.min.js" -term -clobber > $output
(Get-Content $output)[6] | Write-Host -ForegroundColor Green

# Make versionless scripts for use within the build
Copy-Item "$outputPath\jquery.signalR-$version.js" "$outputPath\jquery.signalR.js"
Copy-Item "$outputPath\jquery.signalR-$version.min.js" "$outputPath\jquery.signalR.min.js"

# Copy additional package files

Get-Content "package.json" | 
    ForEach-Object { $_.Replace("[!VERSION!]", $version) } |
    Set-Content -Path "$outputPath\package.json"
Copy-Item "README.md" "$outputPath\README.md"
Copy-Item "LICENSE.md" "$outputPath\LICENSE.md"

Remove-Item $output -Force