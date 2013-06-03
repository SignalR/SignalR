#pragma once

class Connection;

#include "NegotiationResponse.h"

using namespace std;
using namespace utility;

class IClientTransport
{
public:
    IClientTransport(void);
    ~IClientTransport(void);

    string_t GetTransportName();
    bool SupportsKeepAlive();

    virtual pplx::task<NegotiationResponse*> Negotiate(Connection* connection) = 0;
    virtual pplx::task<void> Start(Connection* connection, string_t data, void* state = NULL) = 0;
    virtual pplx::task<void> Send(Connection* connection, string_t data) = 0;
    virtual void Abort(Connection* connection) = 0;

    virtual void LostConnection(Connection* connection) = 0;

protected:
    string_t mTransportName;
    bool mSupportKeepAlive;
};

