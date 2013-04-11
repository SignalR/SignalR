// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Data;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public interface IDbProviderFactory
    {
        IDbConnection CreateConnection();
        IDataParameter CreateParameter();
    }
}
