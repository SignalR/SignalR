#pragma once
#include "ihttpclient.h"
class FakeHttpClient :
    public IHttpClient
{
public:
    FakeHttpClient(void);
    ~FakeHttpClient(void);

    void Get(string url, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL);
    void Post(string url, map<string, string> arguments, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL);
};

