#pragma once

#include "Connection.h"
#include "StringHelper.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace web::json;

class TransportHelper
{
public:
    TransportHelper(void);
    ~TransportHelper(void);

    static task<shared_ptr<NegotiationResponse>> GetNegotiationResponse(shared_ptr<IHttpClient> client, Connection* connnection);
    static string_t GetReceiveQueryString(Connection* connection, string_t data, string_t transport);
    static string_t AppendCustomQueryString(Connection* connection, string_t baseUrl);
    static void ProcessResponse(Connection* connection, string_t response, bool* timedOut, bool* disconnected);
    static void ProcessResponse(Connection* connection, string_t response, bool* timedOut, bool* disconnected, function<void()> onInitialized);
private:

    static void UpdateGroups(Connection* connection, string_t groupsToken);
    static void TryInitialize(value response, function<void()> onInitialized);
};

