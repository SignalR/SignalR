#include "UnitTest++.h"
#include "ChunkBuffer.h"

using namespace MicrosoftAspNetSignalRClientCpp;

SUITE(ChunkBufferTests)
{
    TEST(ReturnsNullIfNoNewLineIfBuffer)
    {
        // Arrange
        shared_ptr<ChunkBuffer> buffer = shared_ptr<ChunkBuffer>(new ChunkBuffer());
        shared_ptr<char> data = shared_ptr<char>(new char[4096]);
        strcpy(data.get(), "hello world");

        // Act
        buffer->Add(data);

        // Assert
        CHECK(buffer->ReadLine().compare(U("")) == 0);
    }
}

