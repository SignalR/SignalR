// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using CmdLine;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    class Program
    {
        static void Main(string[] args)
        {
            IRun run = CreateRun();
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

        private static IRun CreateRun()
        {
            ThreadPool.SetMinThreads(32, 32);

            var args = CommandLine.Parse<StressArguments>();

            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(Program).Assembly));

            compositionContainer.ComposeExportedValue(new RunData
            {
                Connections = args.Connections,
                Payload = GetPayload(args.PayloadSize),
                Senders = args.Senders,
                Transport = args.Transport
            });

            return compositionContainer.GetExportedValue<IRun>(args.RunName);
        }

        private static string GetPayload(int n = 32)
        {
            return new string('a', n);
        }

        [CommandLineArguments(Program = "Stress")]
        private class StressArguments
        {
            [CommandLineParameter(Command = "?", Name = "Help", Default = false, Description = "Show Help", IsHelp = true)]
            public bool Help { get; set; }

            [CommandLineParameter(Command = "Run", Required = false, Default = "MemoryHost", Description = "The type of run to perform")]
            public string RunName { get; set; }

            [CommandLineParameter(Command = "Connections", Required = false, Default = 5000, Description = "Number of connections. Default: 5000")]
            public int Connections { get; set; }

            [CommandLineParameter(Command = "PayloadSize", Required = false, Default = 32, Description = "Payload size in bytes. Default: 32")]
            public int PayloadSize { get; set; }

            [CommandLineParameter(Command = "Senders", Required = false, Default = 1, Description = "Number of senders. Default: 1")]
            public int Senders { get; set; }

            [CommandLineParameter(Command = "Transport", Required = false, Default = "serverSentEvents", Description = "Transport name. Default: serverSentEvents")]
            public string Transport { get; set; }

            [CommandLineParameter(Command = "Duration", Required = false, Default = 30, Description = "Duration in seconds. Default: 30")]
            public int Duration { get; set; }

            [CommandLineParameter(Command = "Warmup", Required = false, Default = 10, Description = "Warmup duration in seconds. Default: 10")]
            public int Warmup { get; set; }
        }
    }
}
