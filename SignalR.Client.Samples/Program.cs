using System;
using SignalR.Client.Hubs;

namespace SignalR.Client.Samples
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
            var demo = hubConnection.CreateProxy("SignalR.Samples.Hubs.DemoHub.DemoHub");

            demo.On("invoke", i =>
            {
                Console.WriteLine("{0} client state index -> {1}", i, demo["index"]);
            });

            hubConnection.Start().Wait();

            demo.Invoke("multipleCalls").Wait();
        }

        private static void RunStreamingSample()
        {
            var connection = new Connection("http://localhost:40476/Streaming/streaming");

            connection.Received += data =>
            {
                Console.WriteLine(data);
            };

            connection.Error += e =>
            {
                Console.WriteLine(e);
            };

            connection.Start().Wait();
        }
    }
}
