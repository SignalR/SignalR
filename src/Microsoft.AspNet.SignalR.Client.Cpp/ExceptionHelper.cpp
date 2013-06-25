#include "ExceptionHelper.h"

ExceptionHelper::ExceptionHelper()
{
}

ExceptionHelper::~ExceptionHelper()
{
}

bool ExceptionHelper::IsRequestAborted(exception& ex)
{
    return strcmp(typeid(ex).name(), typeid(OperationCanceledException).name()) == 0;
}

bool ExceptionHelper::IsNull(exception& ex)
{
    return strcmp(typeid(ex).name(), typeid(ExceptionNone).name()) == 0;
}