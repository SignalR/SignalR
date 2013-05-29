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
using namespace utility;
using namespace web::json;

class Connection
{
public:

    enum ConnectionState
    {
        Connecting,
        Connected,
        Reconnecting,
        Disconnected
    };

    Connection(string_t uri, IConnectionHandler* handler);
    ~Connection(void);

    function<void(string_t message)> Received;

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
    
    pplx::task<void> Start();
    pplx::task<void> Start(IClientTransport* transport);
    pplx::task<void> Start(http_client* client);
    void Stop();
    pplx::task<void> Send(web::json::value::field_map object);
    pplx::task<void> Send(string_t data);

    // Transport API
    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    bool EnsureReconnecting();
    void OnError(exception error);
    void OnReceived(string_t data);

    void SetConnectionState(NegotiationResponse negotiateResponse);

private:
    string_t mUri;
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mGroupsToken;
    string_t mMessageId;

    ConnectionState mState;
    IClientTransport* mTransport;
    IConnectionHandler* mHandler;

    pplx::task<void> StartTransport();
    pplx::task<void> Negotiate(IClientTransport* transport);
    static void OnTransportStartCompleted(exception* error, void* state);
};

