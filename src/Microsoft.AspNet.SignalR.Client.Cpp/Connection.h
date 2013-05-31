#pragma once

class StateChange;
class IClientTransport;
class IConnectionHandler;

#include <mutex>
#include <string>
#include <filestream.h>
#include <http_client.h>
#include <containerstream.h>
#include <producerconsumerstream.h>


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

    Connection(string_t uri, IConnectionHandler* handler);
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
    
    void SetConnectionToken(string_t connectionToken);
    void SetGroupsToken(string_t groupsToken);
    void SetMessageId(string_t groupsToken);
    void SetConnectionId(string_t connectionId);

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

