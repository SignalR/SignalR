#pragma once

#include "Connection.h"
#include "StringHelper.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace web::json;

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportHelper
    {
    public:
        TransportHelper();
        ~TransportHelper();

        static pplx::task<shared_ptr<NegotiationResponse>> GetNegotiationResponse(shared_ptr<IHttpClient> client, shared_ptr<Connection> connnection);
        static string_t GetReceiveQueryString(shared_ptr<Connection> connection, string_t data, string_t transport);
        static string_t AppendCustomQueryString(shared_ptr<Connection> connection, string_t baseUrl);
        static void ProcessResponse(shared_ptr<Connection> connection, string_t response, bool* timedOut, bool* disconnected);
        static void ProcessResponse(shared_ptr<Connection> connection, string_t response, bool* timedOut, bool* disconnected, function<void()> onInitialized);
        static string_t GetSendQueryString(string_t transport, string_t connectionToken, string_t customQuery);

    private:
        static void UpdateGroups(shared_ptr<Connection> connection, string_t groupsToken);
        static void TryInitialize(value response, function<void()> onInitialized);
    };
} // namespace MicrosoftAspNetSignalRClientCpp
