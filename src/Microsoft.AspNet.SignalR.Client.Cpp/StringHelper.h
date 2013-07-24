//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#pragma once

#include <basic_types.h>

namespace MicrosoftAspNetSignalRClientCpp
{
    // should this be a namespace instead?
    class StringHelper
    {
    public:
        StringHelper();
        ~StringHelper();

        // useful string operations that are missing in C++
        static bool BeginsWithIgnoreCase(utility::string_t &string1, utility::string_t &string2);
        static bool EndsWith(utility::string_t &string1, utility::string_t &string2);
        static utility::string_t Trim(utility::string_t string);
        static bool EqualsIgnoreCase(utility::string_t &string1, utility::string_t &string2);
    };
} // namespace MicrosoftAspNetSignalRClientCpp