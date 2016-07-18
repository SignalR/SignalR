// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Implementations handle their own serialization to JSON.
    /// </summary>
    public interface IJsonWritable
    {
        /// <summary>
        /// Serializes itself to JSON via a <see cref="System.IO.TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> that receives the JSON serialized object.</param>
        void WriteJson(TextWriter writer);
    }
}
