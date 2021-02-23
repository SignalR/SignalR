param([string]$SourceDirectory, [string]$OutputDirectory, [string]$ToolsDirectory)

if(!(Test-Path $OutputDirectory)) {
    mkdir $OutputDirectory | Out-Null
}

$SymbolFiles = @(
  "Client",
  "Core",
  "Redis",
  "ServiceBus",
  "ServiceBus3",
  "SqlServer",
  "SystemWeb")

$NuGetExe = Join-Path (Join-Path (Join-Path "$PSScriptRoot" "..") ".nuget") "nuget.exe"
# Install Pdb2Pdb if not already installed
& $NuGetExe install -OutputDirectory "$ToolsDirectory" -ExcludeVersion -Source https://pkgs.dev.azure.com/dnceng/public/_packaging/myget-legacy/nuget/v3/index.json -Prerelease Microsoft.DiaSymReader.Pdb2Pdb
$Pdb2PdbExe = Join-Path (Join-Path (Join-Path $ToolsDirectory "Microsoft.DiaSymReader.Pdb2Pdb") "tools") "Pdb2Pdb.exe"

if(!(Test-Path $Pdb2PdbExe)) {
    throw "Failed to install Pdb2Pdb!"
}

$PortablePdbPath = Join-Path $OutputDirectory "portable"
$WindowsPdbPath = Join-Path $OutputDirectory "windows"
$SymbolFiles | ForEach-Object {
    $FullName = "Microsoft.AspNet.SignalR.$_"
    $ProjectDirectory = Join-Path $SourceDirectory $FullName
    Get-ChildItem $ProjectDirectory | ForEach-Object {
        $TfmName = $_
        $Dll = Join-Path $_.FullName "$FullName.dll"
        $Pdb = Join-Path $_.FullName "$FullName.pdb"
        if(Test-Path $Pdb) {
            $PortableDest = Join-Path $PortablePdbPath $TfmName
            if(!(Test-Path $PortableDest)) {
                mkdir $PortableDest | Out-Null
            }
            Write-Host "Placing Portable PDB file: $FullName ($TfmName)"
            Copy-Item $Pdb $PortableDest
            Copy-Item $Dll $PortableDest

            # Make Windows PDBs
            $WindowsDest = Join-Path $WindowsPdbPath $TfmName
            if(!(Test-Path $WindowsDest)) {
                mkdir $WindowsDest | Out-Null
            }
            Write-Host "Generating Windows PDB file: $FullName ($TfmName)"
            & "$Pdb2PdbExe" "$Dll" /pdb "$Pdb" /out "$WindowsDest\$FullName.pdb"
            Copy-Item $Dll $WindowsDest
        }
    }
}
