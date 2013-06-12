//  Variable Prefix Convention
//  http://stackoverflow.com/questions/1228161/why-use-prefixes-on-member-variables-in-c-classes third answer
//  I use:
//
//  m for members
//  p for member pointers/smart pointers

#pragma once

#include "IConnection.h"
#include "DefaultHttpClient.h"

class Connection : public IConnection, public enable_shared_from_this<Connection>
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

    pplx::task<void> Start();
    pplx::task<void> Start(shared_ptr<IClientTransport> transport);
    pplx::task<void> Start(shared_ptr<IHttpClient> client);
    void Stop();
    pplx::task<void> Send(value::field_map object);
    pplx::task<void> Send(string_t data);
    bool EnsureReconnecting();

private:
    recursive_mutex mStateLock;
    mutex mStartLock;
    unique_ptr<pplx::cancellation_token_source> mDisconnectCts;
    pplx::task<void> mConnectTask;

    void SetState(ConnectionState newState);
    pplx::task<void> StartTransport();
    pplx::task<void> Negotiate(shared_ptr<IClientTransport> transport);

    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    void Disconnect();
    void OnReceived(string_t data);
    void OnError(exception& ex);
    void OnReconnecting();
    void OnReconnected();
    void OnConnectionSlow();
    void PrepareRequest(shared_ptr<HttpRequestWrapper> request);

    // Allowing these classes to access private functions such as ChangeState
    friend class HttpBasedTransport;
    friend class ServerSentEventsTransport;
    friend class TransportHelper;
};

