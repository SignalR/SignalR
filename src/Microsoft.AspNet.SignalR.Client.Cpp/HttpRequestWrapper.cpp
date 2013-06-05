#include "HttpRequestWrapper.h"

HttpRequestWrapper::HttpRequestWrapper(http_request httpRequestMessage, function<void()> cancel)
{
    mHttpRequestMessage = httpRequestMessage;
    mCancel = cancel;
}

HttpRequestWrapper::~HttpRequestWrapper()
{

}

void HttpRequestWrapper::Abort()
{
    mCancel();
}