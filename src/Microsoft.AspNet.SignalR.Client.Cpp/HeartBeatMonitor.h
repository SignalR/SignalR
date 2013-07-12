#pragma once

#include "http_client.h"
#include <mutex>

using namespace std;
using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
    class Connection;

    class HeartBeatMonitor
    {
    public:
        HeartBeatMonitor();
        HeartBeatMonitor(shared_ptr<Connection> connection, shared_ptr<recursive_mutex> connectionStateLock);
        ~HeartBeatMonitor();

        void Start();

    private:
        bool mHasBeenWarned;
        bool mTimedOut;
        pplx::details::timer_t mTimer;
        shared_ptr<recursive_mutex> pConnectionStateLock;
        weak_ptr<Connection> wpConnection;

        static void Beat(void* state);
        void Beat(int timeElapsed);
    };
}