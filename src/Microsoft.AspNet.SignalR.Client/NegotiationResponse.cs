// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Client
{
    [DebuggerDisplay("{ConnectionId} {Url} -> {ProtocolVersion}")]
    public class NegotiationResponse
    {
        public string ConnectionId { get; set; }
        public string ConnectionToken { get; set; }
        public string Url { get; set; }
        public string ProtocolVersion { get; set; }
        public double DisconnectTimeout { get; set; }
        public bool TryWebSockets { get; set; }
        public double? KeepAliveTimeout { get; set; }
        public double TransportConnectTimeout { get; set; }

        // Protocol 2.0: Redirection and custom Negotiation Errors
        public string RedirectUrl { get; set; }
        public string AccessToken { get; set; }
        public string Error { get; set; }
    }
}
