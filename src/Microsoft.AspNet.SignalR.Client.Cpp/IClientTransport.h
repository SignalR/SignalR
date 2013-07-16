//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "NegotiationResponse.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class IClientTransport
    {
    public:
        IClientTransport(void);
        ~IClientTransport(void);

        string_t GetTransportName();
        bool SupportsKeepAlive();

        virtual pplx::task<shared_ptr<NegotiationResponse>> Negotiate(shared_ptr<Connection> connection) = 0;
        virtual pplx::task<void> Start(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken) = 0;
        virtual pplx::task<void> Send(shared_ptr<Connection> connection, string_t data) = 0;
        virtual void Abort(shared_ptr<Connection> connection, seconds timeout) = 0;
        virtual void LostConnection(shared_ptr<Connection> connection) = 0;

    protected:
        string_t mTransportName;
        bool mSupportKeepAlive;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
