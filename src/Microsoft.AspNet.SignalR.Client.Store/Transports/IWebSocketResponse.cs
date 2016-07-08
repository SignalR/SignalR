// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
