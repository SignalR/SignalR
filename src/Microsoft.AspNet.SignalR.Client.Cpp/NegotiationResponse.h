#pragma once

#include <string>
#include <http_client.h>

using namespace std;

class NegotiationResponse
{
public:
    utility::string_t ConnectionId;
    utility::string_t ConnectionToken;
    utility::string_t Uri;
    utility::string_t ProtocolVersion;
    double DisconnectTimeout;
	bool TryWebSockets;
	double KeepAliveTimeout;
};