#include "Connection.h"
#include "ConnectingMessageBuffer.h"

ConnectingMessageBuffer::ConnectingMessageBuffer()
{
}

ConnectingMessageBuffer::~ConnectingMessageBuffer()
{
}

void ConnectingMessageBuffer::Initialize(shared_ptr<Connection> connection, function<void(string_t)> drainCallback)
{
    pConnection = connection;
    DrainCallback = drainCallback;
}

bool ConnectingMessageBuffer::TryBuffer(string_t message)
{
    // Check if we need to buffer message
    if (pConnection->GetState() == ConnectionState::Connecting)
    {
        mBuffer.push(message);
        return true;
    }
    return false;
}

void ConnectingMessageBuffer::Drain()
{
    // Ensure that the connection is connected when we drain (do not want to drain while a connection is not active)          
    while (!mBuffer.empty() && pConnection->GetState() == ConnectionState::Connected)
    {
        DrainCallback(mBuffer.front());
        mBuffer.pop();
    }
}

void ConnectingMessageBuffer::Clear()
{
    queue<string_t> empty;
    swap(mBuffer, empty);

    //clean up
    pConnection = nullptr;
    DrainCallback = [](string_t message){};
}