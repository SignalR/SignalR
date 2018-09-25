// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return Task.FromResult(0);
            }

            switch (args[0])
            {
                case "chat":
                    return ChatSample.SampleMain(args.Skip(1).ToArray());
                default:
                    PrintUsage();
                    Console.Error.WriteLine($"Unknown subcommand: {args[0]}.");
                    return Task.FromResult(1);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("ASP.NET SignalR Sample .NET Client");
            Console.WriteLine();
            Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name} <SAMPLE> <SAMPLE ARGS...>");
            Console.WriteLine();
            Console.WriteLine("Samples:");
            Console.WriteLine("  chat - A chat sample");
            Console.WriteLine();
        }

    }
}
