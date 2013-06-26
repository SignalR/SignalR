#include "AsyncStreamReader.h"

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
        try
        {
            long read = readTask.get();

            if (TryProcessRead(read))
            {
                goto READ;
            }
        }
        catch (task_canceled canceled)
        {
            Close(OperationCanceledException("readTask"));
        }
        catch (exception& ex)
        {
            Close(ex);
        }
    }
    else
    {
        ReadAsync(readTask);
    }
}

void AsyncStreamReader::ReadAsync(task<unsigned int> readTask)
{
    mLastReadTask = readTask.then([this](task<unsigned int> readTask)
    {
        try 
        {
            if (TryProcessRead(readTask.get()))
            {
                Process();
            }
        }
        catch (task_canceled canceled)
        {
            Close(OperationCanceledException("readTask"));
        }
        catch (exception& ex)
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
        Opened();
    }
}

void AsyncStreamReader::OnData(shared_ptr<char> buffer)
{
    if (Data != nullptr)
    {
        Data(buffer);
    }
}

pplx::task<void> AsyncStreamReader::Abort()
{
    mReadCts.cancel();
    return mLastReadTask;
}

// returns a task that reads the incoming stream and stored the messages into a buffer
task<unsigned int> AsyncStreamReader::AsyncReadIntoBuffer(streams::basic_istream<uint8_t> stream)
{
    auto inStringBuffer = shared_ptr<streams::container_buffer<string>>(new streams::container_buffer<string>());
    task_options readTaskOptions(mReadCts.get_token());
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