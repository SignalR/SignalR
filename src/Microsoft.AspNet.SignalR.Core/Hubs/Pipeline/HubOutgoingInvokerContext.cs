// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
