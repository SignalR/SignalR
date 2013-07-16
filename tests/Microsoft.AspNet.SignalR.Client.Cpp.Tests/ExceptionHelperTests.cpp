//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "UnitTest++.h"
#include "ExceptionHelper.h"

using namespace MicrosoftAspNetSignalRClientCpp;

SUITE(ExceptionHelperTests)
{
    TEST(IsNullRecognizeExceptionNullClassOnly)
    {
        // Arrange
        exception ex("baseException");
        OperationCanceledException canceled("someOperation");
        ExceptionNone none("none");

        // Act
        bool isNull1 = ExceptionHelper::IsNull(ex);
        bool isNull2 = ExceptionHelper::IsNull(canceled);
        bool isNull3 = ExceptionHelper::IsNull(none);

        // Assert
        CHECK(!isNull1);
        CHECK(!isNull2);
        CHECK(isNull3);
    }

    TEST(IsRequestAbortedRecognizeOperationCanceledExceptionClassOnly)
    {
        // Arrange
        exception ex("baseException");
        OperationCanceledException canceled("someOperation");
        ExceptionNone none("none");

        // Act
        bool isCanceled1 = ExceptionHelper::IsRequestAborted(ex);
        bool isCanceled2 = ExceptionHelper::IsRequestAborted(canceled);
        bool isCanceled3 = ExceptionHelper::IsRequestAborted(none);

        // Assert
        CHECK(!isCanceled1);
        CHECK(isCanceled2);
        CHECK(!isCanceled3);
    }
}
