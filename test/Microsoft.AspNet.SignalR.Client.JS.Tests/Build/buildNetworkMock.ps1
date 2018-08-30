param(
    [Parameter(Mandatory=$true)][string]$InputFiles,
    [Parameter(Mandatory=$true)][string]$OutputFile
)

# Separator chosen so MSBuild can pass it in...
$files = $InputFiles.Split("!")

$dir = Split-Path -Parent $OutputFile
if(!(Test-Path $dir)) {
    mkdir $dir | Out-Null
}
$path = Join-Path (Convert-Path $dir) (Split-Path -Leaf $OutputFile)

if(Test-Path $path) {
    Remove-Item -Force $path
}

$files | ForEach-Object {
    $name = [IO.Path]::GetFileName($_)
    Add-Content -Path $path -Value "/* $name */"
    Get-Content -Path $_ | Add-Content -Path $path
}