param([string]$SrcRoot,
      [string]$TestRoot)

$rootDir = Split-Path (Split-Path $MyInvocation.MyCommand.Path)
$rootDir = [IO.Path]::GetFullPath($rootDir)

if(!$SrcRoot) {
    $SrcRoot = Join-Path $rootDir "src"
}

if(!$TestRoot) {
    $TestRoot = Join-Path $rootDir "tests"
}

$Header = "// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information."

function NeedsCopyright([string]$FileName) {
    # Skip designer files bin and org files
    if($FileName.Contains("Designer") -or $FileName.Contains("bin\") -or $FileName.Contains("obj\")) {
        return $false;
    }

    # Check the first line
    $line = Get-Content $FileName -totalCount 1;
    
    # Does it have the header?
    return !$line -or !$line.StartsWith("// Copyright") 
}

function Get-FilesWithoutCopyright($Directory) {
    return Get-ChildItem $Directory -Recurse -Filter *.cs | Where-Object { NeedsCopyright $_.FullName } | Select-Object FullName
}

function WriteWarnings([string]$Directory, $Files) {
    Write-Warning "There are $($Files.Length) files in '$Directory' without a copyright header:"
    $Files | ForEach-Object { Write-Warning $_.FullName }
}

$srcFilesWithoutCopyright = Get-FilesWithoutCopyright $SrcRoot

WriteWarnings $SrcRoot $srcFilesWithoutCopyright