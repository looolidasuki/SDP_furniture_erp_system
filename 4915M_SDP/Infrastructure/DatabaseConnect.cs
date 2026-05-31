using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

// System.Configuration is available via System.Configuration.dll reference
namespace Sales_user.Controllers
{
    public static class DatabaseConnect
    {
        private static string _connectionString;

        public static string ConnectionString => GetConnectionString();

        private static string GetConnectionString()
        {
            if (_connectionString != null) return _connectionString;
            try
            {
                _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            }
            catch { }
            if (string.IsNullOrEmpty(_connectionString))
                _connectionString = "Server=localhost;Port=3306;Database=furniture_erp_system;Uid=root;Pwd=;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=Disabled;";
            return _connectionString;
        }

        public static DataTable ExecuteQuery(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    var dt = new DataTable();
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static long ExecuteInsertReturnId(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                    return cmd.LastInsertedId;
                }
            }
        }

        public static int ExecuteNonQuery(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static T ExecuteInTransaction<T>(Func<MySqlConnection, MySqlTransaction, T> action)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        T result = action(conn, trans);
                        trans.Commit();
                        return result;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public static int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction trans, string sql, MySqlParameter[] parameters = null)
        {
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static long ExecuteInsertReturnId(MySqlConnection conn, MySqlTransaction trans, string sql, MySqlParameter[] parameters = null)
        {
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        public static object ExecuteScalar(MySqlConnection conn, MySqlTransaction trans, string sql, MySqlParameter[] parameters = null)
        {
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static DataTable ExecuteQuery(MySqlConnection conn, MySqlTransaction trans, string sql, MySqlParameter[] parameters = null)
        {
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                var dt = new DataTable();
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(dt);
                return dt;
            }
        }
    }
}