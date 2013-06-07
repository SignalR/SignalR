#pragma once

#include "Connection.h"
#include "TransportHelper.h"
#include <agents.h>
#include <typeinfo>

using namespace pplx;
using namespace utility;
using namespace web::json;

class HttpBasedTransport :
    public IClientTransport
{
public:
    HttpBasedTransport(shared_ptr<IHttpClient> httpClient, string_t transport);
    ~HttpBasedTransport(void);

    task<shared_ptr<NegotiationResponse>> Negotiate(Connection* connection);
    task<void> Start(Connection* connection, string_t data, cancellation_token disconnectToken);
    task<void> Send(Connection* connection, string_t data);
    void Abort(Connection* connection);
    void Dispose();

protected:
    shared_ptr<IHttpClient> GetHttpClient();

    void Dispose(bool disposing);
    void CompleteAbort();
    bool TryCompleteAbort();
    virtual void OnStart(Connection* connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback) = 0;
    string_t GetReceiveQueryString(Connection* connection, string_t data);
    string_t GetSendQueryString(string_t transport, string_t connectionToken, string_t customQuery);

private:
    bool mStartedAbort;
    bool mDisposed;
    mutex mAbortLock;
    mutex mDisposeLock;
    shared_ptr<IHttpClient> mHttpClient;
    unique_ptr<Concurrency::event> mAbortResetEvent;
};

