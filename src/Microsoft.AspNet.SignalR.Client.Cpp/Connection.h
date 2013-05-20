#pragma once

class IConnectionHandler;
class IClientTransport;

#include <string>
#include "IHttpClient.h"
#include "IClientTransport.h"
#include <http_client.h>
#include <filestream.h>

using namespace std;
using namespace web::http;
using namespace web::http::client;

class Connection
{
public:
    enum State
    {
        Connecting,
        Connected,
        Reconnecting,
        Disconnecting,
        Disconnected,
    };

    Connection(utility::string_t uri, IConnectionHandler* handler);
    ~Connection(void);

    pplx::task<void> Start();
    pplx::task<void> Start(IClientTransport* tranport);
    pplx::task<void> Start(http_client* client);
    void Stop();
    void Send(string data);
    
    State GetState();
    utility::string_t GetConnectionId();
    utility::string_t GetConnectionToken();
    string GetGroupsToken();
    IClientTransport* GetTransport();
    utility::string_t GetUri();
    string GetMessageId();

    // Transport API
    bool ChangeState(State oldState, State newState);
    bool EnsureReconnecting();
    void OnError(exception error);
    void OnReceived(string data);

    void SetConnectionState(NegotiationResponse negotiateResponse);

private:
    utility::string_t mUri;
    utility::string_t mConnectionId;
    utility::string_t mConnectionToken;
    string mGroupsToken;
    string mMessageId;
    State mState;
    IClientTransport* mTransport;
    IConnectionHandler* mHandler;

    pplx::task<void> StartTransport();
    static void OnTransportStartCompleted(exception* error, void* state);
};

