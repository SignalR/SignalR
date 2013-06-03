#pragma once

#include "Connection.h"
#include "TransportHelper.h"
#include <agents.h>

using namespace pplx;
using namespace utility;
using namespace web::json;

class HttpBasedTransport :
    public IClientTransport
{
public:
    HttpBasedTransport(http_client* httpClient, string_t transport);
    ~HttpBasedTransport(void);

    task<NegotiationResponse*> Negotiate(Connection* connection);
    task<void> Start(Connection* connection, string_t data, void* state = NULL);
    task<void> Send(Connection* connection, string_t data);
    void Stop(Connection* connection);
    void Abort(Connection* connection);


protected:
    http_client* GetHttpClient();

    virtual void OnStart(Connection* connection, string_t data, call<int>* initializeCallback, call<int>* errorCallback) = 0;
    string_t GetReceiveQueryString(Connection* connection, string_t data);

private:
    bool mSending;
    bool mStartedAbort;
    bool mDisposed;
    mutex mAbortLock;
    mutex mDisposeLock;
    http_client* mHttpClient;
};

