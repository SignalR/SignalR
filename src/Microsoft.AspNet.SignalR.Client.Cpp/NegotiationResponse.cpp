//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "NegotiationResponse.h"

namespace MicrosoftAspNetSignalRClientCpp
{

NegotiationResponse::NegotiationResponse()
{
}

NegotiationResponse::NegotiationResponse(value raw)
{
    value uri = raw[U("Url")];
    if (!uri.is_null())
    {
        mUri = uri.as_string();
    }
    else
    {
        mUri = U("");
    }

    value connectionToken = raw[U("ConnectionToken")];
    if (!connectionToken.is_null())
    {
        mConnectionToken = connectionToken.as_string();
    }
    else
    {
        mConnectionToken = U("");
    }

    value connectionId = raw[U("ConnectionId")];
    if (!connectionId.is_null())
    {
        mConnectionId = connectionId.as_string();
    }
    else
    {
        mConnectionId = U("");
    }

    value keepAliveTimeout = raw[U("KeepAliveTimeout")];
    if (!keepAliveTimeout.is_null())
    {
        mKeepAliveTimeout = keepAliveTimeout.as_integer();
    }
    else
    {
        mKeepAliveTimeout = 0;
    }

    value disconnectTimeout = raw[U("DisconnectTimeout")];
    if (!disconnectTimeout.is_null())
    {
        mDisconnectTimeout = disconnectTimeout.as_integer();
    }
    else
    {
        mDisconnectTimeout = 0;
    }

    value tryWebSockets = raw[U("TryWebSockets")];
    if (!tryWebSockets.is_null())
    {
        mTryWebSockets = tryWebSockets.as_bool();
    }
    else
    {
        mTryWebSockets = false;
    }

    value protocolVersion = raw[U("ProtocolVersion")];
    if (!protocolVersion.is_null())
    {
        mProtocolVersion = protocolVersion.as_string();
    }
    else
    {
        mProtocolVersion = U("");
    }

    value transportConnectTimeout = raw[U("TransportConnectTimeout")];
    if (!transportConnectTimeout.is_null())
    {
        mTransportConnectTimeout = transportConnectTimeout.as_integer();
    }
    else
    {
        mTransportConnectTimeout = 5;
    }
}

NegotiationResponse::~NegotiationResponse()
{
}

} // namespace MicrosoftAspNetSignalRClientCpp