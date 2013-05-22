#include "SseEvent.h"


SseEvent::SseEvent(EventType type, string_t data)
{
    this->mType = type;
    this->mData = data;
}


SseEvent::~SseEvent(void)
{
}

EventType SseEvent::GetType()
{
    return this->mType;
}

string_t SseEvent::GetData()
{
    return this->mData;
}

string_t SseEvent::ToString()
{
    return this->mType + U(": ") + this->mData;
}

bool BeginsWithIgnoreCase(string_t string1, string_t string2)
{
    string1 = string1.substr(0, string2.length()-1);
    transform(string1.begin(), string1.end(), string1.begin(), towupper);
    transform(string2.begin(), string2.end(), string2.begin(), towupper);
    return string1 == string2;
}

bool SseEvent::TryParse(string_t line, SseEvent** sseEvent)
{
    sseEvent = NULL;

    if (line == U(""))
    {
        //return some error
    }

    if (BeginsWithIgnoreCase(line, U("data:")))
    {
        string_t data = line.substr(string_t(U("data:")).length(), line.length()); //also trim
        *sseEvent = new SseEvent(EventType::Data, data);
        return true;
    }
    else if (BeginsWithIgnoreCase(line, U("id:")))
    {
        string_t data = line.substr(string_t(U("id:")).length(), line.length()); //also trim
        *sseEvent = new SseEvent(EventType::Id, data);
        return true;
    }
    else return false;
}
