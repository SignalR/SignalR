// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if (NET4 || NET45)
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.AspNet.SignalR.Client
{
    public interface IConnection
    {
        Version Protocol { get; set; }
        KeepAliveData KeepAliveData { get; set; }
        string MessageId { get; set; }
        string GroupsToken { get; set; }
        IDictionary<string, object> Items { get; }
        string ConnectionId { get; }
        string ConnectionToken { get; }
        string Url { get; }
        string QueryString { get; }
        ConnectionState State { get; }
        IClientTransport Transport { get; }

#if !SILVERLIGHT
        /// <summary>
        /// Gets of sets proxy information for the connection.
        /// </summary>
        IWebProxy Proxy { get; set; }
#endif

#if (NET4 || NET45)
        X509CertificateCollection Certificates { get; }
#endif

        bool ChangeState(ConnectionState oldState, ConnectionState newState);

        IDictionary<string, string> Headers { get; }
        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }
        JsonSerializer JsonSerializer { get; }

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "Works in VB.NET.")]
        void Stop();
        void Disconnect();
        Task Send(string data);

        void OnReceived(JToken data);
        void OnError(Exception ex);
        void OnReconnecting();
        void OnReconnected();
        void OnConnectionSlow();
        void PrepareRequest(IRequest request);
        void UpdateLastKeepAlive();
        void Trace(TraceLevels level, string format, params object[] args);
    }
}
