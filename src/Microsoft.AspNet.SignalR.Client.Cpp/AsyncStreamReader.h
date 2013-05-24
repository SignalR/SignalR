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
    function<void()> Opened;
    function<void(exception& ex)> Closed;
    function<void(char buffer[])> Data;
    void Start();

protected:
        mutex mBufferLock;

private:
    mutex mProcessLock;
    Concurrency::streams::basic_istream<uint8_t> mStream;
    char* mReadBuffer;  // char []
    atomic<State> mReadingState;

    function<void()> mSetOpened;
    bool IsProcessing();
    void Close();
    void Close(exception &ex);
    void Process();
    void ReadAsync(pplx::task<unsigned int> readTask);
    bool TryProcessRead(unsigned read);
    void OnOpened();
    void OnData(char buffer[]);
    pplx::task<unsigned int> AsyncReadIntoBuffer(char* buffer[], Concurrency::streams::basic_istream<uint8_t> stream);
};
