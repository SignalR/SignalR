//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

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