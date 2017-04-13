// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.UWP.TestHost
{
    // Used for running End-to-End tests for Store WebSockets transport.
    public class Program
    {
        static void Main(string[] args)
        {
            const string url = "http://localhost:42424";
            using (WebApp.Start(url))
            {
                Console.WriteLine("SignalR host for E2E UWP Client tests running on {0}", url);
                Thread.Sleep(args.Length > 0 ? int.Parse(args[0]) : Timeout.Infinite);
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
