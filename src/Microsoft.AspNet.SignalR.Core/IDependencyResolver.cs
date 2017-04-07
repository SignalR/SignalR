﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR
{
    public interface IDependencyResolver : IDisposable
    {
        object GetService(Type serviceType);
        IEnumerable<object> GetServices(Type serviceType);
        void Register(Type serviceType, Func<object> activator);
        void Register(Type serviceType, IEnumerable<Func<object>> activators);
    }
}
