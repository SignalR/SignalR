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
    class ChunkBuffer
    {
    public:
        ChunkBuffer();
        ~ChunkBuffer();

        bool HasChuncks() const;
        void Add(std::shared_ptr<char> buffer);
        utility::string_t ReadLine();

    private:
        unsigned int mOffset;
        utility::string_t mBuffer;
        utility::string_t mLineBuilder;
    };
} // namespace MicrosoftAspNetSignalRClientCpp