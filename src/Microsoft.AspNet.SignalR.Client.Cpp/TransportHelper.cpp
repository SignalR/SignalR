#include "TransportHelper.h"

TransportHelper::TransportHelper(void)
{
}


TransportHelper::~TransportHelper(void)
{
}

pplx::task<NegotiationResponse*> TransportHelper::GetNegotiationResponse(http_client* client, Connection* connnection)
{
    string_t uri = connnection->GetUri() + U("/negotiate");

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

string_t TransportHelper::GetReceiveQueryString(Connection* connection, string_t data, string_t transport)
{
    // ?transport={0}&connectionToken={1}&messageId={2}&groups={3}&connectionData={4}{5}
    utility::string_t qs = U("");
    qs += U("?transport=") + transport + U("&connectionToken=") + connection->GetConnectionToken();


    if (!connection->GetMessageId().empty())
    {
        qs += U("&messageId=") + connection->GetMessageId();
    }

    if (!connection->GetGroupsToken().empty())
    {
        qs += U("&groupsToken=") + connection->GetGroupsToken();
    }

    if (!data.empty())
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

utility::string_t TransportHelper::CleanString(string_t string)
{
    // strip off extra "" from the string
    return string.substr(1, string.length()-2);
}

utility::string_t TransportHelper::EncodeUri(string_t uri)
{
    // strip off extra "" from the string
    uri = CleanString(uri);
    return uri::encode_data_string(uri);
}

void TransportHelper::ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected)
{
    // Parse some JSON stuff
}

void TransportHelper::ProcessResponse(Connection* connection, string_t response, bool* timedOut, bool* disconnected, function<void()> onInitialized)
{
    // check if connection is null

    // connection.UpdateLastKeepAlive();

    *timedOut = false;
    *disconnected = false;

    if (response.empty())
    {
        return;
    }

    try 
    {
        value result = value::parse(response);

        if (result.is_null())
        {
            return;
        }

        if (!(result[U("I")].is_null()))
        {
            connection->OnReceived(result.to_string());
            return;
        }

        if (!result[U("T")].is_null())
        {
            *timedOut = result[U("T")].as_integer() == 1;
        }
        
        if (!result[U("D")].is_null())
        {
            *disconnected = result[U("D")].as_integer() == 1;
        }

        if (*disconnected)
        {
            return;
        }

        if (!result[U("G")].is_null())
        {
            UpdateGroups(connection, result[U("G")].to_string());
        }

        value messages = result[U("M")];

        if (!messages.is_null())
        {
            if (!result[U("C")].is_null())
            {
                connection->SetMessageId(result[U("C")].to_string());
            }

            if (!(messages.cbegin() == messages.cend()))
            {
                for (auto iter = messages.cbegin(); iter != messages.cend(); iter++)
                {
                    const value &v = iter->second;
                    connection->OnReceived(v.as_string());
                }

                TryInitialize(result, onInitialized);
            }
        }
    }
    catch (exception& ex)
    {
        connection->OnError(ex);
    }
}

void TransportHelper::UpdateGroups(Connection* connection, string_t groupsToken)
{
    if (!groupsToken.empty())
    {
        connection->SetGroupsToken(groupsToken);
    }
}


void TransportHelper::TryInitialize(value response, function<void()> onInitialized)
{
    if (!response[U("S")].is_null() && response[U("S")].as_integer() == 1)
    {
        onInitialized();
    }
}
