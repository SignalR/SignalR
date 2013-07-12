#include "Connection.h"
#include "HeartBeatMonitor.h"

namespace MicrosoftAspNetSignalRClientCpp
{

HeartBeatMonitor::HeartBeatMonitor()
{
}

HeartBeatMonitor::HeartBeatMonitor(shared_ptr<Connection> connection, shared_ptr<recursive_mutex> connectionStateLock)
{
    wpConnection = connection;
    pConnectionStateLock = connectionStateLock;
}

HeartBeatMonitor::~HeartBeatMonitor()
{
    mTimer.stop(false);
}

void HeartBeatMonitor::Start()
{
    if (auto pConnection = wpConnection.lock())
    {
        pConnection->UpdateLaskKeepAlive();
        mHasBeenWarned = false;
        mTimedOut = false;
        int period = pConnection->GetKeepAliveData()->GetCheckInterval()*1000;
        mTimer.start(pConnection->GetKeepAliveData()->GetCheckInterval()*1000, true, Beat, this);
    }
}

void HeartBeatMonitor::Beat(void* state)
{
    auto monitor = static_cast<HeartBeatMonitor*>(state);
    if (auto pConnection = monitor->wpConnection.lock())
    {
        monitor->Beat((int)difftime(time(0), pConnection->GetKeepAliveData()->GetLastKeepAlive()));
    }
}

void HeartBeatMonitor::Beat(int timeElapsed)
{
    lock_guard<recursive_mutex> lock(*pConnectionStateLock.get());

    if (auto pConnection = wpConnection.lock())
    {
        if (pConnection->GetState() == ConnectionState::Connected)
        {
            if (timeElapsed >= pConnection->GetKeepAliveData()->GetTimeout())
            {
                if (!mTimedOut)
                {
                    //trace
                    mTimedOut = true;
                    pConnection->GetTransport()->LostConnection(pConnection);
                }
            }
            else if (timeElapsed >= pConnection->GetKeepAliveData()->GetTimeout())
            {
                if (!mHasBeenWarned)
                {
                    //trace
                    mHasBeenWarned = true;
                    pConnection->OnConnectionSlow();
                }
            }
            else
            {
                mHasBeenWarned = false;
                mTimedOut = false;
            }
        }
    }
}

} // namespace MicrosoftAspNetSignalRClientCpp