#include "NegotiationResponse.h"

namespace MicrosoftAspNetSignalRClientCpp
{

NegotiationResponse::NegotiationResponse()
{
}

NegotiationResponse::NegotiationResponse(value raw)
{
    mUri = raw[U("Url")].as_string();
    mConnectionToken = raw[U("ConnectionToken")].as_string();
    mConnectionId = raw[U("ConnectionId")].as_string();
    mKeepAliveTimeout = raw[U("KeepAliveTimeout")].as_integer();
    mDisconnectTimeout = raw[U("DisconnectTimeout")].as_integer();
    mTryWebSockets = raw[U("TryWebSockets")].as_bool();
    mProtocolVersion = raw[U("ProtocolVersion")].as_string();
    mTransportConnectTimeout = raw[U("TransportConnectTimeout")].as_integer();
}

NegotiationResponse::~NegotiationResponse()
{
}

} // namespace MicrosoftAspNetSignalRClientCpp