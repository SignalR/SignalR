// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal struct DiffPair<T>
    {
        public bool Reset;
        public ICollection<T> Added;
        public ICollection<T> Removed;
    }
}
