#include "WebSocketTransport.h"

WebSocketTransport::WebSocketTransport(http_client* httpClient)
{
    mHttpClient = httpClient;
}


WebSocketTransport::~WebSocketTransport(void)
{
    delete mHttpClient;
}

pplx::task<NegotiationResponse*> WebSocketTransport::Negotiate(Connection* connection)
{
    return TransportHelper::GetNegotiationResponse(mHttpClient, connection);
}

pplx::task<void> WebSocketTransport::Start(Connection* connection, utility::string_t data, void* state)
{
    mConnection = connection;
    return PerformConnect();
}

pplx::task<void> WebSocketTransport::PerformConnect(bool reconnecting)
{
    utility::string_t uri = mConnection->GetUri() + U("connect");

    return pplx::task<void>();
}

void WebSocketTransport::Send(Connection* connection, string data)
{
    
}

void WebSocketTransport::Stop(Connection* connection)
{

}


void WebSocketTransport::Abort(Connection* connection)
{

}