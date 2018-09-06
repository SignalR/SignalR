param(
    [Parameter(Mandatory=$true)][string]$InputTemplate,
    [Parameter(Mandatory=$true)][string]$OutputFile,
    [Parameter(Mandatory=$true)][string]$TestFiles)

# Separator chosen so MSBuild can pass it in...
$TestFilesList = $TestFiles.Split("!")

$Builder = New-Object System.Text.StringBuilder
$TestFilesList | ForEach-Object {
    $url = $_.Replace("\", "/")
    $Builder.AppendLine("        <script src=`"Tests/$url`"></script>") | Out-Null
}

$Content = [IO.File]::ReadAllText((Convert-Path $InputTemplate))

# Trim whitespace because the placeholder is already indented and followed by a newline.
$Content = $Content.Replace("<!-- #TESTLIST# -->", $Builder.ToString().Trim())
$Content = $Content.Replace("<!-- #HEADER# -->", "<!-- This file was auto-generated at $([DateTime]::Now.ToString("O")) -->")

$dir = Split-Path -Parent $OutputFile
$file = Split-Path -Leaf $OutputFile
if (!(Test-Path $dir)) {
    mkdir $dir | Out-Null
}
$OutputFile = Join-Path (Convert-Path $dir) $file

[IO.File]::WriteAllText($OutputFile, $Content)