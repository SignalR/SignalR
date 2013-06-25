#pragma once

#include "http_client.h"

using namespace utility;

// should this be a namespace instead?
class StringHelper
{
public:
    StringHelper();
    ~StringHelper();

    // useful string operations that are missing in C++
    static bool BeginsWithIgnoreCase(string_t &string1, string_t &string2);
    static string_t Trim(string_t string);
    static bool EqualsIgnoreCase(string_t &string1, string_t &string2);
};