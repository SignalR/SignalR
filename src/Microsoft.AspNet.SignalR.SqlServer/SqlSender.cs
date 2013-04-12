﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlSender
    {
        private readonly string _connectionString;
        private readonly ReadOnlyCollection<string> _insertSql;
        private readonly TraceSource _trace;

        public SqlSender(string connectionString, string tablePrefix, int tableCount, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _insertSql = BuildInsertStrings(tablePrefix, tableCount);
            _trace = traceSource;
        }

        private ReadOnlyCollection<string> BuildInsertStrings(string tablePrefix, int tableCount)
        {
            var insertDml = GetType().Assembly.StringResource("Microsoft.AspNet.SignalR.SqlServer.send.sql");
            
            return new ReadOnlyCollection<string>(
                Enumerable.Range(1, tableCount)
                    .Select(tableNumber =>
                        insertDml.Replace("[SignalR]", String.Format(CultureInfo.InvariantCulture, "[{0}]", SqlMessageBus.SchemaName))
                                 .Replace("[Messages_1", String.Format(CultureInfo.InvariantCulture, "[{0}_{1}", tablePrefix, tableNumber))
                    )
                    .ToList()
            );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Reviewed"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        public Task Send(int streamIndex, IList<Message> messages)
        {
            if (streamIndex > _insertSql.Count - 1)
            {
                throw new ArgumentOutOfRangeException("streamIndex", String.Format(CultureInfo.InvariantCulture, Resources.Error_StreamIndexOutOfRange, streamIndex, _insertSql.Count - 1));
            }

            if (messages == null || messages.Count == 0)
            {
                return TaskAsyncHelper.Empty;
            }

            SqlConnection connection = null;
            SqlCommand cmd = null;
            try
            {
                var sql = _insertSql[streamIndex];
                connection = new SqlConnection(_connectionString);
                cmd = new SqlCommand(sql, connection);
                cmd.Parameters.Add(new SqlParameter("Payload", SqlDbType.VarBinary)
                {
                    SqlValue = new SqlBinary(SqlPayload.ToBytes(messages))
                });

                _trace.TraceVerbose("Saving payload of {0} messages(s) to stream {1} in SQL server", messages.Count, streamIndex);

                connection.Open();
                return cmd.ExecuteNonQueryAsync()
                          .Then(c => c.Dispose(), connection) // close the connection if successful
                          .Catch((_, c) => ((SqlConnection)c).Dispose(), connection); // close the connection if it explodes
            }
            catch (SqlException)
            {
                // TODO: Call into base to start buffering here and kick off BG thread
                //       to start pinging SQL server to detect when it comes back online

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
