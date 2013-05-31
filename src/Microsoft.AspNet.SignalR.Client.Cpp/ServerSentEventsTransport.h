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

    //pplx::task<void> Start(Connection* connection, utility::string_t data, void* state = NULL);

protected:
    void OnStart(Connection* connection, string_t data);

private:
    void OpenConnection(Connection* connection, string_t data);
    static void OnReadLine(string data, exception* error, void* state);
    static bool EqualsIgnoreCase(string_t string1, string_t string2);
};

