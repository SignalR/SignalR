using System;
using System.Threading;
using SignalR.Client.Hubs;

namespace SignalR.Client.Net20.Samples
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var hubConnection = new HubConnection("http://localhost:40476/");

			RunDemoHub(hubConnection);

			RunStreamingSample();

			Console.ReadKey();
		}

		private static void RunDemoHub(HubConnection hubConnection)
		{
			var demo = hubConnection.CreateProxy("demo");

			demo.Subscribe("invoke").Data += i =>
			                  	{
			                  		Console.WriteLine("{0} client state index -> {1}", i[0], demo["index"]);
			                  	};

			hubConnection.Start().FollowedBy(_ =>
			                                 	{
			                                 		demo.Invoke("multipleCalls").OnFinish += (sender, e) =>
			                                 		                                         	{
			                                 		                                         		if (e.ResultWrapper.IsFaulted)
			                                 		                                         			Console.WriteLine(
			                                 		                                         				e.ResultWrapper.Exception);
			                                 		                                         	};

			                                 		
			                                 	});

			ThreadPool.QueueUserWorkItem(o =>
			{
				Thread.Sleep(70000);
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

			connection.Start();
		}
	}
}
