//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once
#include "httpbasedtransport.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class LongPollingTransport :
        public HttpBasedTransport
    {
    public:
        LongPollingTransport(shared_ptr<IHttpClient> httpClient);
        ~LongPollingTransport();

    protected:
        void OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, shared_ptr<TransportInitializationHandler> initializeHandler);
        void OnAbort();
        void LostConnection(shared_ptr<Connection> connection);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
