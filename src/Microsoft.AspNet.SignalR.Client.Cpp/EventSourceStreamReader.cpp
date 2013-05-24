#include "EventSourceStreamReader.h"


EventSourceStreamReader::EventSourceStreamReader(Connection* connection, Concurrency::streams::basic_istream<uint8_t> stream)
    : AsyncStreamReader(stream)
{
    mConnection = connection;
    mBuffer = new ChunkBuffer();

    Data = [this](char readBuffer[]){
        ProcessBuffer(readBuffer);
    };
}


EventSourceStreamReader::~EventSourceStreamReader(void)
{
}

void EventSourceStreamReader::ProcessBuffer(char readBuffer[])
{
    mBufferLock.lock();

    mBuffer->Add(readBuffer);

    while(mBuffer->HasChuncks())
    {
        string_t line = mBuffer->ReadLine();

        if (line.empty())
        {
            break;
        }

        SseEvent* sseEvent;
        if (!SseEvent::TryParse(line, &sseEvent))
        {
            continue;
        }

        OnMessage(sseEvent);
    }

    mBufferLock.unlock();
}

void EventSourceStreamReader::OnMessage(SseEvent* sseEvent)
{
    if (Message != NULL)
    {
        Message(sseEvent);
    }
}