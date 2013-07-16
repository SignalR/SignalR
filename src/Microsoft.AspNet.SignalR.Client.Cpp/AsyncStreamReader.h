//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <mutex>
#include <atomic>
#include "SseEvent.h"
#include <http_client.h>
#include "ExceptionHelper.h"
#include "TaskAsyncHelper.h"

using namespace std;

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
        AsyncStreamReader(streams::basic_istream<uint8_t> stream);
        ~AsyncStreamReader(void);
        void SetOpenedCallback(function<void()> opened);
        void SetClosedCallback(function<void(exception& ex)> closed);
        void SetDataCallback(function<void(shared_ptr<char> buffer)> data);
        void Start();
        void Abort();

    protected:
        mutex mBufferLock;

    private:
        mutex mProcessLock;
        streams::basic_istream<uint8_t> mStream;
        shared_ptr<char> pReadBuffer;
        atomic<State> mReadingState;
        pplx::cancellation_token_source mReadCts;
        function<void()> SetOpened;
        mutex mOpenedLock;
        function<void()> Opened;
        mutex mClosedLock;
        function<void(exception& ex)> Closed;
        mutex mDataLock;
        function<void(shared_ptr<char> buffer)> Data;

        bool IsProcessing();
        void Close();
        void Close(exception &ex);
        void Process();
        void ReadAsync(pplx::task<unsigned int> readTask);
        bool TryProcessRead(unsigned read);
        void OnOpened();
        void OnData(shared_ptr<char> buffer);
        pplx::task<unsigned int> AsyncReadIntoBuffer(Concurrency::streams::basic_istream<uint8_t> stream);
    };
} // namespace MicrosoftAspNetSignalRClientCpp