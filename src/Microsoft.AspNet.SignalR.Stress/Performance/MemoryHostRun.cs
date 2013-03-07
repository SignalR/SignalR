// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Connections;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("MemoryHost", typeof(IRun))]
    public class MemoryHostRun : RunBase
    {
        private readonly MemoryHost _host = new MemoryHost();

        [ImportingConstructor]
        public MemoryHostRun(RunData runData)
            : base(runData)
        {
            Transport = runData.Transport;
        }

        public string Transport { get; private set; }

        public override void Run()
        {
            _host.Configure(app =>
            {
                var config = new ConnectionConfiguration
                {
                    Resolver = Resolver
                };

                app.MapConnection<StressConnection>("/echo", config);

                config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            });

            base.Run();
        }

        public override void Dispose()
        {
            base.Dispose();

            _host.Dispose();
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            string connectionId = connectionIndex.ToString();
            if (Transport.Equals("longPolling", StringComparison.OrdinalIgnoreCase))
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    LongPollingLoop((string)state);
                }, 
                connectionId);
            }
            else
            {
                ProcessRequest(connectionId);
            }

            // Abort the request on dispose
            return new DisposableAction(state => Abort((string)state), connectionId);
        }

        protected override Task Send(int senderIndex)
        {
            return ProcessSendRequest(senderIndex.ToString(), Payload);
        }

        private Task ProcessRequest(string connectionToken)
        {
            return _host.Get("http://foo/echo/connect?transport=" + Transport + "&connectionToken=" + connectionToken, disableWrites: true);
        }

        private Task ProcessSendRequest(string connectionToken, string data)
        {
            var postData = new Dictionary<string, string> { { "data", data } };
            return _host.Post("http://foo/echo/send?transport=" + Transport + "&connectionToken=" + connectionToken, postData);
        }

        private Task Abort(string connectionToken)
        {
            return _host.Post("http://foo/echo/abort?transport=" + Transport + "&connectionToken=" + connectionToken, null);
        }

        private void LongPollingLoop(string connectionId)
        {
        LongPoll:

            Task task = ProcessRequest(connectionId);

            if (task.IsCompleted)
            {
                task.Wait();

                goto LongPoll;
            }

            task.ContinueWith(t => LongPollingLoop(connectionId));
        }
    }
}
