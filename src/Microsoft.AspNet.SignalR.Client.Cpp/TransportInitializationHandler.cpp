//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "TransportInitializationHandler.h"

namespace MicrosoftAspNetSignalRClientCpp
{

TransportInitializationHandler::TransportInitializationHandler(utility::seconds failureTimeout, pplx::cancellation_token disconnectToken)
{
    mInitializationTask = pplx::task_completion_event<void>();
    pInitializationInvoker = unique_ptr<ThreadSafeInvoker>(new ThreadSafeInvoker());

    // Default event
    {
        lock_guard<mutex> lock(mOnFailureLock);
        OnFailure = [](){};
    }
    // We want to fail if the disconnect token is tripped while we're waiting on initialization
    mTokenCleanup = disconnectToken.register_callback([this]()
    {
        Fail();
    });

    {
        lock_guard<mutex> lock(mDeregisterCancelCallbackLock);
        DeregisterCancelCallback = [this, disconnectToken]()
        {
            disconnectToken.deregister_callback(mTokenCleanup);
        };
    }

    TaskAsyncHelper::Delay(failureTimeout, mCts.get_token()).then([this]()
    {
        Fail(exception("TimeoutException: Transport timed out trying to connect"));
    });
}

TransportInitializationHandler::~TransportInitializationHandler()
{
    mCts.cancel();
}

void TransportInitializationHandler::Fail()
{
    Fail(exception("InvalidOperationException: Transport failed trying to connect."));
}

void TransportInitializationHandler::Fail(exception& ex)
{
    pInitializationInvoker->Invoke([this, ex]()
    {
        {
            lock_guard<mutex> lock(mOnFailureLock);
            OnFailure();
        }
        mInitializationTask.set_exception(ex);
        {
            lock_guard<mutex> lock(mDeregisterCancelCallbackLock);
            DeregisterCancelCallback();
        }
        mCts.cancel();
    });
}

void TransportInitializationHandler::Success()
{
    pInitializationInvoker->Invoke([this]()
    {
        mInitializationTask.set();
        {
            lock_guard<mutex> lock(mDeregisterCancelCallbackLock);
            DeregisterCancelCallback();
        }
        mCts.cancel();
    });
}

pplx::task<void> TransportInitializationHandler::GetTask()
{
    return pplx::task<void>(mInitializationTask);
}

void TransportInitializationHandler::SetOnFailureCallback(function<void()> onFailure)
{
    lock_guard<mutex> lock(mOnFailureLock);
    OnFailure = onFailure;
}

} // namespace MicrosoftAspNetSignalRClientCpp