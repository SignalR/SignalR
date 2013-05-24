#include "SseEvent.h"


SseEvent::SseEvent(EventType type, string_t data)
{
    mType = type;
    mData = data;
}


SseEvent::~SseEvent(void)
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

bool BeginsWithIgnoreCase(string_t string1, string_t string2)
{
    string1 = string1.substr(0, string2.length());
    transform(string1.begin(), string1.end(), string1.begin(), towupper);
    transform(string2.begin(), string2.end(), string2.begin(), towupper);
    return string1 == string2;
}

string_t Trim(string_t string)
{
    string.erase(0, string.find_first_not_of(' '));
    string.erase(string.find_last_not_of(' ') + 1);
    return string;
}

bool SseEvent::TryParse(string_t line, SseEvent** sseEvent)
{
    if (line == U(""))
    {
        //return some error
    }

    if (BeginsWithIgnoreCase(line, U("data:")))
    {
        string_t data = Trim(line.substr(string_t(U("data:")).length(), line.length()));
        SseEvent* tempEvent = new SseEvent(EventType::Data, data);
        *sseEvent = tempEvent;
        return true;
    }
    else if (BeginsWithIgnoreCase(line, U("id:")))
    {
        string_t data = Trim(line.substr(string_t(U("id:")).length(), line.length()));
        *sseEvent = new SseEvent(EventType::Id, data);
        return true;
    }
    else return false;
}
