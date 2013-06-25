#include "ExceptionHelper.h"

ExceptionHelper::ExceptionHelper()
{
}

ExceptionHelper::~ExceptionHelper()
{
}

bool ExceptionHelper::IsRequestAborted(exception& ex)
{
    return strcmp(typeid(ex).name(), "class OperationCanceledException") == 0;
}

bool ExceptionHelper::IsNull(exception& ex)
{
    return strcmp(typeid(ex).name(), "class ExceptionNone") == 0;
}