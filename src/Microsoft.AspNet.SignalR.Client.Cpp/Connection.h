//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

//  Variable Prefix Convention
//
//  m for members
//  p for member shared_ptr or unique_ptr
//  wp for member weak_ptr
//  c for const/readonly

#pragma once


#include <mutex>
#include <http_client.h>

#include "StateChange.h"
#include "IClientTransport.h"
#include "DefaultHttpClient.h"
#include "HttpRequestWrapper.h"
#include "ConnectingMessageBuffer.h"
#include "HeartBeatMonitor.h"
#include "KeepAliveData.h"
#include "TraceLevels.h"

using namespace std;
using namespace pplx;
using namespace utility;
using namespace concurrency;
using namespace web::json;
using namespace web::http;
using namespace web::http::client;

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection : public enable_shared_from_this<Connection>
    {
    public:
        Connection(string_t uri, string_t queryString = U(""), TraceLevel traceLevel = TraceLevel::None);
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
        shared_ptr<KeepAliveData> GetKeepAliveData();
        ostream& GetTraceWriter();
        TraceLevel GetTraceLevel();
    
        void SetProtocol(string_t protocol);
        void SetMessageId(string_t groupsToken);
        void SetGroupsToken(string_t groupsToken); 
        void SetConnectionToken(string_t connectionToken);
        void SetConnectionId(string_t connectionId); 
        void SetTransportConnectTimeout(seconds transportConnectTimeout);
        void SetKeepAliveData(shared_ptr<KeepAliveData> keepAliveData);
        void SetTraceWriter(ostream& traceWriter);
        void SetTraceLevel(TraceLevel traceLevel);

        void SetStateChangedCallback(function<void(StateChange)> stateChanged);
        void SetReconnectingCallback(function<void()> reconnecting);
        void SetReconnectedCallback(function<void()> reconnected);
        void SetConnectionSlowCallback(function<void()> connectionSlow);
        void SetErrorCallback(function<void(exception&)> error);
        void SetClosedCallback(function<void()> closed);
        void SetReceivedCallback(function<void(string_t)> received);

        pplx::task<void> Start();
        pplx::task<void> Start(shared_ptr<IClientTransport> transport);
        pplx::task<void> Start(shared_ptr<IHttpClient> client);
        void Stop();
        void Stop(seconds timeout);
        pplx::task<void> Send(value::field_map object);
        pplx::task<void> Send(string_t data);
        bool EnsureReconnecting();
        void Trace(TraceLevel flag, string_t message);

    private:
        string_t mProtocol; // temporarily stored as a string
        string_t mMessageId;
        string_t mGroupsToken;
        string_t mConnectionId;
        string_t mConnectionToken;
        string_t mUri;
        string_t mQueryString;
        seconds mTransportConnectTimeout;
        recursive_mutex mStateLock;
        mutex mStartLock;
        mutex mTraceLock;
        ostream mTraceWriter;
        TraceLevel mTraceLevel;
        ConnectionState mState;
        ConnectingMessageBuffer mConnectingMessageBuffer;
        HeartBeatMonitor mMonitor;
        shared_ptr<KeepAliveData> pKeepAliveData;
        shared_ptr<IClientTransport> pTransport;
        unique_ptr<pplx::cancellation_token_source> pDisconnectCts;
        pplx::task<void> mConnectTask;
        const seconds cDefaultAbortTimeout;

        mutex mStateChangedLock;
        function<void(StateChange)> StateChanged;
        mutex mReconnectingLock;
        function<void()> Reconnecting;
        mutex mReconnectedLock;
        function<void()> Reconnected;
        mutex mConnectionSlowLock;
        function<void()> ConnectionSlow;
        mutex mErrorLock;
        function<void(exception&)> Error;
        mutex mClosedLock;
        function<void()> Closed;
        mutex mReceivedLock;
        function<void(string_t)> Received;

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
        void UpdateLaskKeepAlive();
        void PrepareRequest(shared_ptr<HttpRequestWrapper> request);

        // Allowing these classes to access private functions such as ChangeState
        friend class HttpBasedTransport;
        friend class TransportAbortHandler;
        friend class ServerSentEventsTransport;
        friend class TransportHelper;
        friend class HeartBeatMonitor;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
