#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(IHttpClient* httpClient) : 
    HttpBasedTransport(httpClient)
{

}

ServerSentEventsTransport::~ServerSentEventsTransport(void)
{

}

void ServerSentEventsTransport::Start(Connection* connection, START_CALLBACK startCallback, string data, void* state)
{
    string url = connection->GetUrl(); 
    
    if(startCallback != NULL)
    {
        url += "connect";
    }

    url += TransportHelper::GetReceiveQueryString(connection, data, "serverSentEvents");

    auto requestInfo = new HttpRequestInfo();
    requestInfo->CallbackState = state;
    requestInfo->Transport = this;
    requestInfo->Callback = startCallback;
    requestInfo->Connection = connection;
    requestInfo->Data = data;

    mHttpClient->Get(url, &ServerSentEventsTransport::OnStartHttpResponse, requestInfo);
}

void ServerSentEventsTransport::OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto requestInfo = (HttpRequestInfo*)state;

    if(NULL != error)
    {
        requestInfo->Transport->ReadLoop(httpResponse, requestInfo->Connection, requestInfo);
    }
    else
    {
        requestInfo->Callback(error, requestInfo->CallbackState);
        delete requestInfo;
    }
}

void ServerSentEventsTransport::ReadLoop(IHttpResponse* httpResponse, Connection* connection, HttpRequestInfo* requestInfo)
{
    auto readInfo = new ReadInfo();

    readInfo->HttpResponse = httpResponse;
    readInfo->Connection = connection;
    readInfo->RequestInfo = requestInfo;

    httpResponse->ReadLine(&ServerSentEventsTransport::OnReadLine, readInfo);
}

void ServerSentEventsTransport::OnReadLine(string data, exception* error, void* state)
{
    // if data starts with "data:"
    auto readInfo = (ReadInfo*)state;
    bool timedOut, disconnected;

    if(data == "data: initialized")
    {
        if(readInfo->RequestInfo->Callback != NULL)
        {
            readInfo->RequestInfo->Callback(NULL, readInfo->RequestInfo->CallbackState);
            readInfo->RequestInfo->Callback = NULL;
        }
        else
        {
            // Reconnected
            readInfo->Connection->ChangeState(Connection::State::Reconnecting, Connection::State::Connected);
        }
    }
    else if(error != NULL)
    {
        if(readInfo->Connection->EnsureReconnecting())
        {
            // TODO: Delay here
            // There was an error reading so start re-connecting
            readInfo->Transport->Start(readInfo->Connection, NULL, readInfo->RequestInfo->Data);
            return;
        }
        else
        {
            readInfo->Connection->OnError(*error);
        }
    }
    else
    {
        TransportHelper::ProcessMessages(readInfo->Connection, data, &timedOut, &disconnected);
    }

    if(disconnected) 
    {
        readInfo->Connection->Stop();
    }
    else
    {
        readInfo->Transport->ReadLoop(readInfo->HttpResponse, readInfo->Connection, NULL);
    }


    delete readInfo;
}