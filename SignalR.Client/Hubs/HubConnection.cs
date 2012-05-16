﻿using System;
using System.Collections.Generic;
#if NET20
using SignalR.Client.Net20.Infrastructure;
#else
using System.Linq;
using System.Threading.Tasks;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR.Client.Transports;

namespace SignalR.Client.Hubs
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
            : base(url, queryString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public HubConnection(string url, IDictionary<string, string> queryString)
            : base(url, queryString)
        {
        }

        public override Task Start(IClientTransport transport)
        {
            Sending += OnConnectionSending;
            return base.Start(transport);
        }

        public override void Stop()
        {
            Sending -= OnConnectionSending;
            base.Stop();
        }

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

        /// <summary>
        /// Creates an <see cref="IHubProxy"/> for the hub with the specified name.
        /// </summary>
        /// <param name="hubName">The name of the hub.</param>
        /// <returns>A <see cref="IHubProxy"/></returns>
        public IHubProxy CreateProxy(string hubName)
        {
            HubProxy hubProxy;
            if (!_hubs.TryGetValue(hubName, out hubProxy))
            {
                hubProxy = new HubProxy(this, hubName);
                _hubs[hubName] = hubProxy;
            }
            return hubProxy;
        }

        private string OnConnectionSending()
        {
#if NET20
			var data = new List<HubRegistrationData>();
			foreach (var hub in _hubs)
			{
				data.Add(new HubRegistrationData { Name = hub.Key });
			}
#else
            var data = _hubs.Select(p => new HubRegistrationData
            {
                Name = p.Key
            });
#endif

            return JsonConvert.SerializeObject(data);
        }

        private static string GetUrl(string url, bool useDefaultUrl)
        {
            if (!url.EndsWith("/"))
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
