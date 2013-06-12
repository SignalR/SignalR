#pragma once

#include <http_client.h>
#include "IConnection.h"
#include "HttpRequestWrapper.h"

using namespace web::http::client;

class IHttpClient
{
public:
    virtual void Initialize(string_t uri) = 0;
    virtual pplx::task<http_response> Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning) = 0;
    virtual pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning) = 0;
    virtual pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData, bool isLongRunning) = 0;
};