#pragma once

class IConnectionHandler;
class IClientTransport;
class StateChange;

#include <string>
#include <http_client.h>
#include <filestream.h>
#include <containerstream.h>
#include <producerconsumerstream.h>
#include <mutex>

#include "IHttpClient.h"
#include "IClientTransport.h"
#include "StateChange.h"

using namespace utility;
using namespace concurrency;
using namespace pplx;
using namespace web::json;
using namespace web::http;
using namespace web::http::client;
using namespace std;

class Connection
{
public:

    Connection(string_t uri, IConnectionHandler* handler);
    ~Connection(void);

    function<void(string_t message)> Received;
    function<void(StateChange* stateChange)> StateChanged;

    ConnectionState GetState();
    string_t GetConnectionId();
    void SetConnectionId(string_t connectionId);
    string_t GetConnectionToken();
    void SetConnectionToken(string_t connectionToken);
    string_t GetGroupsToken();
    void SetGroupsToken(string_t groupsToken);
    IClientTransport* GetTransport();
    string_t GetUri();
    string_t GetMessageId();
    void SetMessageId(string_t groupsToken);
    
    task<void> Start();
    task<void> Start(IClientTransport* transport);
    task<void> Start(http_client* client);
    void Stop();
    task<void> Send(value::field_map object);
    task<void> Send(string_t data);

    // Transport API
    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    bool EnsureReconnecting();
    void OnError(exception error);
    void OnReceived(string_t data);

private:
    string_t mUri;
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mGroupsToken;
    string_t mMessageId;

    ConnectionState mState;
    IClientTransport* mTransport;
    IConnectionHandler* mHandler;
    mutex mStateLock;

    void SetState(ConnectionState newState);

    pplx::task<void> StartTransport();
    pplx::task<void> Negotiate(IClientTransport* transport);
    static void OnTransportStartCompleted(exception* error, void* state);
};

