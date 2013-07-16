//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "TransportAbortHandler.h"

namespace MicrosoftAspNetSignalRClientCpp
{

TransportAbortHandler::TransportAbortHandler(shared_ptr<IHttpClient> httpClient, utility::string_t transportName, function<void()> callback)
{
    {
        lock_guard<mutex> lock(mAbortCallbackLock);
        AbortCallback = callback;
    }
    pHttpClient = httpClient;
    mTransportName = transportName;
    mStartedAbort = false;
    pAbortResetEvent = unique_ptr<Concurrency::event>(new Concurrency::event());
}

TransportAbortHandler::~TransportAbortHandler()
{
}

void TransportAbortHandler::Abort(shared_ptr<Connection> connection, utility::seconds timeout, utility::string_t connectionData)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }
    
    {
        lock_guard<mutex> lock(mAbortLock);

        if (!mStartedAbort)
        {
            mStartedAbort = true;

            string_t uri = connection->GetUri() + U("abort") + TransportHelper::GetSendQueryString(mTransportName, web::http::uri::encode_data_string(connection->GetConnectionToken()), U(""));
            uri += TransportHelper::AppendCustomQueryString(connection, uri);

            // this currently waits until the task is complete
            pHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
            {
                connection->PrepareRequest(request);
            }).then([this](pplx::task<http_response> abortTask)
            {
                http_response response;
                exception ex;
                TaskStatus status = TaskAsyncHelper::RunTaskToCompletion<http_response>(abortTask, response, ex);

                if (status == TaskStatus::TaskCompleted)
                {
                    lock_guard<mutex> lock(mAbortCallbackLock);
                    AbortCallback();
                } 
                else 
                {
                    CompleteAbort();
                }
            });

            if (pAbortResetEvent->wait(10000) == COOPERATIVE_WAIT_TIMEOUT)
            {
                connection->Trace(TraceLevel::Events, U("Abort never fired"));
            }
        }
    }
}

void TransportAbortHandler::CompleteAbort()
{
    lock_guard<mutex> lock(mDisposeLock);

    mStartedAbort = true;
    pAbortResetEvent->set();
}

bool TransportAbortHandler::TryCompleteAbort()
{
    lock_guard<mutex> lock(mDisposeLock);

    if (mStartedAbort)
    {
        pAbortResetEvent->set();
        return true;
    }
    else
    {
        return false;
    }
}

} // namespace MicrosoftAspNetSignalRClientCpp