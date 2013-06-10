#include "EventSourceStreamReader.h"

EventSourceStreamReader::EventSourceStreamReader(Concurrency::streams::basic_istream<uint8_t> stream)
    : AsyncStreamReader(stream)
{
    mBuffer = unique_ptr<ChunkBuffer>(new ChunkBuffer());

    Data = [this](shared_ptr<char> readBuffer){
        ProcessBuffer(readBuffer);
    };
}


EventSourceStreamReader::~EventSourceStreamReader(void)
{
    mBuffer.reset();
}

void EventSourceStreamReader::ProcessBuffer(shared_ptr<char> readBuffer)
{
    mBufferLock.lock();

    mBuffer->Add(readBuffer);

    while(mBuffer->HasChuncks())
    {
        string_t line = mBuffer->ReadLine();

        if (line.empty())
        {
            continue;
        }

        shared_ptr<SseEvent> sseEvent;
        if (!SseEvent::TryParse(line, &sseEvent))
        {
            continue;
        }

        OnMessage(sseEvent);
    }

    mBufferLock.unlock();
}

void EventSourceStreamReader::OnMessage(shared_ptr<SseEvent> sseEvent)
{
    if (Message != NULL)
    {
        Message(sseEvent);
    }
}