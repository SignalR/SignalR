#pragma once

#include <http_client.h>
#include "HttpRequestWrapper.h"

using namespace utility;
using namespace web::http::client;

namespace MicrosoftAspNetSignalRClientCpp
{
    class IHttpClient
    {
    public:
        virtual void Initialize(string_t uri) = 0;
        virtual pplx::task<http_response> Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest) = 0;
        virtual pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest) = 0;
        virtual pplx::task<http_response> Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData) = 0;
    };
} // namespace MicrosoftAspNetSignalRClientCpp