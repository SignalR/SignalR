#pragma once

#include "HttpBasedTransport.h"
#include "EventSourceStreamReader.h"

using namespace utility;

class ServerSentEventsTransport : 
    public HttpBasedTransport
{
public:
    ServerSentEventsTransport(IHttpClient* client);
    ~ServerSentEventsTransport(void);
    bool SupportsKeepAlive();

protected:
    void OnStart(Connection* connection, string_t data, cancellation_token disconnectToken, call<int>* initializeCallback, call<int>* errorCallback);
    void LostConnection(Connection* connection);

private:
    HttpRequestWrapper* mRequest;

    void Reconnect(Connection* connection, string_t data);
    void OpenConnection(Connection* connection, string_t data, cancellation_token disconnectToken, call<int>* initializeCallback, call<int>* errorCallback);
    static void OnReadLine(string data, exception* error, void* state);
};

