#include "HttpBasedTransport.h"


HttpBasedTransport::HttpBasedTransport(IHttpClient* httpClient)
{
    mHttpClient = httpClient;
}


HttpBasedTransport::~HttpBasedTransport(void)
{
    delete mHttpClient;
}

void HttpBasedTransport::Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state)
{
    TransportHelper::GetNegotiationResponse(mHttpClient, connection, negotiateCallback, state);
}

void HttpBasedTransport::Send(Connection* connection, string data)
{
    auto url = connection->GetUrl() + 
        "send?transport=serverSentEvents&connectionToken=" + 
        connection->GetConnectionToken();

    auto postData = map<string, string>();
    postData["data"] = data;

    // TODO: Queue requests so that we don't send fire the next request off until the previous one is finished
    // This logic should probably be in another class
    mHttpClient->Post(url, postData, &HttpBasedTransport::OnSendHttpResponse, this);
}

void HttpBasedTransport::OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{

}

void HttpBasedTransport::Stop(Connection* connection)
{

}


void HttpBasedTransport::Abort(Connection* connection)
{

}

