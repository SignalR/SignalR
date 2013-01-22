#include "ServerSentEventsTransport.h"

ServerSentEventsTransport::ServerSentEventsTransport(IHttpClient* client)
{
    mHttpClient = client;
}

ServerSentEventsTransport::~ServerSentEventsTransport(void)
{
    delete mHttpClient;
}

void ServerSentEventsTransport::Negotiate(Connection* connection, NEGOTIATE_CALLBACK negotiateCallback, void* state)
{
    TransportHelper::GetNegotiationResponse(mHttpClient, connection, negotiateCallback, state);
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

void ServerSentEventsTransport::Send(Connection* connection, string data)
{
    auto url = connection->GetUrl() + 
               "send?transport=serverSentEvents&connectionToken=" + 
               connection->GetConnectionToken();

    auto postData = map<string, string>();
    postData["data"] = data;

    // TODO: Queue requests so that we don't send fire the next request off until the previous one is finished
    // This logic should probably be in another class
    mHttpClient->Post(url, postData, &ServerSentEventsTransport::OnSendHttpResponse, this);
}

void ServerSentEventsTransport::Stop(Connection* connection)
{

}


void ServerSentEventsTransport::Abort(Connection* connection)
{

}

void ServerSentEventsTransport::OnSendHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{

}

void ServerSentEventsTransport::OnStartHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto startInfo = (StartHttpRequestInfo*)state;

    if(NULL != error)
    {
        startInfo->Callback(NULL, startInfo->UserState);
        startInfo->Transport->ReadLoop(httpResponse, startInfo->Connection);
    }
    else
    {
        startInfo->Callback(error, startInfo->UserState);
    }

    delete startInfo; 
}

void ServerSentEventsTransport::ReadLoop(IHttpResponse* httpResponse, Connection* connection)
{
    auto readInfo = new ReadInfo();

    readInfo->HttpResponse = httpResponse;
    readInfo->Connection = connection;

    httpResponse->ReadLine(&ServerSentEventsTransport::OnReadLine, readInfo);
}

void ServerSentEventsTransport::OnReadLine(string data, exception* error, void* state)
{
    // if data starts with "data:"
    auto readInfo = (ReadInfo*)state;

    // SomeHelper::ProcessMessages(data, readInfo->Connection);

    readInfo->Transport->ReadLoop(readInfo->HttpResponse, readInfo->Connection);

    delete readInfo;
}