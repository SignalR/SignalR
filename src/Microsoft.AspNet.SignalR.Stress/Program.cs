// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    class Program
    {
        static void Main(string[] args)
        {
            IRun run = CreateRun(args);
            long memory = 0;

            using (run)
            {
                run.Run();

                memory = GC.GetTotalMemory(forceFullCollection: false);

                Console.WriteLine("Before GC {0}", Utility.FormatBytes(memory));
                Console.ReadKey();

                memory = GC.GetTotalMemory(forceFullCollection: true);

                Console.WriteLine("After GC and before dispose {0}", Utility.FormatBytes(memory));
                Console.ReadKey();
            }

            memory = GC.GetTotalMemory(forceFullCollection: true);

            Console.WriteLine("After GC and dispose {0}", Utility.FormatBytes(memory));
            Console.ReadKey();
        }

        private static IRun CreateRun(string[] args)
        {
            // TODO: Parse arguments

            ThreadPool.SetMinThreads(32, 32);

            int connections = 5000;
            int senders = 1;
            string payload = GetPayload();

            return new MessageBusRun(connections, senders, payload);
            // return new ConnectionRun(connections, senders, payload);
            // return new MemoryHostRun(connections, senders, payload, "serverSentEvents");
        }

        private static string GetPayload(int n = 32)
        {
            return new string('a', n);
        }

        private class StressArgs
        {
            
        }
    }
}
