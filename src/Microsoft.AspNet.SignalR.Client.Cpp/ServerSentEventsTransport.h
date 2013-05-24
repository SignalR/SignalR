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

    struct HttpRequestInfo
    {
        START_CALLBACK Callback;
        void* CallbackState;
        ServerSentEventsTransport* Transport;
        Connection* Connection;
        string Data;
    };

    struct ReadInfo
    {
        Connection* Connection;
        IHttpResponse* HttpResponse;
        ServerSentEventsTransport* Transport;
        HttpRequestInfo* RequestInfo;
    };

    void ReadLoop(IHttpResponse* httpResponse, Connection* connection, HttpRequestInfo* reqestInfo);

protected:
    void OnStart(Connection* connection, string_t data);

private:
    static void OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
    void OpenConnection(Connection* connection, string_t data);
    static void OnReadLine(string data, exception* error, void* state);
    static bool EqualsIgnoreCase(string_t string1, string_t string2);
};

