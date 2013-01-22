#pragma once


#include "IClientTransport.h"
#include "Connection.h"
#include "IHttpClient.h"
#include "TransportHelper.h"

class ServerSentEventsTransport : 
    public IClientTransport
{
public:
    ServerSentEventsTransport(IHttpClient* client);
    ~ServerSentEventsTransport(void);

    void Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state = NULL);
    void Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL);
    void Send(Connection* connection, string data);
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
    };

    void ReadLoop(IHttpResponse* httpResponse, Connection* connection);

private:
    IHttpClient* mHttpClient; 
    
    static void OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
    static void OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);

    static void OnReadLine(string data, exception* error, void* state);
};

