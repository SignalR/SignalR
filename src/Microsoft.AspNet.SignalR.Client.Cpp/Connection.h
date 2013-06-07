#pragma once

#include "IConnection.h"
#include "DefaultHttpClient.h"

class Connection : public IConnection
{
public:

    Connection(string_t uri);
    ~Connection(void);

    function<void(string_t message)> Received;
    function<void(shared_ptr<StateChange> stateChange)> StateChanged;
    function<void(exception& ex)> Error;
    function<void()> Closed;
    function<void()> Reconnecting;
    function<void()> Reconnected;
    function<void()> ConnectionSlow;

    void SetConnectionToken(string_t connectionToken);
    void SetConnectionId(string_t connectionId);

    task<void> Start();
    task<void> Start(shared_ptr<IClientTransport> transport);
    task<void> Start(shared_ptr<IHttpClient> client);
    void Stop();
    task<void> Send(value::field_map object);
    task<void> Send(string_t data);
    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    bool EnsureReconnecting();
    void Disconnect();

    void OnReceived(string_t data);
    void OnError(exception& ex);
    void OnReconnecting();
    void OnReconnected();
    void OnConnectionSlow();
    void PrepareRequest(shared_ptr<HttpRequestWrapper> request);

private:
    mutex mStateLock;
    mutex mStartLock;
    unique_ptr<cancellation_token_source> mDisconnectCts;
    task<void> mConnectTask;

    void SetState(ConnectionState newState);
    task<void> StartTransport();
    task<void> Negotiate(shared_ptr<IClientTransport> transport);
    static void OnTransportStartCompleted(exception* error, void* state);
};

