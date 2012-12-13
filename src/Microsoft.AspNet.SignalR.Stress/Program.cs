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
            IDisposable run = CreateRun(args);
            long memory = 0;

            using (run)
            {
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

        private static IDisposable CreateRun(string[] args)
        {
            // TODO: Parse arguments

            ThreadPool.SetMinThreads(32, 32);

            int connections = 5000;
            // int senders = 1;
            string payload = GetPayload();

            // return MessageBusRun.Run(connections, senders, payload);
            // return ConnectionRun.LongRunningSubscriptionRun(connections, senders, payload);
            // return ConnectionRun.ReceiveLoopRun(connections, senders, payload);
            // return MemoryHostRun.Run(connections, senders, payload, "serverSentEvents");
            return StressRuns.RunConnectDisconnect(connections);
            // return StressRuns.ManyUniqueGroups(concurrency: 4);            
        }

        private static string GetPayload(int n = 32)
        {
            return new string('a', n);
        }
    }
}
