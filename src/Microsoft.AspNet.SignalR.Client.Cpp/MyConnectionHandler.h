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
    void OnStateChanged(Connection::State old_state, Connection::State new_state);
};

