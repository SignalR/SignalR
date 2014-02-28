// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// A client that can make http request.
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Initializes the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        void Initialize(IConnection connection);

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether it is a long running request</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Performs a GET request")]
        Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning);

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether it is a long running request</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning);
    }
}
