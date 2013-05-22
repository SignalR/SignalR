using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            RunRawConnection();

            Console.ReadKey();
        }

        private static void RunStatusHub()
        {
            var hubConnection = new HubConnection("http://localhost:40476/");
            var proxy = hubConnection.CreateHubProxy("statushub");

            proxy.On<string,string>("joined", (connectionId, date) =>
            {
                 Console.WriteLine(connectionId + " joined on "+date);   
            });

            Console.Read();

            hubConnection.Start().Wait();
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

        private static void RunRawConnection()
        {
            var connection = new Connection("http://localhost:40476/raw-connection");

            Console.WriteLine("URL: {0}", connection.Url);

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
#if NET40
                Console.WriteLine("No .NET4 websockets");
                return;
#else
                startTask = connection.Start(new Client.Transports.WebSocketTransport());
#endif
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
    }
}
