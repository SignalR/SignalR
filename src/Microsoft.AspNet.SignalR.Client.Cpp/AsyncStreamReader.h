#pragma once

#include <mutex>
#include <atomic>
#include "SseEvent.h"
#include <http_client.h>

using namespace std;

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
    mutex GetBufferLock();
    function<void()> Opened;
    function<void()> Closed;
    //function<ArraySegment<byte>> Data;
    void Start();

private:
    mutex mBufferLock;
    Concurrency::streams::basic_istream<uint8_t> mStream;
    char* mReadBuffer;  // char []
    atomic<State> mReadingState;

    function<void()> mSetOpened;
    bool IsProcessing();
    void Close();
    void Close(exception &ex);
    void Process();
    void ReadAsync(pplx::task<long> readTask);
    bool TryProcessRead(long read);
    void OnOpened();
    //void OnData(ArraySegment<byte> buffer);
};
