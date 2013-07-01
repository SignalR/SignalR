#include "TaskAsyncHelper.h"

TaskAsyncHelper::TaskAsyncHelper()
{
}

TaskAsyncHelper::~TaskAsyncHelper()
{
}

pplx::task<void> TaskAsyncHelper::Delay(seconds seconds, pplx::cancellation_token ct)
{
    auto tce = shared_ptr<task_completion_event<void>>(new task_completion_event<void>());
    auto timer_once = shared_ptr<timer<int>>(new timer<int>(seconds.count()*1000, 0, nullptr, false));
    auto callback = shared_ptr<call<int>>(new call<int>([tce](int)
    {
        tce->set();
    }));

    timer_once->link_target(callback.get());
    timer_once->start();

    task<void> event_set(*(tce.get()));

    cancellation_token_source concurrencyCts;
    ct.register_callback([concurrencyCts]()
    {
        concurrencyCts.cancel();
    });

    return pplx::concurrency_task_to_pplx_task<void>(event_set.then([timer_once, callback](task<void> task)
    {
        try
        {
            task.get();
        }
        catch(task_canceled& canceled)
        {
            cout << "I got canceled" << endl;
        }
    }, concurrencyCts.get_token()));
    //.then([timer_once, callback](pplx::task<void> task)
    //{
    //    if (is_task_cancellation_requested())
    //    {
    //        cancel_current_task();
    //    }
    //    // just to prevent the reference count of timer_once and callback from being 0 until the delay is over
    //}, taskOptions);
}