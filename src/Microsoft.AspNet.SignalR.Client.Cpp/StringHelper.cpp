#include "StringHelper.h"

StringHelper::StringHelper()
{
}


StringHelper::~StringHelper(void)
{
}

bool StringHelper::BeginsWithIgnoreCase(string_t string1, string_t string2)
{
    string1 = string1.substr(0, string2.length());
    transform(string1.begin(), string1.end(), string1.begin(), towupper);
    transform(string2.begin(), string2.end(), string2.begin(), towupper);
    return string1 == string2;
}

string_t StringHelper::Trim(string_t string)
{
    string.erase(0, string.find_first_not_of(' '));
    string.erase(string.find_last_not_of(' ') + 1);
    return string;
}

bool StringHelper::EqualsIgnoreCase(string_t string1, string_t string2)
{
    transform(string1.begin(), string1.end(), string1.begin(), towupper);
    transform(string2.begin(), string2.end(), string2.begin(), towupper);
    return string1 == string2;
}

string_t StringHelper::CleanString(string_t string)
{
    // strip off extra "" from the string
    return string.substr(1, string.length()-2);
}

string_t StringHelper::EncodeUri(string_t uri)
{
    // strip off extra "" from the string
    uri = CleanString(uri);
    return web::http::uri::encode_data_string(uri);
}
