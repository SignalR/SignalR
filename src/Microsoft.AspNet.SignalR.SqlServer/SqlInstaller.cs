// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlInstaller
    {
        private const int SchemaVersion = 1;
        private const string SchemaTableName = "[dbo].[SignalR_Schema]";
        private const string CheckSchemaTableExistsSql = "SELECT OBJECT_ID(@TableName)";
        private const string CheckSchemaTableVersionSql = "SELECT [SchemaVersion] FROM " + SchemaTableName;
        private const string CreateSchemaTableSql = "CREATE TABLE " + SchemaTableName + " ( [SchemaVersion] int NOT NULL PRIMARY KEY )";
        private const string InsertSchemaTableSql = "INSERT INTO " + SchemaTableName + " ([SchemaVersion]) VALUES (@SchemaVersion)";
        private const string UpdateSchemaTableSql = "UPDATE " + SchemaTableName + " SET [SchemaVersion] = @SchemaVersion";

        private readonly string _connectionString;
        private readonly string _messagesTableNamePrefix;
        private readonly int _tableCount;

        private string _exstingTablesSql = "SELECT [name] FROM [sys].[objects] WHERE [name] LIKE('{0}%')";
        private string _dropTableSql = "DROP TABLE {0}";
        private string _createMessagesTableSql = @"CREATE TABLE {0} (
                                                      [PayloadId] BIGINT NOT NULL PRIMARY KEY IDENTITY,
	                                                  [Payload] NVARCHAR(MAX) NOT NULL
                                                  )";
        private readonly Lazy<object> _initDummy;

        public SqlInstaller(string connectionString, string tableNamePrefix, int tableCount)
        {
            _connectionString = connectionString;
            _messagesTableNamePrefix = tableNamePrefix;
            _tableCount = tableCount;
            _exstingTablesSql = String.Format(CultureInfo.InvariantCulture, _exstingTablesSql, _messagesTableNamePrefix);
            _initDummy = new Lazy<object>(Install);
        }

        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "dummy", Justification = "Need dummy variable to call _initDummy.Value.")]
        public void EnsureInstalled()
        {
            var dummy = _initDummy.Value;
        }

        private object Install()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var schemaTableExists = false;
                var schemaRowExists = false;
                object objectId = null;

                using (var cmd = new SqlCommand(CheckSchemaTableExistsSql, connection))
                {
                    cmd.Parameters.AddWithValue("TableName", SchemaTableName);
                    objectId = cmd.ExecuteScalar();
                }

                if (objectId != null && objectId != DBNull.Value)
                {
                    // Schema table already exists, check schema version
                    schemaTableExists = true;
                    object schemaVersion = null;
                    using (var cmd = new SqlCommand(CheckSchemaTableVersionSql, connection))
                    {
                        schemaVersion = cmd.ExecuteScalar();
                    }

                    if (schemaVersion == null || schemaVersion == DBNull.Value || (int)schemaVersion < SchemaVersion)
                    {
                        // No schema row or older schema, just continue and we'll update it
                        schemaRowExists = !(schemaVersion == null || schemaVersion == DBNull.Value);
                    }
                    else if ((int)schemaVersion == SchemaVersion)
                    {
                        // Schema up to date!
                        // Ensure all messages tables are created
                        EnsureMessagesTables(connection);
                        return new object();
                    }
                    else if ((int)schemaVersion > SchemaVersion)
                    {
                        // Schema is newer than we expect, not good
                        throw new InvalidOperationException(Resources.Error_SignalRSQLScaleOutNewerThanCurrentVersion);
                    }

                }


                if (!schemaTableExists)
                {
                    // Create schema table
                    using (var cmd = new SqlCommand(CreateSchemaTableSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // Ensure all messages tables are created
                EnsureMessagesTables(connection);

                // Update or Insert the schema row
                using (var cmd = new SqlCommand(schemaRowExists ? UpdateSchemaTableSql : InsertSchemaTableSql, connection))
                {
                    cmd.Parameters.AddWithValue("SchemaVersion", SchemaVersion);
                    cmd.ExecuteNonQuery();
                }
            }

            return new object();
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        private void EnsureMessagesTables(SqlConnection connection)
        {
            var existingTables = new List<string>();

            using (var cmd = new SqlCommand(_exstingTablesSql, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingTables.Add(reader.GetString(0));
                }
            }

            if (existingTables.Count == _tableCount)
            {
                // We have the right number of tables
                return;
            }

            // Drop the existing tables
            foreach (var table in existingTables)
            {
                var dropTableSql = String.Format(CultureInfo.CurrentCulture, _dropTableSql, table);
                using (var cmd = new SqlCommand(dropTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // Create messages tables
            for (var i = 0; i < _tableCount; i++)
            {
                var createTableSql = _tableCount == 1
                    ? String.Format(CultureInfo.CurrentCulture, _createMessagesTableSql, _messagesTableNamePrefix)
                    : String.Format(CultureInfo.CurrentCulture, _createMessagesTableSql, _messagesTableNamePrefix + "_" + i.ToString(CultureInfo.InvariantCulture));
                using (var cmd = new SqlCommand(createTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
