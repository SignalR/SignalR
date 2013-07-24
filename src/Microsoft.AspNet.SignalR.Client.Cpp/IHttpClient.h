//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "HttpRequestWrapper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class IHttpClient
    {
    public:
        virtual void Initialize(utility::string_t uri) = 0;
        virtual pplx::task<web::http::http_response> Get(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest) = 0;
        virtual pplx::task<web::http::http_response> Post(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest) = 0;
        virtual pplx::task<web::http::http_response> Post(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest, utility::string_t postData) = 0;
    };
} // namespace MicrosoftAspNetSignalRClientCpp