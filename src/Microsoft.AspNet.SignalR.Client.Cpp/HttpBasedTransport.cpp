#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(http_client* httpClient, string_t transport)
{
    mHttpClient = httpClient;
    _transport = transport;
}

HttpBasedTransport::~HttpBasedTransport(void)
{
    delete mHttpClient;
}

pplx::task<NegotiationResponse*> HttpBasedTransport::Negotiate(Connection* connection)
{
    return TransportHelper::GetNegotiationResponse(mHttpClient, connection);
}

string_t HttpBasedTransport::GetReceiveQueryString(Connection* connection, string_t data)
{
    return TransportHelper::GetReceiveQueryString(connection, data, _transport);
}

pplx::task<void> HttpBasedTransport::Start(Connection* connection, string_t data, void* state)
{
    OnStart(connection, data);
    return pplx::task<void>();
}

void HttpBasedTransport::Send(Connection* connection, string data)
{
    //auto url = connection->GetUrl() + 
    //    "send?transport=serverSentEvents&connectionToken=" + 
    //    connection->GetConnectionToken();

    //auto postData = map<string, string>();
    //postData["data"] = data;

    //if(mSending)
    //{
    //    auto queueItem = new SendQueueItem();
    //    queueItem->Connection = connection;
    //    queueItem->Url = url;
    //    queueItem->PostData = postData;
    //    mSendQueue.push(queueItem);
    //}
    //else
    //{
    //    //mHttpClient->Post(url, postData, &HttpBasedTransport::OnSendHttpResponse, this);
    //}
}

void HttpBasedTransport::TryDequeueNextWorkItem()
{
    // If the queue is empty then we are free to send
    mSending = mSendQueue.size() > 0;

    if(mSending)
    {
        // Grab the next work item from the queue
        SendQueueItem* workItem = mSendQueue.front();

        // Nuke the work item
        mSendQueue.pop();

        //mHttpClient->Post(workItem->Url, workItem->PostData, &HttpBasedTransport::OnSendHttpResponse, this);

        delete workItem;
    }
}

void HttpBasedTransport::OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{    
    auto transport = (HttpBasedTransport*)state;

    transport->TryDequeueNextWorkItem();
}

void HttpBasedTransport::Stop(Connection* connection)
{

}


void HttpBasedTransport::Abort(Connection* connection)
{

}

