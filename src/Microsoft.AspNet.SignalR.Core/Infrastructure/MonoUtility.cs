// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class MonoUtility
    {
        private static readonly Lazy<bool> _isRunningMono = new Lazy<bool>(() => CheckRunningOnMono());

        internal static bool IsRunningMono
        {
            get
            {
                return _isRunningMono.Value;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This should never fail")]
        private static bool CheckRunningOnMono()
        {
            try
            {
                return Type.GetType("Mono.Runtime") != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
