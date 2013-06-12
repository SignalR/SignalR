#pragma once

#include <http_client.h>
#include "StringHelper.h"

using namespace std;
using namespace utility;

enum EventType
{
    Id,
    Data
};

class SseEvent
{
public:
    SseEvent(EventType type, string_t data);
    ~SseEvent();
    
    EventType GetType();
    string_t GetData();
    string_t ToString();
    static bool TryParse(string_t line, shared_ptr<SseEvent>* sseEvent);

private:
    EventType mType;
    string_t mData;
};