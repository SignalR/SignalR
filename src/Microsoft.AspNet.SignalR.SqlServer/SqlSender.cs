// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlSender
    {
        private readonly string _connectionString;
        private readonly string _tableName;

        private string _insertSql = "INSERT INTO {0} (Payload) VALUES (@Payload)";

        public SqlSender(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _insertSql = String.Format(CultureInfo.CurrentCulture, _insertSql, _tableName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        public Task Send(Message[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return TaskAsyncHelper.Empty;
            }

            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(_connectionString);
                connection.Open();
                using (var cmd = new SqlCommand(_insertSql, connection))
                {
                    cmd.Parameters.AddWithValue("Payload", JsonConvert.SerializeObject(messages));

                    return cmd.ExecuteNonQueryAsync()
                        .Then(() => connection.Close()) // close the connection if successful
                        .Catch(ex => connection.Close()); // close the connection if it explodes
                }
            }
            catch (SqlException)
            {
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
                throw;
            }
        }
    }
}
