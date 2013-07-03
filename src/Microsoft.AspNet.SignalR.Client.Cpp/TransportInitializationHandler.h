#pragma once

#include "ThreadSafeInvoker.h"
#include "TaskAsyncHelper.h"
#include "ExceptionHelper.h"
#include "http_client.h"
#include <mutex>

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