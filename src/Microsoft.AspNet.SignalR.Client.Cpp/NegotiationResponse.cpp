#include "NegotiationResponse.h"

NegotiationResponse::NegotiationResponse()
{
}

NegotiationResponse::NegotiationResponse(value raw)
{
    auto iter = raw.cbegin();

    mUri = (iter++)->second.as_string();
    mConnectionToken = (iter++)->second.as_string();
    mConnectionId = (iter++)->second.as_string();
    mKeepAliveTimeout = (iter++)->second.as_double();
    mDisconnectTimeout = (iter++)->second.as_double();
    mTryWebSockets = (iter++)->second.as_bool();
    mProtocolVersion = iter->second.as_string();
}

NegotiationResponse::~NegotiationResponse()
{
}