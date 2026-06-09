using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Data;
using FurnitureERP.Helpers;

namespace Sales_user.Controllers
{
    public class StaffController
    {
        public bool EnsureDiagnosticAccount()
        {
            const string diagnosticEmail = "diagnostic@erp.local";

            string existsSql = "SELECT COUNT(1) FROM Staff WHERE email = @email";
            object existsObj = DatabaseConnect.ExecuteScalar(
                existsSql,
                new[] { new MySqlParameter("@email", diagnosticEmail) });

            long existsCount = Convert.ToInt64(existsObj == DBNull.Value ? 0 : existsObj);
            if (existsCount > 0)
                return false;

            var diagnosticStaff = new Staff
            {
                Username = "diagnostic",
                Password = "Test@123",
                Title = "QA Tester",
                Department = "System",
                FirstName = "Diagnostic",
                LastName = "User",
                EmployDate = DateTime.Today,
                Phone = "00000000",
                Email = diagnosticEmail,
                Status = 1
            };

            Insert(diagnosticStaff);
            return true;
        }

        public Staff Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrEmpty(password))
                return null;

            string sql = @"SELECT staffID, username, password, firstName, lastName, title, department, email, status
                           FROM Staff
                           WHERE (username = @login OR email = @login)
                             AND (status IS NULL OR status = 1)";

            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@login", usernameOrEmail.Trim())
            });

            if (dt == null || dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            string storedPassword = row["password"]?.ToString() ?? string.Empty;
            if (!VerifyPassword(password, storedPassword))
                return null;

            long staffId = Convert.ToInt64(row["staffID"]);
            if (!PasswordHasher.IsHashed(storedPassword))
                UpgradePasswordHash(staffId, password);

            return MapStaff(row);
        }

        public DataTable GetAllStaff()
        {
            string sql = @"SELECT staffID AS 'Staff ID',
                                  username AS 'Username',
                                  CONCAT(firstName, ' ', lastName) AS 'Name',
                                  title AS 'Title',
                                  department AS 'Department',
                                  email AS 'Email',
                                  phone AS 'Phone',
                                  IF(employDate IS NULL OR employDate = '0000-00-00', NULL, employDate) AS 'Employ Date',
                                  status AS 'Status'
                           FROM Staff
                           ORDER BY IF(employDate IS NULL OR employDate = '0000-00-00', '1900-01-01', employDate) DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public Staff GetById(long staffId)
        {
            string sql = @"SELECT staffID, username, firstName, lastName, title, department, email, phone,
                                  IF(employDate IS NULL OR employDate = '0000-00-00', NULL, employDate) AS employDate,
                                  status
                           FROM Staff WHERE staffID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", staffId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            return MapStaff(dt.Rows[0]);
        }

        public long Insert(Staff staff)
        {
            string sql = @"INSERT INTO Staff
                (staffID, username, password, title, department, firstName, lastName, employDate, phone, email, status)
                VALUES (@id, @user, @pass, @title, @dept, @first, @last, @employDate, @phone, @email, @status)";
            return DatabaseConnect.InsertWithAllocatedId("staff", "staffID", sql, new[] {
                new MySqlParameter("@user", staff.Username),
                new MySqlParameter("@pass", HashPassword(staff.Password)),
                new MySqlParameter("@title", staff.Title),
                new MySqlParameter("@dept", staff.Department),
                new MySqlParameter("@first", staff.FirstName),
                new MySqlParameter("@last", staff.LastName),
                new MySqlParameter("@employDate", staff.EmployDate),
                new MySqlParameter("@phone", staff.Phone),
                new MySqlParameter("@email", staff.Email),
                new MySqlParameter("@status", staff.Status ?? 1)
            });
        }

        public bool Update(Staff staff)
        {
            string sql = @"UPDATE Staff
                           SET username=@user, title=@title, department=@dept,
                               firstName=@first, lastName=@last, phone=@phone, email=@email, status=@status
                           WHERE staffID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@user", staff.Username),
                new MySqlParameter("@title", staff.Title),
                new MySqlParameter("@dept", staff.Department),
                new MySqlParameter("@first", staff.FirstName),
                new MySqlParameter("@last", staff.LastName),
                new MySqlParameter("@phone", staff.Phone),
                new MySqlParameter("@email", staff.Email),
                new MySqlParameter("@status", staff.Status ?? 1),
                new MySqlParameter("@id", staff.StaffID)
            }) > 0;
        }

        public bool ResetPassword(long staffId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;

            return DatabaseConnect.ExecuteNonQuery(
                "UPDATE Staff SET password = @pass WHERE staffID = @id",
                new[]
                {
                    new MySqlParameter("@pass", HashPassword(newPassword)),
                    new MySqlParameter("@id", staffId)
                }) > 0;
        }

        public bool ChangePassword(long staffId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            string sql = "SELECT password FROM Staff WHERE staffID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", staffId) });
            if (dt == null || dt.Rows.Count == 0)
                return false;

            string storedPassword = dt.Rows[0]["password"]?.ToString() ?? string.Empty;
            if (!VerifyPassword(currentPassword, storedPassword))
                return false;

            return ResetPassword(staffId, newPassword);
        }

        public bool SetStatus(long staffId, int status)
        {
            return DatabaseConnect.ExecuteNonQuery(
                "UPDATE Staff SET status = @status WHERE staffID = @id",
                new[]
                {
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@id", staffId)
                }) > 0;
        }

        private static Staff MapStaff(DataRow row)
        {
            return new Staff
            {
                StaffID = Convert.ToInt64(row["staffID"]),
                Username = row["username"].ToString(),
                FirstName = row["firstName"].ToString(),
                LastName = row["lastName"].ToString(),
                Title = row["title"].ToString(),
                Department = row["department"].ToString(),
                Email = row["email"].ToString(),
                Phone = row.Table.Columns.Contains("phone") && row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                EmployDate = ReadEmployDate(row),
                Status = row["status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["status"])
            };
        }

        private static DateTime ReadEmployDate(DataRow row)
        {
            if (!row.Table.Columns.Contains("employDate") || row["employDate"] == DBNull.Value)
                return DateTime.MinValue;

            if (row["employDate"] is DateTime dt)
                return dt.Year <= 1 ? DateTime.MinValue : dt;

            string text = row["employDate"].ToString();
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("0000"))
                return DateTime.MinValue;

            return DateTime.TryParse(text, out DateTime parsed) ? parsed : DateTime.MinValue;
        }

        private static string HashPassword(string password)
        {
            return PasswordHasher.Hash(password ?? string.Empty);
        }

        private static bool VerifyPassword(string password, string storedPassword)
        {
            return PasswordHasher.Verify(password, storedPassword);
        }

        private static void UpgradePasswordHash(long staffId, string plainPassword)
        {
            try
            {
                DatabaseConnect.ExecuteNonQuery(
                    "UPDATE Staff SET password = @pass WHERE staffID = @id",
                    new[]
                    {
                        new MySqlParameter("@pass", HashPassword(plainPassword)),
                        new MySqlParameter("@id", staffId)
                    });
            }
            catch { }
        }
    }
}
