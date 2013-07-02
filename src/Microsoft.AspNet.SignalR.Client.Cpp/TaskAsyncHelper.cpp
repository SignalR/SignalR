#include "TaskAsyncHelper.h"

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
    auto helper = new DelayedTaskHelper();

    ct.register_callback([helper]()
    {
        helper->mTimer.stop(false);
        delete helper;
    });

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