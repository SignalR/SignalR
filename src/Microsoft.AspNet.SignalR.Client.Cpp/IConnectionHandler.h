#pragma once

#include <string>
#include "Connection.h"

using namespace std;

class IConnectionHandler
{
public:
    IConnectionHandler(void);
    virtual ~IConnectionHandler(void);

    virtual void OnStateChanged(Connection::State old_state, Connection::State new_state) = 0;
    virtual void OnError(exception error) = 0;
    virtual void OnReceived(string data) = 0;
};

