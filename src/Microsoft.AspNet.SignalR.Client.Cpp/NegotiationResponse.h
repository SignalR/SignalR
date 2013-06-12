#pragma once

#include <string>
#include <http_client.h>
#include "StringHelper.h"

using namespace std;
using namespace utility;
using namespace web::json;

class NegotiationResponse
{
public:
    NegotiationResponse();
    NegotiationResponse(value raw);
    ~NegotiationResponse();

    // set members to private and write getters/setters?
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mUri;
    string_t mProtocolVersion;
    double mDisconnectTimeout;
	bool mTryWebSockets;
	double mKeepAliveTimeout;
};