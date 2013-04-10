// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Messaging;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlStreamReceivedEventArgs : EventArgs
    {
        public SqlStreamReceivedEventArgs(ulong payloadId, IList<Message> messages)
        {
            PayloadId = payloadId;
            Messages = messages;
        }

        public ulong PayloadId { get; private set; }
        public IList<Message> Messages { get; private set; }
    }
}
