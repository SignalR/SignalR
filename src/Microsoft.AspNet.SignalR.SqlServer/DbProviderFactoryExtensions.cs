// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Common;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal static class DbProviderFactoryExtensions
    {
        public static IDbProviderFactory AsIDbProviderFactory(this DbProviderFactory dbProviderFactory)
        {
            return new DbProviderFactoryAdapter(dbProviderFactory);
        }
    }
}
