// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubOutgoingInvokerContext : IHubOutgoingInvokerContext
    {        
        public HubOutgoingInvokerContext(IConnection connection, string signal, ClientHubInvocation invocation)
        {
            Connection = connection;
            Signal = signal;
            Invocation = invocation;
        }

        public HubOutgoingInvokerContext(IConnection connection, IList<string> signals, ClientHubInvocation invocation)
        {
            Connection = connection;
            Signals = signals;
            Invocation = invocation;
        }

        public IConnection Connection
        {
            get;
            private set;
        }

        public ClientHubInvocation Invocation
        {
            get;
            private set;
        }

        public string Signal
        {
            get;
            private set;
        }

        public IList<string> Signals
        {
            get;
            private set;
        }

        public IList<string> ExcludedSignals
        {
            get;
            set;
        }
    }
}
