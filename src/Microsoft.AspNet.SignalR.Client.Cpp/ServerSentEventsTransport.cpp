#include "ServerSentEventsTransport.h"

void ServerSentEventsTransport::OnReadLine(string data, exception* error, void* state)
{
    // if data starts with "data:"
    auto readInfo = (ReadInfo*)state;

    // SomeHelper::ProcessMessages(data, readInfo->Connection);

    readInfo->Transport->ReadLoop(readInfo->HttpResponse, readInfo->Connection);

    delete readInfo;
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

void ServerSentEventsTransport::OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto negotiateInfo = (NegotiationRequestInfo*)state;

    string raw = httpResponse->GetResponseBody();

    // TODO: Parse using some kind of JSON library into a Negotiate response
    auto response = NegotiateResponse();
    response.ConnectionId = "";
    response.ConnectionToken = "";
    response.ProtocolVersion = "1.2";

    negotiateInfo->Callback(&response, NULL, negotiateInfo->UserState);

    delete negotiateInfo;
}

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
    string url = connection->GetUrl() + "/negotiate";

    auto info = new NegotiationRequestInfo();
    info->UserState = state;
    info->Transport = this;
    info->Callback = negotiateCallback;

    mHttpClient->Get(url, &ServerSentEventsTransport::OnNegotiateHttpResponse, info);
}

void ServerSentEventsTransport::Start(Connection* connection, START_CALLBACK startCallback, void* state)
{
    string url = connection->GetUrl() + 
                 "?connctionToken=" + 
                 connection->GetConnectionToken() + 
                 "&transport=serverSentEvents"; 

    auto info = new StartHttpRequestInfo();
    info->UserState = state;
    info->Transport = this;
    info->Callback = startCallback;
    info->Connection = connection;

    mHttpClient->Get(url, &ServerSentEventsTransport::OnStartHttpResponse, info);
}

void ServerSentEventsTransport::Send(Connection* connection, string data)
{
    auto postData = map<string, string>();
    postData["data"] = data;
    // mHttpClient->Post(url, postData, 
}

void ServerSentEventsTransport::Stop(Connection* connection)
{

}


void ServerSentEventsTransport::Abort(Connection* connection)
{

}