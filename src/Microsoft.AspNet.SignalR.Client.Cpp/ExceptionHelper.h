#pragma once

#include <string>
#include <exception>
#include <typeinfo>

using namespace std;

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
