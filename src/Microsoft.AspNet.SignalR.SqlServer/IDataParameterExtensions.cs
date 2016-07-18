// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data;
using System.Data.Common;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal static class IDataParameterExtensions
    {
        public static IDataParameter Clone(this IDataParameter sourceParameter, IDbProviderFactory dbProviderFactory)
        {
            var newParameter = dbProviderFactory.CreateParameter();

            newParameter.ParameterName = sourceParameter.ParameterName;
            newParameter.DbType = sourceParameter.DbType;
            newParameter.Value = sourceParameter.Value;
            newParameter.Direction = sourceParameter.Direction;

            return newParameter;
        }
    }
}
