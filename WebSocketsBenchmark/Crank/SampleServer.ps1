param
(
  [Parameter(Mandatory=$false, Position=0, HelpMessage="CSV output file")]
  [string]$outputFile = "samples.csv",
  
  [Parameter(Mandatory=$false, Position=1, HelpMessage="SignalR performance counter instance name")]
  [string]$instance = "testdefault",
  
  [Parameter(Mandatory=$false, Position=2, HelpMessage="Whether to include diagnostic counters")]
  [string]$diagnostics = $true,
  
  [Parameter(Mandatory=$false, Position=3, HelpMessage="Sample timeout in seconds")]
  [string]$timeout = 3600
)

$countersFile = "counters.txt"

function AddCounter($counter)
{
    $counter = [string]::format($counter, $instance)
    add-content $countersFile $counter
}

if (test-path $outputFile)
{
    remove-item $outputFile
}

AddCounter("SignalR({0})\Connections Current")
AddCounter("SignalR({0})\Connections Reconnected")
AddCounter("SignalR({0})\Connections Disconnected")
AddCounter("Memory\Available MBytes")
AddCounter("TCPv4\Connections Established")

if ($diagnostics -eq $true)
{
    AddCounter("ASP.NET Applications(__Total__)\Requests Executing")
    AddCounter(".NET CLR Memory(w3wp)\# Bytes in all Heaps")
}
typeperf -sc $timeout -cf $countersFile -o $outputFile

remove-item $countersFile