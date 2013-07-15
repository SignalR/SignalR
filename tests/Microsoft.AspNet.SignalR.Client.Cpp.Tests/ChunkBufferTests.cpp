#include "UnitTest++.h"
#include "ChunkBuffer.h"

using namespace MicrosoftAspNetSignalRClientCpp;

SUITE(ChunkBufferTests)
{
    TEST(ReturnsNullIfNoNewLineIfBuffer)
    {
        // Arrange
        auto pBuffer = shared_ptr<ChunkBuffer>(new ChunkBuffer());
        auto pData = shared_ptr<char>(new char[4096]);
        strcpy(pData.get(), "hello world");

        // Act
        pBuffer->Add(pData);

        // Assert
        CHECK(pBuffer->ReadLine().compare(U("")) == 0);
    }

    TEST(ReturnsTextUpToNewLine)
    {
        // Arrange
        auto pBuffer = shared_ptr<ChunkBuffer>(new ChunkBuffer());
        auto pData = shared_ptr<char>(new char[4096]);
        strcpy(pData.get(), "hello world\noy");

        // Act
        pBuffer->Add(pData);

        // Assert
        CHECK(pBuffer->ReadLine().compare(U("hello world")) == 0);
    }

    TEST(CanReadMultipleLines)
    {
        // Arrange
        auto pBuffer = shared_ptr<ChunkBuffer>(new ChunkBuffer());
        auto pData = shared_ptr<char>(new char[4096]);
        strcpy(pData.get(), "hel\nlo world\noy");

        // Act
        pBuffer->Add(pData);

        // Assert
        CHECK(pBuffer->ReadLine().compare(U("hel")) == 0);
        CHECK(pBuffer->ReadLine().compare(U("lo world")) == 0);
        CHECK(pBuffer->ReadLine().compare(U("")) == 0);
    }

    TEST(WillCompleteNewLine)
    {
        // Arrange
        auto pBuffer = shared_ptr<ChunkBuffer>(new ChunkBuffer());
        auto pData = shared_ptr<char>(new char[4096]);
        strcpy(pData.get(), "hello");
        pBuffer->Add(pData);
        CHECK(pBuffer->ReadLine().compare(U("")) == 0);
        strcpy(pData.get(), "\n");
        pBuffer->Add(pData);
        CHECK(pBuffer->ReadLine().compare(U("hello")) == 0);
        strcpy(pData.get(), "Another line");
        pBuffer->Add(pData);
        CHECK(pBuffer->ReadLine().compare(U("")) == 0);
        strcpy(pData.get(), "\nnext");
        pBuffer->Add(pData);
        CHECK(pBuffer->ReadLine().compare(U("Another line")) == 0);
    }
}

