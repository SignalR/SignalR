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