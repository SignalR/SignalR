// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;

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
        IHub Hub { get; }

        /// <summary>
        /// 
        /// </summary>
        MethodDescriptor MethodDescriptor { get; }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This represents an ordered list of parameter values")]
        object[] Args { get; }

        /// <summary>
        /// 
        /// </summary>
        StateChangeTracker StateTracker { get; }
    }
}
