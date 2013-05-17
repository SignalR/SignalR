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

    void Start();
    void Start(IClientTransport* tranport);
    void Start(http_client* client);
    void Stop();
    void Send(string data);
    
    State GetState();
    string GetConnectionId();
    string GetConnectionToken();
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
    string mConnectionId;
    string mConnectionToken;
    string mGroupsToken;
    string mMessageId;
    State mState;
    IClientTransport* mTransport;
    IConnectionHandler* mHandler;

    static void OnTransportStartCompleted(exception* error, void* state);
    static void OnNegotiateCompleted(NegotiationResponse* negotiateResponse, exception* error, void* state);
};

