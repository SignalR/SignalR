//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "HttpBasedTransport.h"

namespace MicrosoftAspNetSignalRClientCpp
{

HttpBasedTransport::HttpBasedTransport(shared_ptr<IHttpClient> httpClient, string_t transport)
{
    pHttpClient = httpClient;
    mTransportName = transport;

    pAbortHandler = shared_ptr<TransportAbortHandler>(new TransportAbortHandler(httpClient, transport, [this]()
    {
        OnAbort();
    }));
}

HttpBasedTransport::~HttpBasedTransport(void)
{
}

shared_ptr<IHttpClient> HttpBasedTransport::GetHttpClient()
{
    return pHttpClient;
}

shared_ptr<TransportAbortHandler> HttpBasedTransport::GetAbortHandler()
{
    return pAbortHandler;
}

pplx::task<shared_ptr<NegotiationResponse>> HttpBasedTransport::Negotiate(shared_ptr<Connection> connection)
{
    return TransportHelper::GetNegotiationResponse(pHttpClient, connection);
}

string_t HttpBasedTransport::GetReceiveQueryString(shared_ptr<Connection> connection, string_t data)
{
    return TransportHelper::GetReceiveQueryString(connection, data, mTransportName);
}

pplx::task<void> HttpBasedTransport::Start(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    auto initializeHandler = shared_ptr<TransportInitializationHandler>(new TransportInitializationHandler(connection->GetTransportConnectTimeout(), disconnectToken));

    OnStart(connection, data, disconnectToken, initializeHandler);
    
    return initializeHandler->GetTask();
}

pplx::task<void> HttpBasedTransport::Send(shared_ptr<Connection> connection, string_t data)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    string_t uri = connection->GetUri() + U("send");
    string_t customQueryString = connection->GetQueryString().empty()? U("") : U("&") + connection->GetQueryString();
    uri += TransportHelper::GetSendQueryString(mTransportName, web::http::uri::encode_data_string(connection->GetConnectionToken()), customQueryString);

    string_t encodedData = U("data=") + uri::encode_data_string(data);

    return pHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
    {
        connection->PrepareRequest(request);
    }, encodedData).then([connection](pplx::task<http_response> sendTask)
    {
        http_response response;
        exception ex;
        TaskStatus status = TaskAsyncHelper::RunTaskToCompletion<http_response>(sendTask, response, ex);

        if (status == TaskStatus::TaskCompleted)
        {
            if (response.headers().content_length() != 0)
            {
                auto inStringBuffer = shared_ptr<streams::container_buffer<string>>(new streams::container_buffer<string>());
                response.body().read_to_end(*(inStringBuffer.get())).then([connection, inStringBuffer](size_t bytesRead)
                {
                    string &text = inStringBuffer->collection();
                    string_t message;
                    message.assign(text.begin(), text.end());


                    wstringstream ss;
                    ss << "OnMessage(" << message << ")";
                    connection->Trace(TraceLevel::StateChanges, ss.str());
                    
                    if (!message.empty())
                    {
                        connection->OnReceived(message);
                    }
                });
            }
        } 
        else if (status == TaskStatus::TaskCanceled)
        {
            return;
        }
        else
        {
            connection->OnError(ex);
        }
    });
}

void HttpBasedTransport::Abort(shared_ptr<Connection> connection, seconds timeout)
{
    pAbortHandler->Abort(connection, timeout, U(""));
}
} // namespace MicrosoftAspNetSignalRClientCpp