// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// This marker interface can be used in lieu of IDisposable in order to indicate to the dependency resolver that 
    /// it should not retain/track references nor invoke Dispose on instances of the resolved type.
    /// This is useful for transient types that are created by the dependency resolver, but are short-lived and will
    /// be Disposed by some other means outside of the resolver.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "Marker interface is fine here and this will go away in 3.0 anyway.")]
    public interface IUntrackedDisposable : IDisposable
    {
    }
}
