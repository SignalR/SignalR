using System;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client.Sample
{
    public class CommonClient
    {
        private TextWriter _traceWriter;

        public CommonClient(TextWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public async void RunAsync()
        {
            // url can't be localhost because Windows Phone emulator runs in a separate virtual machine. Therefore, server is located
            // in another machine
            string url = "http://signalr01.cloudapp.net/";

            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.TraceLevel = TraceLevels.All;

            var hubProxy = hubConnection.CreateHubProxy("TestHub");
            hubProxy.On<string>("received", (data) => hubConnection.TraceWriter.WriteLine(data)); 

            await hubConnection.Start();
            await hubProxy.Invoke("SendToCaller", "Hello!");
        }
    }
}

