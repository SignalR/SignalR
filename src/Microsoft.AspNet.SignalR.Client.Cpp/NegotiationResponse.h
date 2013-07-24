//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <json.h>
#include "StringHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class NegotiationResponse
    {
    public:
        NegotiationResponse();
        NegotiationResponse(web::json::value raw);
        ~NegotiationResponse();

        // set members to private and write getters/setters?
        utility::string_t mConnectionId;
        utility::string_t mConnectionToken;
        utility::string_t mUri;
        utility::string_t mProtocolVersion;
        int mDisconnectTimeout;
	    bool mTryWebSockets;
	    int mKeepAliveTimeout;
        int mTransportConnectTimeout;
    };
} // namespace MicrosoftAspNetSignalRClientCpp