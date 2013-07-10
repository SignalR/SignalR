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
        HeartBeatMonitor(shared_ptr<Connection> connection, shared_ptr<mutex> connectionStateLock);
        ~HeartBeatMonitor();

    private:
        bool mHasBeenWarned;
        bool mTimedOut;
        pplx::details::timer_t mTimer;
        shared_ptr<mutex> pConnectionStateLock;
        shared_ptr<Connection> pConnection;
    };
}