//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "ThreadSafeInvoker.h"

namespace MicrosoftAspNetSignalRClientCpp
{

ThreadSafeInvoker::ThreadSafeInvoker()
{
    mInvoked = false;
}


ThreadSafeInvoker::~ThreadSafeInvoker()
{
}

bool ThreadSafeInvoker::Invoke(function<void()> function)
{
    if (!atomic_exchange<bool>(&mInvoked, true))
    {
        function();
        return true;
    }
    return false;
}

bool ThreadSafeInvoker::Invoke()
{
    return Invoke([](){});
}

} // namespace MicrosoftAspNetSignalRClientCpp