#include "ExceptionHelper.h"

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