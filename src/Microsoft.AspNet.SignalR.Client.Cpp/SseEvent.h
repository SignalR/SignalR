#pragma once

#include <http_client.h>
#include "StringHelper.h"

using namespace utility;
using namespace std;

enum EventType
{
    Id,
    Data
};

class SseEvent
{
public:
    SseEvent(EventType type, string_t data);
    ~SseEvent(void);
    
    EventType GetType();
    string_t GetData();
    string_t ToString();
    static bool TryParse(string_t line, shared_ptr<SseEvent>* sseEvent);

private:
    EventType mType;
    string_t mData;
};