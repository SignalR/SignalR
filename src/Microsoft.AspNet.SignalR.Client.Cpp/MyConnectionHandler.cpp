#include "MyConnectionHandler.h"
#include <iostream>

MyConnectionHandler::MyConnectionHandler(void)
{
}


MyConnectionHandler::~MyConnectionHandler(void)
{
}

void MyConnectionHandler::OnError(exception error)
{
    cout << "An error occurred: " << error.what() << endl;
}

void MyConnectionHandler::OnReceived(string data)
{
    cout << data << endl;
}

void MyConnectionHandler::OnStateChanged(Connection::State old_state, Connection::State new_state)
{
    cout << "state changed: " << old_state << " -> " << new_state << endl;
}
