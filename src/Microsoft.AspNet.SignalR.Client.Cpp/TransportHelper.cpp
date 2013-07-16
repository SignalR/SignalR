//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "TransportHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

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
    }).then([](http_response response) -> shared_ptr<NegotiationResponse>
    {
        value raw = response.extract_json().get();

        if (raw.is_null())
        {
            throw exception("Invalid Operation Exception: Server negotiation failed.");
        }
        
        return shared_ptr<NegotiationResponse>(new NegotiationResponse(raw));
    });
}

string_t TransportHelper::GetReceiveQueryString(shared_ptr<Connection> connection, string_t data, string_t transport)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    string_t qs = U("");
    qs += U("?transport=") + transport + U("&connectionToken=") + web::http::uri::encode_data_string(connection->GetConnectionToken());

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
    
    return qs;
}

string_t TransportHelper::GetSendQueryString(string_t transport, string_t connectionToken, string_t customQuery)
{
    return U("?transport=") + transport + U("&connectionToken=") + connectionToken + customQuery;
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
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    connection->UpdateLaskKeepAlive();

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
            connection->OnReceived(result.as_string());
            return;
        }

        value timedOutValue = result[U("T")];
        if (!timedOutValue.is_null())
        {
            *timedOut = timedOutValue.as_integer() == 1;
        }
        
        value disconnectedValue = result[U("D")];
        if (!disconnectedValue.is_null())
        {
            *disconnected = disconnectedValue.as_integer() == 1;
        }

        if (*disconnected)
        {
            return;
        }

        value groupsTokenValue = result[U("G")];
        if (!groupsTokenValue.is_null())
        {
            UpdateGroups(connection, groupsTokenValue.as_string());
        }

        value messages = result[U("M")];
        if (!messages.is_null())
        {
            value messageIdValue = result[U("C")];
            if (!messageIdValue.is_null())
            {
                connection->SetMessageId(messageIdValue.as_string());
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
            }

            TryInitialize(result, onInitialized);
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
    value initialize = response[U("S")];
    if (!initialize.is_null() && initialize.as_integer() == 1)
    {
        onInitialized();
    }
}

} // namespace MicrosoftAspNetSignalRClientCpp