#pragma once

#include <string>

using namespace std;

struct NegotiateResponse
{
    string ConnectionId;
    string ConnectionToken;
    string ProtocolVersion;
};