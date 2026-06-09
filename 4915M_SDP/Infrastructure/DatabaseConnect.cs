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
                _connectionString = "Server=localhost;Port=3306;Database=furniture_erp_system;Uid=root;Pwd=;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=Disabled;Convert Zero Datetime=True;";
            if (_connectionString.IndexOf("Convert Zero Datetime", StringComparison.OrdinalIgnoreCase) < 0)
                _connectionString += ";Convert Zero Datetime=True;";
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

        public static object ExecuteScalar(MySqlConnection conn, string sql, MySqlParameter[] parameters = null)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Allocates next numeric primary key when the table has no AUTO_INCREMENT (or as a safe fallback).
        /// </summary>
        public static long AllocateNextId(string tableName, string idColumnName, MySqlConnection conn, MySqlTransaction trans, params long[] reservedIds)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(idColumnName))
                return 0;

            string sql = $"SELECT COALESCE(MAX(`{idColumnName}`), 0) + 1 FROM `{tableName}`";
            object scalar = trans != null
                ? ExecuteScalar(conn, trans, sql)
                : ExecuteScalar(conn, sql);
            long id = scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);

            if (reservedIds != null)
            {
                foreach (long reserved in reservedIds)
                {
                    if (reserved > 0 && id == reserved)
                        id++;
                }
            }

            return id;
        }

        public static long AllocateNextId(string tableName, string idColumnName, params long[] reservedIds)
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                return AllocateNextId(tableName, idColumnName, conn, null, reservedIds);
            }
        }

        /// <summary>
        /// INSERT that always supplies @id using MAX(id)+1. SQL must include the id column and @id placeholder.
        /// </summary>
        public static long InsertWithAllocatedId(
            MySqlConnection conn,
            MySqlTransaction trans,
            string tableName,
            string idColumnName,
            string insertSql,
            MySqlParameter[] parameters,
            params long[] reservedIds)
        {
            long id = AllocateNextId(tableName, idColumnName, conn, trans, reservedIds);
            if (id <= 0)
                throw new InvalidOperationException($"Unable to allocate {tableName}.{idColumnName}.");

            var paramList = new System.Collections.Generic.List<MySqlParameter>();
            if (parameters != null)
                paramList.AddRange(parameters);
            paramList.Add(new MySqlParameter("@id", id));
            ExecuteNonQuery(conn, trans, insertSql, paramList.ToArray());
            return id;
        }

        public static long InsertWithAllocatedId(
            string tableName,
            string idColumnName,
            string insertSql,
            MySqlParameter[] parameters,
            params long[] reservedIds)
        {
            return ExecuteInTransaction((conn, trans) =>
                InsertWithAllocatedId(conn, trans, tableName, idColumnName, insertSql, parameters, reservedIds));
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