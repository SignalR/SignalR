//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "HttpRequestWrapper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

HttpRequestWrapper::HttpRequestWrapper(http_request httpRequestMessage, function<void()> cancel)
{
    mHttpRequestMessage = httpRequestMessage;
    {
        lock_guard<mutex> lock(mCancelLock);
        Cancel = cancel;
    }
}

HttpRequestWrapper::~HttpRequestWrapper()
{
}

void HttpRequestWrapper::Abort()
{
    lock_guard<mutex> lock(mCancelLock);
    Cancel();
}

} // namespace MicrosoftAspNetSignalRClientCpp