// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Performance;
using Microsoft.AspNet.SignalR.StressServer.Connections;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("SendReceive", typeof(IRun))]
    public class SendReceiveRun : HostedRun
    {
        [ImportingConstructor]
        public SendReceiveRun(RunData runData)
            : base(runData)
        {
        }

        public virtual string Endpoint
        {
            get { return "echo"; }
        }
        
        protected override void InitializePerformanceCounters()
        {
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            string connectionId = connectionIndex.ToString();
            if (RunData.Transport.Equals("longPolling", StringComparison.OrdinalIgnoreCase))
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
            var postData = new Dictionary<string, string> { { "data", Payload } };
            
            return Host.Post("http://foo/" + Endpoint + "/send?transport=" + RunData.Transport + "&connectionToken=" + senderIndex.ToString(), postData);
        }

        private Task ProcessRequest(string connectionToken)
        {
            return Host.Get("http://foo/" + Endpoint + "/connect?transport=" + RunData.Transport + "&connectionToken=" + connectionToken + "&disableResponseBody=true");
        }

        private Task Abort(string connectionToken)
        {
            return Host.Post("http://foo/" + Endpoint + "/abort?transport=" + RunData.Transport + "&connectionToken=" + connectionToken, data: null);
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
