#pragma once

class Connection;

#include "NegotiationResponse.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;

class IClientTransport
{
public:
    IClientTransport(void);
    ~IClientTransport(void);

    string_t GetTransportName();
    bool SupportsKeepAlive();

    virtual task<NegotiationResponse*> Negotiate(Connection* connection) = 0;
    virtual task<void> Start(Connection* connection, string_t data, cancellation_token disconnectToken) = 0;
    virtual task<void> Send(Connection* connection, string_t data) = 0;
    virtual void Abort(Connection* connection) = 0;

    virtual void LostConnection(Connection* connection) = 0;

protected:
    string_t mTransportName;
    bool mSupportKeepAlive;
};

