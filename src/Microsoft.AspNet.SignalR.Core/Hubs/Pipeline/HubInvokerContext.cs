// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Specialized;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class HubInvokerContext : IHubIncomingInvokerContext
    {
        public HubInvokerContext(IHub hub, NameValueCollection activationItems, TrackingDictionary state, MethodDescriptor methodDescriptor, object[] args)
        {
            Hub = hub;
            ActivationItems = activationItems;
            MethodDescriptor = methodDescriptor;
            Args = args;
            State = state;
        }

        public NameValueCollection ActivationItems { get; private set; }

        public IHub Hub
        {
            get;
            private set;
        }

        public MethodDescriptor MethodDescriptor
        {
            get;
            private set;
        }

        public object[] Args
        {
            get;
            private set;
        }


        public TrackingDictionary State
        {
            get;
            private set;
        }
    }
}
