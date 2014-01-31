// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    ///  A client side proxy for a server side hub.
    /// </summary>
    public interface IHubProxy
    {
        /// <summary>
        /// Gets or sets state on the hub.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>The value of the field</returns>
        JToken this[string name] { get; set; }

        /// <summary>
        /// Executes a method on the server side hub asynchronously.
        /// </summary>
        /// <param name="method">The name of the method.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task Invoke(string method, params object[] args);

        /// <summary>
        /// Executes a method on the server side hub asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of result returned from the hub.</typeparam>
        /// <param name="method">The name of the method.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task<T> Invoke<T>(string method, params object[] args);

        /// <summary>
        /// Executes a method on the server side hub asynchronously with progress updates.
        /// </summary>
        /// <param name="method">The name of the method.</param>
        /// <param name="onProgress">The callback to invoke when progress updates are received.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task Invoke<T>(string method, Action<T> onProgress, params object[] args);

        /// <summary>
        /// Executes a method on the server side hub asynchronously with progress updates.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned from the hub.</typeparam>
        /// <typeparam name="TProgress">The type of progress update value.</typeparam>
        /// <param name="method">The name of the method.</param>
        /// <param name="onProgress">The callback to invoke when progress updates are received.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task<TResult> Invoke<TResult, TProgress>(string method, Action<TProgress> onProgress, params object[] args);

        /// <summary>
        /// Registers an event for the hub.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <returns>A <see cref="Subscription"/>.</returns>
        Subscription Subscribe(string eventName);

        /// <summary>
        /// Gets the serializer used by the connection.
        /// </summary>
        JsonSerializer JsonSerializer { get; }
    }
}
