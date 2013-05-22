#include "TransportHelper.h"

TransportHelper::TransportHelper(void)
{
}


TransportHelper::~TransportHelper(void)
{
}

pplx::task<NegotiationResponse*> TransportHelper::GetNegotiationResponse(http_client* client, Connection* connnection)
{
    utility::string_t uri = connnection->GetUri() + U("/negotiate");

	//uri += U("?clientProtoco=1.3"); // modify this later

    //auto info = new NegotiationRequestInfo();
    //info->UserState = state;
    //info->Callback = negotiateCallback;

    //http_client client(uri);

    http_request request(methods::GET);
    request.set_request_uri(uri);

    return client->request(request).then([](http_response response) -> NegotiationResponse*
    {
        NegotiationResponse* responseObject = new NegotiationResponse();
        
        web::json::value obj = response.extract_json().get();
        auto iter = obj.cbegin();

        
        responseObject->Uri = CleanString(iter->second.to_string());
        iter++;
        responseObject->ConnectionToken = EncodeUri(iter->second.to_string());
        iter++;
        responseObject->ConnectionId = CleanString(iter->second.to_string());
        iter++;
        responseObject->KeepAliveTimeout = iter->second.as_double();
        iter++;
        responseObject->DisconnectTimeout = iter->second.as_double();
        iter++;
        responseObject->TryWebSockets = iter->second.as_bool();
        iter++;
        responseObject->ProtocolVersion = CleanString(iter->second.to_string());
        
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

utility::string_t TransportHelper::GetReceiveQueryString(Connection* connection, utility::string_t data, utility::string_t transport)
{
    // ?transport={0}&connectionToken={1}&messageId={2}&groups={3}&connectionData={4}{5}
    utility::string_t qs = U("");
    qs += U("?transport=") + transport + U("&connectionToken=") + connection->GetConnectionToken();


    if (connection->GetMessageId() != U(""))
    {
        qs += U("&messageId=") + connection->GetMessageId();
    }

    if (connection->GetGroupsToken() != U(""))
    {
        qs += U("&groupsToken=") + connection->GetGroupsToken();
    }

    if (data != U(""))
    {
        qs += U("&connectionData=") + data;
    }

//    string customQuery = connection.QueryString;
//
//    if (!String.IsNullOrEmpty(customQuery))
//    {
//        qsBuilder.Append("&").Append(customQuery);
//    }
//
//#if SILVERLIGHT || WINDOWS_PHONE
//    qsBuilder.Append("&").Append(GetNoCacheUrlParam());
//#endif
    
    return qs;
}

utility::string_t TransportHelper::CleanString(utility::string_t string)
{
    // strip off extra "" from the string
    return string.substr(1, string.length()-2);
}

utility::string_t TransportHelper::EncodeUri(utility::string_t uri)
{
    // strip off extra "" from the string
    uri = CleanString(uri);
    return uri::encode_data_string(uri);
}

void TransportHelper::ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected)
{
    // Parse some JSON stuff
}