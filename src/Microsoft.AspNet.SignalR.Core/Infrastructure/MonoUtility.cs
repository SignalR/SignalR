// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
