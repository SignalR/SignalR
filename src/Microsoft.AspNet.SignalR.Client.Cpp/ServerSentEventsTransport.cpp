#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(shared_ptr<IHttpClient> httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{
    mConnectionTimeout = seconds(2);
    mReconnectDelay = seconds(5);
}

ServerSentEventsTransport::~ServerSentEventsTransport()
{
    cout << "SSE Transport destructor" << endl;
}

void ServerSentEventsTransport::OnAbort()
{
    // need to clear all the function<> variables to prevent circular referencing
    pEventSource->Opened = [](){};
    pEventSource->Closed = [](exception& ex){};
    pEventSource->Data = [](shared_ptr<char> buffer){};
    pEventSource->Message = [](shared_ptr<SseEvent> sseEvent){};
    //pEventSource->Abort();
}

void ServerSentEventsTransport::OnStart(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken,  function<void()> initializeCallback, function<void(exception)> errorCallback)
{    
    OpenConnection(connection, data, disconnectToken, initializeCallback, errorCallback);
}

void ServerSentEventsTransport::Reconnect(shared_ptr<Connection> connection, string_t data, pplx::cancellation_token disconnectToken)
{
    TaskAsyncHelper::Delay(mReconnectDelay).then([this, connection, disconnectToken, data]()
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
        }
        else if (status == TaskStatus::TaskCanceled)
        {
            return;
        }
        else
        {
            pEventSource = unique_ptr<EventSourceStreamReader>(new EventSourceStreamReader(response.body()));

            mStop = false;
            bool stop = false;
        
            // what is CancellationTokenExtensions.SafeRegister?
            disconnectToken.register_callback<function<void()>>([this, stop]()
            {
                cout << "transport cancel" << endl;
                mStop = true;
                //stop = true;
                pRequest->Abort();
            });

            pEventSource->Opened = [connection]()
            {
                if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
                {
                    connection->OnReconnected();
                }
            };

            pEventSource->Message = [connection, this, initializeInvoke, stop](shared_ptr<SseEvent> sseEvent) 
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
                        //stop = true;
                        connection->Disconnect();
                    }
                }
            };

            pEventSource->Closed = [connection, this, data, disconnectToken, stop](exception& ex)
            {
                //cout << "ASR Closed" << endl;

                //cout << "exception: " << ex.what() << endl;
                if (!ExceptionHelper::IsNull(ex))
                {
                    // Check if the request is aborted
                    if (!ExceptionHelper::IsRequestAborted(ex))
                    {
                        // Don't raise exceptions if the request was aborted (connection was stopped).
                        connection->OnError(ex);
                    }
                }

                //cout << "exception handling complete" << endl;

                //if (stop)
                if (mStop)
                {
                    CompleteAbort();
                }
                else if (TryCompleteAbort())
                {
                    // Abort() was called, so don't reconnect
                }
                else
                {
                    Reconnect(connection, data, disconnectToken);
                }

                
            };

            pEventSource->Start();
        }
    });

    // some more code missing here
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
