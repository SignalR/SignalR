// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public static class SqlPayload
    {
        public static byte[] ToBytes(IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            var message = new ScaleoutMessage(messages);
            return message.ToBytes();
        }

        public static ScaleoutMessage FromBytes(IDataRecord record)
        {
            var message = ScaleoutMessage.FromBytes(record.GetBinary(1));

            return message;
        }
    }
}
