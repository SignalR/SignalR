//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "Connection.h"
#include "TransportHelper.h"
#include <agents.h>
#include "TaskAsyncHelper.h"
#include "TransportInitializationHandler.h"
#include "TransportAbortHandler.h"

using namespace utility;
using namespace web::json;

namespace MicrosoftAspNetSignalRClientCpp
{
    class HttpBasedTransport :
        public IClientTransport
    {
    public:
        HttpBasedTransport(shared_ptr<IHttpClient> httpClient, string_t transport);
        ~HttpBasedTransport(void);

        pplx::task<shared_ptr<NegotiationResponse>> Negotiate(shared_ptr<Connection> connection);
        pplx::task<void> Start(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken);
        pplx::task<void> Send(shared_ptr<Connection> connection, string_t data);
        void Abort(shared_ptr<Connection> connection, seconds timeout);

    protected:
        shared_ptr<IHttpClient> GetHttpClient();
        shared_ptr<TransportAbortHandler> GetAbortHandler();
        string_t GetReceiveQueryString(shared_ptr<Connection> connection, string_t data);
        virtual void OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, shared_ptr<TransportInitializationHandler> initializeHandler) = 0;
        virtual void OnAbort() = 0;

    private:
        shared_ptr<IHttpClient> pHttpClient;
        shared_ptr<TransportAbortHandler> pAbortHandler;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
