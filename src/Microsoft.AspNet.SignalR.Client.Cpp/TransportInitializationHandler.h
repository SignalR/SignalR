//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "ThreadSafeInvoker.h"
#include "TaskAsyncHelper.h"
#include "ExceptionHelper.h"
#include "http_client.h"
#include <mutex>

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportInitializationHandler
    {
    public:
        TransportInitializationHandler(utility::seconds failureTimeout, pplx::cancellation_token disconnectToken);
        ~TransportInitializationHandler();

        void Fail();
        void Fail(exception& ex);
        void Success();
        pplx::task<void> GetTask();

        void SetOnFailureCallback(function<void()> onFailure);

    private:
        mutex mOnFailureLock;
        function<void()> OnFailure;
        mutex mDeregisterCancelCallbackLock;
        function<void()> DeregisterCancelCallback;
        unique_ptr<ThreadSafeInvoker> pInitializationInvoker;
        pplx::task_completion_event<void> mInitializationTask;
        pplx::cancellation_token_registration mTokenCleanup;
        pplx::cancellation_token_source mCts;
    };
} // namespace MicrosoftAspNetSignalRClientCpp