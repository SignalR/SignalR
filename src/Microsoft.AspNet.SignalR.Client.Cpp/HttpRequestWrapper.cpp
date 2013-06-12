#include "HttpRequestWrapper.h"

HttpRequestWrapper::HttpRequestWrapper(http_request httpRequestMessage, function<void()> cancel)
{
    mHttpRequestMessage = httpRequestMessage;
    Cancel = cancel;
}

HttpRequestWrapper::~HttpRequestWrapper()
{

}

void HttpRequestWrapper::Abort()
{
    Cancel();
}