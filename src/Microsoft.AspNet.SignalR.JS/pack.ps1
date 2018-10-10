Param(
    [string]$outputPath,
    [string]$packageOutputPath,
    [string]$version
)

# Pack the package
if(Get-Command -ErrorAction SilentlyContinue npm) {
    Push-Location $outputPath
    try {
        npm pack
        Copy-Item "signalr-$version.tgz" $packageOutputPath
    } finally {
        Pop-Location
    }
}