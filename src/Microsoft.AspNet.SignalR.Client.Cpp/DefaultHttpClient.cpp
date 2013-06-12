#include "DefaultHttpClient.h"

DefaultHttpClient::DefaultHttpClient()
{

}

DefaultHttpClient::~DefaultHttpClient()
{

}

void DefaultHttpClient::Initialize(string_t uri)
{
    // Disabling the Http Client timeout by setting timeout to some large value (min 35 bits)
    http_client_config configurationLong = http_client_config();
    configurationLong.set_timeout(seconds(1<<32 - 1));
    http_client_config configurationShort = http_client_config();
    configurationShort.set_timeout(seconds(1<<32 - 1));

    pLongRunningClient = unique_ptr<http_client>(new http_client(uri, configurationLong));
    pShortRunningClient = unique_ptr<http_client>(new http_client(uri, configurationShort));
}
    
task<http_response> DefaultHttpClient::Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning)
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

     return (isLongRunning ? pLongRunningClient : pShortRunningClient)->request(requestMessage).then([](http_response response)
    {
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Get Failed");
        }

        return response;
    });
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning)
{
    return Post(uri, prepareRequest, U(""), isLongRunning);
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData, bool isLongRunning)
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

    return (isLongRunning ? pLongRunningClient : pShortRunningClient)->request(requestMessage).then([](http_response response)
    {
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Get Failed");
        }

        return response;
    });
}