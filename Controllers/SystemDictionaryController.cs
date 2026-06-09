using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sales_user.Controllers
{
    public class SystemDictionaryController
    {
        // 獲取所有數據字典配置（用於 DataGridView 顯示）
        public DataTable GetAllDictionaries()
        {
            string sql = "SELECT category AS 'Category', codeValue AS 'CodeValue', " +
                         "displayNameEnglish AS 'DisplayNameEnglish', sortOrder AS 'SortOrder' " +
                         "FROM SystemDictionary " +
                         "ORDER BY category, sortOrder";

            return DatabaseConnect.ExecuteQuery(sql);
        }

        // 💡 核心方法：根據類別（如 DEPARTMENT）獲取對應的鍵值對，用於動態綁定 ComboBox
        public DataTable GetByCategory(string category)
        {
            string sql = "SELECT codeValue, displayNameEnglish " +
                         "FROM SystemDictionary " +
                         "WHERE category = @category " +
                         "ORDER BY sortOrder";

            MySqlParameter[] parameters = {
                new MySqlParameter("@category", category)
            };

            return DatabaseConnect.ExecuteQuery(sql, parameters);
        }

        public long Insert(Sales_user.Models.SystemDictionary entry)
        {
            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                long nextId = DatabaseConnect.AllocateNextId("systemdictionary", "dictionaryID", conn, trans);
                if (nextId <= 0)
                    throw new InvalidOperationException("Unable to allocate dictionary ID.");

                string sql = @"INSERT INTO SystemDictionary (dictionaryID, category, codeValue, displayNameEnglish, sortOrder)
                               VALUES (@id, @category, @codeValue, @displayName, @sortOrder)";
                DatabaseConnect.ExecuteNonQuery(conn, trans, sql, new[] {
                    new MySqlParameter("@id", nextId),
                    new MySqlParameter("@category", entry.Category),
                    new MySqlParameter("@codeValue", entry.CodeValue),
                    new MySqlParameter("@displayName", entry.DisplayNameEnglish ?? (object)System.DBNull.Value),
                    new MySqlParameter("@sortOrder", entry.SortOrder)
                });
                return nextId;
            });
        }

        public bool Update(Sales_user.Models.SystemDictionary entry, string originalCategory, int originalCodeValue)
        {
            string sql = @"UPDATE SystemDictionary
                           SET category=@category, codeValue=@codeValue, displayNameEnglish=@displayName, sortOrder=@sortOrder
                           WHERE category=@originalCategory AND codeValue=@originalCodeValue";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@category", entry.Category),
                new MySqlParameter("@codeValue", entry.CodeValue),
                new MySqlParameter("@displayName", entry.DisplayNameEnglish ?? (object)System.DBNull.Value),
                new MySqlParameter("@sortOrder", entry.SortOrder),
                new MySqlParameter("@originalCategory", originalCategory),
                new MySqlParameter("@originalCodeValue", originalCodeValue)
            }) > 0;
        }
    }
}
