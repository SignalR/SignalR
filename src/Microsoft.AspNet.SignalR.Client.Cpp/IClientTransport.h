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

    virtual task<shared_ptr<NegotiationResponse>> Negotiate(shared_ptr<Connection> connection) = 0;
    virtual task<void> Start(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken) = 0;
    virtual task<void> Send(Connection* connection, string_t data) = 0;
    virtual void Abort(shared_ptr<Connection> connection) = 0;
    virtual void Dispose() = 0;
    virtual void LostConnection(Connection* connection) = 0;

protected:
    string_t mTransportName;
    bool mSupportKeepAlive;
};

