using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Hubs;
#if !NET35
using SignalR.Hosting.Memory;
#endif

namespace SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
#if !NET35
            // RunInMemoryHost();
#endif

            //var hubConnection = new HubConnection("http://localhost:40476/");

            //RunDemoHub(hubConnection);

            RunStreamingSample();

            Console.ReadKey();
        }

#if !NET35
        private static void RunInMemoryHost()
        {
            var host = new MemoryHost();
            host.MapConnection<MyConnection>("/echo");

            var connection = new Connection("http://foo/echo");

            connection.Received += data =>
            {
                Console.WriteLine(data);
            };

            connection.Start(host).Wait();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (true)
                    {
                        connection.Send(DateTime.Now.ToString());

                        Thread.Sleep(2000);
                    }
                }
                catch
                {

                }
            });
        }
#endif

        private static void RunDemoHub(HubConnection hubConnection)
        {
            var demo = hubConnection.CreateProxy("demo");

            demo.On<int>("invoke", i =>
            {
                Console.WriteLine("{0} client state index -> {1}", i, demo["index"]);
            });

            hubConnection.Start().Wait();


            demo.Invoke("multipleCalls").ContinueWith(task =>
            {
                Console.WriteLine(task.Exception);

            }, TaskContinuationOptions.OnlyOnFaulted);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(7000);
                hubConnection.Stop();
            });
        }

        private static void RunStreamingSample()
        {
            var connection = new Connection("http://localhost:40476/Raw/raw");

            connection.Received += data =>
            {
                Console.WriteLine(data);
            };

            connection.Reconnected += () =>
            {
                Console.WriteLine("[{0}]: Connection restablished", DateTime.Now);
            };

            connection.Error += e =>
            {
                Console.WriteLine(e);
            };

            connection.Start().Wait();
        }

#if !NET35
        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnectedAsync(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} Connected", connectionId);
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnReconnectedAsync(IRequest request, System.Collections.Generic.IEnumerable<string> groups, string connectionId)
            {
                Console.WriteLine("{0} Reconnected", connectionId);
                return base.OnReconnectedAsync(request, groups, connectionId);
            }

            protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }
#endif
    }
}
