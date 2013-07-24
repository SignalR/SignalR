//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include "IHttpClient.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class DefaultHttpClient : public IHttpClient
    {
    public:
        DefaultHttpClient();
        ~DefaultHttpClient();

        void Initialize(utility::string_t uri);
        pplx::task<web::http::http_response> Get(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest);
        pplx::task<web::http::http_response> Post(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest);
        pplx::task<web::http::http_response> Post(utility::string_t uri, std::function<void(std::shared_ptr<HttpRequestWrapper>)> prepareRequest, utility::string_t postData);

    private:
        std::unique_ptr<web::http::client::http_client> pClient;
    };
} // namespace MicrosoftAspNetSignalRClientCpp