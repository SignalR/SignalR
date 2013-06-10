#include "LongPollingTransport.h"

LongPollingTransport::LongPollingTransport(shared_ptr<IHttpClient> httpClient) :
    HttpBasedTransport(httpClient, U("longPolling"))
{
}


LongPollingTransport::~LongPollingTransport(void)
{
}

void LongPollingTransport::OnStart(shared_ptr<Connection> connection, utility::string_t data)
{

}
