#include "TransportHelper.h"

TransportHelper::TransportHelper(void)
{
}


TransportHelper::~TransportHelper(void)
{
}

void TransportHelper::GetNegotiationResponse(http_client* httpClient, Connection* connnection, IClientTransport::NEGOTIATE_CALLBACK negotiateCallback, void* state)
{
    utility::string_t uri = connnection->GetUri() + U("/negotiate");

	//uri += U("?clientProtoco=1.3"); // modify this later

    auto info = new NegotiationRequestInfo();
    info->UserState = state;
    info->Callback = negotiateCallback;

    http_client client(uri);

    client.request(methods::GET).then([](http_response response) -> pplx::task<NegotiationResponse>
    {

        // Print the status code.
        std::wostringstream ss;
        // In this example, we print the length of the response to the console.
		ss.str(std::wstring());
		ss << response.extract_json().get() << endl;
        std::wcout << ss.str();

        //return response.extract_json().get();

    }).wait();

    //httpClient->Get(url, &TransportHelper::OnNegotiateHttpResponse, info);
}


void TransportHelper::OnNegotiateHttpResponse(IHttpResponse* httpResponse, exception* error, void* state)
{
    auto negotiateInfo = (NegotiationRequestInfo*)state;

    string raw = httpResponse->GetResponseBody();

    // TODO: Parse using some kind of JSON library into a Negotiate response
    auto response = NegotiationResponse();
    response.ConnectionId = "";
    response.ConnectionToken = "";
    response.ProtocolVersion = "1.2";

    negotiateInfo->Callback(&response, NULL, negotiateInfo->UserState);

    delete negotiateInfo;
}

string TransportHelper::GetReceiveQueryString(Connection* connection, string data, string transport)
{
    // TODO: Encoding
    string qs = "?transport=" + transport + "&connectionToken=" + connection->GetConnectionToken();

    auto messageId = connection->GetMessageId();
    auto groupsToken = connection->GetGroupsToken();
    
    if(!messageId.empty())
    {
        qs += "&messageId=" + messageId;
    }

    if(!groupsToken.empty())
    {
        qs += "&groupsToken=" + groupsToken;
    }

    return qs;
}

void TransportHelper::ProcessMessages(Connection* connection, string raw, bool* timedOut, bool* disconnected)
{
    // Parse some JSON stuff
}