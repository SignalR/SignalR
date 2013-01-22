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
    IHttpClient* mHttpClient;

public:
    HttpBasedTransport(IHttpClient* httpClient);
    ~HttpBasedTransport(void);

    void Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state = NULL);
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

