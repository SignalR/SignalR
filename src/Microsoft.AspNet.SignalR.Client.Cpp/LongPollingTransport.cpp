#include "LongPollingTransport.h"

LongPollingTransport::LongPollingTransport(shared_ptr<IHttpClient> httpClient) :
    HttpBasedTransport(httpClient, U("longPolling"))
{
}


LongPollingTransport::~LongPollingTransport()
{
}

void LongPollingTransport::OnStart(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken, function<void()> initializeCallback, function<void()> errorCallback)
{
}

void LongPollingTransport::OnAbort()
{
}

void LongPollingTransport::LostConnection(shared_ptr<Connection> connection)
{
}
