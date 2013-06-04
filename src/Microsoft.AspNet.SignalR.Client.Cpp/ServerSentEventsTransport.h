#pragma once

#include "HttpBasedTransport.h"
#include "EventSourceStreamReader.h"

using namespace utility;

class ServerSentEventsTransport : 
    public HttpBasedTransport
{
public:
    ServerSentEventsTransport(http_client* client);
    ~ServerSentEventsTransport(void);
    bool SupportsKeepAlive();

protected:
    void OnStart(Connection* connection, string_t data, call<int>* initializeCallback, call<int>* errorCallback);
    void LostConnection(Connection* connection);

private:
    void OpenConnection(Connection* connection, string_t data, call<int>* initializeCallback, call<int>* errorCallback);
    static void OnReadLine(string data, exception* error, void* state);
};

