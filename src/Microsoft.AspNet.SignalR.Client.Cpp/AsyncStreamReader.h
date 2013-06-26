#pragma once

#include <mutex>
#include <atomic>
#include "SseEvent.h"
#include <http_client.h>
#include "ExceptionHelper.h"

using namespace std;
using namespace pplx;
using namespace Concurrency;

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
    function<void()> Opened;
    function<void(exception& ex)> Closed;
    function<void(shared_ptr<char> buffer)> Data;
    void Start();
    task<void> Abort();

protected:
    mutex mBufferLock;

private:
    mutex mProcessLock;
    streams::basic_istream<uint8_t> mStream;
    shared_ptr<char> pReadBuffer;
    atomic<State> mReadingState;
    pplx::cancellation_token_source mReadCts;
    function<void()> SetOpened;
    pplx::task<void> mLastReadTask; 

    bool IsProcessing();
    void Close();
    void Close(exception &ex);
    void Process();
    void ReadAsync(pplx::task<unsigned int> readTask);
    bool TryProcessRead(unsigned read);
    void OnOpened();
    void OnData(shared_ptr<char> buffer);
    pplx::task<unsigned int> AsyncReadIntoBuffer(streams::basic_istream<uint8_t> stream);
};
