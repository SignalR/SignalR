// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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

        private readonly Lazy<object> _initDummy;

        public SqlInstaller(string connectionString, string tableNamePrefix, int tableCount, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _messagesTableNamePrefix = tableNamePrefix;
            _tableCount = tableCount;
            _initDummy = new Lazy<object>(Install);
            _trace = traceSource;
        }

        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "dummy", Justification = "Need dummy variable to call _initDummy.Value.")]
        public void EnsureInstalled()
        {
            var dummy = _initDummy.Value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification="Query doesn't come from user code")]
        private object Install()
        {
            _trace.TraceInformation("Start installing SignalR SQL objects");

            string script;
            using (var resourceStream = typeof(SqlInstaller).Assembly.GetManifestResourceStream("Microsoft.AspNet.SignalR.SqlServer.install.sql"))
            {
                var reader = new StreamReader(resourceStream);
                script = reader.ReadToEnd();
            }

            script = script.Replace("SET @SCHEMA_NAME = 'SignalR';", "SET @SCHEMA_NAME = '" + SqlMessageBus.SchemaName + "';");
            script = script.Replace("SET @SCHEMA_TABLE_NAME = 'Schema';", "SET @SCHEMA_TABLE_NAME = '" + SchemaTableName + "';");
            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 1;", "SET @TARGET_SCHEMA_VERSION = " + SchemaVersion + ";");
            script = script.Replace("SET @MESSAGE_TABLE_COUNT = 3;", "SET @MESSAGE_TABLE_COUNT = " + _tableCount + ";");
            script = script.Replace("SET @MESSAGE_TABLE_NAME = 'Messages';", "SET @MESSAGE_TABLE_NAME = '" + _messagesTableNamePrefix + "';");

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = script;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            _trace.TraceInformation("SignalR SQL objects installed");

            return new object();
        }
    }
}
