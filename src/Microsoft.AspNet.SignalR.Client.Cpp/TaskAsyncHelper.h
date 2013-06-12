#pragma once

#include <ppltasks.h>
#include <agents.h>
#include "http_client.h"
#include "pplxconv.h"

using namespace std;
using namespace utility;
using namespace concurrency;

class TaskAsyncHelper
{
public:
    TaskAsyncHelper();
    ~TaskAsyncHelper();

    static pplx::task<void> Delay(seconds seconds);
};