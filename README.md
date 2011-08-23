# LICENSE
MIT License <http://www.opensource.org/licenses/mit-license.php>

## Get it on NuGet!
SignalR is broken up into a few package on NuGet:

* [SignalR](http://nuget.org/List/Packages/SignalR) - A meta package that brings in SignalR.Server and SignalR.Js (you should install this)

* [SignalR.Server](http://nuget.org/List/Packages/SignalR.Server) - Server side components needed to build SignalR endpoints

* [SignalR.Js](http://nuget.org/List/Packages/SignalR.Js) - Javascript client for SignalR

* [SignalR.Client](http://nuget.org/List/Packages/SignalR.Client) - .NET client for SignalR

* [SignalR.Ninject](http://nuget.org/List/Packages/SignalR.Ninject) - Ninject dependeny resolver for SignalR


To get started:

    Install-Package SignalR

# RAW Connection API
## Server
Create a class the derives from PersistentConnection:

    using SignalR;
    
    public class MyConnection : PersistentConnection {
        protected override Task OnReceivedAsync(string clientId, string data) {
            // Broadcast data to all clients
            return Connection.Broadcast(data);
        }
    }

## Setup Routing
Make a route for your connection:

Global.asax

    using System;
    using System.Web.Routing;
    using SignalR.Routing;

    public class Global : System.Web.HttpApplication {
        protected void Application_Start(object sender, EventArgs e) {
            // Register the route for chat
            RouteTable.Routes.MapConnection<MyConnection>("echo", "echo/{*operation}");
        }
    }

## Client
### Javascript
Add these scripts to your page:

    <script src="Scripts/jquery-1.6.2.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.signalR.min.js" type="text/javascript"></script>

HTML:

    <script type="text/javascript">
    $(function () {
        var connection = $.connection('echo');

        connection.received(function (data) {
            $('#messages').append('<li>' + data + '</li>');
        });
        
        connection.start();
        
        $("#broadcast").click(function () {
            connection.send($('#msg').val());
        });
    });
    </script>

    <input type="text" id="msg" />
    <input type="button" id="broadcast" />

    <ul id="messages">
    </ul>
    
### C# (Events)
    
    var connection = new Connection("http://localhost/echo");
    connection.Received += data => {
        Console.WriteLine(data);
    };

    connection.Start().Wait();
    connection.Send("From C#");
    
### C# (IObservable)
    
    var connection = new Connection("http://localhost/echo");
    connection.AsObservable()
              .Subscribe(Console.WriteLine);
    
    connection.Start().Wait();
    connection.Send("From C#");
    
# Higher level API

## Server

    public class Chat : Hub {
        public void Send(string message) {
            // Call the addMessage method on all clients
            Clients.addMessage(message);
        }
    }
    
## Client
### Javascript

Import the magic script to generate the server side proxy

    <script src="Scripts/jquery-1.6.2.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.signalR.min.js" type="text/javascript"></script>
    <script src="/signalr/hubs" type="text/javascript"></script>
   
HTML:

    <script type="text/javascript">
    $(function () {
        // Proxy created on the fly
        var chat = $.connection.chat;
        
        // Declare a function on the chat hub so the server can invoke it
        chat.addMessage = function(message) {
            $('#messages').append('<li>' + message + '</li>');
        };
        
        $("#broadcast").click(function () {
            // Call the chat method on the server
            chat.send($('#msg').val())
                .fail(function(e) { alert(e); }) // Supports jQuery deferred
        });
        
        // Start the connection
        $.connection.hub.start();
    });
    </script>
    
    <input type="text" id="msg" />
    <input type="button" id="broadcast" />

    <ul id="messages">
    </ul>