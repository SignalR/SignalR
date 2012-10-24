// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub.
    /// </summary>
    public class HubDescriptor : Descriptor
    {
        /// <summary>
        /// Hub type.
        /// </summary>
        public virtual Type Type { get; set; }

        /// <summary>
        /// Attributes on the hub
        /// </summary>
        public IEnumerable<Attribute> Attributes { get; set; }

        public string CreateQualifiedName(string unqualifiedName)
        {
            return Name + "." + unqualifiedName;
        }
    }
}
