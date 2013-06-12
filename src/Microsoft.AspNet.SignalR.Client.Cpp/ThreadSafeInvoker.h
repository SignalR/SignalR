#pragma once

#include <http_client.h>
#include <atomic>

using namespace std;
using namespace pplx;


class ThreadSafeInvoker
{
public:
    ThreadSafeInvoker();
    ~ThreadSafeInvoker();

    bool Invoke(function<void()> function);
    template <typename T>
    bool Invoke(function<void(T)> function, T arg);
    template <typename T1, typename T2>
    bool Invoke(function<void(T1, T2)> function, T1 arg1, T2 arg2);
    bool Invoke();

private:
    atomic<bool> mInvoked;
};