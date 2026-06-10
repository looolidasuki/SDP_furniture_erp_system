using System;
using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class SupplierController
    {
        public DataTable GetAllSuppliers()
        {
            string sql = @"SELECT supplierName AS 'Supplier Name',
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
            string sql = @"SELECT supplierID FROM Supplier
                           WHERE supplierName = @name
                           ORDER BY supplierID LIMIT 1";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@name", supplierName.Trim())
            });
            if (dt == null || dt.Rows.Count == 0) return 0;
            return System.Convert.ToInt64(dt.Rows[0]["supplierID"]);
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
