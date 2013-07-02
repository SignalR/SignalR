using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client40.Samples
{
    public class Client
    {
        private TextWriter _traceWriter;

        public Client(TextWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public void Run(string url)
        {
            try
            {
                RunHubConnectionAPI(url);
            }
            catch (Exception exception)
            {
                _traceWriter.WriteLine("Exception: {0}", exception);
                throw;
            }
        }       

        private void RunHubConnectionAPI(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            var hubProxy = hubConnection.CreateHubProxy("HubConnectionAPI");
            hubProxy.On<string>("displayMessage", (data) => hubConnection.TraceWriter.WriteLine(data));
            
            hubConnection.Start().Wait();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            hubProxy.Invoke("DisplayMessageCaller", "Hello Caller!").Wait();

            string joinGroupResponse = hubProxy.Invoke<string>("JoinGroup", hubConnection.ConnectionId, "CommonClientGroup").Result;
            hubConnection.TraceWriter.WriteLine("joinGroupResponse={0}", joinGroupResponse);
            
            hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members!").Wait();

            string leaveGroupResponse = hubProxy.Invoke<string>("LeaveGroup", hubConnection.ConnectionId, "CommonClientGroup").Result;
            hubConnection.TraceWriter.WriteLine("leaveGroupResponse={0}", leaveGroupResponse);

            hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members! (caller should not see this message)").Wait();

            hubProxy.Invoke("DisplayMessageCaller", "Hello Caller again!").Wait();
        }

        private void RunDemo(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            var hubProxy = hubConnection.CreateHubProxy("demo");
            hubProxy.On<int>("invoke", (i) => 
            {
                int n = hubProxy.GetValue<int>("index");
                hubConnection.TraceWriter.WriteLine("{0} client state index -> {1}", i, n);
            });

            hubConnection.Start().Wait();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            hubProxy.Invoke("multipleCalls").Wait();
        }

        private void RunRawConnection(string serverUrl)
        {
            string url = serverUrl + "raw-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            connection.Start().Wait();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);

            connection.Send(new { type = 1, value = "first message" }).Wait();
            connection.Send(new { type = 1, value = "second message" }).Wait();
        }


        private void RunStreaming(string serverUrl)
        {
            string url = serverUrl + "streaming-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            connection.Start().Wait();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);
        }

        private void RunBasicAuth(string serverUrl)
        {
            string url = serverUrl + "basicauth";

            var connection = new Connection(url + "/echo");
            connection.TraceWriter = _traceWriter;
            connection.Received += (data) => connection.TraceWriter.WriteLine(data);
            connection.Credentials = new NetworkCredential("user", "password");
            connection.Start().Wait();
            connection.Send("sending to AuthenticatedEchoConnection").Wait();

            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.Credentials = new NetworkCredential("user", "password");

            var hubProxy = hubConnection.CreateHubProxy("AuthHub");
            hubProxy.On<string, string>("invoked", (connectionId, date) => hubConnection.TraceWriter.WriteLine("connectionId={0}, date={1}", connectionId, date));

            hubConnection.Start().Wait();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            hubProxy.Invoke("InvokedFromClient").Wait();
        }

        private void RunWindowsAuth(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            hubConnection.Credentials = CredentialCache.DefaultCredentials;

            var hubProxy = hubConnection.CreateHubProxy("AuthHub");
            hubProxy.On<string, string>("invoked", (connectionId, date) => hubConnection.TraceWriter.WriteLine("connectionId={0}, date={1}", connectionId, date));

            hubConnection.Start().Wait();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            hubProxy.Invoke("InvokedFromClient").Wait();
        }

        private void RunHeaderAuthHub(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.Headers.Add("username", "john");

            var hubProxy = hubConnection.CreateHubProxy("HeaderAuthHub");
            hubProxy.On<string>("display", (msg) => hubConnection.TraceWriter.WriteLine(msg));

            hubConnection.Start().Wait();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);
        }   
    }    
}

