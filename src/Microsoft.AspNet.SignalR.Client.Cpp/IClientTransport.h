#pragma once

class Connection;

#include <string>
#include "NegotiateResponse.h"

using namespace std;

class IClientTransport
{
public:
    IClientTransport(void);
    ~IClientTransport(void);

    typedef void (*START_CALLBACK)(exception* error, void* state);
    typedef void (*NEGOTIATE_CALLBACK)(NegotiateResponse* negotiateResponse, exception* error, void* state);

    virtual void Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state = NULL) = 0;
    virtual void Start(Connection* connection, START_CALLBACK startCallback, void* state = NULL) = 0;
    virtual void Send(Connection* connection, string data) = 0;
    virtual void Stop(Connection* connection) = 0;
    virtual void Abort(Connection connection) = 0;
};

