﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client
{
    public interface IConnection
    {
        string MessageId { get; set; }
        string GroupsToken { get; set; }
        IDictionary<string, object> Items { get; }
        string ConnectionId { get; }
        string ConnectionToken { get; }
        string Url { get; }
        string QueryString { get; }
        ConnectionState State { get; }

        bool ChangeState(ConnectionState oldState, ConnectionState newState);

        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "Works in VB.NET.")]
        void Stop();
        void Disconnect();
        Task Send(string data);

        void OnReceived(JToken data);
        void OnError(Exception ex);
        void OnReconnecting();
        void OnReconnected();
        void PrepareRequest(IRequest request);

        JsonSerializer JsonSerializer { get; set; }
        string JsonSerializeObject(object value);
    }
}
