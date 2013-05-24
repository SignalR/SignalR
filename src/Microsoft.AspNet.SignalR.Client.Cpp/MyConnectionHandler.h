#pragma once
#include "iconnectionhandler.h"
class MyConnectionHandler :
    public IConnectionHandler
{
public:
    MyConnectionHandler(void);
    ~MyConnectionHandler(void);

    void OnReceived(string data);
    void OnError(exception error);
    void OnStateChanged(Connection::ConnectionState old_state, Connection::ConnectionState new_state);
};

