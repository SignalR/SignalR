#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    function<void(SseEvent* sseEvent)> Message;

    EventSourceStreamReader(Connection* connection, Concurrency::streams::basic_istream<uint8_t> stream);
    ~EventSourceStreamReader(void);

private:
    Connection* mConnection;
    ChunkBuffer* mBuffer;

    void ProcessBuffer(char readBuffer[]);
    void OnMessage(SseEvent* sseEvent);
};