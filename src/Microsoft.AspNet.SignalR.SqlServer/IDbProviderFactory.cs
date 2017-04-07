// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public interface IDbProviderFactory
    {
        IDbConnection CreateConnection();
        IDataParameter CreateParameter();
    }
}
