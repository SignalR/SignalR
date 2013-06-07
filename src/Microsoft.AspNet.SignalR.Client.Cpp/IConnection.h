#pragma once

#include <mutex>
#include <http_client.h>

#include "StateChange.h"
#include "IClientTransport.h"
#include "HttpRequestWrapper.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;
using namespace web::json;
using namespace web::http;
using namespace web::http::client;

class IConnection
{
public:
    string_t GetProtocol();
    string_t GetMessageId();
    string_t GetGroupsToken();
    string_t GetConnectionId();
    string_t GetConnectionToken();
    string_t GetUri();
    string_t GetQueryString();
    ConnectionState GetState();
    shared_ptr<IClientTransport> GetTransport();
    
    void SetProtocol(string_t protocol);
    void SetMessageId(string_t groupsToken);
    void SetGroupsToken(string_t groupsToken);  

protected:
    string_t mProtocol;
    string_t mMessageId;
    string_t mGroupsToken;
    string_t mConnectionId;
    string_t mConnectionToken;
    string_t mUri;
    string_t mQueryString;

    ConnectionState mState;
    shared_ptr<IClientTransport> mTransport;

    virtual void Stop() = 0;
    virtual void Disconnect() = 0;
    virtual task<void> Send(string_t data) = 0;
    virtual void OnReceived(string_t data) = 0;
    virtual void OnError(exception& ex) = 0;
    virtual void OnReconnecting() = 0;
    virtual void OnReconnected() = 0;
    virtual void OnConnectionSlow() = 0;
    virtual void PrepareRequest(shared_ptr<HttpRequestWrapper> request) = 0;
};