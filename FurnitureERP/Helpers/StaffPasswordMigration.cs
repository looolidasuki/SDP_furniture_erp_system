using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// One-time-safe: re-hash any staff.password still stored as plain text (PBKDF2 prefix marks hashed rows).
    /// Existing logins keep working because the current column value is treated as the plaintext password.
    /// </summary>
    public static class StaffPasswordMigration
    {
        public static void EnsureApplied()
        {
            DataTable dt;
            try
            {
                dt = DatabaseConnect.ExecuteQuery(
                    @"SELECT staffID, password
                      FROM Staff
                      WHERE password IS NOT NULL
                        AND TRIM(password) <> ''
                        AND password NOT LIKE 'PBKDF2:%'");
            }
            catch
            {
                return;
            }

            if (dt == null || dt.Rows.Count == 0)
                return;

            foreach (DataRow row in dt.Rows)
            {
                long staffId = Convert.ToInt64(row["staffID"]);
                string plain = row["password"]?.ToString();
                if (string.IsNullOrEmpty(plain))
                    continue;

                try
                {
                    string hashed = PasswordHasher.Hash(plain);
                    DatabaseConnect.ExecuteNonQuery(
                        "UPDATE Staff SET password = @pass WHERE staffID = @id",
                        new[]
                        {
                            new MySqlParameter("@pass", hashed),
                            new MySqlParameter("@id", staffId)
                        });
                }
                catch
                {
                    // Skip individual rows; do not block application startup.
                }
            }
        }
    }
}
