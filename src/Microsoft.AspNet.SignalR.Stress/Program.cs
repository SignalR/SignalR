// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using CmdLine;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;
using Microsoft.AspNet.SignalR.StressServer.Hubs;

namespace Microsoft.AspNet.SignalR.Stress
{
    class Program
    {
        static void Main(string[] args)
        {
            IRun run = CreateRun();

            long memory = 0;

            // When you specify the incorrect run name, the CreateRun() will throw exception
            using (run)
            {
                run.Run();

                memory = GC.GetTotalMemory(forceFullCollection: false);

                Console.WriteLine("Before GC {0}", Utility.FormatBytes(memory));

                memory = GC.GetTotalMemory(forceFullCollection: true);

                Console.WriteLine("After GC and before dispose {0}", Utility.FormatBytes(memory));
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

        private static IRun CreateRun()
        {
            ThreadPool.SetMinThreads(32, 32);

            var args = ParseArguments();

            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(Program).Assembly));

            compositionContainer.ComposeExportedValue(new RunData
            {
                SampleRate = args.SampleRate,
                Warmup = args.Warmup,
                Duration = args.Duration,
                Connections = args.Connections,
                Payload = GetPayload(args.PayloadSize),
                Senders = args.Senders,
                Transport = args.Transport,
                Host = args.Host,
                Url = args.Url,
                SendDelay = args.SendDelay,

                // Scaleout
                RedisServer = args.RedisServer,
                RedisPort = args.RedisPort,
                RedisPassword = args.RedisPassword,
                ServiceBusConnectionString = args.ServiceBusConnectionString,
                SqlConnectionString = args.SqlConnectionString,
                SqlTableCount = args.SqlTableCount,
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

            [CommandLineParameter(Command = "Url", Required = false, Default = "http://localhost", Description = "URL for external host.")]
            public string Url { get; set; }

            [CommandLineParameter(Command = "Run", Required = false, Default = "SendReceive", Description = "The type of run to perform. Default: SendReceive")]
            public string RunName { get; set; }

            [CommandLineParameter(Command = "SampleRate", Required = false, Default = 500, Description = "The sampling rate in miliseconds. Default: 500")]
            public int SampleRate { get; set; }

            [CommandLineParameter(Command = "SendDelay", Required = false, Default = 0, Description = "The send delay in miliseconds. Default: 0")]
            public int SendDelay { get; set; }

            [CommandLineParameter(Command = "Connections", Required = false, Default = 5000, Description = "Number of connections. Default: 5000")]
            public int Connections { get; set; }

            [CommandLineParameter(Command = "PayloadSize", Required = false, Default = 32, Description = "Payload size in bytes. Default: 32")]
            public int PayloadSize { get; set; }

            [CommandLineParameter(Command = "Senders", Required = false, Default = 1, Description = "Number of senders. Default: 1")]
            public int Senders { get; set; }

            [CommandLineParameter(Command = "Transport", Required = false, Default = "serverSentEvents", Description = "Transport name. Default: serverSentEvents")]
            public string Transport { get; set; }

            [CommandLineParameter(Command = "Host", Required = false, Default = "Memory", Description = "Host type name ( Memory, IISExpress, Owin, External ). Default: Memory")]
            public string Host { get; set; }

            [CommandLineParameter(Command = "Duration", Required = false, Default = 30, Description = "Duration in seconds. Default: 30")]
            public int Duration { get; set; }

            [CommandLineParameter(Command = "Warmup", Required = false, Default = 10, Description = "Warmup duration in seconds. Default: 10")]
            public int Warmup { get; set; }

            [CommandLineParameter(Command = "RedisPassword", Required = false, Default = "", Description = "Redis password to use. Default: empty")]
            public string RedisPassword { get; set; }

            [CommandLineParameter(Command = "RedisServer", Required = false, Default = "127.0.0.1", Description = "Redis server to use. Default: 127.0.0.1")]
            public string RedisServer { get; set; }

            [CommandLineParameter(Command = "RedisPort", Required = false, Default = 6379, Description = "Redis port to use. Default: 6379")]
            public int RedisPort { get; set; }

            [CommandLineParameter(Command = "ServiceBusConnectionString", Required = false, Default = "", Description = "ServiceBus connection string to use. Default: empty")]
            public string ServiceBusConnectionString { get; set; }

            [CommandLineParameter(Command = "SqlConnectionString", Required = false, Default = "Data Source=(local);Initial Catalog=SignalRSamples;Integrated Security=SSPI;MultipleActiveResultSets=true;Asynchronous Processing=True;", Description = "Warmup duration in seconds. Default: Local sql server")]
            public string SqlConnectionString { get; set; }

            [CommandLineParameter(Command = "SqlTableCount", Required = false, Default = 1, Description = "The number of tables to store messages in the SqlServer DB")]
            public int SqlTableCount { get; set; }
        }
    }
}
