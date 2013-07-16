//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include <mutex>

using namespace std;
using namespace web::http;

namespace MicrosoftAspNetSignalRClientCpp
{
    class HttpRequestWrapper
    {
    public:
        HttpRequestWrapper(http_request httpRequestMessage, function<void()> cancel);
        ~HttpRequestWrapper();

        void Abort();

    private:
        http_request mHttpRequestMessage;
        mutex mCancelLock;
        function<void()> Cancel;
    };
} // namespace MicrosoftAspNetSignalRClientCpp