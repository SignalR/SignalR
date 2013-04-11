using System;
using Microsoft.AspNet.SignalR.Client.Hubs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Samples
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

        private static void RunHeaderAuthSample(HubConnection hubConnection)
        {
            var authHub = hubConnection.CreateHubProxy("HeaderAuthHub");
            hubConnection.Headers.Add("username", "john");
            authHub.On("display", (msg) => Console.WriteLine(msg));
            hubConnection.Start().Wait();
        }

        private static void RunDemoHub(HubConnection hubConnection)
        {
            var demo = hubConnection.CreateHubProxy("demo");

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
                using (var error = task.Exception.GetError())
                {
                    Console.WriteLine(error);
                }

            }, TaskContinuationOptions.OnlyOnFaulted);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(7000);
                hubConnection.Stop();
            });
        }

        private static void RunStreamingSample()
        {
            var connection = new Connection("http://localhost:40476/raw-connection");

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
                using (var error = e.GetError())
                {
                    Console.Error.WriteLine(error);
                }
                Console.Error.WriteLine("=======================");
            };


            Console.WriteLine("Choose transport:");
            Console.WriteLine("1. AutoTransport");
            Console.WriteLine("2. WebSocketsTransort");
            Console.WriteLine("3. ServerSentEventsTransport");
            Console.WriteLine("4. LongPollingTransport");
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
                startTask = connection.Start(new Client.Transports.WebSocketTransport());
            }
            else if (key.Key == ConsoleKey.D3)
            {
                startTask = connection.Start(new Client.Transports.ServerSentEventsTransport());
            }
            else if (key.Key == ConsoleKey.D4)
            {
                startTask = connection.Start(new Client.Transports.LongPollingTransport());
            }

            var wh = new ManualResetEvent(false);
            startTask.ContinueWith(task =>
            {
                try
                {
                    task.Wait();

                    Console.WriteLine("Using {0}", connection.Transport.Name);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("========ERROR==========");
                    using (var error = ex.GetError())
                    {
                        Console.Error.WriteLine(error);
                    }
                    Console.Error.WriteLine("=======================");
                    return;
                }

                string line = null;
                while ((line = Console.ReadLine()) != null)
                {
                    connection.Send(new { type = 1, value = line }).Wait();
                }

                wh.Set();
            });

            wh.WaitOne();
        }

#if !NET35
        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnected(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} Connected", connectionId);
                return base.OnConnected(request, connectionId);
            }

            protected override Task OnReconnected(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} Reconnected", connectionId);
                return base.OnReconnected(request, connectionId);
            }

            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }
#endif
    }
}
