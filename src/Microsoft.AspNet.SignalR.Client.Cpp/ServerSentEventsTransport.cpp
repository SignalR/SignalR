#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(shared_ptr<IHttpClient> httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{

}

ServerSentEventsTransport::~ServerSentEventsTransport()
{

}

void ServerSentEventsTransport::OnAbort()
{
    mEventSource->Opened = [](){};
    mEventSource->Closed = [](exception& ex){};
    mEventSource->Data = [](shared_ptr<char> buffer){};
    mEventSource->Message = [](shared_ptr<SseEvent> sseEvent){};
}

void ServerSentEventsTransport::OnStart(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken,  function<void()> initializeCallback, function<void()> errorCallback)
{    
    OpenConnection(connection, data, disconnectToken, initializeCallback, errorCallback);
}

void ServerSentEventsTransport::Reconnect(Connection* connection, string_t data)
{

}

void ServerSentEventsTransport::OpenConnection(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback)
{
    bool reconnecting = initializeCallback == NULL;
    unique_ptr<ThreadSafeInvoker> callbackInvoker = unique_ptr<ThreadSafeInvoker>(new ThreadSafeInvoker());

    string_t uri = connection->GetUri() + (reconnecting ? U("reconnect") : U("connect")) + GetReceiveQueryString(connection.get(), data);

    GetHttpClient()->Get(uri, [this, connection](shared_ptr<HttpRequestWrapper> request)
    {
        mRequest = request;
        connection->PrepareRequest(request);
    }, true).then([this, connection, data, disconnectToken](http_response response) 
    {
        // check if the task failed

        mEventSource = unique_ptr<EventSourceStreamReader>(new EventSourceStreamReader(response.body()));

        mStop = false;
        
        disconnectToken.register_callback<function<void()>>([this]()
        {
            this->mStop = true;
            mRequest->Abort();
        });

        long count = connection.use_count();
        bool unique = connection.unique();

        mEventSource->Opened = [connection]()
        {
            long count = connection.use_count();
            bool unique = connection.unique();
            if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
            {
                connection->OnReconnected();
            }
            count = connection.use_count();
            unique = connection.unique();
        };

        mEventSource->Message = [connection, this](shared_ptr<SseEvent> sseEvent) 
        {
            if (sseEvent->GetType() == EventType::Data)
            {
                if (StringHelper::EqualsIgnoreCase(sseEvent->GetData(), U("initialized")))
                {
                    return;
                }

                bool timedOut, disconnected;

                TransportHelper::ProcessResponse(connection.get(), sseEvent->GetData(), &timedOut, &disconnected, [](){});
                disconnected = false;

                if (disconnected)
                {
                    this->mStop = true;
                    connection->Disconnect();
                }
            }
        };

        mEventSource->Closed = [this, connection, data](exception& ex)
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
            //requestDisposer.Dispose();
            //esCancellationRegistration.Dispose();
            //response.Dispose();

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
                Reconnect(connection.get(), data);
            }
        };

        mEventSource->Start();
    });
}

void ServerSentEventsTransport::LostConnection(Connection* connection)
{
    if (mRequest != NULL)
    {
        mRequest->Abort();
    }
}

bool ServerSentEventsTransport::SupportsKeepAlive()
{
    return true;
}
