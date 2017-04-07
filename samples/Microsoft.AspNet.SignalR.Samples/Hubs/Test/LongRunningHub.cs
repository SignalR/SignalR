// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Test
{
    public class LongRunningHub : Hub
    {
        private static ManualResetEvent myEvent = new ManualResetEvent(false);

        public void Set()
        {
            myEvent.Set();
        }

        public void Reset()
        {
            myEvent.Reset();
        }

        public Task LongRunningMethod(int i)
        {
            Clients.Caller.serverIsWaiting(i);

            return Task.Run(() =>
            {
                myEvent.WaitOne();
            });
        }
    }
}