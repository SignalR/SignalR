using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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

        public async Task RunAsync(string url)
        {
            try
            {
                await RunDemo(url);
            }
            catch (HttpClientException httpClientException)
            {
                _traceWriter.WriteLine("HttpClientException: {0}", httpClientException.Response);
                throw;
            }
            catch (Exception exception)
            {
                _traceWriter.WriteLine("Exception: {0}", exception);
                throw;
            }
        }

        private async Task RunHubConnectionAPI(string url)
        {
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
            hubConnection.TraceWriter.WriteLine("leaveGroupResponse={0}", leaveGroupResponse);

            await hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members! (caller should not see this message)");

            await hubProxy.Invoke("DisplayMessageCaller", "Hello Caller again!");
        }

        private async Task RunDemo(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            var hubProxy = hubConnection.CreateHubProxy("demo");
            hubProxy.On<int>("invoke", (i) =>
            {
                int n = hubProxy.GetValue<int>("index");
                hubConnection.TraceWriter.WriteLine("{0} client state index -> {1}", i, n);
            });

            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            hubConnection.TraceWriter.WriteLine("Invoking long running hub method with progress...");
            var result = await hubProxy.Invoke<string, int>("ReportProgress",
                percent => hubConnection.TraceWriter.WriteLine("{0}% complete", percent),
                /* jobName */ "Long running job");
            hubConnection.TraceWriter.WriteLine("{0}", result);

            await hubProxy.Invoke("multipleCalls");
        }

        private async Task RunRawConnection(string serverUrl)
        {
            string url = serverUrl + "raw-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            await connection.Start();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);

            await connection.Send(new { type = 1, value = "first message" });
            await connection.Send(new { type = 1, value = "second message" });
        }


        private async Task RunStreaming(string serverUrl)
        {
            string url = serverUrl + "streaming-connection";

            var connection = new Connection(url);
            connection.TraceWriter = _traceWriter;

            await connection.Start();
            connection.TraceWriter.WriteLine("transport.Name={0}", connection.Transport.Name);
        }

        private async Task RunAuth(string serverUrl)
        {
            string url = serverUrl + "cookieauth";

            var handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            using (var httpClient = new HttpClient(handler))
            {
                var content = string.Format("UserName={0}&Password={1}", "user", "password");
                var response = httpClient.PostAsync(url + "/Account/Login", new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")).Result;
            }

            var connection = new Connection(url + "/echo");
            connection.TraceWriter = _traceWriter;
            connection.Received += (data) => connection.TraceWriter.WriteLine(data);
#if !ANDROID && !iOS
            connection.CookieContainer = handler.CookieContainer;
#endif
            await connection.Start();
            await connection.Send("sending to AuthenticatedEchoConnection");

            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
#if !ANDROID && !iOS
            hubConnection.CookieContainer = handler.CookieContainer;
#endif
            var hubProxy = hubConnection.CreateHubProxy("AuthHub");
            hubProxy.On<string, string>("invoked", (connectionId, date) => hubConnection.TraceWriter.WriteLine("connectionId={0}, date={1}", connectionId, date));

            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            await hubProxy.Invoke("InvokedFromClient");
        }

        private async Task RunWindowsAuth(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;

            // Windows Auth is not supported on SL and WindowsStore apps
#if !SILVERLIGHT && !NETFX_CORE && !ANDROID && !iOS
            hubConnection.Credentials = CredentialCache.DefaultCredentials;
#endif
            var hubProxy = hubConnection.CreateHubProxy("AuthHub");
            hubProxy.On<string, string>("invoked", (connectionId, date) => hubConnection.TraceWriter.WriteLine("connectionId={0}, date={1}", connectionId, date));

            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);

            await hubProxy.Invoke("InvokedFromClient");
        }

        private async Task RunHeaderAuthHub(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.Headers.Add("username", "john");

            var hubProxy = hubConnection.CreateHubProxy("HeaderAuthHub");
            hubProxy.On<string>("display", (msg) => hubConnection.TraceWriter.WriteLine(msg));

            await hubConnection.Start();
            hubConnection.TraceWriter.WriteLine("transport.Name={0}", hubConnection.Transport.Name);
        }

        private async Task RunPendingCallbacks(string url)
        {
            var hubConnection = new HubConnection(url);
            hubConnection.TraceWriter = _traceWriter;
            hubConnection.TraceLevel = TraceLevels.StateChanges;

            var hubProxy = hubConnection.CreateHubProxy("LongRunningHub");
            ManualResetEvent event1 = new ManualResetEvent(false);
            ManualResetEvent event2 = new ManualResetEvent(false);

            int callbacks = 1000;
            int counter = 0;
            hubProxy.On<int>("serverIsWaiting", (i) =>
            {
                if (i % 100 == 0)
                {
                    hubConnection.TraceWriter.WriteLine("{0} serverIsWaiting: {1}", DateTime.Now, i);
                }

                if (i == callbacks)
                {
                    event1.Set();
                }
            });

            await hubConnection.Start();
            await hubProxy.Invoke("Reset");

            hubConnection.TraceWriter.WriteLine("check memory size before sending longRunning");

            for (int messageNumber = 1; messageNumber <= callbacks; messageNumber++)
            {
#pragma warning disable 4014
                hubProxy.Invoke("LongRunningMethod", messageNumber).ContinueWith(task =>
                {
                    int i = Interlocked.Increment(ref counter);
                    if (i % 100 == 0)
                    {
                        hubConnection.TraceWriter.WriteLine("{0} completed: {1} task.Status={2}", DateTime.Now, i, task.Status);
                    }

                    if (i == callbacks)
                    {
                        event2.Set();
                    }
                });
#pragma warning restore 4014
            }

            await Task.Factory.StartNew(() => event1.WaitOne());
            hubConnection.TraceWriter.WriteLine("check memory size after sending longRunning");
            await hubProxy.Invoke("Set");
            await Task.Factory.StartNew(() => event2.WaitOne());
            hubConnection.TraceWriter.WriteLine("check memory size after all callbacks completed");
        }
    }
}

