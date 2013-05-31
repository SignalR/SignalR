#include "IClientTransport.h"


IClientTransport::IClientTransport(void)
{
}


IClientTransport::~IClientTransport(void)
{
}

string_t IClientTransport::GetTransportName()
{
    return mTransportName;
}