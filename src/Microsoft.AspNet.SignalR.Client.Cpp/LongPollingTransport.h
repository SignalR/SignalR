#pragma once
#include "httpbasedtransport.h"
class LongPollingTransport :
    public HttpBasedTransport
{
public:
    LongPollingTransport(shared_ptr<IHttpClient> httpClient);
    ~LongPollingTransport(void);

protected:
    void OnStart(shared_ptr<Connection> connection, utility::string_t data);
};

