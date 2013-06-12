#include "SseEvent.h"

SseEvent::SseEvent(EventType type, string_t data)
{
    mType = type;
    mData = data;
}


SseEvent::~SseEvent()
{
}

EventType SseEvent::GetType()
{
    return mType;
}

string_t SseEvent::GetData()
{
    return mData;
}

string_t SseEvent::ToString()
{
    return mType + U(": ") + mData;
}

bool SseEvent::TryParse(string_t line, shared_ptr<SseEvent>* sseEvent)
{
    *sseEvent = nullptr;

    if (line.empty())
    {
        throw exception("ArgumentNullException: line");
    }

    if (StringHelper::BeginsWithIgnoreCase(line, U("data:")))
    {
        string_t data = StringHelper::Trim(line.substr(string_t(U("data:")).length(), line.length() - string_t(U("data:")).length()));
        *sseEvent = shared_ptr<SseEvent>(new SseEvent(EventType::Data, data));
        return true;
    }
    else if (StringHelper::BeginsWithIgnoreCase(line, U("id:")))
    {
        string_t data = StringHelper::Trim(line.substr(string_t(U("id:")).length(), line.length() - string_t(U("id:")).length()));
        *sseEvent = shared_ptr<SseEvent>(new SseEvent(EventType::Id, data));
        return true;
    }
    else return false;
}
