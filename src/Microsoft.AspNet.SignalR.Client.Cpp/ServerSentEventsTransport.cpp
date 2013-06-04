#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(http_client* httpClient) : 
    HttpBasedTransport(httpClient, U("serverSentEvents"))
{

}

void ServerSentEventsTransport::OnStart(Connection* connection, string_t data,  call<int>* initializeCallback, call<int>* errorCallback)
{
    OpenConnection(connection, data, initializeCallback, errorCallback);
}

void ServerSentEventsTransport::OpenConnection(Connection* connection, string_t data, call<int>* initializeCallback, call<int>* errorCallback)
{
    utility::string_t uri = connection->GetUri() + U("connect") + GetReceiveQueryString(connection, data);

    http_request request(methods::GET);
    request.set_request_uri(uri);

    //streams::producer_consumer_buffer<uint8_t> buffer;
    //streams::basic_ostream<uint8_t> stream = buffer.create_ostream();
    //request.set_response_stream(stream);

    GetHttpClient()->request(request).then([connection](http_response response) 
    {
        // check if the task failed
        EventSourceStreamReader* eventSource = new EventSourceStreamReader(connection, response.body());

        bool stop = false;
        bool* stopPtr = &stop;

        eventSource->Opened = [connection]()
        {
            if (connection->ChangeState(ConnectionState::Reconnecting, ConnectionState::Connected))
            {
                // connection->OnReconnected(); still need to define this
            }
        };

        eventSource->Message = [connection, stopPtr](SseEvent* sseEvent) 
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
                    *stopPtr = true;
                    //connection->Disconnected(); still need to define this
                }
            }
        };

        eventSource->Closed = [](exception& ex)
        {
            //if (exception != null)
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

            //if (stop)
            //{
            //    CompleteAbort();
            //}
            //else if (TryCompleteAbort())
            //{
            //    // Abort() was called, so don't reconnect
            //}
            //else
            //{
            //    Reconnect(connection, data, disconnectToken);
            //}
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
