#include "EventSourceStreamReader.h"


EventSourceStreamReader::EventSourceStreamReader(Connection* connection, Concurrency::streams::basic_istream<uint8_t> stream)
    : AsyncStreamReader(stream)
{

}


EventSourceStreamReader::~EventSourceStreamReader(void)
{
}
