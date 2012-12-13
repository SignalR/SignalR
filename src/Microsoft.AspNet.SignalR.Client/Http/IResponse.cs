// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The http response.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Reads the response body as a string.
        /// </summary>
        /// <returns></returns>
        string ReadAsString();

        /// <summary>
        /// Gets the steam that represents the response body.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This could be expensive.")]
        Stream GetResponseStream();

        /// <summary>
        /// Closes the response.
        /// </summary>
        void Close();
    }
}
