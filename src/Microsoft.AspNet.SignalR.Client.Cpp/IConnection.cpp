#include "IConnection.h"

shared_ptr<IClientTransport> IConnection::GetTransport()
{
    return mTransport;
}

string_t IConnection::GetUri()
{
    return mUri;
}

string_t IConnection::GetConnectionId()
{
    return mConnectionId;
}

string_t IConnection::GetConnectionToken()
{
    return mConnectionToken;
}

string_t IConnection::GetGroupsToken()
{
    return mGroupsToken;
}

string_t IConnection::GetMessageId()
{
    return mMessageId;
}

string_t IConnection::GetQueryString()
{
    return mQueryString;
}

string_t IConnection::GetProtocol()
{
    return mProtocol;
}

void IConnection::SetMessageId(string_t messageId)
{
    mMessageId = messageId;
}

void IConnection::SetConnectionToken(string_t connectionToken)
{
    mConnectionToken = connectionToken;
}

void IConnection::SetConnectionId(string_t connectionId)
{
    mConnectionId = connectionId;
}

void IConnection::SetProtocol(string_t protocol)
{
    mProtocol = protocol;
}

void IConnection::SetGroupsToken(string_t groupsToken)
{
    mGroupsToken = groupsToken;
}
