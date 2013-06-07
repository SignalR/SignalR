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
            await RunHubConnectionAPI();
        }

        private async Task RunHubConnectionAPI()
        {
            // Url can't be localhost because Windows Phone emulator runs in a separate virtual machine. Therefore, server is located
            // in another machine
            string url = "http://signalr01.cloudapp.net/";

            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            var hubProxy = hubConnection.CreateHubProxy("HubConnectionAPI");
            hubProxy.On<string>("displayMessage", (data) => hubConnection.TraceWriter.WriteLine(data));
            
            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            await hubProxy.Invoke("DisplayMessageCaller", "Hello Caller!");

            string joinGroupResponse = await hubProxy.Invoke<string>("JoinGroup", hubConnection.ConnectionId, "CommonClientGroup");
            hubConnection.TraceWriter.WriteLine("joinGroupResponse={0}", joinGroupResponse);
            
            await hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members!");

            string leaveGroupResponse = await hubProxy.Invoke<string>("LeaveGroup", hubConnection.ConnectionId, "CommonClientGroup");
            hubConnection.TraceWriter.WriteLine("leaveGroupResponse={0}", joinGroupResponse);

            await hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members! (caller should not see this message)");

            await hubProxy.Invoke("DisplayMessageCaller", "Hello Caller again!");
        }

        private async Task RunRawConnection()
        {
            string url = "http://signalr01.cloudapp.net/raw-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            await connection.Start();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);

            await connection.Send(new { type = 1, value = "first message" });
            await connection.Send(new { type = 1, value = "second message" });
        }


        private async Task RunStreaming()
        {
            string url = "http://signalr01.cloudapp.net:81/streaming-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            await connection.Start();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);
        }
    }    
}

