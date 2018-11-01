<#
.SYNOPSIS Imports translated Intellisense files
#>
param(
    [Parameter(Position = 0, Mandatory = $true)][string]$SourceDirectory,
    [switch]$Force
)

$RepoRoot = Convert-Path "$PSScriptRoot\.."

$SkippedLanguages = @(
    # We don't do PLOC builds any more
    "PLOC",

    # These are in "language" folders in the source :)
    "net40",
    "net45"
)

Get-ChildItem $SourceDirectory | ForEach-Object {
    $language = $_.Name

    if ($SkippedLanguages -notcontains $language) {
        Get-ChildItem $_.FullName | ForEach-Object {
            $packageId = [IO.Path]::GetFileNameWithoutExtension($_.FullName)
            $dest = Join-Path $RepoRoot "src/$packageId/loc/intellisense/$language/$($_.Name)"

            Write-Host "Importing $packageId intellisense for $language to $dest ..."

            $destDir = Split-Path -Parent $dest
            if (!(Test-Path $destDir)) {
                mkdir $destDir | Out-Null
            }
            Copy-Item $_.FullName $dest
        }
    }
}