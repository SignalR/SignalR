#include "DefaultHttpClient.h"

DefaultHttpClient::DefaultHttpClient()
{

}

DefaultHttpClient::~DefaultHttpClient()
{
    if (mLongRunningClient)
    {
        delete mLongRunningClient;
    }
    if (mShortRunningClient)
    {
        delete mShortRunningClient;
    }
}

void DefaultHttpClient::Initialize(IConnection* connection)
{
    mConnection = connection;

    // set timeout to some large value
    //http_client_config configurationLong = http_client_config();
    //configurationLong.set_timeout();
    //http_client_config configurationShort = http_client_config();
    //configurationShort.set_timeout();

    mLongRunningClient = new http_client(mConnection->GetUri()/*, configurationLong*/);
    mShortRunningClient = new http_client(mConnection->GetUri()/*, configurationShort*/);
}
    
task<http_response> DefaultHttpClient::Get(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, bool isLongRunning)
{
    cancellation_token_source* cts = new cancellation_token_source();

    http_request requestMessage = http_request(methods::GET);
    requestMessage.set_request_uri(uri);
    
    HttpRequestWrapper* request = new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    });

    //prepareRequest(request);

    http_client* httpClient = GetHttpClient(isLongRunning);

    return httpClient->request(requestMessage).then([](http_response response)
    {
        // check for errors, temporary solution
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClient Get Failed");
        }

        return response;
    });

}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, bool isLongRunning)
{
    return Post(uri, prepareRequest, U(""), isLongRunning);
}

task<http_response> DefaultHttpClient::Post(string_t uri, function<void(HttpRequestWrapper*)> prepareRequest, string_t postData, bool isLongRunning)
{
    cancellation_token_source* cts = new cancellation_token_source();

    http_request requestMessage = http_request(methods::POST);
    requestMessage.set_request_uri(uri);
    requestMessage.set_body(postData);

    HttpRequestWrapper* request = new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    });

    //prepareRequest(request);

    http_client* httpClient = GetHttpClient(isLongRunning);

    return httpClient->request(requestMessage).then([](http_response response)
    {
        // check for errors, temporary solution
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClient Post Failed");
        }

        return response;
    });
}

http_client* DefaultHttpClient::GetHttpClient(bool isLongRunning)
{
    return isLongRunning ? mLongRunningClient : mShortRunningClient;
}