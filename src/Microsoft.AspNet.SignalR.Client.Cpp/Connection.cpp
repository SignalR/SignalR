#include "Connection.h"
#include "LongPollingTransport.h"
#include "ServerSentEventsTransport.h"

Connection::Connection(string_t uri)
{
    mUri = uri;
    if (!(mUri.back() == U('/')))
    {
        mUri += U("/");
    }

    mState = ConnectionState::Disconnected;

    mProtocol = U("1.3");
}

Connection::~Connection()
{
}

task<void> Connection::Start() 
{
    // Start(new DefaultHttpClient());
    return Start(new DefaultHttpClient());
}

task<void> Connection::Start(IHttpClient* client) 
{	
    // Start(new AutoTransport(client));
    //return Start(new WebSocketTransport(client));
    //return Start(new LongPollingTransport(client));
    return Start(new ServerSentEventsTransport(client));
}

task<void> Connection::Start(IClientTransport* transport) 
{	
    mDisconnectCts = new cancellation_token_source();
    mTransport = transport;

    if(!ChangeState(ConnectionState::Disconnected, ConnectionState::Connecting))
    {
        // temp failure resolution
        return pplx::task<void>();
    }
    
    return Negotiate(transport);
}

task<void> Connection::Negotiate(IClientTransport* transport) 
{
    return mTransport->Negotiate(this).then([this](NegotiationResponse* response)
    {
        mConnectionId = response->ConnectionId;
        mConnectionToken = response->ConnectionToken;

        StartTransport();
        ChangeState(ConnectionState::Connecting, ConnectionState::Connected);

    });
}

task<void> Connection::StartTransport()
{
    return mTransport->Start(this, U(""), mDisconnectCts->get_token());
}

task<void> Connection::Send(value::field_map object)
{
    stringstream_t stream;
    value v1 = value::object(object);
    v1.serialize(stream); 

    return Send(stream.str());
}

task<void> Connection::Send(string_t data)
{
    return mTransport->Send(this, data);
}

bool Connection::ChangeState(ConnectionState oldState, ConnectionState newState)
{
    if(mState == oldState)
    {
        SetState(newState);
        return true;
    }

    // Invalid transition
    return false;
}

bool Connection::EnsureReconnecting()
{
    ChangeState(ConnectionState::Connected, ConnectionState::Reconnecting);
            
    return mState == ConnectionState::Reconnecting;
}


void Connection::Stop() 
{
    
}

void Connection::Disconnect()
{
    mStateLock.lock();

    if (mState != ConnectionState::Disconnected)
    {
        SetState(ConnectionState::Disconnected);

        mConnectionId.clear();
        mConnectionToken.clear();
        mGroupsToken.clear();
        mMessageId.clear();

        if (Closed != NULL)
        {
            Closed();
        }
    }

    mStateLock.unlock();
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

void Connection::OnError(exception& ex)
{
    if (Error != NULL)
    {
        Error(ex);
    }
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

void Connection::OnReconnecting()
{
    if (Reconnecting != NULL)
    {
        Reconnecting();
    }
}

void Connection::OnReconnected()
{
    if (Reconnected != NULL)
    {
        Reconnected();
    }
}

void Connection::OnConnectionSlow()
{
    if (ConnectionSlow != NULL)
    {
        ConnectionSlow();
    }
}

void Connection::PrepareRequest(HttpRequestWrapper* request)
{

}

void Connection::SetConnectionToken(string_t connectionToken)
{
    mConnectionToken = connectionToken;
}

void Connection::SetConnectionId(string_t connectionId)
{
    mConnectionId = connectionId;
}

void IConnection::SetMessageId(string_t messageId)
{
    mMessageId = messageId;
}

void Connection::SetState(ConnectionState newState)
{
    mStateLock.lock();

    StateChange* stateChange = new StateChange(mState, newState);
    mState = newState;

    if (StateChanged != NULL)
    {
        StateChanged(stateChange);
    }
    
    delete stateChange;

    mStateLock.unlock();
}
