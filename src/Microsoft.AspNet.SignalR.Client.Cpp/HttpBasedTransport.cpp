#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(http_client* httpClient, string_t transport)
{
    mHttpClient = httpClient;
    mTransport = transport;
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
    return TransportHelper::GetReceiveQueryString(connection, data, mTransport);
}

pplx::task<void> HttpBasedTransport::Start(Connection* connection, string_t data, void* state)
{
    OnStart(connection, data);
    return pplx::task<void>();
}

pplx::task<void> HttpBasedTransport::Send(Connection* connection, string_t data)
{
    string_t uri = connection->GetUri() + U("send?transport=") + mTransport + U("&connectionToken=") + connection->GetConnectionToken();

    http_request request(methods::POST);
    request.set_request_uri(uri);
    
    //value dataObj;
    //dataObj[U("data")] = value::string(data);
    //stringstream_t stream;
    //dataObj.serialize(stream);

    //map<string_t,string_t> postData;
    //postData.insert(pair<string_t, string_t>(U("data"), data));

    string_t encodedData = U("data=") + uri::encode_data_string(data);

    request.set_body(encodedData);

    wcout << request.to_string() << endl;

    return mHttpClient->request(request).then([request](http_response response)
    {
        cout << "message sent" << endl;
        cout << "status code: " << response.status_code() << endl;
    });
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

