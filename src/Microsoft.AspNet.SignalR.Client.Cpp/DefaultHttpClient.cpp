//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "DefaultHttpClient.h"

namespace MicrosoftAspNetSignalRClientCpp
{

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

    auto cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    auto requestMessage = http_request(methods::GET);
    requestMessage.set_request_uri(uri);
    
    auto request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);
    pplx::task_options readTaskOptions(cts->get_token());
    return pClient->request(requestMessage).then([](http_response response)
    {
        if (is_task_cancellation_requested())
        {
            cancel_current_task();
        }
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Get Failed");
        }

        return response;
    }, readTaskOptions);
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
    auto cts = shared_ptr<cancellation_token_source>(new cancellation_token_source());

    auto requestMessage = http_request(methods::POST);
    requestMessage.set_request_uri(uri);
    requestMessage.set_body(postData);

    auto request = shared_ptr<HttpRequestWrapper>(new HttpRequestWrapper(requestMessage, [cts]()
    {
        cts->cancel();
    }));

    prepareRequest(request);
    pplx::task_options readTaskOptions(cts->get_token());
    return pClient->request(requestMessage).then([](http_response response)
    {
        if (is_task_cancellation_requested())
        {
            cancel_current_task();
        }
        // check if the request was successful, temporary
        if (response.status_code()/100 != 2)
        {
            throw exception("HttpClientException: Post Failed");
        }

        return response;
    }, readTaskOptions);
}

} // namespace MicrosoftAspNetSignalRClientCpp