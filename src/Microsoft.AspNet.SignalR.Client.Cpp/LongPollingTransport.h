#pragma once
#include "httpbasedtransport.h"
class LongPollingTransport :
    public HttpBasedTransport
{
public:
    LongPollingTransport(shared_ptr<IHttpClient> httpClient);
    ~LongPollingTransport();

protected:
    void OnStart(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback);
    void OnAbort();
    void LostConnection(shared_ptr<Connection> connection);
};

