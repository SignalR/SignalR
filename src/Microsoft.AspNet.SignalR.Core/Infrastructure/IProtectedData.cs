// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public interface IProtectedData
    {
        string Protect(string data, string purpose);
        string Unprotect(string protectedValue, string purpose);
    }
}
