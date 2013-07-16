//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "LongPollingTransport.h"

namespace MicrosoftAspNetSignalRClientCpp
{

LongPollingTransport::LongPollingTransport(shared_ptr<IHttpClient> httpClient) :
    HttpBasedTransport(httpClient, U("longPolling"))
{
}

LongPollingTransport::~LongPollingTransport()
{
}

void LongPollingTransport::OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, shared_ptr<TransportInitializationHandler> initializeHandler)
//void LongPollingTransport::OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback)
{
}

void LongPollingTransport::OnAbort()
{
}

void LongPollingTransport::LostConnection(shared_ptr<Connection> connection)
{
}

} // namespace MicrosoftAspNetSignalRClientCpp