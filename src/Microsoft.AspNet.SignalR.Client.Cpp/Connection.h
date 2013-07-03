//  Variable Prefix Convention
//  http://stackoverflow.com/questions/1228161/why-use-prefixes-on-member-variables-in-c-classes third answer
//  I use:
//
//  m for members
//  p for member pointers/smart pointers
//  c for const/readonly

#pragma once


#include <mutex>
#include <http_client.h>

#include "StateChange.h"
#include "IClientTransport.h"
#include "DefaultHttpClient.h"
#include "HttpRequestWrapper.h"
#include "ConnectingMessageBuffer.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;
using namespace web::json;
using namespace web::http;
using namespace web::http::client;

class Connection : public enable_shared_from_this<Connection>
{
public:
    Connection(string_t uri);
    ~Connection(void);

    string_t GetProtocol();
    string_t GetMessageId();
    string_t GetGroupsToken();
    string_t GetConnectionId();
    string_t GetConnectionToken();
    string_t GetUri();
    string_t GetQueryString();
    seconds GetTransportConnectTimeout();
    ConnectionState GetState();
    shared_ptr<IClientTransport> GetTransport();
    
    void SetProtocol(string_t protocol);
    void SetMessageId(string_t groupsToken);
    void SetGroupsToken(string_t groupsToken); 
    void SetConnectionToken(string_t connectionToken);
    void SetConnectionId(string_t connectionId); 
    void GetTransportConnectTimeout(seconds transportConnectTimeout);

    function<void(string_t message)> Received;
    function<void(StateChange stateChange)> StateChanged;
    function<void(exception& ex)> Error;
    function<void()> Closed;
    function<void()> Reconnecting;
    function<void()> Reconnected;
    function<void()> ConnectionSlow;

    pplx::task<void> Start();
    pplx::task<void> Start(shared_ptr<IClientTransport> transport);
    pplx::task<void> Start(shared_ptr<IHttpClient> client);
    void Stop();
    void Stop(seconds timeout);
    pplx::task<void> Send(value::field_map object);
    pplx::task<void> Send(string_t data);
    bool EnsureReconnecting();

private:
    string_t mProtocol; // temporarily stored as a string
    string_t mMessageId;
    string_t mGroupsToken;
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mUri;
    string_t mQueryString;
    seconds mTransportConnectTimeout;
    ConnectionState mState;
    recursive_mutex mStateLock;
    mutex mStartLock;
    ConnectingMessageBuffer mConnectingMessageBuffer;
    shared_ptr<IClientTransport> pTransport;
    unique_ptr<pplx::cancellation_token_source> pDisconnectCts;
    pplx::task<void> mConnectTask;
    const seconds cDefaultAbortTimeout;

    pplx::task<void> StartTransport();
    pplx::task<void> Negotiate(shared_ptr<IClientTransport> transport);
    bool ChangeState(ConnectionState oldState, ConnectionState newState);
    void SetState(ConnectionState newState);
    void Disconnect();
    void OnReceived(string_t data);
    void OnMessageReceived(string_t data);
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

