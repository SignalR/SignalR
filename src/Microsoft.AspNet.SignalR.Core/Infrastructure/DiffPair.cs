// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal struct DiffPair<T>
    {
        public ICollection<T> Added;
        public ICollection<T> Removed;

        public bool AnyChanges
        {
            get
            {
                return Added.Count > 0 || Removed.Count > 0;
            }
        }
    }
}
