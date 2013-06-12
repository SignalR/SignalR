#pragma once

#include <http_client.h>

using namespace std;
using namespace web::http;

class HttpRequestWrapper
{
public:
    HttpRequestWrapper(http_request httpRequestMessage, function<void()> cancel);
    ~HttpRequestWrapper();

    void Abort();

private:
    http_request mHttpRequestMessage;
    function<void()> Cancel;
};