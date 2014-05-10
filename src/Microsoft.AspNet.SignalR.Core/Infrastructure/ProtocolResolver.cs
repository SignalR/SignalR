// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public class ProtocolResolver
    {
        private const string ProtocolQueryParameter = "clientProtocol";
        private readonly Version _minSupportedProtocol;
        private readonly Version _maxSupportedProtocol;
        private readonly Version _minimumDelayedStartVersion = new Version(1, 4);

        public ProtocolResolver() :
            this(new Version(1, 2), new Version(1, 4))
        {
        }

        public ProtocolResolver(Version min, Version max)
        {
            _minSupportedProtocol = min;
            _maxSupportedProtocol = max;
        }

        public Version Resolve(IRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            Version clientProtocol;

            if (Version.TryParse(request.QueryString[ProtocolQueryParameter], out clientProtocol))
            {
                if (clientProtocol > _maxSupportedProtocol)
                {
                    clientProtocol = _maxSupportedProtocol;
                }
                else if (clientProtocol < _minSupportedProtocol)
                {
                    clientProtocol = _minSupportedProtocol;
                }
            }

            return clientProtocol ?? _minSupportedProtocol;
        }

        public bool SupportsDelayedStart(IRequest request)
        {
            return Resolve(request) >= _minimumDelayedStartVersion;
        }
    }
}
