//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "Connection.h"
#include "TransportHelper.h"
#include "TransportInitializationHandler.h"
#include "TransportAbortHandler.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class HttpBasedTransport :
        public IClientTransport
    {
    public:
        HttpBasedTransport(std::shared_ptr<IHttpClient> httpClient, utility::string_t transport);
        ~HttpBasedTransport(void);

        pplx::task<std::shared_ptr<NegotiationResponse>> Negotiate(std::shared_ptr<Connection> connection);
        pplx::task<void> Start(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken);
        pplx::task<void> Send(std::shared_ptr<Connection> connection, utility::string_t data);
        void Abort(std::shared_ptr<Connection> connection, utility::seconds timeout);

    protected:
        std::shared_ptr<IHttpClient> GetHttpClient();
        std::shared_ptr<TransportAbortHandler> GetAbortHandler();
        utility::string_t GetReceiveQueryString(std::shared_ptr<Connection> connection, utility::string_t data) const;
        virtual void OnStart(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken, std::shared_ptr<TransportInitializationHandler> initializeHandler) = 0;
        virtual void OnAbort() = 0;

    private:
        std::shared_ptr<IHttpClient> pHttpClient;
        std::shared_ptr<TransportAbortHandler> pAbortHandler;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
