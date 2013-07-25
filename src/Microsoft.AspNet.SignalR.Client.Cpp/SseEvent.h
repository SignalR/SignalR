//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <memory>
#include "StringHelper.h"

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
        SseEvent(EventType type, utility::string_t data);
        ~SseEvent();
    
        EventType GetType() const;
        utility::string_t GetData() const;
        utility::string_t ToString() const;
        static bool TryParse(utility::string_t line, std::shared_ptr<SseEvent>* sseEvent);

    private:
        EventType mType;
        utility::string_t mData;
    };
} // namespace MicrosoftAspNetSignalRClientCpp