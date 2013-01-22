#pragma once
#include "httpbasedtransport.h"
class LongPollingTransport :
    public HttpBasedTransport
{
public:
    LongPollingTransport(IHttpClient* httpClient);
    ~LongPollingTransport(void);

    void Start(Connection* connection, START_CALLBACK startCallback, string data, void* state = NULL);
    void Abort(Connection* connection);

    struct PollHttpRequestInfo
    {
        START_CALLBACK Callback;
        void* CallbackState;
        LongPollingTransport* Transport;
        Connection* Connection;
        string Data;
    };

private:
    static void OnPollHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

