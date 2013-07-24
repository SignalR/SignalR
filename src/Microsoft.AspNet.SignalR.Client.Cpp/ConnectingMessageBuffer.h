//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <basic_types.h>
#include <queue>
#include <mutex>

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class ConnectingMessageBuffer
    {
    public:
        ConnectingMessageBuffer();
        ~ConnectingMessageBuffer();

        void Initialize(std::shared_ptr<Connection> connection, std::function<void(utility::string_t)> drainCallback);
        bool TryBuffer(utility::string_t message);
        void Drain();
        void Clear();

    private:
        std::shared_ptr<Connection> pConnection;
        std::queue<utility::string_t> mBuffer;
        std::mutex mDrainCallbackLock;
        std::function<void(utility::string_t)> DrainCallback;
    };
} // namespace MicrosoftAspNetSignalRClientCpp
