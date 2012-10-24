// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Describes a hub manager - main point in the whole hub and method lookup process.
    /// </summary>
    public interface IHubManager
    {
        /// <summary>
        /// Retrieves a single hub descriptor.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <returns>Hub descriptor, if found. Null, otherwise.</returns>
        HubDescriptor GetHub(string hubName);

        /// <summary>
        /// Retrieves all available hubs.
        /// </summary>
        /// <returns>List of hub descriptors.</returns>
        IEnumerable<HubDescriptor> GetHubs(Func<HubDescriptor, bool> predicate = null);

        /// <summary>
        /// Resolves a given hub name to a concrete object.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <returns>Hub implementation instance, if found. Null otherwise.</returns>
        HubActivationResult ResolveHub(string hubName);

        /// <summary>
        /// Resolves all available hubs to their concrete objects.
        /// </summary>
        /// <returns>List of hub instances.</returns>
        IEnumerable<HubActivationResult> ResolveHubs();

        /// <summary>
        /// Retrieves a method with a given name on a given hub.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <param name="method">Name of the method to find.</param>
        /// <param name="parameters">Method parameters to match.</param>
        /// <returns>Descriptor of the method, if found. Null otherwise.</returns>
        MethodDescriptor GetHubMethod(string hubName, string method, params IJsonValue[] parameters);

        /// <summary>
        /// Gets all methods available to call on a given hub.
        /// </summary>
        /// <param name="hubName">Name of the hub,</param>
        /// <param name="predicate">Optional predicate for filtering results.</param>
        /// <returns>List of available methods.</returns>
        IEnumerable<MethodDescriptor> GetHubMethods(string hubName, Func<MethodDescriptor, bool> predicate = null);

        /// <summary>
        /// Disposes the hub and cleans the activation items if necassery
        /// </summary>
        /// <param name="hubActivationResult"></param>
        void Deactivate(HubActivationResult hubActivationResult);
    }
}
