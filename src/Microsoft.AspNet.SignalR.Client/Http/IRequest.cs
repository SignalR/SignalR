﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The http request
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The user agent for this request.
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// The accept header for this request.
        /// </summary>
        string Accept { get; set; }

        /// <summary>
        /// Aborts the request.
        /// </summary>
        void Abort();

        /// <summary>
        /// Set Request Headers
        /// </summary>
        /// <param name="headers">request headers</param>
        void SetRequestHeaders(IDictionary<string, string> headers);
    }
}
