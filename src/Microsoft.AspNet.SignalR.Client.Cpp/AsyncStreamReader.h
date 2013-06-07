#pragma once

#include <mutex>
#include <atomic>
#include "SseEvent.h"
#include <http_client.h>

using namespace std;
using namespace pplx;

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
    function<void()> Opened;
    function<void(exception& ex)> Closed;
    function<void(shared_ptr<char> buffer)> Data;
    void Start();

protected:
    mutex mBufferLock;

private:
    mutex mProcessLock;
    Concurrency::streams::basic_istream<uint8_t> mStream;
    shared_ptr<char> mReadBuffer;
    atomic<State> mReadingState;
    function<void()> mSetOpened;

    bool IsProcessing();
    void Close();
    void Close(exception &ex);
    void Process();
    void ReadAsync(pplx::task<unsigned int> readTask);
    bool TryProcessRead(unsigned read);
    void OnOpened();
    void OnData(shared_ptr<char> buffer);
    task<unsigned int> AsyncReadIntoBuffer(shared_ptr<char>* buffer, Concurrency::streams::basic_istream<uint8_t> stream);
};
