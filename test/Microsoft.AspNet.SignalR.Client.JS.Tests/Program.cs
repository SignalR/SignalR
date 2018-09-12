// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");

            var startOptions = new StartOptions();

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-u":
                    case "--url":
                        i += 1;
                        var url = args[i];
                        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            Console.Error.WriteLine($"Value is not a valid absolute url: {url}");
                            return 1;
                        }
                        startOptions.Urls.Add(uri.ToString());
                        break;
                    case "--azure-signalr":
                        i += 1;
                        // Hacky settings :)
                        Startup.AzureSignalRConnectionString = args[i];
                        Console.WriteLine("Using Azure SignalR");
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown option: {args[i]}");
                        return 1;
                }
            }

            if (startOptions.Urls.Count == 0)
            {
                startOptions.Urls.Add("http://localhost:8989");
            }

            var shutdownTcs = new TaskCompletionSource<object>();
            Console.CancelKeyPress += (sender, a) =>
            {
                if (shutdownTcs.Task.IsCompleted)
                {
                    Console.WriteLine("Terminating process");
                    a.Cancel = false;
                }
                else
                {
                    Console.WriteLine("Shutting down, press Ctrl-C again to terminate forcibly.");
                    shutdownTcs.TrySetResult(null);
                    a.Cancel = true;
                }
            };

            try
            {
                using (WebApp.Start<Startup>(startOptions))
                {
                    foreach (var url in startOptions.Urls)
                    {
                        Console.WriteLine($"Now listening on {url}");
                    }
                    Console.WriteLine("Server started, press Ctrl-C to shut down.");
                    await shutdownTcs.Task;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An error occurred:");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}
