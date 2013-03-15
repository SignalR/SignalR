// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutMapping : IScaleoutMapping
    {
        public ScaleoutMapping(ulong id, IList<LocalEventKeyInfo> localKeyInfo, StoreLink store)
        {
            Id = id;
            LocalKeyInfo = localKeyInfo;
            Store = store;
        }

        public ulong Id { get; private set; }
        public IList<LocalEventKeyInfo> LocalKeyInfo { get; private set; }
        
        public StoreLink Store { get; private set; }
        public ScaleoutMapping Next { get; set; }

        public IScaleoutMapping NextMapping()
        {
            return Next;
        }
    }
}
