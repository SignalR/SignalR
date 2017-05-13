# ASP.NET SignalR
ASP.NET SignalR is a library for developers that simplifies the process of adding real-time web functionality to applications. Real-time web functionality is the ability to have server code push content to connected clients instantly as it becomes available, rather than having the server wait for a client to request new data.

ASP.NET SignalR provides a simple API for creating server-to-client remote procedure calls (RPC) that call functions in clients from server-side .NET code.  It also includes API for connection management (for instance, connect and disconnect events), and grouping connections.



## ASP.NET SignalR Xamarin Client
The ASP.NET SignalR Client not only works in JavaScript and browsers, but also .NET applications such as Xamarin.Android, Xamarin.iOS, Windows Phone and Windows Store.  The .NET client uses the System.Net.Http namespace to connect to a SignalR server.

With the ASP.NET SignalR .NET Client, you can connect to Hubs, Invoke commands, and listen for commands locally which are invoked by the server.  You can also listen to events in the .NET client pertaining to connection state and errors.



## Quick Usage
Setting up a connection and proxy to an ASP.NET SignalR Hub is easy.  You establish the connection, generate a hub proxy, and then define handlers for hub methods the server can call on your client.  After starting the hub connection, you can then invoke methods on the server as needed:

```
// Connect to the server
var hubConnection = new HubConnection("http://server.com/");

// Create a proxy to the 'ChatHub' SignalR Hub
var chatHubProxy = hubConnection.CreateHubProxy("ChatHub");

// Wire up a handler for the 'UpdateChatMessage' for the server
// to be called on our client
chatHubProxy.On<string>("UpdateChatMessage", message => 
	text.Text += string.Format("Received Msg: {0}\r\n", message));

// Start the connection
await hubConnection.Start();

// Invoke the 'UpdateNick' method on the server
await chatHubProxy.Invoke("UpdateNick", "JohnDoe");
```


## Learn More
You can read more about ASP.NET SignalR by visiting http://www.asp.net/signalr
