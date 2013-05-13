using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    public class CommonClient
    {
        private TextWriter _traceWriter;

        public CommonClient(TextWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public async Task RunAsync()
        {
            // url can't be localhost because Windows Phone emulator runs in a separate virtual machine. Therefore, server is located
            // in another machine
            string url = "http://signalr01.cloudapp.net/";

            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.TraceLevel = TraceLevels.All;

            var hubProxy = hubConnection.CreateHubProxy("HubConnectionAPI");
            hubProxy.On<string>("displayMessage", (data) => hubConnection.TraceWriter.WriteLine(data));

            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            await hubProxy.Invoke("DisplayMessageCaller", "Hello Caller!");
            
            string joinGroupResponse = await hubProxy.Invoke<string>("JoinGroup", hubConnection.ConnectionId, "CommonClientGroup");
            hubConnection.TraceWriter.WriteLine("joinGroupResponse={0}", joinGroupResponse);

            await hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members!");
        }
    }    
}

