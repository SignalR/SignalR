#include "Connection.h"
#include "IConnectionHandler.h"
#include <string>

Connection::Connection(const string url, IConnectionHandler* handler)
{
    mUrl = url;
    mState = State::Disconnected;
    mHandler = handler;
}

void Connection::Start() 
{
    // Start(new DefaultHttpClient());
}

void Connection::Start(IHttpClient* client) 
{	
    // Start(new AutoTransport(client));
}

void Connection::Start(IClientTransport* transport) 
{	
    ChangeState(State::Disconnected, State::Connecting);

    string url = mUrl + "/negotiate";

    mTransport = transport;

    transport->Negotiate(this, &Connection::OnNegotiateCompleted, this);
}

void Connection::Send(string data, CONNECTION_SEND_CALLBACK callback, void* state)
{
    // TODO: Add callback here
    mTransport->Send(this, data);

    callback(this, NULL, state);
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

void Connection::SetConnectionState(NegotiateResponse negotiateResponse)
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

string Connection::GetUrl()
{
    return mUrl;
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
}
void Connection::OnNegotiateCompleted(NegotiateResponse* negotiateResponse, exception* error, void* state) 
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
