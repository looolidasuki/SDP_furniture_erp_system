using System;
using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class SupplierController
    {
        private const string SupplierCodeSql = "CONCAT('SUP-', supplierID)";

        public DataTable GetAllSuppliers()
        {
            string sql = $@"SELECT {SupplierCodeSql} AS 'Supplier Code',
                                  supplierName AS 'Supplier Name',
                                  contactPerson AS 'Contact Person',
                                  phone AS 'Phone',
                                  email AS 'Email',
                                  billingAddress AS 'Billing Address',
                                  status AS 'Status',
                                  supplierID AS 'Supplier ID'
                           FROM Supplier
                           ORDER BY supplierName";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public static DataTable BuildPickerTableSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("Supplier ID", typeof(long));
            dt.Columns.Add("Supplier Code", typeof(string));
            dt.Columns.Add("Supplier Name", typeof(string));
            dt.Columns.Add("Contact Person", typeof(string));
            dt.Columns.Add("Phone", typeof(string));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Billing Address", typeof(string));
            dt.Columns.Add("Status", typeof(int));
            dt.Columns.Add("DisplayText", typeof(string));
            return dt;
        }

        public DataTable GetSupplierPickerById(long supplierId)
        {
            if (supplierId <= 0) return BuildPickerTableSchema();

            string sql = $@"SELECT {SupplierCodeSql} AS 'Supplier Code',
                                  supplierName AS 'Supplier Name',
                                  contactPerson AS 'Contact Person',
                                  phone AS 'Phone',
                                  email AS 'Email',
                                  billingAddress AS 'Billing Address',
                                  status AS 'Status',
                                  supplierID AS 'Supplier ID'
                           FROM Supplier
                           WHERE supplierID = @id
                           LIMIT 1";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", supplierId) });
            return ApplyPickerDisplayText(dt ?? BuildPickerTableSchema());
        }

        public DataTable SearchForPicker(string prefix, int limit = 25)
        {
            limit = SqlGuard.ClampLimit(limit, 50);
            prefix = (prefix ?? "").Trim();

            string select = $@"SELECT {SupplierCodeSql} AS 'Supplier Code',
                                      supplierName AS 'Supplier Name',
                                      contactPerson AS 'Contact Person',
                                      phone AS 'Phone',
                                      email AS 'Email',
                                      billingAddress AS 'Billing Address',
                                      status AS 'Status',
                                      supplierID AS 'Supplier ID'
                               FROM Supplier";

            DataTable dt;
            if (string.IsNullOrEmpty(prefix))
            {
                dt = DatabaseConnect.ExecuteQuery(
                    select + " ORDER BY supplierName LIMIT @lim",
                    new[] { new MySqlParameter("@lim", limit) });
            }
            else
            {
                string needle = prefix.TrimEnd('\\');
                dt = DatabaseConnect.ExecuteQuery(
                    select + $@" WHERE LOCATE(@needle, supplierName) > 0
                                    OR LOCATE(@needle, contactPerson) > 0
                                    OR LOCATE(@needle, phone) > 0
                                    OR LOCATE(@needle, email) > 0
                                    OR LOCATE(@needle, {SupplierCodeSql}) > 0
                                 ORDER BY supplierName
                                 LIMIT @lim",
                    new[]
                    {
                        new MySqlParameter("@needle", needle),
                        new MySqlParameter("@lim", limit)
                    });
            }

            return ApplyPickerDisplayText(dt ?? BuildPickerTableSchema());
        }

        public static DataTable ApplyPickerDisplayText(DataTable dt)
        {
            if (dt == null) return BuildPickerTableSchema();
            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));

            foreach (DataRow row in dt.Rows)
                row["DisplayText"] = FormatPickerDisplayText(row, dt);

            return dt;
        }

        public static string FormatPickerDisplayText(DataRow row, DataTable dt)
        {
            if (row == null || dt == null) return "";

            string name = dt.Columns.Contains("Supplier Name") ? row["Supplier Name"]?.ToString() : "";
            string code = dt.Columns.Contains("Supplier Code") ? row["Supplier Code"]?.ToString() : "";
            if (string.IsNullOrWhiteSpace(code)
                && dt.Columns.Contains("Supplier ID")
                && row["Supplier ID"] != DBNull.Value)
            {
                code = "SUP-" + Convert.ToInt64(row["Supplier ID"]);
            }

            string contact = dt.Columns.Contains("Contact Person") ? row["Contact Person"]?.ToString() : "";
            string phone = dt.Columns.Contains("Phone") ? row["Phone"]?.ToString() : "";

            string head = string.IsNullOrWhiteSpace(code)
                ? (name ?? "")
                : string.IsNullOrWhiteSpace(name) ? code : $"{code} — {name}";

            var extras = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(contact)) extras.Add(contact.Trim());
            if (!string.IsNullOrWhiteSpace(phone)) extras.Add(phone.Trim());

            return extras.Count == 0 ? head : $"{head} ({string.Join(" | ", extras)})";
        }

        public long Insert(Supplier supplier)
        {
            string sql = @"INSERT INTO Supplier
                (supplierID, supplierName, billingAddress, contactPerson, phone, email, paymentTerm, bankAccount, status)
                VALUES (@id, @name, @address, @contact, @phone, @email, @term, @bank, @status)";
            long id = DatabaseConnect.InsertWithAllocatedId("supplier", "supplierID", sql, new[] {
                new MySqlParameter("@name", supplier.SupplierName),
                new MySqlParameter("@address", supplier.BillingAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@contact", supplier.ContactPerson ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", supplier.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", supplier.Email ?? (object)System.DBNull.Value),
                new MySqlParameter("@term", supplier.PaymentTerm ?? (object)System.DBNull.Value),
                new MySqlParameter("@bank", supplier.BankAccount ?? (object)System.DBNull.Value),
                new MySqlParameter("@status", supplier.Status)
            });
            if (id > 0)
                DocumentAuditService.LogCreate(DocumentAuditService.Types.Supplier, id, "SUP-" + id);
            return id;
        }

        public long FindSupplierIdByName(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName)) return 0;
            supplierName = supplierName.Trim();

            if (long.TryParse(supplierName, out long numericId) && numericId > 0 && GetById(numericId) != null)
                return numericId;

            if (supplierName.StartsWith("SUP-", StringComparison.OrdinalIgnoreCase))
            {
                string digits = supplierName.Substring(4).Trim();
                if (long.TryParse(digits, out long parsedId) && parsedId > 0 && GetById(parsedId) != null)
                    return parsedId;
            }

            string sqlExact = @"SELECT supplierID FROM Supplier
                                WHERE supplierName = @name
                                ORDER BY supplierID LIMIT 1";
            var exact = DatabaseConnect.ExecuteQuery(sqlExact, new[] {
                new MySqlParameter("@name", supplierName)
            });
            if (exact != null && exact.Rows.Count > 0)
                return Convert.ToInt64(exact.Rows[0]["supplierID"]);

            int separator = supplierName.IndexOf('—');
            if (separator < 0)
                separator = supplierName.IndexOf(" - ", StringComparison.Ordinal);
            if (separator > 0)
            {
                long fromPrefix = FindSupplierIdByName(supplierName.Substring(0, separator).Trim());
                if (fromPrefix > 0) return fromPrefix;
            }

            string needle = supplierName.TrimEnd('\\');
            if (string.IsNullOrWhiteSpace(needle)) return 0;

            try
            {
                string sqlFuzzy = $@"SELECT supplierID FROM Supplier
                                    WHERE LOCATE(@needle, supplierName) > 0
                                       OR LOCATE(@needle, contactPerson) > 0
                                       OR LOCATE(@needle, phone) > 0
                                       OR LOCATE(@needle, email) > 0
                                       OR LOCATE(@needle, {SupplierCodeSql}) > 0
                                    ORDER BY supplierID LIMIT 1";
                var fuzzy = DatabaseConnect.ExecuteQuery(sqlFuzzy, new[] {
                    new MySqlParameter("@needle", needle)
                });
                if (fuzzy != null && fuzzy.Rows.Count > 0)
                    return Convert.ToInt64(fuzzy.Rows[0]["supplierID"]);
            }
            catch (MySqlException)
            {
            }

            return 0;
        }

        public Supplier GetById(long id)
        {
            string sql = "SELECT * FROM Supplier WHERE supplierID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Supplier
            {
                SupplierID = Convert.ToInt64(row["supplierID"]),
                SupplierName = row["supplierName"]?.ToString(),
                BillingAddress = row["billingAddress"]?.ToString(),
                ContactPerson = row["contactPerson"]?.ToString(),
                Phone = row["phone"]?.ToString(),
                Email = row["email"]?.ToString(),
                PaymentTerm = row["paymentTerm"]?.ToString(),
                BankAccount = row["bankAccount"]?.ToString(),
                Status = Convert.ToInt32(row["status"])
            };
        }

        public void Update(Supplier supplier)
        {
            string sql = @"UPDATE Supplier SET supplierName=@name, billingAddress=@address, contactPerson=@contact,
                           phone=@phone, email=@email, paymentTerm=@term, bankAccount=@bank, status=@status
                           WHERE supplierID=@id";
            DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@name", supplier.SupplierName),
                new MySqlParameter("@address", supplier.BillingAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@contact", supplier.ContactPerson ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", supplier.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", supplier.Email ?? (object)System.DBNull.Value),
                new MySqlParameter("@term", supplier.PaymentTerm ?? (object)System.DBNull.Value),
                new MySqlParameter("@bank", supplier.BankAccount ?? (object)System.DBNull.Value),
                new MySqlParameter("@status", supplier.Status),
                new MySqlParameter("@id", supplier.SupplierID)
            });
            DocumentAuditService.LogUpdate(DocumentAuditService.Types.Supplier, supplier.SupplierID,
                supplier.SupplierName ?? ("SUP-" + supplier.SupplierID));
        }

        public DataTable GetRawMaterialQuotesBySupplier(long supplierId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material', rms.basePrice AS 'Price',
                                  rms.unit AS 'Unit', rms.status AS 'Status'
                           FROM RawMaterialSupplier rms
                           INNER JOIN RawMaterial rm ON rms.rawMaterialID = rm.rawMaterialID
                           WHERE rms.supplierID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", supplierId) });
        }
    }
}
