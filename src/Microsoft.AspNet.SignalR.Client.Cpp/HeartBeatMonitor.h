//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include <mutex>

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class HeartBeatMonitor
    {
    public:
        HeartBeatMonitor();
        HeartBeatMonitor(std::shared_ptr<Connection> connection, std::shared_ptr<std::recursive_mutex> connectionStateLock);
        ~HeartBeatMonitor();

        void Start();

    private:
        bool mHasBeenWarned;
        bool mTimedOut;
        pplx::details::timer_t mTimer;
        std::shared_ptr<std::recursive_mutex> pConnectionStateLock;
        std::weak_ptr<Connection> wpConnection;

        static void Beat(void* state);
        void Beat(int timeElapsed);
    };
}