//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <http_client.h>
#include "StringHelper.h"

using namespace std;
using namespace utility;

namespace MicrosoftAspNetSignalRClientCpp
{
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
} // namespace MicrosoftAspNetSignalRClientCpp