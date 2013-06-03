#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(http_client* httpClient, string_t transport)
{
    mHttpClient = httpClient;
    mTransportName = transport;
}

HttpBasedTransport::~HttpBasedTransport(void)
{
    delete mHttpClient;
}

http_client* HttpBasedTransport::GetHttpClient()
{
    return mHttpClient;
}

task<NegotiationResponse*> HttpBasedTransport::Negotiate(Connection* connection)
{
    return TransportHelper::GetNegotiationResponse(mHttpClient, connection);
}

string_t HttpBasedTransport::GetReceiveQueryString(Connection* connection, string_t data)
{
    return TransportHelper::GetReceiveQueryString(connection, data, mTransportName);
}

task<void> HttpBasedTransport::Start(Connection* connection, string_t data, void* state)
{
    task_completion_event<void> tce;
    
    auto initializeCallback = new call<int>([tce](int)
    {
        tce.set();
    });

    exception ex;

    auto errorCallback = new call<int>([tce, &ex](int)
    {
        tce.set_exception(ex);
    });

    OnStart(connection, data, initializeCallback, errorCallback);
    return task<void>(tce).then([initializeCallback, errorCallback]()
    {
        delete initializeCallback;
        delete errorCallback;
    });
}

task<void> HttpBasedTransport::Send(Connection* connection, string_t data)
{
    string_t uri = connection->GetUri() + U("send?transport=") + mTransportName + U("&connectionToken=") + connection->GetConnectionToken();

    http_request request(methods::POST);
    request.set_request_uri(uri);

    string_t encodedData = U("data=") + uri::encode_data_string(data);

    request.set_body(encodedData);

    return mHttpClient->request(request).then([request](http_response response)
    {

    });
}

void HttpBasedTransport::Stop(Connection* connection)
{

}


void HttpBasedTransport::Abort(Connection* connection)
{

}
