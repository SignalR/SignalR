//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "KeepAliveData.h"

namespace MicrosoftAspNetSignalRClientCpp
{

KeepAliveData::KeepAliveData(int timeout) : cKeepAliveWarnAt(2.0/3.0)
{
    mTimeout = timeout;
    mTimeoutWarning = (int)timeout*cKeepAliveWarnAt;
    mCheckInterval = (mTimeout - mTimeoutWarning)/3;
}

KeepAliveData::KeepAliveData(time_t lastKeepAlive, int timeout, int timeoutWarning, int checkInterval) : cKeepAliveWarnAt(2.0/3.0)
{
    mLastKeepAlive = lastKeepAlive;
    mTimeout = timeout;
    mTimeoutWarning = timeoutWarning;
    mCheckInterval = checkInterval;
}

KeepAliveData::~KeepAliveData()
{
}

time_t KeepAliveData::GetLastKeepAlive() const
{
    return mLastKeepAlive;
}

int KeepAliveData::GetTimeout() const
{
    return mTimeout;
}

int KeepAliveData::GetTimeoutWarning() const
{
    return mTimeoutWarning;
}

int KeepAliveData::GetCheckInterval() const
{
    return mCheckInterval;
}

void KeepAliveData::SetLastKeepAlive(time_t lastKeepAlive)
{
    mLastKeepAlive = lastKeepAlive;
}

} // namespace MicrosoftAspNetSignalRClientCpp