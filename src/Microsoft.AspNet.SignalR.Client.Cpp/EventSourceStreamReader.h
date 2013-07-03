#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"
#include <mutex>

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    EventSourceStreamReader(Concurrency::streams::basic_istream<uint8_t> stream);
    ~EventSourceStreamReader();

    void SetMessageCallback(function<void(shared_ptr<SseEvent> sseEvent)> message);

private:
    unique_ptr<ChunkBuffer> pBuffer;
    mutex mMessageLock;
    function<void(shared_ptr<SseEvent> sseEvent)> Message;

    void ProcessBuffer(shared_ptr<char> readBuffer);
    void OnMessage(shared_ptr<SseEvent> sseEvent);
};