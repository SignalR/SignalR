#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    function<void(shared_ptr<SseEvent> sseEvent)> Message;

    EventSourceStreamReader(Connection* connection, Concurrency::streams::basic_istream<uint8_t> stream);
    ~EventSourceStreamReader(void);

private:
    Connection* mConnection;
    unique_ptr<ChunkBuffer> mBuffer;

    void ProcessBuffer(shared_ptr<char> readBuffer);
    void OnMessage(shared_ptr<SseEvent> sseEvent);
};