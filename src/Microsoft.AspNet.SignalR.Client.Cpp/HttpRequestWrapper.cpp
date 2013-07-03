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