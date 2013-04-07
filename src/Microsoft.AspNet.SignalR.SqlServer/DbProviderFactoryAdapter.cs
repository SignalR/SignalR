// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Data;
using System.Data.Common;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class DbProviderFactoryAdapter : IDbProviderFactory
    {
        private readonly DbProviderFactory _dbProviderFactory;

        public DbProviderFactoryAdapter(DbProviderFactory dbProviderFactory)
        {
            _dbProviderFactory = dbProviderFactory;
        }

        public IDbConnection CreateConnection()
        {
            return _dbProviderFactory.CreateConnection();
        }

        public IDataParameter CreateParameter()
        {
            return _dbProviderFactory.CreateParameter();
        }
    }
}
