#pragma once

class IConnectionHandler;
class IClientTransport;

#include <string>
#include "IHttpClient.h"
#include "IClientTransport.h"
#include <http_client.h>
#include <filestream.h>
#include <containerstream.h>
#include <producerconsumerstream.h>

using namespace concurrency;
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
    pplx::task<void> Start(IClientTransport* transport);
    pplx::task<void> Start(http_client* client);
    void Stop();
    void Send(string data);
    
    State GetState();
    utility::string_t GetConnectionId();
    utility::string_t GetConnectionToken();
    void SetConnectionId(utility::string_t connectionId);
    void SetConnectionToken(utility::string_t connectionToken);
    utility::string_t GetGroupsToken();
    IClientTransport* GetTransport();
    utility::string_t GetUri();
    utility::string_t GetMessageId();

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
    utility::string_t mGroupsToken;
    utility::string_t mMessageId;

    State mState;
    IClientTransport* mTransport;
    IConnectionHandler* mHandler;

    pplx::task<void> StartTransport();
    pplx::task<void> Negotiate(IClientTransport* transport);
    static void OnTransportStartCompleted(exception* error, void* state);
};

