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
    class ChunkBuffer
    {
    public:
        ChunkBuffer();
        ~ChunkBuffer();

        bool HasChuncks();
        void Add(shared_ptr<char> buffer);
        string_t ReadLine();

    private:
        unsigned int mOffset;
        string_t mBuffer;
        string_t mLineBuilder;
    };
} // namespace MicrosoftAspNetSignalRClientCpp