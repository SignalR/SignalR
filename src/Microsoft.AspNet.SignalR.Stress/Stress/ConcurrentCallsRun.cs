// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.StressServer.Hubs;

namespace Microsoft.AspNet.SignalR.Stress.Stress
{
    [Export("ConcurrentCalls", typeof(IRun))]
    public class ConcurrentCallsRun : HostedStressRun
    {
        private const string HubName = "ConcurrentCallsHub";

        private static readonly object SyncLock = new object();
        private HubConnection _connection = null;
        private IHubProxy _hubProxy = null;
        private Dictionary<string, ManualResetEvent> _eventsMre = new Dictionary<string, ManualResetEvent>();

        [ImportingConstructor]
        public ConcurrentCallsRun(RunData runData)
            : base(runData)
        {
        }

        public override void Initialize()
        {
            // Note this only works with one connection and one proxy
            _connection = new HubConnection(RunData.Url);

            // uncomment this line to turn on tracing
            //_connection.TraceWriter = Console.Out;
            
            _hubProxy = _connection.CreateHubProxy(HubName);

            base.Initialize();
        }

        protected override IDisposable CreateReceiver(int sendIndex)
        {
            _hubProxy.On<string, DataClass>(
                "Echo",
                (str, c) =>
                {
                    SetMre(str);
                });

            _hubProxy.On<string, DataClass, int>(
                "EchoAll",
                (str, c, hash) =>
                {
                    if(string.IsNullOrEmpty(str)) 
                    {
                        throw new Exception("EchoAll returns null string");    
                    };
                });

            _connection.Start(Host.Transport).Wait();

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        protected override Task Send(int senderIndex, string source)
        {
            // We can adds weight here
            EchoCaller();
            EchoMessage();
            return EchoAll();
        }

        private void EchoCaller()
        {
            if (!InvokeAndWait("EchoCaller"))
            {
                throw new Exception("EchoCaller failed because the MRE's were not set.");
            }
        }

        private void EchoMessage()
        {
            if (!this.InvokeAndWait("EchoMessage"))
            {
                throw new Exception("EchoMessage failed because the MRE's were not set.");
            }
        }

        private Task EchoAll()
        {
            string str = Guid.NewGuid().ToString();

            return _hubProxy.Invoke("EchoAll", new object[] { str, DataClass.CreateDataClass() });
        }

        private bool InvokeAndWait(string methodName)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Guid guid = Guid.NewGuid();
            string str = string.Format("{0}_{1}", methodName, guid);

            lock (SyncLock)
            {
                _eventsMre.Add(str, mre);
            }

            _hubProxy.Invoke(methodName, new object[] { str, DataClass.CreateDataClass() });

            return mre.WaitOne(TimeSpan.FromSeconds(180));
        }

        private void SetMre(string str)
        {
            ManualResetEvent mre = _eventsMre[str];

            lock (SyncLock)
            {
                _eventsMre.Remove(str);
            }

            mre.Set();
        }
    }
}
