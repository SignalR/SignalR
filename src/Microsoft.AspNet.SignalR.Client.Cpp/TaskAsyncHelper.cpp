//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "TaskAsyncHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

TaskAsyncHelper::TaskAsyncHelper()
{
}

TaskAsyncHelper::~TaskAsyncHelper()
{
}

pplx::task<void> TaskAsyncHelper::Delay(utility::seconds seconds, pplx::cancellation_token ct)
{
    return DelayedTaskHelper<void>::CreateVoid(seconds, ct).then([](){}, ct);
}

} // namespace MicrosoftAspNetSignalRClientCpp