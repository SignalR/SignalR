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

pplx::task<void> Connection::Start() 
{
    return Start(shared_ptr<IHttpClient>(new DefaultHttpClient()));
}

pplx::task<void> Connection::Start(shared_ptr<IHttpClient> client) 
{	
    //return Start(new AutoTransport(client));
    return Start(shared_ptr<IClientTransport>(new ServerSentEventsTransport(client)));
}

pplx::task<void> Connection::Start(shared_ptr<IClientTransport> transport) 
{	
    mStartLock.lock();

    mConnectTask = pplx::task<void>();
    mDisconnectCts = unique_ptr<pplx::cancellation_token_source>(new pplx::cancellation_token_source());

    if(!ChangeState(ConnectionState::Disconnected, ConnectionState::Connecting))
    {
        // temp failure resolution
        return mConnectTask; 
    }
    
    mTransport = transport;
    mConnectTask = Negotiate(transport);
    
    mStartLock.unlock();

    return mConnectTask;
}

pplx::task<void> Connection::Negotiate(shared_ptr<IClientTransport> transport) 
{
    return mTransport->Negotiate(shared_from_this()).then([this](shared_ptr<NegotiationResponse> response)
    {
        mConnectionId = response->ConnectionId;
        mConnectionToken = response->ConnectionToken;

        StartTransport();
    });
}

pplx::task<void> Connection::StartTransport()
{
    return mTransport->Start(shared_from_this(), U(""), mDisconnectCts->get_token()).then([this]()
    {
        ChangeState(ConnectionState::Connecting, ConnectionState::Connected);
    });
}

pplx::task<void> Connection::Send(value::field_map object)
{
    stringstream_t stream;
    value v1 = value::object(object);
    v1.serialize(stream); 

    return Send(stream.str());
}

pplx::task<void> Connection::Send(string_t data)
{
    return mTransport->Send(shared_from_this(), data);
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
    mStartLock.lock();

    if (mConnectTask != pplx::task<void>())
    {
        try
        {
            mConnectTask.wait();
        }
        catch (exception& ex)
        {

        }
    }

    if (mState != ConnectionState::Disconnected)
    {
        mTransport->Abort(shared_from_this());

        Disconnect();

        if (mTransport)
        {
            mTransport->Dispose();
        }
    }

    mStartLock.unlock();
}

void Connection::Disconnect()
{
    //mStateLock.lock();

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

    //mStateLock.unlock();
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

void Connection::PrepareRequest(shared_ptr<HttpRequestWrapper> request)
{

}

void IConnection::SetMessageId(string_t messageId)
{
    mMessageId = messageId;
}

void Connection::SetState(ConnectionState newState)
{
    mStateLock.lock();

    shared_ptr<StateChange> stateChange = shared_ptr<StateChange>(new StateChange(mState, newState));
    mState = newState;

    if (StateChanged != NULL)
    {
        StateChanged(stateChange);
    }

    mStateLock.unlock();
}
