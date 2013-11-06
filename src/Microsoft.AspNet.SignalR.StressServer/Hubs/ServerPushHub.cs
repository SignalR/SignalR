// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.StressServer.Hubs
{
    public class ServerPushHub : Hub
    {
        private bool _sending;

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(StartSendLoop, this);
        }

        public void Stop()
        {
            _sending = false;
        }

        private static void StartSendLoop(object state)
        {
            var hub = (ServerPushHub)state;

            hub._sending = true;
            do
            {
                hub.Clients.All.send("message");
            }
            while (hub._sending);
        }
    }
}
