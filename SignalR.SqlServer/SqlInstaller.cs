using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading;

namespace SignalR.SqlServer
{
    internal class SqlInstaller
    {
        private const string CheckSql = "SELECT OBJECT_ID(@TableName)";
        private readonly string _connectionString;
        private readonly string _tableName;
        private object _initDummy = null;

        public SqlInstaller(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }

        public void EnsureInstalled()
        {
            LazyInitializer.EnsureInitialized(ref _initDummy, Install);
        }

        private object Install()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(CheckSql, connection))
                {
                    cmd.Parameters.AddWithValue("TableName", _tableName);
                    var objectId = cmd.ExecuteScalar();
                    if (objectId != DBNull.Value)
                    {
                        // Table already exists
                        return new object();
                    }
                }

                string sql = null;
                using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SignalR.SqlServer.install.sql")))
                {
                    sql = reader.ReadToEnd();
                }

                sql = String.Format(sql, _tableName);

                connection.Open();
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }

            return new object();
        }
    }
}
