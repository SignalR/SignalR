#include "IConnection.h"

IClientTransport* IConnection::GetTransport()
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

void IConnection::SetProtocol(string_t protocol)
{
    mProtocol = protocol;
}

void IConnection::SetGroupsToken(string_t groupsToken)
{
    mGroupsToken = groupsToken;
}
