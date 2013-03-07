// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlSender
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly TraceSource _trace;

        // TODO: Investigate SQL locking options
        private string _insertSql = "INSERT INTO [" + SqlMessageBus.SchemaName + "].[{0}] (Payload, InsertedOn) VALUES (@Payload, GETDATE())";

        public SqlSender(string connectionString, string tableName, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _tableName = tableName + "_1";
            _insertSql = String.Format(CultureInfo.CurrentCulture, _insertSql, _tableName);
            _trace = traceSource;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification="Reviewed"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        public Task Send(IList<Message> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return TaskAsyncHelper.Empty;
            }

            SqlConnection connection = null;
            SqlCommand cmd = null;
            try
            {
                connection = new SqlConnection(_connectionString);
                cmd = new SqlCommand(_insertSql, connection);
                var payload = cmd.Parameters.Add("Payload", SqlDbType.VarBinary);
                payload.SqlValue = new SqlBinary(SqlPayload.ToBytes(messages));

                _trace.TraceVerbose("Saving payload of {0} messages(s) to SQL server", messages.Count);

                connection.Open();
                return cmd.ExecuteNonQueryAsync()
                    .Then(c => c.Dispose(), connection) // close the connection if successful
                    .Catch(_ => connection.Dispose()); // close the connection if it explodes
            }
            catch (SqlException)
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Dispose();
                }
                throw;
            }
        }
    }
}
