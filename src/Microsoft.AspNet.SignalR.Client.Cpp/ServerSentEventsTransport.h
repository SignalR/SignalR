#pragma once


#include "HttpBasedTransport.h"

class ServerSentEventsTransport : 
    public HttpBasedTransport
{
public:
    ServerSentEventsTransport(IHttpClient* client);
    ~ServerSentEventsTransport(void);

    void Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL);
    void Stop(Connection* connection);
    void Abort(Connection* connection);

    struct StartHttpRequestInfo
    {
        void* UserState;
        ServerSentEventsTransport* Transport;
        START_CALLBACK Callback;
        Connection* Connection;
    };

    struct ReadInfo
    {
        Connection* Connection;
        IHttpResponse* HttpResponse;
        ServerSentEventsTransport* Transport;
        StartHttpRequestInfo* StartInfo;
    };

    void ReadLoop(IHttpResponse* httpResponse, Connection* connection, StartHttpRequestInfo* startInfo);

private:
    static void OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);

    static void OnReadLine(string data, exception* error, void* state);
};

