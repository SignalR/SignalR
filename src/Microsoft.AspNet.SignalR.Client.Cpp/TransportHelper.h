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

    static pplx::task<NegotiationResponse*> GetNegotiationResponse(Connection* connnection);
    static string GetReceiveQueryString(Connection* connection, string data, string transport);
    static void ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected);

private:
    struct NegotiationRequestInfo
    {
        void* UserState;
        IClientTransport::NEGOTIATE_CALLBACK Callback;
    };

    static void OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state);
};

