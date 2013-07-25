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

#include "StateChange.h"
#include "IClientTransport.h"
#include "DefaultHttpClient.h"
#include "HttpRequestWrapper.h"
#include "ConnectingMessageBuffer.h"
#include "HeartBeatMonitor.h"
#include "KeepAliveData.h"
#include "TraceLevels.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection : public std::enable_shared_from_this<Connection>
    {
    public:
        Connection(utility::string_t uri, utility::string_t queryString = U(""), TraceLevel traceLevel = TraceLevel::None);
        ~Connection(void);

        utility::string_t GetProtocol() const;
        utility::string_t GetMessageId() const;
        utility::string_t GetGroupsToken() const;
        utility::string_t GetConnectionId() const;
        utility::string_t GetConnectionToken() const;
        utility::string_t GetUri() const;
        utility::string_t GetQueryString() const;
        utility::seconds GetTransportConnectTimeout() const;
        ConnectionState GetState() const;
        std::shared_ptr<IClientTransport> GetTransport() const;
        std::shared_ptr<KeepAliveData> GetKeepAliveData() const;
        std::ostream& GetTraceWriter();
        TraceLevel GetTraceLevel() const;
    
        void SetProtocol(utility::string_t protocol);
        void SetMessageId(utility::string_t groupsToken);
        void SetGroupsToken(utility::string_t groupsToken); 
        void SetConnectionToken(utility::string_t connectionToken);
        void SetConnectionId(utility::string_t connectionId); 
        void SetTransportConnectTimeout(utility::seconds transportConnectTimeout);
        void SetKeepAliveData(std::shared_ptr<KeepAliveData> keepAliveData);
        void SetTraceWriter(std::ostream& traceWriter);
        void SetTraceLevel(TraceLevel traceLevel);

        void SetStateChangedCallback(std::function<void(StateChange)> stateChanged);
        void SetReconnectingCallback(std::function<void()> reconnecting);
        void SetReconnectedCallback(std::function<void()> reconnected);
        void SetConnectionSlowCallback(std::function<void()> connectionSlow);
        void SetErrorCallback(std::function<void(std::exception&)> error);
        void SetClosedCallback(std::function<void()> closed);
        void SetReceivedCallback(std::function<void(utility::string_t)> received);

        pplx::task<void> Start();
        pplx::task<void> Start(std::shared_ptr<IClientTransport> transport);
        pplx::task<void> Start(std::shared_ptr<IHttpClient> client);
        void Stop();
        void Stop(utility::seconds timeout);
        pplx::task<void> Send(web::json::value::field_map object);
        pplx::task<void> Send(utility::string_t data);
        bool EnsureReconnecting();
        void Trace(TraceLevel flag, utility::string_t message);

    private:
        utility::string_t mProtocol; // temporarily stored as a string
        utility::string_t mMessageId;
        utility::string_t mGroupsToken;
        utility::string_t mConnectionId;
        utility::string_t mConnectionToken;
        utility::string_t mUri;
        utility::string_t mQueryString;
        utility::seconds mTransportConnectTimeout;
        std::recursive_mutex mStateLock;
        std::mutex mStartLock;
        std::mutex mTraceLock;
        std::ostream mTraceWriter;
        TraceLevel mTraceLevel;
        ConnectionState mState;
        ConnectingMessageBuffer mConnectingMessageBuffer;
        HeartBeatMonitor mMonitor;
        std::shared_ptr<KeepAliveData> pKeepAliveData;
        std::shared_ptr<IClientTransport> pTransport;
        std::unique_ptr<pplx::cancellation_token_source> pDisconnectCts;
        pplx::task<void> mConnectTask;
        const utility::seconds cDefaultAbortTimeout;

        std::mutex mStateChangedLock;
        std::function<void(StateChange)> StateChanged;
        std::mutex mReconnectingLock;
        std::function<void()> Reconnecting;
        std::mutex mReconnectedLock;
        std::function<void()> Reconnected;
        std::mutex mConnectionSlowLock;
        std::function<void()> ConnectionSlow;
        std::mutex mErrorLock;
        std::function<void(std::exception&)> Error;
        std::mutex mClosedLock;
        std::function<void()> Closed;
        std::mutex mReceivedLock;
        std::function<void(utility::string_t)> Received;

        pplx::task<void> StartTransport();
        pplx::task<void> Negotiate(std::shared_ptr<IClientTransport> transport);
        bool ChangeState(ConnectionState oldState, ConnectionState newState);
        void SetState(ConnectionState newState);
        void Disconnect();
        void OnReceived(utility::string_t data);
        void OnMessageReceived(utility::string_t data);
        void OnError(std::exception& ex);
        void OnReconnecting();
        void OnReconnected();
        void OnConnectionSlow();
        void UpdateLaskKeepAlive();
        void PrepareRequest(std::shared_ptr<HttpRequestWrapper> request);

        // Allowing these classes to access private functions such as ChangeState
        friend class HttpBasedTransport;
        friend class TransportAbortHandler;
        friend class ServerSentEventsTransport;
        friend class TransportHelper;
        friend class HeartBeatMonitor;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
