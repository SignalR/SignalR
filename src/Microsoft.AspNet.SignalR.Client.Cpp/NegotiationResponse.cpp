#include "NegotiationResponse.h"

NegotiationResponse::NegotiationResponse()
{
}

NegotiationResponse::NegotiationResponse(value raw)
{
    mUri = raw[U("Url")].as_string();
    mConnectionToken = raw[U("ConnectionToken")].as_string();
    mConnectionId = raw[U("ConnectionId")].as_string();
    mKeepAliveTimeout = raw[U("KeepAliveTimeout")].as_double();
    mDisconnectTimeout = raw[U("DisconnectTimeout")].as_double();
    mTryWebSockets = raw[U("TryWebSockets")].as_bool();
    mProtocolVersion = raw[U("ProtocolVersion")].as_string();
}

NegotiationResponse::~NegotiationResponse()
{
}