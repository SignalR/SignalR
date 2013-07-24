//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "HttpBasedTransport.h"
#include "EventSourceStreamReader.h"
#include "ThreadSafeInvoker.h"

namespace MicrosoftAspNetSignalRClientCpp
{
class ServerSentEventsTransport : 
    public HttpBasedTransport
{
    public:
        ServerSentEventsTransport(std::shared_ptr<IHttpClient> client);
        ~ServerSentEventsTransport();
    
        bool SupportsKeepAlive();

    protected:
        void OnStart(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken, std::shared_ptr<TransportInitializationHandler> initializeHandler);
        void OnAbort();
        void LostConnection(std::shared_ptr<Connection> connection);

    private:
        bool mStop;
        utility::seconds mConnectionTimeout;
        utility::seconds mReconnectDelay;
        std::mutex mDeregisterRequestCancellationLock;
        std::function<void()> DeregisterRequestCancellation;
        std::shared_ptr<HttpRequestWrapper> pRequest;
        std::unique_ptr<EventSourceStreamReader> pEventSource;
        std::unique_ptr<ThreadSafeInvoker> pCallbackInvoker;
        std::shared_ptr<TransportInitializationHandler> pInitializeHandler;

        void Reconnect(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken);
        void OpenConnection(std::shared_ptr<Connection> connection, utility::string_t data, pplx::cancellation_token disconnectToken, std::function<void()> initializeCallback, std::function<void(std::exception)> errorCallback);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
