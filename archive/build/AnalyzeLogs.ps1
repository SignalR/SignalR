param([Parameter(Mandatory=$true)][string]$LogPath)

function Get-FileName($line)
{
    $index = $_.IndexOf("log:"); 
    return $_.Substring(0, $index) + "log" 
}

New-Item $LogPath\LogAnalysis -type directory -force

# UnobservedTask exceptions
gci -recurse $LogPath\*.log | select-string "unobserved" | group Filename | select Name | out-string -width 1000 > $LogPath\LogAnalysis\unobserved_exceptions.log

# ObjectDisposed exceptions
gci -recurse $LogPath\*.log | select-string "disposed" | group Filename | select Name | out-string -width 1000 > $LogPath\LogAnalysis\ode_exceptions.log

# Network errors exceptions
gci -recurse $LogPath\*.log | select-string "unexpected" | group Filename | select Name | out-string -width 1000 > $LogPath\LogAnalysis\unexpected.log

# Connection Forcibly closed Errors
gci -recurse $LogPath\*.log | select-string "An existing connection was forcibly closed" | group Filename | select Name | out-string -width 1000 > $LogPath\LogAnalysis\connection_closed.log