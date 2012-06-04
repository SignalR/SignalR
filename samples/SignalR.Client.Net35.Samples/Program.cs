using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Hubs;

namespace SignalR.Client.Net35.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var hubConnection = new HubConnection("http://localhost:40476/");

            RunDemoHub(hubConnection);

            RunStreamingSample();

            Console.ReadKey();
        }

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
    }
}
