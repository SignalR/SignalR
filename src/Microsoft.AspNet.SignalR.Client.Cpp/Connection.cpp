#include "Connection.h"
#include "LongPollingTransport.h"
#include "ServerSentEventsTransport.h"

Connection::Connection(string_t uri)
{
    if (uri.empty())
    {
        throw exception("ArgumentNullException: uri");
    }
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
    lock_guard<mutex> lock(mStartLock);

    mConnectTask = pplx::task<void>();
    mDisconnectCts = unique_ptr<pplx::cancellation_token_source>(new pplx::cancellation_token_source());

    if(!ChangeState(ConnectionState::Disconnected, ConnectionState::Connecting))
    {
        return mConnectTask; 
    }
    
    mTransport = transport;
    mConnectTask = Negotiate(transport);

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
    if (mState == ConnectionState::Disconnected)
    {
        throw exception("InvalidOperationException: Error_StartMustBeCalledBeforeDataCanBeSent");
    }
    if (mState == ConnectionState::Connecting)
    {
        throw exception("InvalidOperationException: Error_ConnectionHasNotBeenEstablished");
    }

    return mTransport->Send(shared_from_this(), data);
}

bool Connection::ChangeState(ConnectionState oldState, ConnectionState newState)
{
    lock_guard<recursive_mutex> lock (mStateLock);

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
    if(ChangeState(ConnectionState::Connected, ConnectionState::Reconnecting))
    {
        OnReconnecting();
    }
            
    return mState == ConnectionState::Reconnecting;
}


void Connection::Stop() 
{
    lock_guard<mutex> lock(mStartLock);

    if (mConnectTask != pplx::task<void>())
    {
        try
        {
            mConnectTask.wait();
        }
        catch (exception& ex)
        {
            //Trace
        }
    }

    {
        lock_guard<recursive_mutex> lock(mStateLock);

        if (mState != ConnectionState::Disconnected)
        {
            mTransport->Abort(shared_from_this());

            Disconnect();

            if (mTransport)
            {
                mTransport->Dispose();
            }
        }
    }
}

void Connection::Disconnect()
{
    lock_guard<recursive_mutex> lock(mStateLock);

    if (mState != ConnectionState::Disconnected)
    {
        SetState(ConnectionState::Disconnected);

        mConnectionId.clear();
        mConnectionToken.clear();
        mGroupsToken.clear();
        mMessageId.clear();

        if (Closed != nullptr)
        {
            Closed();
        }
    }
}

void Connection::OnError(exception& ex)
{
    if (Error != nullptr)
    {
        Error(ex);
    }
}

void Connection::OnReceived(string_t message)
{
    if (Received != nullptr)
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
    if (Reconnecting != nullptr)
    {
        Reconnecting();
    }
}

void Connection::OnReconnected()
{
    if (Reconnected != nullptr)
    {
        Reconnected();
    }
}

void Connection::OnConnectionSlow()
{
    if (ConnectionSlow != nullptr)
    {
        ConnectionSlow();
    }
}

void Connection::PrepareRequest(shared_ptr<HttpRequestWrapper> request)
{

}

void Connection::SetState(ConnectionState newState)
{
    lock_guard<recursive_mutex> lock(mStateLock);

    shared_ptr<StateChange> stateChange = shared_ptr<StateChange>(new StateChange(mState, newState));
    mState = newState;

    if (StateChanged != nullptr)
    {
        StateChanged(stateChange);
    }
}
