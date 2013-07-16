//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "Connection.h"
#include "ConnectingMessageBuffer.h"

namespace MicrosoftAspNetSignalRClientCpp
{

ConnectingMessageBuffer::ConnectingMessageBuffer()
{
}

ConnectingMessageBuffer::~ConnectingMessageBuffer()
{
}

void ConnectingMessageBuffer::Initialize(shared_ptr<Connection> connection, function<void(string_t)> drainCallback)
{
    pConnection = connection;

    {
        lock_guard<mutex> lock(mDrainCallbackLock);
        DrainCallback = drainCallback;
    }
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
        {
            lock_guard<mutex> lock(mDrainCallbackLock);
            DrainCallback(mBuffer.front());
        }
        mBuffer.pop();
    }
}

void ConnectingMessageBuffer::Clear()
{
    queue<string_t> empty;
    swap(mBuffer, empty);

    //clean up
    pConnection = nullptr;
    {
        lock_guard<mutex> lock(mDrainCallbackLock);
        DrainCallback = [](string_t message){};
    }
}

} // namespace MicrosoftAspNetSignalRClientCpp