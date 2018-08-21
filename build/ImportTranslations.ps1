<#
.SYNOPSIS Imports Translation files
#>
param(
    [Parameter(Position=0, Mandatory=$true)][string]$SourceDirectory,
    [Parameter(Position=1, Mandatory=$false)][string]$Destination,
    [switch]$Force
)

if(!$Destination) {
    $Destination = Join-Path (Split-Path -Parent $PSScriptRoot) "loc"
}

if(Test-Path $Destination) {
    if($Force) {
        Remove-Item -Recurse -Force $Destination
    } else {
        throw "Destination path: '$Destination' exists, use -Force to remove it."
    }
}

$supportedTfms = @(
    "net40",
    "net45",
    "netstandard1.3"
)

$files = @()
Get-ChildItem $SourceDirectory | ForEach-Object {
    $language = $_.Name

    Get-ChildItem $_.FullName | ForEach-Object {
        $category = $_.Name

        Get-ChildItem $_.FullName | ForEach-Object {
            $baseFile = [IO.Path]::GetFileNameWithoutExtension($_.FullName)

            if($baseFile.EndsWith(".resources.dll")) {
                $packageName = $baseFile.Substring(0, $baseFile.Length - ".resources.dll".Length)                
                $extension = ".resources.dll"
            } else {
                $packageName = [IO.Path]::GetFileNameWithoutExtension($baseFile)
                $extension = [IO.Path]::GetExtension($baseFile)
            }

            if($packageName -eq "SignalR") {
                $packageName = "Microsoft.AspNet.SignalR.Utils"
            }

            if($category -eq "netstandard") {
                $category = "netstandard1.3"
            }

            if($category -eq "packages") {
                $packagePath = $_.Name
            } elseif(($category -eq "tools") -or ($supportedTfms -notcontains $category.ToLowerInvariant())) {
                return;
            } else {
                $packagePath = "lib/$($category.ToLowerInvariant())/$($_.Name)"
            }

            $files += @(New-Object PSCustomObject -Property @{
                "Language"=$language;
                "Base"=$baseFile;
                "Package"=$packageName;
                "PackagePath"=$packagePath;
                "Source"=$_.FullName;
            })
        }
    }
}

# Now write the files to the destination path
mkdir $Destination | Out-Null

$files | Group-Object Language | ForEach-Object {
    if($_.Name -eq "comments") {
        Write-Host "Importing lci files ..."
    } else {
        Write-Host "Importing $($_.Name) resources ..."
    }
    $_.Group | ForEach-Object {
        if($_.Language -eq "comments") {
            $destinationPath = Join-Path (Join-Path (Join-Path $Destination $_.Package) "lci") $_.PackagePath
        }
        else {
            $destinationPath = Join-Path (Join-Path (Join-Path (Join-Path $Destination $_.Package) "lcl") $_.Language) $_.PackagePath
        }

        $x = [xml][IO.File]::ReadAllText((Convert-Path $_.Source))
        $modifiedNodes = $x.SelectNodes("//*") | Where-Object { $_.LocalName -eq "Modified" }
        $modifiedNodes | ForEach-Object {
            $_.By = "translator"
        }

        $destinationDir = Split-Path -Parent $destinationPath

        if(!(Test-Path $destinationDir)) {
            mkdir $destinationDir | Out-Null
        }

        $x.Save($destinationPath)
        Write-Debug "Imported $($_.Source)"
    }
}