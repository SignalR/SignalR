#include "Connection.h"
#include "IConnectionHandler.h"
#include "LongPollingTransport.h"
#include "ServerSentEventsTransport.h"
#include "WebSocketTransport.h"

Connection::Connection(string_t uri, IConnectionHandler* handler)
{
    mUri = uri;
    if (!(mUri.back() == U('/')))
    {
        mUri += U("/");
    }
    mState = ConnectionState::Disconnected;
    mHandler = handler;
}

pplx::task<void> Connection::Start() 
{
    // Start(new DefaultHttpClient());
    return Start(new http_client(mUri));
}

pplx::task<void> Connection::Start(http_client* client) 
{	
    // Start(new AutoTransport(client));
    //return Start(new WebSocketTransport(client));
    //return Start(new LongPollingTransport(client));
    return Start(new ServerSentEventsTransport(client));
}

pplx::task<void> Connection::Start(IClientTransport* transport) 
{	
    mTransport = transport;

    if(!ChangeState(ConnectionState::Disconnected, ConnectionState::Connecting))
    {
        // temp failure resolution
        return pplx::task<void>();
    }
    
    return Negotiate(transport);
}

pplx::task<void> Connection::Negotiate(IClientTransport* transport) 
{
    return mTransport->Negotiate(this).then([this](NegotiationResponse* response)
    {
        mConnectionId = response->ConnectionId;
        mConnectionToken = response->ConnectionToken;

        StartTransport();
    });
}

pplx::task<void> Connection::StartTransport()
{
    return mTransport->Start(this, U(""));
}

pplx::task<void> Connection::Send(web::json::value::field_map object)
{
    stringstream_t stream;
    value v1 = value::object(object);
    v1.serialize(stream); 

    return Send(stream.str());
}

pplx::task<void> Connection::Send(string_t data)
{
    return mTransport->Send(this, data);
}

bool Connection::ChangeState(ConnectionState oldState, ConnectionState newState)
{
    if(mState == oldState)
    {
        mState = newState;

        mHandler->OnStateChanged(oldState, oldState);

        return true;
    }

    return false;
}

bool Connection::EnsureReconnecting()
{
    ChangeState(ConnectionState::Connected, ConnectionState::Reconnecting);
            
    return mState == ConnectionState::Reconnecting;
}

void Connection::SetConnectionState(NegotiationResponse negotiateResponse)
{
    mConnectionId = negotiateResponse.ConnectionId;
    mConnectionToken = negotiateResponse.ConnectionToken;
}

void Connection::OnError(exception error)
{
    mHandler->OnError(error);
}

void Connection::OnReceived(string_t message)
{
    if (Received != NULL)
    {
        try 
        {
            Received(message);
        }
        catch (exception& ex)
        {
            OnError(ex);
        }
    }
}

IClientTransport* Connection::GetTransport()
{
    return mTransport;
}

string_t Connection::GetUri()
{
    return mUri;
}

string_t Connection::GetConnectionId()
{
    return mConnectionId;
}

void Connection::SetConnectionId(string_t connectionId)
{
    mConnectionId = connectionId;
}

string_t Connection::GetConnectionToken()
{
    return mConnectionToken;
}

void Connection::SetConnectionToken(string_t connectionToken)
{
    mConnectionToken = connectionToken;
}

void Connection::SetGroupsToken(string_t groupsToken)
{
    mGroupsToken = groupsToken;
}

string_t Connection::GetGroupsToken()
{
    return mGroupsToken;
}

string_t Connection::GetMessageId()
{
    return mMessageId;
}

void Connection::SetMessageId(string_t messageId)
{
    mMessageId = messageId;
}

void Connection::Stop() 
{
    mTransport->Stop(this);
}

void Connection::OnTransportStartCompleted(exception* error, void* state) 
{
    auto connection = (Connection*)state;

    if(NULL != error)
    {
        connection->ChangeState(ConnectionState::Connecting, ConnectionState::Connected);
    }
    else 
    {
        connection->OnError(*error);
        connection->Stop();
    }
}

Connection::~Connection()
{
}
