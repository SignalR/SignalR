using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.SqlServer
{
    internal class SqlSender
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly IJsonSerializer _json;

        private string _insertSql = "INSERT INTO {0} (Payload) VALUES (@Payload)";

        public SqlSender(string connectionString, string tableName, IJsonSerializer jsonSerializer)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _json = jsonSerializer;
            _insertSql = String.Format(_insertSql, _tableName);
        }

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
                var cmd = new SqlCommand(_insertSql, connection);
                cmd.Parameters.AddWithValue("Payload", _json.Stringify(messages));
                
                return cmd.ExecuteNonQueryAsync()
                    .Then(() => connection.Close()) // close the connection if successful
                    .Catch(ex => connection.Close()); // close the connection if it explodes
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
