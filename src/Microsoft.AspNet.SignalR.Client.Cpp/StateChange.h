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
    string_t GetOldStateName();
    ConnectionState GetNewState();
    string_t GetNewStateName();

private:
    string_t StateName(ConnectionState state);
    ConnectionState mOldState;
    ConnectionState mNewState;
};