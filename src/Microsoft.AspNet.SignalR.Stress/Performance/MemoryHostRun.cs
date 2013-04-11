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
        [ImportingConstructor]
        public MemoryHostRun(RunData runData)
            : base(runData)
        {
            Transport = runData.Transport;
            Host = new MemoryHost();
        }

        public virtual string Endpoint
        {
            get { return "echo"; }
        }

        public string Transport { get; private set; }

        protected MemoryHost Host { get; private set; }

        public override void InitializePerformanceCounters()
        {
        }

        public override void Initialize()
        {
            Host.Configure(ConfigureApp);
            base.Initialize();
        }

        protected virtual void ConfigureApp(IAppBuilder app)
        {
            var config = new ConnectionConfiguration
            {
                Resolver = Resolver
            };

            app.MapConnection<StressConnection>(Endpoint, config);

            config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
        }

        public override void Dispose()
        {
            base.Dispose();

            Host.Dispose();
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

        protected override Task Send(int senderIndex, string source)
        {
            return ProcessSendRequest(senderIndex.ToString(), Payload);
        }

        private Task ProcessRequest(string connectionToken)
        {
            return Host.Get("http://foo/" + Endpoint + "/connect?transport=" + Transport + "&connectionToken=" + connectionToken, disableWrites: true);
        }

        private Task ProcessSendRequest(string connectionToken, string data)
        {
            var postData = new Dictionary<string, string> { { "data", data } };
            return Host.Post("http://foo/" + Endpoint + "/send?transport=" + Transport + "&connectionToken=" + connectionToken, postData);
        }

        private Task Abort(string connectionToken)
        {
            return Host.Post("http://foo/" + Endpoint + "/abort?transport=" + Transport + "&connectionToken=" + connectionToken, null);
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
