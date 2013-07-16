//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "SseEvent.h"

namespace MicrosoftAspNetSignalRClientCpp
{

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

    if (StringHelper::BeginsWithIgnoreCase(line, string_t(U("data:"))))
    {
        string_t data = StringHelper::Trim(line.substr(string_t(U("data:")).length(), line.length() - string_t(U("data:")).length()));
        *sseEvent = shared_ptr<SseEvent>(new SseEvent(EventType::Data, data));
        return true;
    }
    else if (StringHelper::BeginsWithIgnoreCase(line, string_t(U("id:"))))
    {
        string_t data = StringHelper::Trim(line.substr(string_t(U("id:")).length(), line.length() - string_t(U("id:")).length()));
        *sseEvent = shared_ptr<SseEvent>(new SseEvent(EventType::Id, data));
        return true;
    }
    else return false;
}

} // namespace MicrosoftAspNetSignalRClientCpp