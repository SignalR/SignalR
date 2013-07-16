//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include <atomic>

using namespace std;
using namespace pplx;

namespace MicrosoftAspNetSignalRClientCpp
{
    class ThreadSafeInvoker
    {
    public:
        ThreadSafeInvoker();
        ~ThreadSafeInvoker();

        bool Invoke(function<void()> function);
    
        template <typename T>
        bool Invoke(function<void(T)> function, T arg)
        {
            if (!atomic_exchange<bool>(&mInvoked, true))
            {
                function(arg);
                return true;
            }
            return false;
        }

        template <typename T1, typename T2>
        bool Invoke(function<void(T1, T2)> function, T1 arg1, T2 arg2)
        {
            if (!atomic_exchange<bool>(&mInvoked, true))
            {
                function(arg1, arg2);
                return true;
            }

            return false;
        }

        bool Invoke();

    private:
        atomic<bool> mInvoked;
    };
} // namespace MicrosoftAspNetSignalRClientCpp