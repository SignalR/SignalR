#include "Connection.h"
#include "IConnectionHandler.h"
#include "LongPollingTransport.h"

Connection::Connection(utility::string_t uri, IConnectionHandler* handler)
{
    mUri = uri;
    mState = State::Disconnected;
    mHandler = handler;
}

void Connection::Start() 
{
    // Start(new DefaultHttpClient());
    Start(new http_client(L"http://junk"));
}

void Connection::Start(http_client* client) 
{	
    // Start(new AutoTransport(client));
	Start(new LongPollingTransport(client));
}

void Connection::Start(IClientTransport* transport) 
{	
    mTransport = transport;

    if(ChangeState(State::Disconnected, State::Connecting))
    {
        mTransport->Negotiate(this, &Connection::OnNegotiateCompleted, this);
    }
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

string Connection::GetConnectionToken()
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

void Connection::OnNegotiateCompleted(NegotiationResponse* negotiateResponse, exception* error, void* state) 
{	
    auto connection = (Connection*)state;

    if(error == NULL) 
    {
        if(negotiateResponse->ProtocolVersion != "1.2")
        {
            connection->OnError(exception("Invalid protocol version"));
            connection->Stop();
        }
        else
        {
            connection->SetConnectionState(*negotiateResponse);
            connection->GetTransport()->Start(connection, Connection::OnTransportStartCompleted, "", connection);
        }
    }
    else 
    {
        connection->OnError(exception("Negotiation failed"));
        connection->Stop();
    }
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
