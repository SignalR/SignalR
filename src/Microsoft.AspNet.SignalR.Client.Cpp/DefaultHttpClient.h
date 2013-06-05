#pragma once

#include "IHttpClient.h"

class DefaultHttpClient : public IHttpClient
{
public:
    DefaultHttpClient();
    ~DefaultHttpClient();

    void Initialize(IConnection* connection);
    task<http_response> Get(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, bool isLongRunning);
    task<http_response> Post(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, bool isLongRunning);
    task<http_response> Post(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, string_t postData, bool isLongRunning);

private:
    http_client* mLongRunningClient;
    http_client* mShortRunningClient;
    IConnection* mConnection;

    http_client* GetHttpClient(bool isLongRunning);
};