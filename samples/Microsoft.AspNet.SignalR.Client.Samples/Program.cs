// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            if (args.Any(a => a == "--debug"))
            {
                args = args.Where(a => a != "--debug").ToArray();
                Console.WriteLine($"Ready for debugger to attach. Process ID: {Process.GetCurrentProcess().Id}.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }
#endif

            var writer = Console.Out;
            var client = new CommonClient(writer);
            client.Run("http://localhost:40476/");

            Console.ReadKey();
        }
    }
}
