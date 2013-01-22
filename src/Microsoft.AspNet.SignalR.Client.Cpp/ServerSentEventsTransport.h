#pragma once


#include "HttpBasedTransport.h"

class ServerSentEventsTransport : 
    public HttpBasedTransport
{
public:
    ServerSentEventsTransport(IHttpClient* client);
    ~ServerSentEventsTransport(void);

    void Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL);
    void Abort(Connection* connection);

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

private:
    static void OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);

    static void OnReadLine(string data, exception* error, void* state);
};

