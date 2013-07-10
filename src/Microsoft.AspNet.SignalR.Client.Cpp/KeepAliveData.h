#pragma once

#include <ctime>

using namespace std;

namespace MicrosoftAspNetSignalRClientCpp
{
    class KeepAliveData
    {
    public:
        KeepAliveData(int timeout);
        KeepAliveData(time_t lastKeepAlive, int timeout, int timeoutWarning, int checkInterval);
        ~KeepAliveData();
        
        time_t GetLastKeepAlive();
        int GetTimeout();
        int GetTimeoutWarning();
        int GetCheckInterval();

        void SetLastKeepAlive(time_t lastKeepAlive);

    private:
        const double cKeepAliveWarnAt;
        time_t mLastKeepAlive;
        int mTimeout;
        int mTimeoutWarning;
        int mCheckInterval;
    };
}