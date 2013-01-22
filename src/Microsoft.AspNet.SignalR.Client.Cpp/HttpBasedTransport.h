#pragma once

#include "IClientTransport.h"
#include "Connection.h"
#include "IHttpClient.h"
#include "TransportHelper.h"

class HttpBasedTransport :
    public IClientTransport
{

protected:
    IHttpClient* mHttpClient;

public:
    HttpBasedTransport(IHttpClient* httpClient);
    ~HttpBasedTransport(void);

    void Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state = NULL);
    void Send(Connection* connection, string data);
    void Abort(Connection* connection);

private:
        static void OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

