#include "DefaultHttpClient.h"

DefaultHttpClient::DefaultHttpClient()
{

}

DefaultHttpClient::~DefaultHttpClient()
{

}

void DefaultHttpClient::Initialize(string_t uri)
{
    // set timeout to some large value
    //http_client_config configurationLong = http_client_config();
    //configurationLong.set_timeout();
    //http_client_config configurationShort = http_client_config();
    //configurationShort.set_timeout();

    mLongRunningClient = unique_ptr<http_client>(new http_client(uri/*, configurationLong*/));
    mShortRunningClient = unique_ptr<http_client>(new http_client(uri/*, configurationShort*/));
}
    
task<http_response> DefaultHttpClient::Get(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning)
{
    shared_ptr<cancellation_token_source> cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    http_request requestMessage = http_request(methods::GET);
    requestMessage.set_request_uri(uri);
    
    shared_ptr<HttpRequestWrapper> request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);

    if (isLongRunning)
    {
        return mLongRunningClient->request(requestMessage).then([](http_response response)
        {
            // check for errors, temporary solution
            if (response.status_code()/100 != 2)
            {
                throw exception("HttpClient Get Failed");
            }

            return response;
        });
    }
    else
    {
        return mShortRunningClient->request(requestMessage).then([](http_response response)
        {
            // check for errors, temporary solution
            if (response.status_code()/100 != 2)
            {
                throw exception("HttpClient Get Failed");
            }

            return response;
        });
    }
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, bool isLongRunning)
{
    return Post(uri, prepareRequest, U(""), isLongRunning);
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(shared_ptr<HttpRequestWrapper>)> prepareRequest, string_t postData, bool isLongRunning)
{
    shared_ptr<cancellation_token_source> cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    http_request requestMessage = http_request(methods::POST);
    requestMessage.set_request_uri(uri);
    requestMessage.set_body(postData);

    shared_ptr<HttpRequestWrapper> request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);

    if (isLongRunning)
    {
        return mLongRunningClient->request(requestMessage).then([](http_response response)
        {
            // check for errors, temporary solution
            if (response.status_code()/100 != 2)
            {
                throw exception("HttpClient Post Failed");
            }

            return response;
        });
    }
    else
    {
        return mShortRunningClient->request(requestMessage).then([](http_response response)
        {
            // check for errors, temporary solution
            if (response.status_code()/100 != 2)
            {
                throw exception("HttpClient Post Failed");
            }

            return response;
        });
    }
}