#pragma once

#include <http_client.h>
#include "IHttpClient.h"

using namespace pplx;

class DefaultHttpClient : public IHttpClient
{
public:
    DefaultHttpClient();
    ~DefaultHttpClient();

    void Initialize(string_t uri);
    pplx::task<http_response> Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest);
    pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest);
    pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData);

private:
    unique_ptr<http_client> pClient;
};