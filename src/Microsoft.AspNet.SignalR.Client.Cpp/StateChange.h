#pragma once

#include "ConnectionState.h"
#include <http_client.h>

using namespace std;
using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
    class StateChange
    {
    public:
        StateChange(ConnectionState oldState, ConnectionState newState);
        ~StateChange(void);
    
        ConnectionState GetOldState();
        ConnectionState GetNewState();

    private:
        ConnectionState mOldState;
        ConnectionState mNewState;
    };
} // namespace MicrosoftAspNetSignalRClientCpp