#pragma once

#include <string>

using namespace std;

class NegotiationResponse
{
public:
    string ConnectionId;
    string ConnectionToken;
	string Url;
    string ProtocolVersion;
    double DisconnectTimeout;
	bool TryWebSockets;
	double KeepAliveTimeout;
};