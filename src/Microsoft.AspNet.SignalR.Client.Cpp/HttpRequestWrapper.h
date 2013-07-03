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