// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMapping
    {
        public ScaleoutMapping(ulong id, ScaleoutMessage message)
            : this(id, message, new Dictionary<string, IList<LocalEventKeyInfo>>())
        {
        }

        public ScaleoutMapping(ulong id, ScaleoutMessage message, IDictionary<string, IList<LocalEventKeyInfo>> localKeyInfo)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (localKeyInfo == null)
            {
                throw new ArgumentNullException("localKeyInfo");
            }

            Id = id;
            LocalKeyInfo = localKeyInfo;
            CreationTime = message.CreationTime;
        }

        public ulong Id { get; private set; }
        public IDictionary<string, IList<LocalEventKeyInfo>> LocalKeyInfo { get; private set; }
        public DateTime CreationTime { get; private set; }
    }
}
