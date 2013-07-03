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
        shared_ptr<HttpRequestWrapper> pRequest;
        unique_ptr<EventSourceStreamReader> pEventSource;
        unique_ptr<ThreadSafeInvoker> pCallbackInvoker;
        shared_ptr<TransportInitializationHandler> pInitializeHandler;

        void Reconnect(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken);
        void OpenConnection(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, function<void()> initializeCallback, function<void(exception)> errorCallback);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
