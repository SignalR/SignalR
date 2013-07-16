//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <string>
#include <exception>
#include <typeinfo>

using namespace std;

namespace MicrosoftAspNetSignalRClientCpp
{
    class OperationCanceledException : public exception
    {
    public:
        OperationCanceledException(const char *const& s) : exception(s){};
    };

    class ExceptionNone : public exception
    {
    public:
        ExceptionNone(const char *const& s) : exception(s){};
    };

    class ExceptionHelper
    {
    public:
        ExceptionHelper();
        ~ExceptionHelper();

        static bool IsRequestAborted(exception& ex);
        static bool IsNull(exception& ex);
    };
} // namespace MicrosoftAspNetSignalRClientCpp