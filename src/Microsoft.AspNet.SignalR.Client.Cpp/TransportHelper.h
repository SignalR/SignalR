#pragma once

#include <string>
#include "IHttpClient.h"
#include "IClientTransport.h"
#include "Connection.h"

using namespace std;


class TransportHelper
{
public:
    TransportHelper(void);
    ~TransportHelper(void);

    static pplx::task<NegotiationResponse*> GetNegotiationResponse(http_client* client, Connection* connnection);
    static utility::string_t GetReceiveQueryString(Connection* connection, utility::string_t data, utility::string_t transport);
    static void ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected);

private:
    struct NegotiationRequestInfo
    {
        void* UserState;
        IClientTransport::NEGOTIATE_CALLBACK Callback;
    };

    static void OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
    static utility::string_t CleanString(utility::string_t uri);
    static utility::string_t EncodeUri(utility::string_t uri);
};

