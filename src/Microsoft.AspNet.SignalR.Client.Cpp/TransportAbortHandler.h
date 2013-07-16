#pragma once

#include "http_client.h"
#include "IHttpClient.h"
#include "Connection.h"
#include <agents.h>
//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "TaskAsyncHelper.h"
#include "TransportHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportAbortHandler
    {
    public:
        TransportAbortHandler(shared_ptr<IHttpClient> httpClient, utility::string_t transportName, function<void()> callback);
        ~TransportAbortHandler();
        
        void Abort(shared_ptr<Connection> connection, utility::seconds timeout, utility::string_t connectionData);
        void CompleteAbort();
        bool TryCompleteAbort();

    private:        
        bool mStartedAbort;
        mutex mAbortLock;
        mutex mDisposeLock;
        utility::string_t mTransportName;
        shared_ptr<IHttpClient> pHttpClient;
        unique_ptr<Concurrency::event> pAbortResetEvent;
        mutex mAbortCallbackLock;
        function<void()> AbortCallback;
    };
}