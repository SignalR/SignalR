// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public static class MessageResultExtensions
    {
        public static IEnumerable<Message> GetMessages(this MessageResult result)
        {
            for (int i = 0; i < result.Messages.Count; i++)
            {
                for (int j = result.Messages[i].Offset; j < result.Messages[i].Offset + result.Messages[i].Count; j++)
                {
                    Message message = result.Messages[i].Array[j];
                    yield return message;
                }
            }
        }
    }
}
