using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Hubs;
#if !NET35
using SignalR.Hosting.Memory;
#endif
using System.Diagnostics;

namespace SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
#if !NET35
            // RunInMemoryHost();
#endif

            // var hubConnection = new HubConnection("http://localhost:40476/");

            // RunDemoHub(hubConnection);

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

            connection.StateChanged += change =>
            {
                Console.WriteLine(change.OldState + " => " + change.NewState);
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

            hubConnection.StateChanged += change =>
            {
                Console.WriteLine(change.OldState + " => " + change.NewState);
            };

            demo.On<int>("invoke", i =>
            {
                int n = demo.GetValue<int>("index");
                Console.WriteLine("{0} client state index -> {1}", i, n);
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

            connection.StateChanged += change =>
            {
                Console.WriteLine(change.OldState + " => " + change.NewState);
            };

            connection.Error += e =>
            {
                Console.Error.WriteLine("========ERROR==========");
                Console.Error.WriteLine(e.GetBaseException());
                Console.Error.WriteLine("=======================");
            };


            Console.WriteLine("Choose transport:");
            Console.WriteLine("1. AutoTransport");
            Console.WriteLine("2. ServerSentEventsTransport");
            Console.WriteLine("3. LongPollingTransport");
            Console.Write("Option: ");

            Task startTask = null;

            var key = Console.ReadKey(false);
            Console.WriteLine();

            if (key.Key == ConsoleKey.D1)
            {
                startTask = connection.Start();
            }
            else if (key.Key == ConsoleKey.D2)
            {
                startTask = connection.Start(new Client.Transports.ServerSentEventsTransport());
            }
            else if (key.Key == ConsoleKey.D3)
            {
                startTask = connection.Start(new Client.Transports.LongPollingTransport());
            }

            var wh = new ManualResetEvent(false);
            startTask.ContinueWith(task =>
            {
                try
                {
                    task.Wait();
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine("========ERROR==========");
                    Console.Error.WriteLine(ex.GetBaseException());
                    Console.Error.WriteLine("=======================");
                    return;
                }

                string line = null;
                while ((line = Console.ReadLine()) != null)
                {
                    connection.Send(new { type = 1, value = line });
                }

                wh.Set();
            });

            wh.WaitOne();
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
