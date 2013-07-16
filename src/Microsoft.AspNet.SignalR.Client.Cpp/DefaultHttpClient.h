//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include "IHttpClient.h"

using namespace pplx;

namespace MicrosoftAspNetSignalRClientCpp
{
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
} // namespace MicrosoftAspNetSignalRClientCpp