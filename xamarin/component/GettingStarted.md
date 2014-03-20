## Getting Started
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


## Complex Object Types
You can also use more complex object types when you expose client method handlers and invoke methods on the server.  Internally, the ASP.NET SignalR .NET Client uses Newtonsoft.Json to serialize and deserialize objects for transport.  The example from above could become something like this:

```
chatHubProxy.On<ChatMessage>("UpdateChatMessage", message => 
	text.Text += string.Format("{0}: {1} \r\n", message.User, message.Text));

chatHubProxy.Invoke("SendMessage", new ChatMessage() { User = "JohnDoe", Text = "Hello!" });
```


## Connection Lifetime Events
SignalR provides the following connection lifetime events that you can handle:

 - **Received**: Raised when any data is received on the connection.  Provides the received data.
 - **ConnectionSlow**: Raised when the client detects a slow or frequently dropping connection.
 - **Reconnecting**: Raised when the underlying transport begins reconnecting.
 - **Reconnected**: Raised when the underlying transport has reconnected.
 - **StateChanged**: Raised when the connection state changes. Provides the old state and the new state.
 - **Closed**: Raised when the connection has disconnected.


For example, if you want to display warning messages for errors that are not fatal but cause intermittent connection problems, such as slowness or frequent dropping of the connection, handle the ConnectionSlow event.

```
hubConnection.ConnectionSlow += () => text.Text += "Connection problems.\r\n";
```



## How to handle errors
If you don't explicitly enable detailed error messages on the server, the exception object that SignalR returns after an error contains minimal information about the error. For example, if a call to `SendMessage` fails, the error message in the error object contains *"There was an error invoking Hub method 'ChatHub.SendMessage'."* Sending detailed error messages to clients in production is not recommended for security reasons, but can be enabled by setting the `HubConfiguration.EnabledDetailedErrors` property on the server.

To handle errors that SignalR raises, you can add a handler for the Error event on the connection object.

```
hubConnection.Error += ex => text.Text += string.Format("SignalR error: {0}\r\n", ex.Message);
```

To handle errors from method invocations, wrap the code in a try-catch block. 

```
try
{
    var users = await chatHubProxy.Invoke<IEnumerable<ChatUser>>("GetAllUsers");
    foreach (var user in users)
        text.Text += string.Format("{0} : {1}", user.Name, user.Status);
}
catch (Exception ex)
{
    Console.WriteLine("Error invoking GetAllUsers: {0}", ex.Message);
}
```

## Learn More
You can read more about ASP.NET SignalR by visiting http://www.asp.net/signalr
