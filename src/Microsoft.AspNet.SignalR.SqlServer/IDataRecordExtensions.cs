// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal static class IDataRecordExtensions
    {
        public static byte[] GetBinary(this IDataRecord reader, int ordinalIndex)
        {
            var sqlReader = reader as SqlDataReader;
            if (sqlReader == null)
            {
                throw new NotSupportedException();
            }

            return sqlReader.GetSqlBinary(ordinalIndex).Value;
        }
    }
}
