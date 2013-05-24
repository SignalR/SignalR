#pragma once

#include <string>
#include "Connection.h"

using namespace std;

class IConnectionHandler
{
public:
    IConnectionHandler(void);
    virtual ~IConnectionHandler(void);

    virtual void OnStateChanged(Connection::ConnectionState old_state, Connection::ConnectionState new_state) = 0;
    virtual void OnError(exception error) = 0;
    virtual void OnReceived(string_t data) = 0;
};

