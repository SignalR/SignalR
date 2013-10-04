# ASP.NET SignalR  Boo
Async signaling library for .NET to help build real-time, multi-user interactive web applications

## What can it be used for?
Pushing data from the server to the client (not just browser clients) has always been a tough problem. SignalR makes 
it dead easy and handles all the heavy lifting for you.

## Documentation
See the [documentation](https://github.com/SignalR/SignalR/wiki)

## Get it on NuGet!

    Install-Package Microsoft.AspNet.SignalR -pre

## Get a sample on NuGet, straight into your app!

    Install-Package Microsoft.AspNet.SignalR.Sample
	
## LICENSE
[Apache 2.0 License](https://github.com/SignalR/SignalR/blob/master/LICENSE.md)

## Building the source

```
git clone git@github.com:SignalR/SignalR.git (or https if you use https)
```

### Windows
After cloning the repository, run `build.cmd`.

If the ASP.NET samples csproj won't load when opening the solution in Visual Studio then 
download [Web Platform Installer](http://www.microsoft.com/web/downloads/platform.aspx) and install IIS Express.

### Mono
After cloning the repository, run `make`.

**NOTE:** Run `make tests` to run the unit tests. After running them it'll probably hang. If it does hang
use `Ctrl+C` to break out (We're still working on this).

Open Microsoft.AspNet.SignalR.Mono.sln to do development.

## Continuous Integration

We have a CI Server (http://ci.signalr.net/)

We generate packages from our ci builds to this feed http://www.myget.org/F/signalr/. If you want to live on the bleeding
edge and don't want to clone the source. You can try things out this way.

## Questions?
The SignalR team hangs out in the [signalr](http://jabbr.net/#/rooms/signalr) room at on [JabbR](http://jabbr.net/).