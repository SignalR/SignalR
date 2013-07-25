//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "ConnectionState.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class StateChange
    {
    public:
        StateChange(ConnectionState oldState, ConnectionState newState);
        ~StateChange(void);
    
        ConnectionState GetOldState() const;
        ConnectionState GetNewState() const;

    private:
        ConnectionState mOldState;
        ConnectionState mNewState;
    };
} // namespace MicrosoftAspNetSignalRClientCpp