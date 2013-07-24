//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "Connection.h"
#include "StringHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportHelper
    {
    public:
        TransportHelper();
        ~TransportHelper();

        static pplx::task<std::shared_ptr<NegotiationResponse>> GetNegotiationResponse(std::shared_ptr<IHttpClient> client, std::shared_ptr<Connection> connnection);
        static utility::string_t GetReceiveQueryString(std::shared_ptr<Connection> connection, utility::string_t data, utility::string_t transport);
        static utility::string_t AppendCustomQueryString(std::shared_ptr<Connection> connection, utility::string_t baseUrl);
        static void ProcessResponse(std::shared_ptr<Connection> connection, utility::string_t response, bool* timedOut, bool* disconnected);
        static void ProcessResponse(std::shared_ptr<Connection> connection, utility::string_t response, bool* timedOut, bool* disconnected, std::function<void()> onInitialized);
        static utility::string_t GetSendQueryString(utility::string_t transport, utility::string_t connectionToken, utility::string_t customQuery);

    private:
        static void UpdateGroups(std::shared_ptr<Connection> connection, utility::string_t groupsToken);
        static void TryInitialize(web::json::value response, std::function<void()> onInitialized);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
