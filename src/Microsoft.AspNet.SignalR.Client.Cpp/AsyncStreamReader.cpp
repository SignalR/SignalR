#include "AsyncStreamReader.h"


AsyncStreamReader::AsyncStreamReader(Concurrency::streams::basic_istream<uint8_t> stream)
{
    mStream = stream;
}


AsyncStreamReader::~AsyncStreamReader(void)
{
}

bool AsyncStreamReader::IsProcessing()
{
    return mReadingState == State::Processing;
}

void AsyncStreamReader::Start()
{
    State initial = State::Initial;

    if (atomic_compare_exchange_strong<State>(&mReadingState, &initial, State::Processing))
    {
        mSetOpened = [this](){
            OnOpened();
        };

        // FIX: Potential memory leak if Close is called between the CompareExchange and here.
        mReadBuffer = new char[4096];

        // Start the process loop
        Process();
    }
}

void AsyncStreamReader::Close()
{
    Close(exception(NULL));
}

void AsyncStreamReader::Process()
{
READ:
    pplx::task<unsigned int> readTask;
    
    mBufferLock.lock();
    if(IsProcessing() && mReadBuffer != NULL)
    {
        readTask = AsyncReadIntoBuffer(&mReadBuffer, mStream);
    }
    else
    {
        mBufferLock.unlock();
        return;
    }
    mBufferLock.unlock();

    if (readTask.is_done())
    {
        try
        {
            // readTask.wait(); // .get waits already?

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
    // not well translated from C# to C++
    try
    {
        if (TryProcessRead(readTask.get()))
        {
            Process();
        }
    }
    catch (exception& ex)
    {
        Close(ex);
    }
}

bool AsyncStreamReader::TryProcessRead(unsigned read)
{
    // run the setOpened method and then clear it, atomically
    mProcessLock.lock();

    function<void()> mPreviousSetOpened = mSetOpened;
    mSetOpened = [](){};
    mPreviousSetOpened();

    mProcessLock.unlock();

    if (read > 0)
    {
        OnData(mReadBuffer);
        return true;
    }
    else if (read == 0)
    {
        Close();
    }
    
    return false;
}

void AsyncStreamReader::Close(exception& ex)
{
    State previousState = atomic_exchange<State>(&mReadingState, State::Stopped);

    if(previousState != State::Stopped)
    {
        if (Closed != NULL)
        {
            //unwrap exception if not null?

            Closed(ex);
        }

        mBufferLock.lock();
        {
            mReadBuffer = NULL;
        }
    }
}

void AsyncStreamReader::OnOpened()
{
    if (Opened != NULL)
    {
        Opened();
    }
}

void AsyncStreamReader::OnData(char buffer[])
{
    if (Data != NULL)
    {
        Data(buffer);
    }
}

pplx::task<unsigned int> AsyncStreamReader::AsyncReadIntoBuffer(char* buffer[], Concurrency::streams::basic_istream<uint8_t> stream)
{
    concurrency::streams::container_buffer<string> inStringBuffer;
    return stream.read(inStringBuffer, 4096).then([inStringBuffer, buffer](size_t bytesRead)
    {
        string &text = inStringBuffer.collection();
        *buffer = new char[text.length() + 1];
        strcpy(*buffer, text.c_str());

        return (unsigned int)bytesRead;
    });
}