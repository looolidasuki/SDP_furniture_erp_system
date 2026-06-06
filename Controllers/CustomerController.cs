using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class CustomerController
    {
        // 在 CustomerController.cs 中加入

        public DataTable GetAllCustomers()
        {
            string sql = @"SELECT customerCode AS 'Customer Code',
                                  customerRefNumber AS 'Customer Ref Number',
                                  customerName AS 'Customer Name',
                                  billingAddress AS 'Billing Address',
                                  paymentTerm AS 'Payment Term',
                                  createDate AS 'Create Date',
                                  lastModifyDate AS 'Last Modify Date',
                                  customerID AS 'Customer ID'
                           FROM Customer
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public Customer GetById(long customerId)
        {
            string sql = @"SELECT customerID, customerCode, customerRefNumber, customerName, billingAddress, paymentTerm
                           FROM Customer WHERE customerID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@id", customerId)
            });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Customer
            {
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                CustomerCode = row["customerCode"] == System.DBNull.Value ? null : row["customerCode"].ToString(),
                CustomerRefNumber = row["customerRefNumber"] == System.DBNull.Value ? null : row["customerRefNumber"].ToString(),
                CustomerName = row["customerName"].ToString(),
                BillingAddress = row["billingAddress"].ToString(),
                PaymentTerm = row["paymentTerm"].ToString()
            };
        }

        public long Insert(Customer customer)
        {
            string sql = @"INSERT INTO Customer (customerCode, customerRefNumber, customerName, billingAddress, paymentTerm)
                           VALUES (@code, @ref, @name, @address, @term)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", string.IsNullOrWhiteSpace(customer.CustomerCode) ? (object)System.DBNull.Value : customer.CustomerCode.Trim()),
                new MySqlParameter("@ref", string.IsNullOrWhiteSpace(customer.CustomerRefNumber) ? (object)System.DBNull.Value : customer.CustomerRefNumber.Trim()),
                new MySqlParameter("@name", customer.CustomerName ?? ""),
                new MySqlParameter("@address", customer.BillingAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@term", customer.PaymentTerm ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long customerId)
        {
            string code = "CU-" + customerId.ToString("D9");
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE Customer SET customerCode = @code WHERE customerID = @id",
                new[]
                {
                    new MySqlParameter("@code", code),
                    new MySqlParameter("@id", customerId)
                });
        }

        public void UpdateRefNumberAfterInsert(long customerId)
        {
            string refNo = FormatCustomerRefNumber(customerId);
            DatabaseConnect.ExecuteNonQuery(
                @"UPDATE Customer SET customerRefNumber = @ref
                  WHERE customerID = @id
                    AND (customerRefNumber IS NULL OR TRIM(customerRefNumber) = '')",
                new[]
                {
                    new MySqlParameter("@ref", refNo),
                    new MySqlParameter("@id", customerId)
                });
        }

        public static string FormatCustomerRefNumber(long id)
        {
            return "PO-PL-" + id.ToString("D9");
        }

        public bool Update(Customer customer)
        {
            string sql = @"UPDATE Customer SET customerCode = @code, customerRefNumber = @ref, customerName = @name, billingAddress = @address,
                           paymentTerm = @term, lastModifyDate = NOW() WHERE customerID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@code", string.IsNullOrWhiteSpace(customer.CustomerCode) ? (object)System.DBNull.Value : customer.CustomerCode.Trim()),
                new MySqlParameter("@ref", string.IsNullOrWhiteSpace(customer.CustomerRefNumber) ? (object)System.DBNull.Value : customer.CustomerRefNumber.Trim()),
                new MySqlParameter("@name", customer.CustomerName ?? ""),
                new MySqlParameter("@address", customer.BillingAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@term", customer.PaymentTerm ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", customer.CustomerID)
            }) > 0;
        }

        public DataTable GetSalesOrdersByCustomer(long customerId)
        {
            string sql = @"SELECT salesOrderCode AS 'Order Code', deliveryAddress AS 'Delivery Address',
                                  createDate AS 'Create Date', status AS 'Status'
                           FROM SalesOrder WHERE customerID = @id ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", customerId) });
        }

        public DataTable GetQuotationsByCustomer(long customerId)
        {
            string sql = @"SELECT quotationCode AS 'Quotation Code', createDate AS 'Create Date', status AS 'Status'
                           FROM Quotation WHERE customerID = @id ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", customerId) });
        }

        public DataTable Search(SearchFilterCriteria filter)
        {
            string sql = @"SELECT customerCode AS 'Customer Code',
                                  customerRefNumber AS 'Customer Ref Number',
                                  customerName AS 'Customer Name',
                                  billingAddress AS 'Billing Address',
                                  paymentTerm AS 'Payment Term',
                                  createDate AS 'Create Date',
                                  lastModifyDate AS 'Last Modify Date',
                                  customerID AS 'Customer ID'
                           FROM Customer WHERE 1=1";
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            SearchQueryHelper.AddLike(conditions, parameters, "customerCode", filter.Keyword, "@code");
            SearchQueryHelper.AddLike(conditions, parameters, "customerRefNumber", filter.Keyword, "@ref");
            SearchQueryHelper.AddLike(conditions, parameters, "customerName", filter.Name ?? filter.Keyword, "@name");
            SearchQueryHelper.AddLike(conditions, parameters, "billingAddress", filter.Keyword, "@addr");
            SearchQueryHelper.AddDateFrom(conditions, parameters, "createDate", filter.FromDate);
            SearchQueryHelper.AddDateTo(conditions, parameters, "createDate", filter.ToDate);
            if (conditions.Count > 0) sql += " AND " + string.Join(" AND ", conditions);
            sql += " ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, parameters.ToArray());
        }

        public int GetCount()
        {
            string sql = "SELECT COUNT(*) FROM Customer";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                return System.Convert.ToInt32(dt.Rows[0][0]);
            }
            return 0;
        }

        public List<ContactPerson> GetContactPersons(long customerId)
        {
            string sql = @"SELECT contactPersonID, customerID, contactPerson, title, phone, email
                           FROM contactperson WHERE customerID = @id ORDER BY contactPersonID";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", customerId) });
            var list = new List<ContactPerson>();
            if (dt == null) return list;
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ContactPerson
                {
                    ContactPersonID = System.Convert.ToInt64(row["contactPersonID"]),
                    CustomerID = System.Convert.ToInt64(row["customerID"]),
                    Name = row["contactPerson"]?.ToString(),
                    Title = row["title"]?.ToString(),
                    Phone = row["phone"]?.ToString(),
                    Email = row["email"]?.ToString()
                });
            }
            return list;
        }

        public List<CustomerDeliveryAddress> GetDeliveryAddresses(long customerId)
        {
            string sql = @"SELECT addressID, customerID, deliveryAddress, contactPerson, phone, email
                           FROM customerdeliveryaddress WHERE customerID = @id ORDER BY addressID";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", customerId) });
            var list = new List<CustomerDeliveryAddress>();
            if (dt == null) return list;
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new CustomerDeliveryAddress
                {
                    AddressID = System.Convert.ToInt64(row["addressID"]),
                    CustomerID = System.Convert.ToInt64(row["customerID"]),
                    DeliveryAddress = row["deliveryAddress"]?.ToString(),
                    ContactPerson = row["contactPerson"]?.ToString(),
                    Phone = row["phone"]?.ToString(),
                    Email = row["email"]?.ToString()
                });
            }
            return list;
        }

        public long InsertContactPerson(ContactPerson cp)
        {
            string sql = @"INSERT INTO contactperson (customerID, contactPerson, title, phone, email)
                           VALUES (@cid, @name, @title, @phone, @email)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[]
            {
                new MySqlParameter("@cid", cp.CustomerID),
                new MySqlParameter("@name", cp.Name ?? (object)System.DBNull.Value),
                new MySqlParameter("@title", cp.Title ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", cp.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", cp.Email ?? (object)System.DBNull.Value)
            });
        }

        public bool UpdateContactPerson(ContactPerson cp)
        {
            string sql = @"UPDATE contactperson SET contactPerson = @name, title = @title,
                           phone = @phone, email = @email WHERE contactPersonID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@name", cp.Name ?? (object)System.DBNull.Value),
                new MySqlParameter("@title", cp.Title ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", cp.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", cp.Email ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", cp.ContactPersonID)
            }) > 0;
        }

        public bool DeleteContactPerson(long contactPersonId)
        {
            string sql = "DELETE FROM contactperson WHERE contactPersonID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] { new MySqlParameter("@id", contactPersonId) }) > 0;
        }

        public long InsertDeliveryAddress(CustomerDeliveryAddress addr)
        {
            string sql = @"INSERT INTO customerdeliveryaddress (customerID, deliveryAddress, contactPerson, phone, email)
                           VALUES (@cid, @addr, @contact, @phone, @email)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[]
            {
                new MySqlParameter("@cid", addr.CustomerID),
                new MySqlParameter("@addr", addr.DeliveryAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@contact", addr.ContactPerson ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", addr.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", addr.Email ?? (object)System.DBNull.Value)
            });
        }

        public bool UpdateDeliveryAddress(CustomerDeliveryAddress addr)
        {
            string sql = @"UPDATE customerdeliveryaddress SET deliveryAddress = @addr, contactPerson = @contact,
                           phone = @phone, email = @email WHERE addressID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@addr", addr.DeliveryAddress ?? (object)System.DBNull.Value),
                new MySqlParameter("@contact", addr.ContactPerson ?? (object)System.DBNull.Value),
                new MySqlParameter("@phone", addr.Phone ?? (object)System.DBNull.Value),
                new MySqlParameter("@email", addr.Email ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", addr.AddressID)
            }) > 0;
        }

        public bool DeleteDeliveryAddress(long addressId)
        {
            string sql = "DELETE FROM customerdeliveryaddress WHERE addressID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] { new MySqlParameter("@id", addressId) }) > 0;
        }

        public void SyncContactPersons(long customerId, List<ContactPerson> current, IEnumerable<long> originalIds)
        {
            var keptIds = new HashSet<long>();
            foreach (var cp in current)
            {
                cp.CustomerID = customerId;
                if (cp.ContactPersonID > 0)
                {
                    UpdateContactPerson(cp);
                    keptIds.Add(cp.ContactPersonID);
                }
                else if (!string.IsNullOrWhiteSpace(cp.Name) || !string.IsNullOrWhiteSpace(cp.Phone) || !string.IsNullOrWhiteSpace(cp.Email))
                {
                    InsertContactPerson(cp);
                }
            }
            foreach (long id in originalIds)
            {
                if (!keptIds.Contains(id))
                    DeleteContactPerson(id);
            }
        }

        public void SyncDeliveryAddresses(long customerId, List<CustomerDeliveryAddress> current, IEnumerable<long> originalIds)
        {
            var keptIds = new HashSet<long>();
            foreach (var addr in current)
            {
                addr.CustomerID = customerId;
                if (addr.AddressID > 0)
                {
                    UpdateDeliveryAddress(addr);
                    keptIds.Add(addr.AddressID);
                }
                else if (!string.IsNullOrWhiteSpace(addr.DeliveryAddress) || !string.IsNullOrWhiteSpace(addr.ContactPerson))
                {
                    InsertDeliveryAddress(addr);
                }
            }
            foreach (long id in originalIds)
            {
                if (!keptIds.Contains(id))
                    DeleteDeliveryAddress(id);
            }
        }
    }
}
