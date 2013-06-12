#include "TaskAsyncHelper.h"

TaskAsyncHelper::TaskAsyncHelper()
{
}

TaskAsyncHelper::~TaskAsyncHelper()
{
}

pplx::task<void> TaskAsyncHelper::Delay(seconds seconds)
{
    task_completion_event<void> tce;
    shared_ptr<timer<int>> timer_once = shared_ptr<timer<int>>(new timer<int>(seconds.count()*1000, 0, nullptr, false));
    shared_ptr<call<int>> callback = shared_ptr<call<int>>(new call<int>([tce](int)
    {
        tce.set();
    }));

    timer_once->link_target(callback.get());
    timer_once->start();

    task<void> event_set(tce);

    return pplx::concurrency_task_to_pplx_task<void>(event_set.then([timer_once, callback]()
    {
        // just to prevent the reference count of timer_once and callback from being 0 until the delay is over
    }));
}
