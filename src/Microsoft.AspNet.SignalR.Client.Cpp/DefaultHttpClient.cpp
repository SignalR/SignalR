#include "DefaultHttpClient.h"

DefaultHttpClient::DefaultHttpClient()
{

}

DefaultHttpClient::~DefaultHttpClient()
{

}

void DefaultHttpClient::Initialize(string_t uri)
{
    // Disabling the Http Client timeout by setting timeout to 0?
    http_client_config configuration = http_client_config();
    configuration.set_timeout(seconds(0));

    pClient = unique_ptr<http_client>(new http_client(uri, configuration));
}
    
task<http_response> DefaultHttpClient::Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest)
{
    if (prepareRequest == nullptr)
    {
        throw exception("ArgumentNullException: prepareRequest");
    }

    shared_ptr<cancellation_token_source> cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    http_request requestMessage = http_request(methods::GET);
    requestMessage.set_request_uri(uri);
    
    shared_ptr<HttpRequestWrapper> request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);

     return pClient->request(requestMessage).then([](http_response response)
    {
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Get Failed");
        }

        return response;
    });
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest)
{
    return Post(uri, prepareRequest, U(""));
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData)
{
    if (prepareRequest == nullptr)
    {
        throw exception("ArgumentNullException: prepareRequest");
    }
    shared_ptr<cancellation_token_source> cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    http_request requestMessage = http_request(methods::POST);
    requestMessage.set_request_uri(uri);
    requestMessage.set_body(postData);

    shared_ptr<HttpRequestWrapper> request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);

    return pClient->request(requestMessage).then([](http_response response)
    {
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Get Failed");
        }

        return response;
    });
}