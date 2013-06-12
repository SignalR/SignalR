#include "ThreadSafeInvoker.h"

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

template <typename T>
bool ThreadSafeInvoker::Invoke(function<void(T)> function, T arg)
{
    if (!atomic_exchange<bool>(&mInvoked, true))
    {
        function(arg);
        return true;
    }
    return false;
}

template <typename T1, typename T2>
bool ThreadSafeInvoker::Invoke(function<void(T1, T2)> function, T1 arg1, T2 arg2)
{
    if (!atomic_exchange<bool>(&mInvoked, true))
    {
        function(arg1, arg2);
        return true;
    }

    return false;
}

bool ThreadSafeInvoker::Invoke()
{
    return Invoke([](){});
}