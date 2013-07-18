//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "ServerSentEventsTransport.h"

namespace MicrosoftAspNetSignalRClientCpp
{

ServerSentEventsTransport::ServerSentEventsTransport(shared_ptr<IHttpClient> httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{
    mConnectionTimeout = seconds(5);
    mReconnectDelay = seconds(2);
}

ServerSentEventsTransport::~ServerSentEventsTransport()
{
    int count = pInitializeHandler.use_count();
}

void ServerSentEventsTransport::OnAbort()
{
    // need to clear all the function<> variables to prevent circular referencing
    pEventSource->SetOpenedCallback([](){});
    pEventSource->SetClosedCallback([](exception&){});
    pEventSource->SetDataCallback([](shared_ptr<char>){});
    pEventSource->SetMessageCallback([](shared_ptr<SseEvent>){});
}

void ServerSentEventsTransport::OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken,  shared_ptr<TransportInitializationHandler> initializeHandler)
{    
    if (initializeHandler == nullptr)
    {
        throw exception("ArgumentNullException: initializeHandler");
    }

    initializeHandler->SetOnFailureCallback([this]()
    {
        mStop = true;
        pRequest->Abort();
    });

    pInitializeHandler = initializeHandler;

    function<void()> successCallback = [this]()
    {
        pInitializeHandler->Success();
        pInitializeHandler.reset();
    };
    function<void(exception&)> errorCallback = [this](exception& ex)
    {
        pInitializeHandler->Fail(ex);
        pInitializeHandler.reset();
    };

    OpenConnection(connection, data, disconnectToken, successCallback, errorCallback);
}

void ServerSentEventsTransport::Reconnect(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken)
{
    TaskAsyncHelper::Delay(mReconnectDelay, disconnectToken).then([this, connection, disconnectToken, data]()
    {
        if (disconnectToken.is_canceled() && connection->EnsureReconnecting())
        {
            OpenConnection(connection, data, disconnectToken, nullptr, nullptr);
        }
    });
}

void ServerSentEventsTransport::OpenConnection(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken, function<void()> initializeCallback, function<void(exception)> errorCallback)
{
    bool reconnecting = initializeCallback == nullptr;
    pCallbackInvoker = unique_ptr<ThreadSafeInvoker>(new ThreadSafeInvoker());
    function<void()> initializeInvoke = [this, initializeCallback]()
    {
        pCallbackInvoker->Invoke(initializeCallback);
    };

    string_t uri = connection->GetUri() + (reconnecting ? U("reconnect") : U("connect")) + GetReceiveQueryString(connection, data);

    wstringstream ss;
    ss << "SSE: GET " << uri;
    connection->Trace(TraceLevel::Events, ss.str());

    GetHttpClient()->Get(uri, [this, connection](shared_ptr<HttpRequestWrapper> request)
    {
        pRequest = request;
        connection->PrepareRequest(request);
    }).then([this, connection, data, disconnectToken, errorCallback, initializeInvoke, reconnecting](pplx::task<http_response> connectRequest) 
    {
        http_response response;
        exception ex;
        TaskStatus status = TaskAsyncHelper::RunTaskToCompletion<http_response>(connectRequest, response, ex);
        
        if (status == TaskStatus::TaskFaulted)
        {
            if (!ExceptionHelper::IsRequestAborted(ex))
            {
                if (errorCallback != nullptr)
                {
                    pCallbackInvoker->Invoke<function<void(exception&)>, exception&>([](function<void(exception&)> cb, exception& ex)
                    {
                        cb(ex);        
                    }, errorCallback, ex);
                }
                else if (reconnecting)
                {
                    // raise error event if failed to reconnect
                    connection->OnError(ex);
                    Reconnect(connection, data, disconnectToken);
                }
            }
            {
                lock_guard<mutex> lock(mDeregisterRequestCancellationLock);
                DeregisterRequestCancellation();
                DeregisterRequestCancellation = [](){};
            }
        }
        else if (status == TaskStatus::TaskCanceled)
        {
            return;
        }
        else
        {
            pEventSource = unique_ptr<EventSourceStreamReader>(new EventSourceStreamReader(connection, response.body()));

            mStop = false;
            bool stop = false;
        
            // what is CancellationTokenExtensions.SafeRegister?
            disconnectToken.register_callback<function<void()>>([this, stop]()
            {
                mStop = true;
                pRequest->Abort();
            });

            pEventSource->SetOpenedCallback([connection]()
            {
                if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
                {
                    connection->OnReconnected();
                }
            });

            pEventSource->SetMessageCallback([connection, this, initializeInvoke, stop](shared_ptr<SseEvent> sseEvent) 
            {
                if (sseEvent->GetType() == EventType::Data)
                {
                    if (StringHelper::EqualsIgnoreCase(sseEvent->GetData(), string_t(U("initialized"))))
                    {
                        return;
                    }

                    bool timedOut, disconnected;

                    TransportHelper::ProcessResponse(connection, sseEvent->GetData(), &timedOut, &disconnected, initializeInvoke);
                    disconnected = false;

                    if (disconnected)
                    {
                        mStop = true;
                        connection->Disconnect();
                    }
                }
            });

            pEventSource->SetClosedCallback([connection, this, data, disconnectToken, stop](exception& ex)
            {
                if (!ExceptionHelper::IsNull(ex))
                {
                    // Check if the request is aborted
                    if (!ExceptionHelper::IsRequestAborted(ex))
                    {
                        // Don't raise exceptions if the request was aborted (connection was stopped).
                        connection->OnError(ex);
                    }
                }

                {
                    lock_guard<mutex> lock(mDeregisterRequestCancellationLock);
                    DeregisterRequestCancellation();
                    DeregisterRequestCancellation = [](){};
                }

                if (mStop)
                {
                    GetAbortHandler()->CompleteAbort();
                }
                else if (GetAbortHandler()->TryCompleteAbort())
                {
                    // Abort() was called, so don't reconnect
                }
                else
                {
                    Reconnect(connection, data, disconnectToken);
                }
            });

            pEventSource->Start();
        }
    });

    auto requestCancellationRegistration = disconnectToken.register_callback([this, errorCallback]()
    {
        if (pRequest != nullptr)
        {
            pRequest->Abort();
        }

        if (errorCallback != nullptr)
        {
            pCallbackInvoker->Invoke<function<void(exception&)>>([](function<void(exception&)> cb)
            {
                cb(exception("Operation Canceled Exception: The connection was stopped before it could be started."));
            }, errorCallback);
        }
    });

    {
        lock_guard<mutex> lock(mDeregisterRequestCancellationLock);
        DeregisterRequestCancellation = [disconnectToken, requestCancellationRegistration]()
        {
            disconnectToken.deregister_callback(requestCancellationRegistration);
        };
    }

    if (errorCallback != nullptr)
    {
        // the memory will eventually be reclaimed after 5 seconds (default) so it's not a leak here
        TaskAsyncHelper::Delay(mConnectionTimeout, disconnectToken).then([this, errorCallback]()
        {
            pCallbackInvoker->Invoke<function<void(exception)>>([this](function<void(exception)> errorCallback)
            {
                // Abort the request before cancelling
                pRequest->Abort();

                // Connection timeout occurred
                errorCallback(exception("TimeoutException"));
            }, errorCallback);
        });
    }
}

void ServerSentEventsTransport::LostConnection(shared_ptr<Connection> connection)
{
    if (pRequest != nullptr)
    {
        pRequest->Abort();
    }
}

bool ServerSentEventsTransport::SupportsKeepAlive()
{
    return true;
}

} // namespace MicrosoftAspNetSignalRClientCpp