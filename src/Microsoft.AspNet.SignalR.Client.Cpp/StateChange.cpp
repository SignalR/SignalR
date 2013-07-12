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