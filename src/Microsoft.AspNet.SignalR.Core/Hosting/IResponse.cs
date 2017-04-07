﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting
{
    /// <summary>
    /// Represents a connection to the client.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Gets a cancellation token that represents the client's lifetime.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets or sets the status code of the response.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the content type of the response.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Writes buffered data.
        /// </summary>
        /// <param name="data">The data to write to the buffer.</param>
        void Write(ArraySegment<byte> data);

        /// <summary>
        /// Flushes the buffered response to the client.
        /// </summary>
        /// <returns>A task that represents when the data has been flushed.</returns>
        Task Flush();
    }
}
