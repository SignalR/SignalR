// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using CmdLine;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    class Program
    {
        static void Main(string[] args)
        {
            var run = CreateRun();
            long memory = 0;

            using (run)
            {
                run.Run();
                Console.WriteLine("Warming up: " + run.Warmup);
                Thread.Sleep(run.Warmup * 1000);

                Console.WriteLine("Test started: " + run.Duration);
                run.Sample();
                Thread.Sleep(run.Duration * 1000);
                run.Sample();
                Console.WriteLine("Test finished");

                memory = GC.GetTotalMemory(forceFullCollection: false);

                Console.WriteLine("Before GC {0}", Utility.FormatBytes(memory));

                memory = GC.GetTotalMemory(forceFullCollection: true);

                Console.WriteLine("After GC and before dispose {0}", Utility.FormatBytes(memory));
                run.Record();
            }

            memory = GC.GetTotalMemory(forceFullCollection: true);

            Console.WriteLine("After GC and dispose {0}", Utility.FormatBytes(memory));
        }

        private static StressArguments ParseArguments()
        {
            StressArguments args = null;
            try
            {
                args = CommandLine.Parse<StressArguments>();
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.ArgumentHelp.Message);
                Console.WriteLine(e.ArgumentHelp.GetHelpText(Console.BufferWidth));
                Environment.Exit(1);
            }
            return args;
        }

        private static RunBase CreateRun()
        {
            ThreadPool.SetMinThreads(32, 32);

            var args = ParseArguments();

            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(Program).Assembly));

            compositionContainer.ComposeExportedValue(new RunData
            {
                Warmup = args.Warmup,
                Duration = args.Duration,
                Connections = args.Connections,
                Payload = GetPayload(args.PayloadSize),
                Senders = args.Senders,
                Transport = args.Transport
            });

            return (RunBase)compositionContainer.GetExportedValue<IRun>(args.RunName);
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

            [CommandLineParameter(Command = "Run", Required = false, Default = "MemoryHost", Description = "The type of run to perform (MemoryHost, MessageBus, ConnectionRun, RedisMessageBus). Default: MemoryHost")]
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
