// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client
{
    public interface IConnection
    {
        string MessageId { get; set; }
        ICollection<string> Groups { get; }
        IDictionary<string, object> Items { get; }
        string ConnectionId { get; }
        string Url { get; }
        string QueryString { get; }
        ConnectionState State { get; }

        bool ChangeState(ConnectionState oldState, ConnectionState newState);

        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }

        void Stop();
        Task Send(string data);
        Task<T> Send<T>(string data);

        void OnReceived(JToken data);
        void OnError(Exception ex);
        void OnReconnected();
        void PrepareRequest(IRequest request);
    }
}
