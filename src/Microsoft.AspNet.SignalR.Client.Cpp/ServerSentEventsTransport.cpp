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
    string url = connection->GetUrl() + TransportHelper::GetReceiveQueryString(connection, data, "serverSentEvents");

    auto info = new StartHttpRequestInfo();
    info->UserState = state;
    info->Transport = this;
    info->Callback = startCallback;
    info->Connection = connection;

    mHttpClient->Get(url, &ServerSentEventsTransport::OnStartHttpResponse, info);
}

void ServerSentEventsTransport::OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto startInfo = (StartHttpRequestInfo*)state;

    if(NULL != error)
    {
        startInfo->Transport->ReadLoop(httpResponse, startInfo->Connection, startInfo);
    }
    else
    {
        startInfo->Callback(error, startInfo->UserState);
        delete startInfo;
    }
}

void ServerSentEventsTransport::ReadLoop(IHttpResponse* httpResponse, Connection* connection, StartHttpRequestInfo* startInfo)
{
    auto readInfo = new ReadInfo();

    readInfo->HttpResponse = httpResponse;
    readInfo->Connection = connection;
    readInfo->StartInfo = startInfo;

    httpResponse->ReadLine(&ServerSentEventsTransport::OnReadLine, readInfo);
}

void ServerSentEventsTransport::OnReadLine(string data, exception* error, void* state)
{
    // if data starts with "data:"
    auto readInfo = (ReadInfo*)state;
    bool timedOut, disconnected;

    if(data == "data: initialized")
    {
        if(readInfo->StartInfo != NULL)
        {
            readInfo->StartInfo->Callback(NULL, NULL);
            readInfo->StartInfo = NULL;
        }
        else
        {
            // Reconnected
            readInfo->Connection->ChangeState(Connection::State::Reconnecting, Connection::State::Connected);
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