ASP.NET SignalR is a new library for ASP.NET developers that simplifies the process of adding 
real-time web functionality to your applications. Real-time web functionality is the ability 
to have server-side code push content to connected clients instantly as it becomes available.

```csharp

using Microsoft.AspNet.SignalR.Client;

...
  public void Run()

  {

    var hubConnection = new HubConnection("url");
    var hubProxy = hubConnection.CreateHubProxy("HubName");
    hubProxy.On<string>("receivingData", (data) => hubConnection.TraceWriter.WriteLine(data));
    
    await hubConnection.Start();
    await hubProxy.Invoke("sendingData", "Hello World!");
  }

```



