#include "StringHelper.h"

namespace MicrosoftAspNetSignalRClientCpp
{

StringHelper::StringHelper()
{
}


StringHelper::~StringHelper()
{
}

// string2 must be shorter than or euqal to string1 in length
bool StringHelper::BeginsWithIgnoreCase(string_t &string1, string_t &string2)
{
    return _wcsnicmp(string1.c_str(), string2.c_str(), string2.length()) == 0;
}

// Currently only trims spaces
string_t StringHelper::Trim(string_t string)
{
    string.erase(0, string.find_first_not_of(' '));
    string.erase(string.find_last_not_of(' ') + 1);
    return string;
}

bool StringHelper::EqualsIgnoreCase(string_t &string1, string_t &string2)
{
    return _wcsicmp(string1.c_str(), string2.c_str()) == 0;
}

} // namespace MicrosoftAspNetSignalRClientCpp