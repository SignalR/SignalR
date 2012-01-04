using System;
using SignalR.Client.Hubs;

namespace SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            RunStreamingSample();

            var hubConnection = new HubConnection("http://localhost:40476/");

            RunDemoHub(hubConnection);

            hubConnection.Start().Wait();

            Console.ReadKey();
        }

        private static void RunDemoHub(HubConnection hubConnection)
        {
            var demo = hubConnection.CreateProxy("SignalR.Samples.Hubs.DemoHub.DemoHub");

            demo.On("fromArbitraryCode", value =>
            {
                Console.WriteLine("Sending {0} from arbitrary code without the hub itself!", value);
            });
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
