# ASP.NET SignalR [![Build Status](http://ci.signalr.net/app/rest/builds/buildType:\(id:bt2\)/statusIcon)](http://ci.signalr.net/?guest=1)
ASP.NET SignalR is a library for ASP.NET developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients right when it happens.

## What Can SignalR Be Used For?
SignalR is ideal for situations where events on your server need to be communicated to one or more clients (browsers, mobile apps, etc). This can be in the form of a chat application, messaging interface, updates to user-critical information that require notification, or just simple alerts that a long-running event has completed.

## Documentation
We [have a dedicated documentation site up at asp.net](http://asp.net/signalr). If you find any issues with the docs or just want to help - please create an issue here.

## Install via NuGet

    Install-Package Microsoft.AspNet.SignalR

## Install a Sample App Using NuGet

    Install-Package Microsoft.AspNet.SignalR.Sample
	
## LICENSE
SignalR is licensed under the [Apache 2.0 License](https://github.com/SignalR/SignalR/blob/master/LICENSE.md). This license is designed to protect copyrighted code, but allows you to change things as you need provided you document those changes. Please [review the license](https://github.com/SignalR/SignalR/blob/master/LICENSE.md) before you download the code. 

## Contributing

We would love to have your help! If you would like to help out, please have a look at the [contribution  guidelines](https://github.com/SignalR/SignalR/blob/master/CONTRIBUTING.md). It's a small bit of formality but it would really help us if you could review these guidelines before submitting an issue or pull request. Thank you!

## Building the source

Building the project from source requires a few simple steps - download the code then run a simple command. To download the code simply open up Powershell (or the git command line) and run:

```
git clone git@github.com:SignalR/SignalR.git (or https if you use https)
```

### Windows
After cloning the repository - open up Powershell and navigate to where you downloaded the source. Once you're in the source directory, run the build command that we've created for you:

```
cd [PATH TO SOURCE]
build.cmd
```

**NOTE:** If you want to change anything - opening the solution requires VS 2012. Please be sure you have that installed.

### Mono
After cloning the repository, navigate to the source directory and run `make`:

```
cd [PATH TO SOURCE]
make
```

## Questions?

We welcome any feedback or thoughts you might have! If you just want to say "hi" or have a basic question, you can [drop by our chat room on jabbr.net]((http://jabbr.net/#/rooms/signalr)). 

If you have a question about the code, a suggestion, or improvement - feel free to create an Issue here. Before you do - if you could help us out by reading our [contribution guidelines](https://github.com/SignalR/SignalR/blob/master/CONTRIBUTING.md) we would really appreciate it!
