[System.Reflection.Assembly]::LoadFile("C:\Users\nimullen\Documents\GitHub\SignalR\tests\Microsoft.AspNet.SignalR.FunctionalTests\artifacts\Debug\bin\Microsoft.AspNet.SignalR.FunctionalTests.dll");
[System.Reflection.Assembly]::LoadFile("C:\Users\nimullen\Documents\GitHub\SignalR\tests\Microsoft.AspNet.SignalR.FunctionalTests\artifacts\Debug\bin\Microsoft.AspNet.SignalR.Client.dll");
[System.IO.Directory]::SetCurrentDirectory("C:\Users\nimullen\Documents\GitHub\SignalR\artifacts\Debug\Microsoft.AspNet.SignalR.FunctionalTests")

$myHost = New-Object Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IISExpressTestHost "C:\Users\nimullen\Documents\GitHub\SignalR\artifacts\Debug\Microsoft.AspNet.SignalR.FunctionalTests"

$myHost.Initialize(15,120,10,1)