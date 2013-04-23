// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlInstaller
    {
        private const int SchemaVersion = 1;
        private const string SchemaTableName = "Schema";

        private readonly string _connectionString;
        private readonly string _messagesTableNamePrefix;
        private readonly int _tableCount;
        private readonly TraceSource _trace;

        public SqlInstaller(string connectionString, string tableNamePrefix, int tableCount, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _messagesTableNamePrefix = tableNamePrefix;
            _tableCount = tableCount;
            _trace = traceSource;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query doesn't come from user code")]
        public void Install()
        {
            _trace.TraceInformation("Start installing SignalR SQL objects");

            if (!IsSqlEditionSupported(_connectionString))
            {
                throw new PlatformNotSupportedException(Resources.Error_UnsupportedSqlEdition);
            }

            var script = GetType().Assembly.StringResource("Microsoft.AspNet.SignalR.SqlServer.install.sql");

            script = script.Replace("SET @SCHEMA_NAME = 'SignalR';", "SET @SCHEMA_NAME = '" + SqlMessageBus.SchemaName + "';");
            script = script.Replace("SET @SCHEMA_TABLE_NAME = 'Schema';", "SET @SCHEMA_TABLE_NAME = '" + SchemaTableName + "';");
            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 1;", "SET @TARGET_SCHEMA_VERSION = " + SchemaVersion + ";");
            script = script.Replace("SET @MESSAGE_TABLE_COUNT = 1;", "SET @MESSAGE_TABLE_COUNT = " + _tableCount + ";");
            script = script.Replace("SET @MESSAGE_TABLE_NAME = 'Messages';", "SET @MESSAGE_TABLE_NAME = '" + _messagesTableNamePrefix + "';");

            var operation = new DbOperation(_connectionString, script, _trace);
            operation.ExecuteNonQuery();

            _trace.TraceInformation("SignalR SQL objects installed");
        }

        private bool IsSqlEditionSupported(string connectionString)
        {
            var operation = new DbOperation(connectionString, "SELECT SERVERPROPERTY ( 'EngineEdition' )", _trace);
            var edition = (int)operation.ExecuteScalar();

            return edition >= SqlEngineEdition.Standard && edition <= SqlEngineEdition.Express;
        }

        private static class SqlEngineEdition
        {
            // See article http://technet.microsoft.com/en-us/library/ms174396.aspx for details on EngineEdition
            public const int Personal = 1;
            public const int Standard = 2;
            public const int Enterprise = 3;
            public const int Express = 4;
            public const int SqlAzure = 5;
        }
    }
}
