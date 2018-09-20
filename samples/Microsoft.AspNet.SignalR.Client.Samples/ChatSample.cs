using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    public static class ChatSample
    {
        private static readonly string DefaultHub = "EchoHub";

        public static async Task<int> SampleMain(string[] args)
        {
            string url = null;
            string hub = null;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-u":
                    case "--url":
                        if(!TryReadArg(args, ref i, out url))
                        {
                            return 1;
                        }
                        break;
                    case "-h":
                    case "--hub":
                        if(!TryReadArg(args, ref i, out hub))
                        {
                            return 1;
                        }
                        break;
                }
            }

            if(string.IsNullOrEmpty(url))
            {
                Console.Error.WriteLine("Missing required argument '--url'");
                return 1;
            }

            Console.Write("Enter your name: ");
            var name = Console.ReadLine();

            hub = string.IsNullOrEmpty(hub) ? DefaultHub : hub;

            var connection = new HubConnection(url);
            var proxy = connection.CreateHubProxy(hub);

            proxy.On<string, string>("receive", (user, message) =>
            {
                Console.WriteLine($"{user}: {message}");
            });

            Console.WriteLine("Connecting...");
            await connection.Start();
            Console.WriteLine("Connected. Start chatting! Type 'exit' to exit.");

            var running = true;
            while(running)
            {
                Console.Write("> ");
                var message = Console.ReadLine();
                if(string.Equals(message, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    running = false;
                }
                else
                {
                    await proxy.Invoke("Broadcast", name, message);
                }
            }

            connection.Stop();
            return 0;
        }

        private static bool TryReadArg(string[] args, ref int index, out string value)
        {
            if (index >= args.Length - 1)
            {
                Console.Error.WriteLine($"Missing value for argument '{args[index]}'");
                value = null;
                return false;
            }

            index += 1;
            value = args[index];
            return true;
        }
    }
}
