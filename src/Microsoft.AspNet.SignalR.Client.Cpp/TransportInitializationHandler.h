//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <mutex>
#include "ThreadSafeInvoker.h"
#include "TaskAsyncHelper.h"
#include "ExceptionHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportInitializationHandler
    {
    public:
        TransportInitializationHandler(utility::seconds failureTimeout, pplx::cancellation_token disconnectToken);
        ~TransportInitializationHandler();

        void Fail();
        void Fail(std::exception& ex);
        void Success();
        pplx::task<void> GetTask() const;

        void SetOnFailureCallback(std::function<void()> onFailure);

    private:
        std::mutex mOnFailureLock;
        std::function<void()> OnFailure;
        std::mutex mDeregisterCancelCallbackLock;
        std::function<void()> DeregisterCancelCallback;
        std::unique_ptr<ThreadSafeInvoker> pInitializationInvoker;
        pplx::task_completion_event<void> mInitializationTask;
        pplx::cancellation_token_registration mTokenCleanup;
        pplx::cancellation_token_source mCts;
    };
} // namespace MicrosoftAspNetSignalRClientCpp