#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(http_client* httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{

}

void ServerSentEventsTransport::OnStart(Connection* connection, string_t data, cancellation_token disconnectToken,  call<int>* initializeCallback, call<int>* errorCallback)
{
    OpenConnection(connection, data, disconnectToken, initializeCallback, errorCallback);
}

void ServerSentEventsTransport::Reconnect(Connection* connection, string_t data)
{

}

void ServerSentEventsTransport::OpenConnection(Connection* connection, string_t data, cancellation_token disconnectToken, call<int>* initializeCallback, call<int>* errorCallback)
{
    bool reconnecting = initializeCallback == NULL;

    utility::string_t uri = connection->GetUri() + U("connect") + GetReceiveQueryString(connection, data);

    http_request request(methods::GET);
    request.set_request_uri(uri);

    GetHttpClient()->request(request).then([this, connection, data](http_response response) 
    {
        // check if the task failed
        EventSourceStreamReader* eventSource = new EventSourceStreamReader(connection, response.body());

        bool* stop = false;

        eventSource->Opened = [connection]()
        {
            if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
            {
                connection->OnReconnected();
            }
        };

        eventSource->Message = [connection, stop](SseEvent* sseEvent) 
        {
            if (sseEvent->GetType() == EventType::Data)
            {
                if (StringHelper::EqualsIgnoreCase(sseEvent->GetData(), U("initialized")))
                {
                    return;
                }

                bool timedOut, disconnected;
                TransportHelper::ProcessResponse(connection, sseEvent->GetData(), &timedOut, &disconnected, [](){});

                if (disconnected)
                {
                    *stop = true;
                    connection->Disconnect();
                }
            }
        };

        eventSource->Closed = [stop, this, connection, data](exception& ex)
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

            if (*stop)
            {
                CompleteAbort();
            }
            else if (TryCompleteAbort())
            {
                // Abort() was called, so don't reconnect
            }
            else
            {
                Reconnect(connection, data);
            }
        };

        eventSource->Start();
    });
}

void ServerSentEventsTransport::LostConnection(Connection* connection)
{

}

bool ServerSentEventsTransport::SupportsKeepAlive()
{
    return true;
}
