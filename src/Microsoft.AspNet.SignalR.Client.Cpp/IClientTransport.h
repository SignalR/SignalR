#pragma once

class Connection;

#include "NegotiationResponse.h"

using namespace std;

class IClientTransport
{
public:
    IClientTransport(void);
    ~IClientTransport(void);

    typedef void (*START_CALLBACK)(exception* error, void* state);
    typedef void (*NEGOTIATE_CALLBACK)(NegotiationResponse* negotiateResponse, exception* error, void* state);
    virtual pplx::task<NegotiationResponse*> Negotiate(Connection* connection) = 0;
    virtual pplx::task<void> Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL) = 0;
    virtual void Send(Connection* connection, string data) = 0;
    virtual void Stop(Connection* connection) = 0;
    virtual void Abort(Connection* connection) = 0;
};

