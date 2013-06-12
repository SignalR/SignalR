#include "StateChange.h"

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

string_t StateChange::GetOldStateName()
{
    return StateName(mOldState);
}

string_t StateChange::GetNewStateName()
{
    return StateName(mNewState);
}

string_t StateChange::StateName(ConnectionState state)
{
    switch (state)
    {
    case Connecting:
        return U("Connecting");
        break;
    case Connected:
        return U("Connected");
        break;
    case Reconnecting:
        return U("Reconnecting");
        break;
    case Disconnected:
        return U("Disconnected");
        break;
    default:
        throw(exception("InvalidArgumentException: state"));
        break;
    }
}
