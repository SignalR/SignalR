//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>

using namespace std;
using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
    enum ConnectionState
    {
        Connecting,
        Connected,
        Reconnecting,
        Disconnected
    };

    class ConnectionStateString
    {
    public:
        static string_t ToString(ConnectionState state)
        {
            switch (state)
            {
            case Connecting:
                return U("Connecting");
                break;
            case Connected:
                return U("Connected");
                break;
            case Reconnecting:
                return U("Reconnecting");
                break;
            case Disconnected:
                return U("Disconnected");
                break;
            default:
                throw(exception("InvalidArgumentException: state"));
                break;
            }
        };
    };
} // namespace MicrosoftAspNetSignalRClientCpp