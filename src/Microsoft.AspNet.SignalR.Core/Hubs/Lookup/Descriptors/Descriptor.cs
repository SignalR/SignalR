// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Hubs
{
    public abstract class Descriptor
    {
        /// <summary>
        /// Name of Descriptor.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Flags whether the name was specified.
        /// </summary>
        public virtual bool NameSpecified { get; set; }
    }
}
