//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <string>
#include <http_client.h>
#include "StringHelper.h"

using namespace std;
using namespace utility;
using namespace web::json;

namespace MicrosoftAspNetSignalRClientCpp
{
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
        int mDisconnectTimeout;
	    bool mTryWebSockets;
	    int mKeepAliveTimeout;
        int mTransportConnectTimeout;
    };
} // namespace MicrosoftAspNetSignalRClientCpp