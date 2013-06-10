#pragma once

#include "IHttpClient.h"

class DefaultHttpClient : public IHttpClient
{
public:
    DefaultHttpClient();
    ~DefaultHttpClient();

    void Initialize(string_t uri);
    task<http_response> Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning);
    task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning);
    task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData, bool isLongRunning);

private:
    unique_ptr<http_client> mLongRunningClient;
    unique_ptr<http_client> mShortRunningClient;
};