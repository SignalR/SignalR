// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public class BinaryMemoryPoolTextWriter : MemoryPoolTextWriter, IBinaryWriter
    {
        public BinaryMemoryPoolTextWriter(IMemoryPool memory)
            : base(memory)
        {
        }
    }
}
