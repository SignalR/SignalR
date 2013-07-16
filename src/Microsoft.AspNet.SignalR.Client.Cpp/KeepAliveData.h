//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

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