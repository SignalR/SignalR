#pragma once

#include <string>

using namespace std;

class IHttpResponse
{
public:
    IHttpResponse(void);
    ~IHttpResponse(void);

    typedef void (*READ_CALLBACK)(string data, exception* error, void* state);

    virtual string GetResponseBody() = 0;
    virtual int GetStatusCode() = 0;
    
    virtual void ReadLine(READ_CALLBACK readCallback, void* state = NULL) = 0;
};

