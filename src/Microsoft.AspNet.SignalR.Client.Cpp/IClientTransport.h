//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "NegotiationResponse.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class IClientTransport
    {
    public:
        IClientTransport(void);
        ~IClientTransport(void);

        utility::string_t GetTransportName();
        bool SupportsKeepAlive();

        virtual pplx::task<std::shared_ptr<NegotiationResponse>> Negotiate(std::shared_ptr<Connection> connection) = 0;
        virtual pplx::task<void> Start(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken) = 0;
        virtual pplx::task<void> Send(std::shared_ptr<Connection> connection, utility::string_t data) = 0;
        virtual void Abort(std::shared_ptr<Connection> connection, utility::seconds timeout) = 0;
        virtual void LostConnection(std::shared_ptr<Connection> connection) = 0;

    protected:
        utility::string_t mTransportName;
        bool mSupportKeepAlive;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
