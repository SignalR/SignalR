#pragma once

#include <ppltasks.h>
#include <agents.h>
#include "http_client.h"
#include "pplxconv.h"

using namespace std;
using namespace utility;
using namespace concurrency;

enum TaskStatus
{
    TaskFaulted = 0,
    TaskCanceled,
    TaskCompleted
};

class TaskAsyncHelper
{
public:
    TaskAsyncHelper();
    ~TaskAsyncHelper();

    static pplx::task<void> Delay(seconds seconds, pplx::cancellation_token ct);

    template <typename T>
    static TaskStatus RunTaskToCompletion(pplx::task<T> task, T& result, exception& ex)
    {
        try
        {
            result = task.get();
            return TaskStatus::TaskCompleted;
        }
        catch(pplx::task_canceled canceled)
        {
            return TaskStatus::TaskCanceled;
        }
        catch(exception& exception)
        {
            ex = exception;
            return TaskStatus::TaskFaulted;
        }
    }

};