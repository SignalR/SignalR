#pragma once
#include "httpbasedtransport.h"
class LongPollingTransport :
    public HttpBasedTransport
{
public:
    LongPollingTransport(http_client* httpClient);
    ~LongPollingTransport(void);

    //pplx::task<void> Start(Connection* connection, utility::string_t data, void* state = NULL);

    struct PollHttpRequestInfo
    {
        START_CALLBACK Callback;
        void* CallbackState;
        LongPollingTransport* Transport;
        Connection* Connection;
        string Data;
    };

protected:
    void OnStart(Connection* connection, utility::string_t data);

private:
    static void OnPollHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

