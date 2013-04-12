// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMapping
    {
        public ConcurrentDictionary<string, LocalEventKeyInfo> EventKeyMappings { get; private set; }

        public ScaleoutMapping(IDictionary<string, LocalEventKeyInfo> mappings)
        {
            EventKeyMappings = new ConcurrentDictionary<string, LocalEventKeyInfo>(mappings, StringComparer.OrdinalIgnoreCase);
        }
    }
}
