#include "EventSourceStreamReader.h"

EventSourceStreamReader::EventSourceStreamReader(Concurrency::streams::basic_istream<uint8_t> stream)
    : AsyncStreamReader(stream)
{
    pBuffer = unique_ptr<ChunkBuffer>(new ChunkBuffer());

    Data = [this](shared_ptr<char> readBuffer){
        ProcessBuffer(readBuffer);
    };
}


EventSourceStreamReader::~EventSourceStreamReader()
{
}

void EventSourceStreamReader::ProcessBuffer(shared_ptr<char> readBuffer)
{
    {
        lock_guard<mutex> lock(mBufferLock);

        pBuffer->Add(readBuffer);

        while(pBuffer->HasChuncks())
        {
            string_t line = pBuffer->ReadLine();

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
    }
}

void EventSourceStreamReader::OnMessage(shared_ptr<SseEvent> sseEvent)
{
    if (Message != nullptr)
    {
        Message(sseEvent);
    }
}