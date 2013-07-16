//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <ppltasks.h>
#include <agents.h>
#include "http_client.h"
#include "pplxconv.h"

using namespace std;
using namespace utility;
using namespace concurrency;

namespace MicrosoftAspNetSignalRClientCpp
{
    enum TaskStatus
    {
        TaskFaulted = 0,
        TaskCanceled,
        TaskCompleted
    };

    class DelayedTaskHelper
    {
    public:
        ~DelayedTaskHelper();

        static pplx::task<void> Create(utility::seconds delay, pplx::cancellation_token ct);

    private:
        DelayedTaskHelper();
    
        static void TimerCallback(void* context);

        pplx::task_completion_event<void> mTce;
        pplx::details::timer_t mTimer;
    };

    class TaskAsyncHelper
    {
    public:
        TaskAsyncHelper();
        ~TaskAsyncHelper();

        static pplx::task<void> Delay(seconds seconds, pplx::cancellation_token ct = pplx::cancellation_token::none());

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
} // namespace MicrosoftAspNetSignalRClientCpp