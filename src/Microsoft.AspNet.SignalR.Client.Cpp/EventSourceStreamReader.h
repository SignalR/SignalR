#pragma once

#include "AsyncStreamReader.h"
#include "Connection.h"
#include "ChunkBuffer.h"

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    EventSourceStreamReader(Connection* connection, Concurrency::streams::basic_istream<uint8_t> stream);
    ~EventSourceStreamReader(void);

private:
    Connection* mConnection;
};