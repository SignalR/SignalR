﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client40.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var writer = Console.Out;
            var client = new Client(writer);
            client.Run("http://localhost:40476/");

            Console.ReadKey();
        }
    }
}
