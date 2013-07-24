//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_msg.h>
#include <mutex>

namespace MicrosoftAspNetSignalRClientCpp
{
    class HttpRequestWrapper
    {
    public:
        HttpRequestWrapper(web::http::http_request httpRequestMessage, std::function<void()> cancel);
        ~HttpRequestWrapper();

        void Abort();

    private:
        web::http::http_request mHttpRequestMessage;
        std::mutex mCancelLock;
        std::function<void()> Cancel;
    };
} // namespace MicrosoftAspNetSignalRClientCpp