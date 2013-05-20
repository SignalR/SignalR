#pragma once

#include "IClientTransport.h"
#include "Connection.h"
#include "TransportHelper.h"

class WebSocketTransport :
    public IClientTransport
{
public:
    WebSocketTransport(http_client* httpClient);
    ~WebSocketTransport(void);
    
    pplx::task<NegotiationResponse*> Negotiate(Connection* connection);
    pplx::task<void> Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL);
    void Send(Connection* connection, string data);
    void Stop(Connection* connection);
    void Abort(Connection* connection);

protected:
    http_client* mHttpClient;
    Connection* mConnection;

private:
    pplx::task<void> PerformConnect(bool reconnecting = false);
};