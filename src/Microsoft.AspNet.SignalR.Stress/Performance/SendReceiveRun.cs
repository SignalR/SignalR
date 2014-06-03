// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Performance;
using Owin;
using System.IO;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("SendReceive", typeof(IRun))]
    public class SendReceiveRun : HostedRun
    {
        private readonly CancellationTokenSource _stopPollingCts;

        [ImportingConstructor]
        public SendReceiveRun(RunData runData)
            : base(runData)
        {
            _stopPollingCts = new CancellationTokenSource();
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
            Task receiverTask;

            if (RunData.Transport.Equals("longPolling", StringComparison.OrdinalIgnoreCase))
            {
                receiverTask = Task.Factory.StartNew(state =>
                {
                    LongPollingLoop((string)state);
                }, connectionId);
            }
            else
            {
                receiverTask = ProcessRequest(connectionId);
            }

            // Abort the request on dispose
            return new DisposableAction(state =>
            {
                var data = (ReceiverData)state;
                data.CancellationTokenSource.Cancel();
                receiverTask.Wait();
                Abort(data.ConnectionId).Wait();
            }, new ReceiverData()
            {
                ConnectionId = connectionId,
                Task = receiverTask,
                CancellationTokenSource = _stopPollingCts
            });
        }

        protected override Task Send(int senderIndex, string source)
        {
            var postData = new Dictionary<string, string> { { "data", Payload } };

            return Host.Post(Host.Url + "/" + Endpoint + "/send?transport=" + RunData.Transport + "&connectionToken=" + senderIndex.ToString(), postData);
        }

        private Task<IResponse> ProcessRequest(string connectionToken)
        {
            return Host.Get(Host.Url + "/" + Endpoint + "/connect?transport=" + RunData.Transport + "&connectionToken=" + connectionToken + "&disableResponseBody=true");
        }

        private Task Abort(string connectionToken)
        {
            return Host.Post(Host.Url + "/" + Endpoint + "/abort?transport=" + RunData.Transport + "&connectionToken=" + connectionToken, data: null);
        }

        private async void LongPollingLoop(string connectionId)
        {
            while (!_stopPollingCts.Token.IsCancellationRequested)
            {
                var response = await ProcessRequest(connectionId);
                var responseString = await response.ReadAsString();
            }
        }

        private class ReceiverData
        {
            public string ConnectionId { get; set; }
            public Task Task { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
    }
}
