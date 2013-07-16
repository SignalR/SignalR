//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "ExceptionHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

ExceptionHelper::ExceptionHelper()
{
}

ExceptionHelper::~ExceptionHelper()
{
}

bool ExceptionHelper::IsRequestAborted(exception& ex)
{
    return typeid(ex) == typeid(OperationCanceledException);
}

bool ExceptionHelper::IsNull(exception& ex)
{
    return typeid(ex) == typeid(ExceptionNone);
}

} // namespace MicrosoftAspNetSignalRClientCpp