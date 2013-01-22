#include "TransportHelper.h"


TransportHelper::TransportHelper(void)
{
}


TransportHelper::~TransportHelper(void)
{
}

void TransportHelper::GetNegotiationResponse(IHttpClient* httpClient, Connection* connnection, IClientTransport::NEGOTIATE_CALLBACK negotiateCallback, void* state)
{
    string url = connnection->GetUrl() + "/negotiate";

    auto info = new NegotiationRequestInfo();
    info->UserState = state;
    info->Callback = negotiateCallback;

    httpClient->Get(url, &TransportHelper::OnNegotiateHttpResponse, info);
}


void TransportHelper::OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
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

string TransportHelper::GetReceiveQueryString(Connection* connection, string data, string transport)
{
    // TODO: Encoding
    string qs = "?transport=" + transport + "&connectionToken=" + connection->GetConnectionToken();

    auto messageId = connection->GetMessageId();
    auto groupsToken = connection->GetGroupsToken();
    
    if(!messageId.empty())
    {
        qs += "&messageId=" + messageId;
    }

    if(!groupsToken.empty())
    {
        qs += "&groupsToken=" + groupsToken;
    }

    return qs;
}
