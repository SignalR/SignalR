//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "AsyncStreamReader.h"

namespace MicrosoftAspNetSignalRClientCpp
{

AsyncStreamReader::AsyncStreamReader(streams::basic_istream<uint8_t> stream)
{
    mStream = stream;
    mReadingState = State::Initial;
}


AsyncStreamReader::~AsyncStreamReader(void)
{
}

void AsyncStreamReader::Start()
{

    State initial = State::Initial;

    if (atomic_compare_exchange_strong<State>(&mReadingState, &initial, State::Processing))
    {
        SetOpened = [this](){
            OnOpened();
        };

        // FIX: Potential memory leak if Close is called between the CompareExchange and here.
        pReadBuffer = shared_ptr<char>(new char[4096], [](char *s){delete[] s;});

        // Start the process loop
        Process();
    }
}

void AsyncStreamReader::Process()
{
READ:
    pplx::task<unsigned int> readTask;
    
    {
        lock_guard<mutex> lock(mBufferLock);
        if(IsProcessing() && pReadBuffer != nullptr)
        {
            readTask = AsyncReadIntoBuffer(mStream);
        }
        else
        {
            return;
        }
    }

    if (readTask.is_done())
    {        
        unsigned int bytesRead;
        exception ex;
        TaskStatus status = TaskAsyncHelper::RunTaskToCompletion<unsigned int>(readTask, bytesRead, ex);

        if (status == TaskStatus::TaskCompleted)
        {
            if (TryProcessRead(bytesRead))
            {
                goto READ;
            }
        }
        else if (status == TaskStatus::TaskCanceled)
        {
            Close(OperationCanceledException("readTask"));
        }
        else
        {
            Close(ex);
        }
    }
    else
    {
        ReadAsync(readTask);
    }
}

void AsyncStreamReader::ReadAsync(pplx::task<unsigned int> readTask)
{
    readTask.then([this](pplx::task<unsigned int> readTask)
    {
        unsigned int bytesRead;
        exception ex;
        TaskStatus status = TaskAsyncHelper::RunTaskToCompletion<unsigned int>(readTask, bytesRead, ex);
        
        if (status == TaskStatus::TaskCompleted)
        {
            if (TryProcessRead(bytesRead))
            {
                Process();
            }
        }
        else if (status == TaskStatus::TaskCanceled)
        {
            Close(OperationCanceledException("readTask"));
        }
        else
        {
            Close(ex);
        }
    });
}

bool AsyncStreamReader::TryProcessRead(unsigned read)
{
    // run the setOpened method and then clear it, atomically
    function<void()> mPreviousSetOpened;
    {
        lock_guard<mutex> lock(mProcessLock);
        mPreviousSetOpened = SetOpened;
        SetOpened = [](){};
    }
    mPreviousSetOpened();

    if (read > 0)
    {
        OnData(pReadBuffer);
        return true;
    }
    else if (read == 0)
    {
        Close();
    }
    
    return false;
}

bool AsyncStreamReader::IsProcessing()
{
    return mReadingState == State::Processing;
}

void AsyncStreamReader::Close()
{
    Close(ExceptionNone("none"));
}

void AsyncStreamReader::Close(exception& ex)
{
    State previousState = atomic_exchange<State>(&mReadingState, State::Stopped);

    if(previousState != State::Stopped)
    {
        if (Closed != nullptr)
        {
            lock_guard<mutex> lock(mClosedLock);
            Closed(ex);
        }

        {
            lock_guard<mutex> lock(mBufferLock);
            pReadBuffer.reset();
        }
    }
}

void AsyncStreamReader::OnOpened()
{
    if (Opened != nullptr)
    {
        lock_guard<mutex> lock(mOpenedLock);
        Opened();
    }
}

void AsyncStreamReader::OnData(shared_ptr<char> buffer)
{
    if (Data != nullptr)
    {
        lock_guard<mutex> lock(mDataLock);
        Data(buffer);
    }
}

void AsyncStreamReader::Abort()
{
    mReadCts.cancel();
}

// returns a task that reads the incoming stream and stored the messages into a buffer
pplx::task<unsigned int> AsyncStreamReader::AsyncReadIntoBuffer(Concurrency::streams::basic_istream<uint8_t> stream)
{
    auto inStringBuffer = shared_ptr<streams::container_buffer<string>>(new streams::container_buffer<string>());
    pplx::task_options readTaskOptions(mReadCts.get_token());
    return stream.read(*(inStringBuffer.get()), 4096).then([inStringBuffer, this](size_t bytesRead)
    {
        if (is_task_cancellation_requested())
        {
            cancel_current_task();
        }

        string &text = inStringBuffer->collection();

        int length = text.length() + 1;
        pReadBuffer = shared_ptr<char>(new char[length], [](char *s){delete[] s;});
        strcpy_s(pReadBuffer.get(), length, text.c_str()); // this only works in visual studio, should use strcpy for linux

        return (unsigned int)bytesRead;
    }, readTaskOptions);
}

void AsyncStreamReader::SetOpenedCallback(function<void()> opened)
{
    lock_guard<mutex> lock(mOpenedLock);
    Opened = opened;
}

void AsyncStreamReader::SetClosedCallback(function<void(exception& ex)> closed)
{
    lock_guard<mutex> lock(mClosedLock);
    Closed = closed;
}

void AsyncStreamReader::SetDataCallback(function<void(shared_ptr<char> buffer)> data)
{
    lock_guard<mutex> lock(mDataLock);
    Data = data;
}

} // namespace MicrosoftAspNetSignalRClientCpp