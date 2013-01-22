#pragma once

#include <string>

using namespace std;

class IHttpResponse
{
public:
    IHttpResponse(void);
    ~IHttpResponse(void);

    virtual string GetResponseBody() = 0;
    virtual int GetStatusCode() = 0;
    virtual int Read(char* buffer) = 0;
};

