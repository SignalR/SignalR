#pragma once

#include "IHttpRequest.h"
#include "IHttpResponse.h"
#include <string>
#include <map>

using namespace std;

class IHttpClient
{
public:
    IHttpClient(void);
    virtual	~IHttpClient(void);

    typedef void (*HTTP_REQUEST_CALLBACK)(IHttpResponse* httpResponse, exception* error, void* state);

    virtual void Get(string url, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL) = 0;
    virtual void Post(string url, map<string, string> arguments, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL) = 0;
};

