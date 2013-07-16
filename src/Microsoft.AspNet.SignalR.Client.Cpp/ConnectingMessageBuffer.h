//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "http_client.h"
#include <queue>
#include <mutex>

using namespace std;
using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class ConnectingMessageBuffer
    {
    public:
        ConnectingMessageBuffer();
        ~ConnectingMessageBuffer();

        void Initialize(shared_ptr<Connection> connection, function<void(string_t)> drainCallback);
        bool TryBuffer(string_t message);
        void Drain();
        void Clear();

    private:
        shared_ptr<Connection> pConnection;
        queue<string_t> mBuffer;
        mutex mDrainCallbackLock;
        function<void(string_t)> DrainCallback;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
