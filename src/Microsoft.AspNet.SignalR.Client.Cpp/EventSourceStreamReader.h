#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    EventSourceStreamReader(Concurrency::streams::basic_istream<uint8_t> stream);
    ~EventSourceStreamReader();

    function<void(shared_ptr<SseEvent> sseEvent)> Message;

private:
    unique_ptr<ChunkBuffer> pBuffer;

    void ProcessBuffer(shared_ptr<char> readBuffer);
    void OnMessage(shared_ptr<SseEvent> sseEvent);
};