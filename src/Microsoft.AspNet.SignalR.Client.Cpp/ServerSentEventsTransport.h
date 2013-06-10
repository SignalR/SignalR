#pragma once

#include "HttpBasedTransport.h"
#include "EventSourceStreamReader.h"
#include "ThreadSafeInvoker.h"

using namespace utility;

class ServerSentEventsTransport : 
    public HttpBasedTransport
{
public:
    ServerSentEventsTransport(shared_ptr<IHttpClient> client);
    ~ServerSentEventsTransport(void);
    bool SupportsKeepAlive();

protected:
    void OnStart(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback);
    void OnAbort();
    void LostConnection(shared_ptr<Connection> connection);

private:
    shared_ptr<HttpRequestWrapper> mRequest;
    unique_ptr<EventSourceStreamReader> mEventSource;
    bool mStop;

    void Reconnect(shared_ptr<Connection> connection, string_t data);
    void OpenConnection(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback);
};

