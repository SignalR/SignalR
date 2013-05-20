#pragma once

#include "IClientTransport.h"
#include "Connection.h"
#include "IHttpClient.h"
#include "TransportHelper.h"
#include <queue>

class HttpBasedTransport :
    public IClientTransport
{

protected:
    http_client* mHttpClient;

public:
    HttpBasedTransport(http_client* httpClient);
    ~HttpBasedTransport(void);

    pplx::task<NegotiationResponse*> Negotiate(Connection* connection);
    pplx::task<void> Start(Connection* connection, utility::string_t data);
    void Send(Connection* connection, string data);
    void Stop(Connection* connection);
    void Abort(Connection* connection);

    void TryDequeueNextWorkItem();

private:    
    
    struct SendQueueItem
    {
        Connection* Connection;
        string Url;
        map<string, string> PostData;
    };
    
    queue<SendQueueItem*> mSendQueue;
    bool mSending;
    static void OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

