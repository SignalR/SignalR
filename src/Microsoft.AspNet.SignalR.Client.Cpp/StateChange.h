#pragma once

#include "ConnectionState.h"
#include <http_client.h>

using namespace std;
using namespace utility;

class StateChange
{
public:
    StateChange(ConnectionState oldState, ConnectionState newState);
    ~StateChange(void);
    
    ConnectionState GetOldState();
    string_t GetOldStateName(); // for tracing purposes
    ConnectionState GetNewState();
    string_t GetNewStateName(); // for tracing purposes

private:
    string_t StateName(ConnectionState state);
    ConnectionState mOldState;
    ConnectionState mNewState;
};