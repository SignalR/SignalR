#include "NegotiationResponse.h"

NegotiationResponse::NegotiationResponse()
{
}

NegotiationResponse::NegotiationResponse(value raw)
{
    // temporary solution, couldn't find a JSON parser in C++ with the Apache Licence
    auto iter = raw.cbegin();

    mUri = StringHelper::CleanString(iter->second.to_string());
    iter++;
    mConnectionToken = StringHelper::EncodeUri(iter->second.to_string());
    iter++;
    mConnectionId = StringHelper::CleanString(iter->second.to_string());
    iter++;
    mKeepAliveTimeout = iter->second.as_double();
    iter++;
    mDisconnectTimeout = iter->second.as_double();
    iter++;
    mTryWebSockets = iter->second.as_bool();
    iter++;
    mProtocolVersion = StringHelper::CleanString(iter->second.to_string());
}

NegotiationResponse::~NegotiationResponse()
{
}