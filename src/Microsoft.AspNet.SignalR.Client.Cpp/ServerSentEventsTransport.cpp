#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(shared_ptr<IHttpClient> httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{
}

ServerSentEventsTransport::~ServerSentEventsTransport()
{
    mConnectionTimeout = seconds(2);
    mReconnectDelay = seconds(5);
}

void ServerSentEventsTransport::OnAbort()
{
    pEventSource->Opened = [](){};
    pEventSource->Closed = [](exception& ex){};
    pEventSource->Data = [](shared_ptr<char> buffer){};
    pEventSource->Message = [](shared_ptr<SseEvent> sseEvent){};
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
    }, true).then([this, connection, data, disconnectToken, errorCallback](http_response response) 
    {
        // check if the task failed

        pEventSource = unique_ptr<EventSourceStreamReader>(new EventSourceStreamReader(response.body()));

        mStop = false;
        
        disconnectToken.register_callback<function<void()>>([this]()
        {
            mStop = true;
            pRequest->Abort();
        });

        pEventSource->Opened = [connection]()
        {
            if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
            {
                connection->OnReconnected();
            }
        };

        pEventSource->Message = [connection, this](shared_ptr<SseEvent> sseEvent) 
        {
            if (sseEvent->GetType() == EventType::Data)
            {
                if (StringHelper::EqualsIgnoreCase(sseEvent->GetData(), U("initialized")))
                {
                    return;
                }

                bool timedOut, disconnected;

                TransportHelper::ProcessResponse(connection, sseEvent->GetData(), &timedOut, &disconnected, [](){});
                disconnected = false;

                if (disconnected)
                {
                    this->mStop = true;
                    connection->Disconnect();
                }
            }
        };

        pEventSource->Closed = [this, connection, data, disconnectToken](exception& ex)
        {
            //if (ex != null)
            //{
            //    // Check if the request is aborted
            //    bool isRequestAborted = ExceptionHelper.IsRequestAborted(exception);

            //    if (!isRequestAborted)
            //    {
            //        // Don't raise exceptions if the request was aborted (connection was stopped).
            //        connection.OnError(exception);
            //    }
            //}

            if (this->mStop)
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
    });
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
