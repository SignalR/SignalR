#include "TransportHelper.h"

TransportHelper::TransportHelper(void)
{
}


TransportHelper::~TransportHelper(void)
{
}

pplx::task<NegotiationResponse*> TransportHelper::GetNegotiationResponse(Connection* connnection)
{
    utility::string_t uri = connnection->GetUri() + U("/negotiate");

	//uri += U("?clientProtoco=1.3"); // modify this later

    //auto info = new NegotiationRequestInfo();
    //info->UserState = state;
    //info->Callback = negotiateCallback;

    http_client client(uri);

    return client.request(methods::GET).then([](http_response response) -> NegotiationResponse*
    {
        NegotiationResponse* responseObject = new NegotiationResponse();
        
        web::json::value obj = response.extract_json().get();
        auto iter = obj.cbegin();
        responseObject->Uri = iter->second.to_string();
        iter++;
        responseObject->ConnectionToken = iter->second.to_string();
        iter++;
        responseObject->ConnectionId = iter->second.to_string();
        iter++;
        responseObject->KeepAliveTimeout = iter->second.as_double();
        iter++;
        responseObject->DisconnectTimeout = iter->second.as_double();
        iter++;
        responseObject->TryWebSockets = iter->second.as_bool();
        iter++;
        responseObject->ProtocolVersion = iter->second.to_string();
        
        return responseObject;

    });

    //httpClient->Get(url, &TransportHelper::OnNegotiateHttpResponse, info);
}


void TransportHelper::OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto negotiateInfo = (NegotiationRequestInfo*)state;

    string raw = httpResponse->GetResponseBody();

    // TODO: Parse using some kind of JSON library into a Negotiate response
    auto response = NegotiationResponse();
    response.ConnectionId = U("");
    response.ConnectionToken = U("");
    response.ProtocolVersion = U("1.2");

    negotiateInfo->Callback(&response, NULL, negotiateInfo->UserState);

    delete negotiateInfo;
}

string TransportHelper::GetReceiveQueryString(Connection* connection, string data, string transport)
{
    //// TODO: Encoding
    //string qs = "?transport=" + transport + "&connectionToken=" + connection->GetConnectionToken();

    //auto messageId = connection->GetMessageId();
    //auto groupsToken = connection->GetGroupsToken();
    //
    //if(!messageId.empty())
    //{
    //    qs += "&messageId=" + messageId;
    //}

    //if(!groupsToken.empty())
    //{
    //    qs += "&groupsToken=" + groupsToken;
    //}

    return "";
}

void TransportHelper::ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected)
{
    // Parse some JSON stuff
}