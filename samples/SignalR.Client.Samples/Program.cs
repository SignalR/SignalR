using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;

namespace SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            // RunInMemoryHost();

            // var hubConnection = new HubConnection("http://localhost:40476/");

            //RunDemoHub(hubConnection);

            RunStreamingSample();

            Console.ReadKey();
        }

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
        private static void RunDemoHub(HubConnection hubConnection)
        {
            var demo = hubConnection.CreateProxy("demo");

            demo.On("invoke", i =>
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
            var connection = new Connection("http://localhost:40476/streaming/streaming");

            connection.Received += data =>
            {
                Console.WriteLine(data);
            };

            connection.Reconnected += () =>
            {
                Console.WriteLine("[{0}]: Connection restablished", DateTime.Now);
            };

            connection.StateChanged += change =>
            {
                Console.WriteLine("{0} => {1}", change.OldState, change.NewState);
            };

            connection.Error += e =>
            {
                Console.WriteLine(e);
            };

            connection.Start().Wait();
        }

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
    }
}
