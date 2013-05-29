#pragma once

#include "IClientTransport.h"
#include "Connection.h"
#include "IHttpClient.h"
#include "TransportHelper.h"
#include <queue>

using namespace utility;
using namespace web::json;

class HttpBasedTransport :
    public IClientTransport
{

protected:
    http_client* mHttpClient;

public:
    HttpBasedTransport(http_client* httpClient, string_t transport);
    ~HttpBasedTransport(void);

    pplx::task<NegotiationResponse*> Negotiate(Connection* connection);
    pplx::task<void> Start(Connection* connection, string_t data, void* state = NULL);
    pplx::task<void> Send(Connection* connection, string_t data);
    void Stop(Connection* connection);
    void Abort(Connection* connection);

    void TryDequeueNextWorkItem();

protected:
    virtual void OnStart(Connection* connection, string_t data) = 0;
    utility::string_t GetReceiveQueryString(Connection* connection, string_t data);


private:    
    
    struct SendQueueItem
    {
        Connection* Connection;
        string Url;
        map<string, string> PostData;
    };
    
    string_t mTransport;
    queue<SendQueueItem*> mSendQueue;
    bool mSending;
    static void OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

