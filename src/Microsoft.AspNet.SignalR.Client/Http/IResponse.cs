// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The http response.
    /// </summary>
    public interface IResponse : IDisposable
    {
        /// <summary>
        /// Gets the steam that represents the response body.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This could be expensive.")]
        Stream GetStream();
    }
}
