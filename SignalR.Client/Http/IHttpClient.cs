using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client.Http
{
    /// <summary>
    /// A client that can make http request.
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <returns>A <see cref="Task{IResponse}"/>.</returns>
        Task<IResponse> GetAsync(string url, Action<IRequest> prepareRequest);

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <returns>A <see cref="Task{IResponse}"/>.</returns>
        Task<IResponse> PostAsync(string url, Action<IRequest> prepareRequest, Dictionary<string, string> postData);
    }
}
