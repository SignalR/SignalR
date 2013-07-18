#include "AsyncStreamReader.h"

AsyncStreamReader::AsyncStreamReader(Concurrency::streams::basic_istream<uint8_t> stream)
{
    mStream = stream;
    mReadingState = State::Initial;
}


AsyncStreamReader::~AsyncStreamReader(void)
{
    // cancel any ongoing reads
    mReadCts.cancel();
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
        pReadBuffer = shared_ptr<char>(new char[4096]);

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

void AsyncStreamReader::ReadAsync(pplx::task<unsigned int> readTask)
{
    try
    {
        readTask.then([readTask, this](unsigned int bytesRead)
        {
            if (TryProcessRead(bytesRead))
            {
                Process();
            }
        });
    }
    catch (exception& ex)
    {
        // how to differentiate between faulted and canceled tasks?
        Close(ex);
    }
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
    Close(exception(""));
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

// returns a task that reads the incoming stream and stored the messages into a buffer
task<unsigned int> AsyncStreamReader::AsyncReadIntoBuffer(Concurrency::streams::basic_istream<uint8_t> stream)
{
    concurrency::streams::container_buffer<string> inStringBuffer;
    task_options readTaskOptions(mReadCts.get_token());
    return stream.read(inStringBuffer, 4096).then([inStringBuffer, this](size_t bytesRead)
    {
        if (is_task_cancellation_requested())
        {
            cancel_current_task();
        }

        string &text = inStringBuffer.collection();

        pReadBuffer = shared_ptr<char>(new char[text.length() + 1]);
        int length = text.length();
        strcpy(pReadBuffer.get(), text.c_str());

        return (unsigned int)bytesRead;
    }, readTaskOptions);
}