#include "LongPollingTransport.h"

LongPollingTransport::LongPollingTransport(shared_ptr<IHttpClient> httpClient) :
    HttpBasedTransport(httpClient, U("longPolling"))
{
}


LongPollingTransport::~LongPollingTransport(void)
{
}

//pplx::task<void> LongPollingTransport::Start(Connection* connection, utility::string_t data, void* state)
//{    
//    //string url = connection->GetUrl();
//
//    //if(startCallback != NULL)
//    //{
//    //    url += "connect";
//    //}
//
//    //// TODO: Handle reconnect
//
//    //url += TransportHelper::GetReceiveQueryString(connection, data, "longPolling");
//
//    //auto info = new PollHttpRequestInfo();
//    //info->CallbackState = state;
//    //info->Transport = this;
//    //info->Callback = startCallback;
//    //info->Connection = connection;
//    //info->Data = data;
//
//    ////mHttpClient->Get(url, &LongPollingTransport::OnPollHttpResponse, info);
//
//    //// TODO: Need to set a timer here to trigger connected after 2 seconds or so
//
//    return pplx::task<void>();
//}

void LongPollingTransport::OnStart(Connection* connection, utility::string_t data)
{

}
