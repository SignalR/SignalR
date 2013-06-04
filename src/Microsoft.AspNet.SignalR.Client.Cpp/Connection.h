#pragma once

#include <mutex>
#include <http_client.h>

#include "StateChange.h"
#include "IClientTransport.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;
using namespace web::json;
using namespace web::http;
using namespace web::http::client;

class Connection
{
public:

    Connection(string_t uri);
    ~Connection(void);

    function<void(string_t message)> Received;
    function<void(StateChange* stateChange)> StateChanged;
    function<void(exception& ex)> Error;
    function<void()> Closed;
    function<void()> Reconnecting;
    function<void()> Reconnected;
    function<void()> ConnectionSlow;


    ConnectionState GetState();
    string_t GetConnectionId();
    string_t GetConnectionToken();
    string_t GetGroupsToken();
    IClientTransport* GetTransport();
    string_t GetUri();
    string_t GetMessageId();
    string_t GetQueryString();
    string_t GetProtocol();
    
    void SetConnectionToken(string_t connectionToken);
    void SetGroupsToken(string_t groupsToken);
    void SetMessageId(string_t groupsToken);
    void SetConnectionId(string_t connectionId);
    void SetProtocol(string_t protocol);

    task<void> Start();
    task<void> Start(IClientTransport* transport);
    task<void> Start(http_client* client);
    void Stop();
    task<void> Send(value::field_map object);
    task<void> Send(string_t data);

    // Transport API
    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    bool EnsureReconnecting();
    void OnError(exception& ex);
    void OnReceived(string_t data);
    void Disconnect();
    void OnReconnecting();
    void OnReconnected();
    void OnConnectionSlow();

private:
    string_t mUri;
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mGroupsToken;
    string_t mMessageId;
    string_t mQueryString;
    string_t mProtocol;

    ConnectionState mState;
    IClientTransport* mTransport;
    mutex mStateLock;
    cancellation_token_source* mDisconnectCts;

    void SetState(ConnectionState newState);

    task<void> StartTransport();
    task<void> Negotiate(IClientTransport* transport);
    static void OnTransportStartCompleted(exception* error, void* state);
};

