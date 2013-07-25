//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <mutex>
#include <atomic>
#include <ppltasks.h>
#include <containerstream.h>
#include "SseEvent.h"
#include "ExceptionHelper.h"
#include "TaskAsyncHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{
    enum State {
        Initial = 0,
        Processing,
        Stopped
    };


    class AsyncStreamReader
    {
    public:
        AsyncStreamReader(Concurrency::streams::basic_istream<uint8_t> stream);
        ~AsyncStreamReader(void);
        void SetOpenedCallback(std::function<void()> opened);
        void SetClosedCallback(std::function<void(std::exception& ex)> closed);
        void SetDataCallback(std::function<void(std::shared_ptr<char> buffer)> data);
        void Start();
        void Abort();

    protected:
        std::mutex mBufferLock;

    private:
        std::mutex mProcessLock;
        Concurrency::streams::basic_istream<uint8_t> mStream;
        std::shared_ptr<char> pReadBuffer;
        std::atomic<State> mReadingState;
        pplx::cancellation_token_source mReadCts;
        std::function<void()> SetOpened;
        std::mutex mOpenedLock;
        std::function<void()> Opened;
        std::mutex mClosedLock;
        std::function<void(std::exception& ex)> Closed;
        std::mutex mDataLock;
        std::function<void(std::shared_ptr<char> buffer)> Data;

        bool IsProcessing() const;
        void Close();
        void Close(std::exception &ex);
        void Process();
        void ReadAsync(pplx::task<unsigned int> readTask);
        bool TryProcessRead(unsigned read);
        void OnOpened();
        void OnData(std::shared_ptr<char> buffer);
        pplx::task<unsigned int> AsyncReadIntoBuffer(Concurrency::streams::basic_istream<uint8_t> stream);
    };
} // namespace MicrosoftAspNetSignalRClientCpp