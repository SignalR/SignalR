#pragma once

#include "AsyncStreamReader.h"

using namespace std;

class EventSourceStreamReader :
    public AsyncStreamReader
{
public:
    EventSourceStreamReader(void);
    ~EventSourceStreamReader(void);
};