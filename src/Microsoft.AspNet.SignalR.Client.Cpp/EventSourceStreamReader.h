//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    class EventSourceStreamReader :
        public AsyncStreamReader
    {
    public:
        EventSourceStreamReader(std::shared_ptr<Connection> connection, Concurrency::streams::basic_istream<uint8_t> stream);
        ~EventSourceStreamReader();

        void SetMessageCallback(std::function<void(std::shared_ptr<SseEvent> sseEvent)> message);

    private:
        std::weak_ptr<Connection> wpConnection;
        std::unique_ptr<ChunkBuffer> pBuffer;
        std::mutex mMessageLock;
        std::function<void(std::shared_ptr<SseEvent> sseEvent)> Message;

        void ProcessBuffer(std::shared_ptr<char> readBuffer);
        void OnMessage(std::shared_ptr<SseEvent> sseEvent);
    };
} // namespace MicrosoftAspNetSignalRClientCpp