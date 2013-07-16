//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "StateChange.h"

namespace MicrosoftAspNetSignalRClientCpp
{

StateChange::StateChange(ConnectionState oldState, ConnectionState newState)
{
    mOldState = oldState;
    mNewState = newState;
}

StateChange::~StateChange(void)
{
}

ConnectionState StateChange::GetOldState()
{
    return mOldState;
}

ConnectionState StateChange::GetNewState()
{
    return mNewState;
}

} // namespace MicrosoftAspNetSignalRClientCpp