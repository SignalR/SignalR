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
    void OnStart(Connection* connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback);
    void LostConnection(Connection* connection);

private:
    shared_ptr<HttpRequestWrapper> mRequest;
    unique_ptr<EventSourceStreamReader> mEventSource;
    bool mStop;

    void Reconnect(Connection* connection, string_t data);
    void OpenConnection(Connection* connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback);
    static void OnReadLine(string data, exception* error, void* state);
};

