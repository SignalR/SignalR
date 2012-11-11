// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    /// <summary>
    /// A <see cref="Connection"/> for interacting with Hubs.
    /// </summary>
    public class HubConnection : Connection
    {
        private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        public HubConnection(string url)
            : this(url, useDefaultUrl: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="useDefaultUrl">Determines if the default "/signalr" path should be appended to the specified url.</param>
        public HubConnection(string url, bool useDefaultUrl)
            : base(GetUrl(url, useDefaultUrl))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public HubConnection(string url, string queryString)
            : this(url, queryString, useDefaultUrl: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        /// <param name="useDefaultUrl">Determines if the default "/signalr" path should be appended to the specified url.</param>
        public HubConnection(string url, string queryString, bool useDefaultUrl)
            : base(GetUrl(url, useDefaultUrl), queryString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public HubConnection(string url, IDictionary<string, string> queryString)
            : this(url, queryString, useDefaultUrl: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        /// <param name="useDefaultUrl">Determines if the default "/signalr" path should be appended to the specified url.</param>
        public HubConnection(string url, IDictionary<string, string> queryString, bool useDefaultUrl)
            : base(GetUrl(url, useDefaultUrl), queryString)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "")]
        protected override void OnReceived(JToken message)
        {
            var invocation = message.ToObject<HubInvocation>();
            HubProxy hubProxy;
            if (_hubs.TryGetValue(invocation.Hub, out hubProxy))
            {
                if (invocation.State != null)
                {
                    foreach (var state in invocation.State)
                    {
                        hubProxy[state.Key] = state.Value;
                    }
                }

                hubProxy.InvokeEvent(invocation.Method, invocation.Args);
            }

            base.OnReceived(message);
        }

        protected override string OnSending()
        {
            var data = _hubs.Select(p => new HubRegistrationData
            {
                Name = p.Key
            });

            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// Creates an <see cref="IHubProxy"/> for the hub with the specified name.
        /// </summary>
        /// <param name="hubName">The name of the hub.</param>
        /// <returns>A <see cref="IHubProxy"/></returns>
        public IHubProxy CreateHubProxy(string hubName)
        {
            if (State != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ProxiesCannotBeAddedConnectionStarted));
            }

            HubProxy hubProxy;
            if (!_hubs.TryGetValue(hubName, out hubProxy))
            {
                hubProxy = new HubProxy(this, hubName);
                _hubs[hubName] = hubProxy;
            }
            return hubProxy;
        }

        private static string GetUrl(string url, bool useDefaultUrl)
        {
            if (!url.EndsWith("/", StringComparison.Ordinal))
            {
                url += "/";
            }

            if (useDefaultUrl)
            {
                return url + "signalr";
            }

            return url;
        }
    }
}
