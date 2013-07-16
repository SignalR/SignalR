//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "HttpBasedTransport.h"
#include "EventSourceStreamReader.h"
#include "ThreadSafeInvoker.h"
#include "TaskAsyncHelper.h"

using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
class ServerSentEventsTransport : 
    public HttpBasedTransport
{
    public:
        ServerSentEventsTransport(shared_ptr<IHttpClient> client);
        ~ServerSentEventsTransport();
    
        bool SupportsKeepAlive();

    protected:
        void OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, shared_ptr<TransportInitializationHandler> initializeHandler);
        void OnAbort();
        void LostConnection(shared_ptr<Connection> connection);

    private:
        bool mStop;
        seconds mConnectionTimeout;
        seconds mReconnectDelay;
        mutex mDeregisterRequestCancellationLock;
        function<void()> DeregisterRequestCancellation;
        shared_ptr<HttpRequestWrapper> pRequest;
        unique_ptr<EventSourceStreamReader> pEventSource;
        unique_ptr<ThreadSafeInvoker> pCallbackInvoker;
        shared_ptr<TransportInitializationHandler> pInitializeHandler;

        void Reconnect(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken);
        void OpenConnection(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, function<void()> initializeCallback, function<void(exception)> errorCallback);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
