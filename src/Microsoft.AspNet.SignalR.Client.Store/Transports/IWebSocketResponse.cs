// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    // This is for wrapping MessageWebSocketMessageReceivedEventArgs which is neither constructible 
    // nor mockable and therefore blocks any testing of the MessageReceived event
    interface IWebSocketResponse
    {
        IDataReader GetDataReader();
    }
}
