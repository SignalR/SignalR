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
            var writer = Console.Out;
            var client = new CommonClient(writer);
            client.RunAsync("http://localhost:40476/").Wait();

            // var hubConnection = new HubConnection("http://localhost:40476/");

            // RunDemoHub(hubConnection);

            RunStreamingSample();

            //RunStatusHub();

            Console.ReadKey();
        }
    }
}
