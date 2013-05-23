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
            //OnOpened();
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
    pplx::task<long> readTask;
    
    mBufferLock.lock();
    if(IsProcessing() && mReadBuffer != NULL)
    {

        readTask = mStream.read();
        //mStream.ReadAsync(mReadBuffer);
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

            //if (TryProcessRead(read))
            //{
            //    goto READ;
            //}
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

void AsyncStreamReader::ReadAsync(pplx::task<long> readTask)
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

bool AsyncStreamReader::TryProcessRead(long read)
{
    return false;
}

void AsyncStreamReader::Close(exception& ex)
{

}

void AsyncStreamReader::OnOpened()
{
    if (Opened != NULL)
    {
        Opened();
    }
}

//void AsyncStreamReader::OnData(ArraySegment<byte> buffer)
//{
//    if (Data != null)
//    {
//        Data(buffer);
//    }
//}