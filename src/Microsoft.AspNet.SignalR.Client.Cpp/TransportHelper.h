#pragma once

#include <string>
#include "IHttpClient.h"
#include "IClientTransport.h"
#include "Connection.h"

using namespace std;
using namespace utility;

class TransportHelper
{
public:
    TransportHelper(void);
    ~TransportHelper(void);

    static pplx::task<NegotiationResponse*> GetNegotiationResponse(http_client* client, Connection* connnection);
    static string_t GetReceiveQueryString(Connection* connection, string_t data, string_t transport);
    static void ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected);
    static void ProcessResponse(Connection* connection, string_t response, bool* timeOut, bool* disconnected, function<void()> onInitialized);
private:
    struct NegotiationRequestInfo
    {
        void* UserState;
        IClientTransport::NEGOTIATE_CALLBACK Callback;
    };

    static void OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
    static string_t CleanString(string_t uri);
    static string_t EncodeUri(string_t uri);
};

