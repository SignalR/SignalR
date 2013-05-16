#pragma once
#include "ihttpclient.h"
#include <http_client.h>
#include <filestream.h>
using namespace web::http;
using namespace web::http::client;


class FakeHttpClient :
    public IHttpClient
{
public:
    FakeHttpClient(void);
    ~FakeHttpClient(void);

    void Get(string url, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL);
    void Post(string url, map<string, string> arguments, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state = NULL);
};

