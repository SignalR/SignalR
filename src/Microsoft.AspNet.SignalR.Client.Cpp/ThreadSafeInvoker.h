#pragma once

#include <http_client.h>
#include <atomic>

using namespace std;
using namespace pplx;

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