//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"
#include <mutex>

using namespace std;

namespace MicrosoftAspNetSignalRClientCpp
{
    class EventSourceStreamReader :
        public AsyncStreamReader
    {
    public:
        EventSourceStreamReader(shared_ptr<Connection> connection, Concurrency::streams::basic_istream<uint8_t> stream);
        ~EventSourceStreamReader();

        void SetMessageCallback(function<void(shared_ptr<SseEvent> sseEvent)> message);

    private:
        weak_ptr<Connection> wpConnection;
        unique_ptr<ChunkBuffer> pBuffer;
        mutex mMessageLock;
        function<void(shared_ptr<SseEvent> sseEvent)> Message;

        void ProcessBuffer(shared_ptr<char> readBuffer);
        void OnMessage(shared_ptr<SseEvent> sseEvent);
    };
} // namespace MicrosoftAspNetSignalRClientCpp