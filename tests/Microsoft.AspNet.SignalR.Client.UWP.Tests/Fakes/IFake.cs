﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal interface IFake
    {
        void Setup<T>(string methodName, Func<T> behavior);
        void Verify(string methodName, List<object[]> expectedParameters);
        IEnumerable<object[]> GetInvocations(string methodName);
    }
}
