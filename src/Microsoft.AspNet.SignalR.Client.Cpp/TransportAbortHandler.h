//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <agents.h>
#include "Connection.h"
#include "TaskAsyncHelper.h"
#include "TransportHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class TransportAbortHandler
    {
    public:
        TransportAbortHandler(std::shared_ptr<IHttpClient> httpClient, utility::string_t transportName, std::function<void()> callback);
        ~TransportAbortHandler();
        
        void Abort(std::shared_ptr<Connection> connection, utility::seconds timeout, utility::string_t connectionData);
        void CompleteAbort();
        bool TryCompleteAbort();

    private:        
        bool mStartedAbort;
        std::mutex mAbortLock;
        std::mutex mDisposeLock;
        utility::string_t mTransportName;
        std::shared_ptr<IHttpClient> pHttpClient;
        std::unique_ptr<Concurrency::event> pAbortResetEvent;
        std::mutex mAbortCallbackLock;
        std::function<void()> AbortCallback;
    };
}