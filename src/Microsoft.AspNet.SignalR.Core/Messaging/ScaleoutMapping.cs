// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMapping
    {
        public ScaleoutMapping(ulong id, IList<LocalEventKeyInfo> localKeyInfo)
        {
            Id = id;
            LocalKeyInfo = localKeyInfo;
        }

        public ulong Id { get; private set; }
        public IList<LocalEventKeyInfo> LocalKeyInfo { get; private set; }
        public ScaleoutMapping Next { get; set; }
    }
}
