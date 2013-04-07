// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
