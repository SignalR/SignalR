#include "Connection.h"
#include "IConnectionHandler.h"
#include "LongPollingTransport.h"
#include "WebSocketTransport.h"

Connection::Connection(utility::string_t uri, IConnectionHandler* handler)
{
    mUri = uri;
    mState = State::Disconnected;
    mHandler = handler;
}

pplx::task<void> Connection::Start() 
{
    // Start(new DefaultHttpClient());
    return Start(new http_client(L"http://junk"));
}

pplx::task<void> Connection::Start(http_client* client) 
{	
    // Start(new AutoTransport(client));
    //return Start(new WebSocketTransport(client));
    return Start(new LongPollingTransport(client));
}

pplx::task<void> Connection::Start(IClientTransport* transport) 
{	
    mTransport = transport;

    if(ChangeState(State::Disconnected, State::Connecting))
    {
        mTransport->Negotiate(this).then([this](NegotiationResponse* response)
        {
            return StartTransport();
        });
    }
    
    // temp failure resolution
    return pplx::task<void>();
}

pplx::task<void> Connection::StartTransport()
{
    return mTransport->Start(this, NULL, NULL);
}

void Connection::Send(string data)
{
    mTransport->Send(this, data);
}

bool Connection::ChangeState(State oldState, State newState)
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
    ChangeState(State::Connected, State::Reconnecting);
            
    return mState == State::Reconnecting;
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

IClientTransport* Connection::GetTransport()
{
    return mTransport;
}

utility::string_t Connection::GetUri()
{
    return mUri;
}

utility::string_t Connection::GetConnectionToken()
{
    return mConnectionToken;
}

string Connection::GetGroupsToken()
{
    return mGroupsToken;
}

string Connection::GetMessageId()
{
    return mMessageId;
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
        connection->ChangeState(State::Connecting, State::Connected);
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
