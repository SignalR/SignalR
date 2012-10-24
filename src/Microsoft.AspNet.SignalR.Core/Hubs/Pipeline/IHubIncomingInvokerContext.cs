// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.


using System.Collections.Specialized;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubIncomingInvokerContext
    {

        /// <summary>
        /// 
        /// </summary>
        NameValueCollection ActivationItems { get; }

        /// <summary>
        /// 
        /// </summary>
        IHub Hub { get; }

        /// <summary>
        /// 
        /// </summary>
        MethodDescriptor MethodDescriptor { get; }

        /// <summary>
        /// 
        /// </summary>
        object[] Args { get; }

        /// <summary>
        /// 
        /// </summary>
        TrackingDictionary State { get; }
    }
}
