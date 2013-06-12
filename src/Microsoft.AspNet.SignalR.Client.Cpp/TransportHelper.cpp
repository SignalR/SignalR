#include "TransportHelper.h"

TransportHelper::TransportHelper()
{
}


TransportHelper::~TransportHelper()
{
}

task<shared_ptr<NegotiationResponse>> TransportHelper::GetNegotiationResponse(shared_ptr<IHttpClient> httpClient, shared_ptr<Connection> connection)
{
    if (httpClient == nullptr)
    {
        throw exception("ArgumentNullException: httpClient");
    }

    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    string_t uri = connection->GetUri() + U("negotiate");
    uri += AppendCustomQueryString(connection, uri);

    string_t appender = U("?");
    if (uri.find(appender) != string_t::npos)
    {
        appender = U("&");
    }

    uri += appender + U("clientProtocol=") + connection->GetProtocol();

    httpClient->Initialize(connection->GetUri());

    return httpClient->Get(uri, [connection](shared_ptr<HttpRequestWrapper> request)
    {
        connection->PrepareRequest(request);
    }, false).then([](http_response response) -> shared_ptr<NegotiationResponse>
    {
        shared_ptr<NegotiationResponse> responseObject = shared_ptr<NegotiationResponse>(new NegotiationResponse());
        
        // temporary solution, couldn't find a JSON parser in C++ 

        value raw = response.extract_json().get();

        if (raw.is_null())
        {
            throw exception("Invalid Operation Exception: Error_serverNegotiationFailed");
        }

        auto iter = raw.cbegin();

        responseObject->Uri = StringHelper::CleanString(iter->second.to_string());
        iter++;
        responseObject->ConnectionToken = StringHelper::EncodeUri(iter->second.to_string());
        iter++;
        responseObject->ConnectionId = StringHelper::CleanString(iter->second.to_string());
        iter++;
        responseObject->KeepAliveTimeout = iter->second.as_double();
        iter++;
        responseObject->DisconnectTimeout = iter->second.as_double();
        iter++;
        responseObject->TryWebSockets = iter->second.as_bool();
        iter++;
        responseObject->ProtocolVersion = StringHelper::CleanString(iter->second.to_string());
        
        return responseObject;
    });
}

string_t TransportHelper::GetReceiveQueryString(shared_ptr<Connection> connection, string_t data, string_t transport)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    string_t qs = U("");
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

    string_t customQuery = connection->GetQueryString();

    if (!customQuery.empty())
    {
        qs += U("&") + customQuery;
    }

//#if SILVERLIGHT || WINDOWS_PHONE
//    qsBuilder.Append("&").Append(GetNoCacheUrlParam());
//#endif
    
    return qs;
}

string_t TransportHelper::AppendCustomQueryString(shared_ptr<Connection> connection, string_t baseUrl)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    if (baseUrl.empty())
    {
        baseUrl = U("");
    }

    string_t appender = U(""), customQuery = connection->GetQueryString(), qs = U("");
    
    if (!customQuery.empty())
    {
        if (customQuery.front() != U('?') && customQuery.front() != U('&'))
        {
            appender = U("?");

            if (baseUrl.find(appender) != string_t::npos)
            {
                appender = U("&");
            }
        }

        qs += appender + customQuery;
    }

    return qs;
}

void TransportHelper::ProcessResponse(shared_ptr<Connection> connection, string_t response, bool* timedOut, bool* disconnected)
{
    ProcessResponse(connection, response, timedOut, disconnected, [](){});
}

void TransportHelper::ProcessResponse(shared_ptr<Connection> connection, string_t response, bool* timedOut, bool* disconnected, function<void()> onInitialized)
{
    if (connection == NULL)
    {
        throw exception("ArgumentNullException: connection");
    }

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
                    if (v.is_string())
                    {
                        connection->OnReceived(v.as_string());
                    }
                    else if (v.is_object())
                    {
                        connection->OnReceived(v.to_string());
                    }
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

void TransportHelper::UpdateGroups(shared_ptr<Connection> connection, string_t groupsToken)
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
