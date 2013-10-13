// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about a SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public interface IHubConnectionContext<T>
    {
        T All { get; }
        T AllExcept(params string[] excludeConnectionIds);

        T Client(string connectionId);
        T Clients(IList<string> connectionIds);

        T Group(string groupName, params string[] excludeConnectionIds);
        T Groups(IList<string> groupNames, params string[] excludeConnectionIds);

        T User(string userId);

        T Users(IList<string> userIds);
    }
}
