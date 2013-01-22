#include "FakeHttpClient.h"


FakeHttpClient::FakeHttpClient(void)
{
}


FakeHttpClient::~FakeHttpClient(void)
{
}

void FakeHttpClient::Get(string url, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state)
{
    // Make a fake http request
    httpRequestCallback(NULL, state);
}

void FakeHttpClient::Post(string url, map<string, string> arguments, HTTP_REQUEST_CALLBACK httpRequestCallback, void* state)
{
    // Make a fake http request
    httpRequestCallback(NULL, state);
}
