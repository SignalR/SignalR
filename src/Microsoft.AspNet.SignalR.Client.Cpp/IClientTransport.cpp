//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "IClientTransport.h"

namespace MicrosoftAspNetSignalRClientCpp
{

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

bool IClientTransport::SupportsKeepAlive()
{
    return mSupportKeepAlive;
}

} // namespace MicrosoftAspNetSignalRClientCpp