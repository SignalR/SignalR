//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "ChunkBuffer.h"

namespace MicrosoftAspNetSignalRClientCpp
{

ChunkBuffer::ChunkBuffer()
{
    mOffset = 0;
    mBuffer = U("");
    mLineBuilder = U("");
}


ChunkBuffer::~ChunkBuffer()
{
}

bool ChunkBuffer::HasChuncks()
{
    return mOffset < mBuffer.length();
}

void ChunkBuffer::Add(shared_ptr<char> buffer)
{
    string str(buffer.get());
    mBuffer = mBuffer.append(string_t(str.begin(), str.end()));
    str.clear();
}

string_t ChunkBuffer::ReadLine()
{
    for (unsigned int i = mOffset; i < mBuffer.length(); i++, mOffset++)
    {
        if (mBuffer.at(i) == U('\n'))
        {
            mBuffer = mBuffer.substr(mOffset + 1, mBuffer.length() - 1);

            string_t line = StringHelper::Trim(mLineBuilder);
            mLineBuilder.clear();

            mOffset = 0;
            return line;
        }

        mLineBuilder.append(mBuffer, i, 1);
    }
    return U("");
}

} // namespace MicrosoftAspNetSignalRClientCpp