#include "ChunkBuffer.h"

ChunkBuffer::ChunkBuffer(void)
{
    mOffset = 0;
    mBuffer = U("");
    mLineBuilder = U("");
}


ChunkBuffer::~ChunkBuffer(void)
{

}

bool ChunkBuffer::HasChuncks()
{
    return mOffset < mBuffer.length();
}

void ChunkBuffer::Add(shared_ptr<char> buffer)
{
    string str(buffer.get());
    string_t wstr;
    wstr.assign(str.begin(), str.end());
    mBuffer = mBuffer.append(wstr);
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