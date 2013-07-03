#include "TaskAsyncHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

TaskAsyncHelper::TaskAsyncHelper()
{
}

TaskAsyncHelper::~TaskAsyncHelper()
{
}

pplx::task<void> TaskAsyncHelper::Delay(seconds seconds, pplx::cancellation_token ct)
{
    return DelayedTaskHelper::Create(seconds, ct).then([](){}, ct);
}

DelayedTaskHelper::DelayedTaskHelper()
{
}

DelayedTaskHelper::~DelayedTaskHelper()
{
}

pplx::task<void> DelayedTaskHelper::Create(utility::seconds seconds, pplx::cancellation_token ct)
{
    // using raw pointers here, ported from casablanca cpp rest sdk
    auto helper = new DelayedTaskHelper();

    if (ct != pplx::cancellation_token::none())
    {
        ct.register_callback([helper]()
        {
            helper->mTimer.stop(false);
            delete helper;
        });
    }

    auto task = pplx::create_task(helper->mTce);
    helper->mTimer.start(seconds.count()*1000, false, TimerCallback, (void *)helper);
    return task;
}

void DelayedTaskHelper::TimerCallback(void* context)
{
    auto helper = static_cast<DelayedTaskHelper*>(context);
    helper->mTce.set();
    delete helper;
}

} // namespace MicrosoftAspNetSignalRClientCpp