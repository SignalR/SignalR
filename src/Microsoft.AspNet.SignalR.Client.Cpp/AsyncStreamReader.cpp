#include "AsyncStreamReader.h"

AsyncStreamReader::AsyncStreamReader(Concurrency::streams::basic_istream<uint8_t> stream)
{
    mStream = stream;
    mReadingState = State::Initial;
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
        mReadBuffer = shared_ptr<char>(new char[4096]);

        // Start the process loop
        Process();
    }
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
    readTask.then([readTask, this](unsigned int bytesRead)
    {
        // differentiate between faulted and canceled tasks?
        try
        {
            if (TryProcessRead(bytesRead))
            {
                Process();
            }
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
    mProcessLock.lock();

    function<void()> mPreviousSetOpened = mSetOpened;
    mSetOpened = [](){};

    mProcessLock.unlock();

    mPreviousSetOpened();

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


void AsyncStreamReader::Close()
{
    Close(exception(NULL));
}

void AsyncStreamReader::Close(exception& ex)
{
    State previousState = atomic_exchange<State>(&mReadingState, State::Stopped);

    if(previousState != State::Stopped)
    {
        if (Closed != NULL)
        {
            Closed(ex);
        }

        mBufferLock.lock();
            
        mReadBuffer.reset();

        mBufferLock.unlock();
    }
}

void AsyncStreamReader::OnOpened()
{
    if (Opened != NULL)
    {
        Opened();
    }
}

void AsyncStreamReader::OnData(shared_ptr<char> buffer)
{
    if (Data != NULL)
    {
        Data(buffer);
    }
}

task<unsigned int> AsyncStreamReader::AsyncReadIntoBuffer(shared_ptr<char>* buffer, Concurrency::streams::basic_istream<uint8_t> stream)
{
    concurrency::streams::container_buffer<string> inStringBuffer;
    return stream.read(inStringBuffer, 4096).then([inStringBuffer, buffer](size_t bytesRead)
    {
        string &text = inStringBuffer.collection();
        (*buffer) = shared_ptr<char>(new char[text.length() + 1]);
        int length = text.length();
        strcpy((*buffer).get(), text.c_str());

        return (unsigned int)bytesRead;
    });
}