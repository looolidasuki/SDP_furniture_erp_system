using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Data;

namespace Sales_user.Controllers
{
    public class InvoiceController
    {
        public const int InvoiceTypeDeposit = 1;
        public const int InvoiceTypeNormal = 2;

        /// <summary>Legacy alias; prefer <see cref="DeliveryNoteController.EnsureDepositDeliveryNoteId"/>.</summary>
        public const long DepositDeliveryNoteId = 999999;

        private readonly DeliveryNoteController _deliveryNoteCtrl = new DeliveryNoteController();

        private long ResolveDepositDeliveryNoteId() => _deliveryNoteCtrl.EnsureDepositDeliveryNoteId();

        public DataTable GetAllInvoices()
        {
            string sql = @"SELECT i.invoiceID AS 'Invoice ID',
                                  i.invoiceCode AS 'Invoice Code',
                                  c.customerName AS 'Customer',
                                  so.salesOrderCode AS 'Sales Order',
                                  i.invoiceType AS 'Invoice Type',
                                  i.createDate AS 'Create Date',
                                  i.status AS 'Status',
                                  i.remark AS 'Remark'
                           FROM Invoice i
                           LEFT JOIN Customer c ON i.customerID = c.customerID
                           LEFT JOIN SalesOrder so ON i.salesOrderID = so.salesOrderID
                           ORDER BY i.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(Invoice invoice)
        {
            string sql = @"INSERT INTO Invoice
                (invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark)
                VALUES (@code, @customerID, @soID, @staffID, @type, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", invoice.InvoiceCode),
                new MySqlParameter("@customerID", invoice.CustomerID),
                new MySqlParameter("@soID", invoice.SalesOrderID),
                new MySqlParameter("@staffID", invoice.StaffID),
                new MySqlParameter("@type", invoice.InvoiceType),
                new MySqlParameter("@status", invoice.Status),
                new MySqlParameter("@remark", invoice.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long invoiceId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE Invoice SET invoiceCode = @code WHERE invoiceID = @id",
                new[] {
                    new MySqlParameter("@code", "INV-" + invoiceId),
                    new MySqlParameter("@id", invoiceId)
                });
        }

        public bool InsertLine(long invoiceId, long deliveryNoteId, long productId, int invoiceQuantity, decimal amount)
        {
            string sql = @"INSERT INTO InvoiceLine (invoiceID, deliveryNoteID, productID, invoiceQuantity, amount)
                           VALUES (@invId, @dnId, @pid, @qty, @amount)";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@invId", invoiceId),
                new MySqlParameter("@dnId", deliveryNoteId),
                new MySqlParameter("@pid", productId),
                new MySqlParameter("@qty", invoiceQuantity),
                new MySqlParameter("@amount", amount)
            }) > 0;
        }

        public bool DeleteLines(long invoiceId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "DELETE FROM InvoiceLine WHERE invoiceID = @id",
                new[] { new MySqlParameter("@id", invoiceId) });
            return true;
        }

        public DataTable GetInvoiceLines(long invoiceId)
        {
            return GetInvoiceLinesInternal(invoiceId, forView: false);
        }

        /// <summary>Invoice lines formatted for view / PDF (friendly delivery note labels, simplified deposit rows).</summary>
        public DataTable GetInvoiceLinesForView(long invoiceId)
        {
            var invoice = GetById(invoiceId);
            if (invoice == null) return new DataTable();
            if (invoice.InvoiceType == InvoiceTypeDeposit)
                return GetDepositInvoiceLinesForView(invoiceId);
            return GetInvoiceLinesInternal(invoiceId, forView: true);
        }

        private DataTable GetDepositInvoiceLinesForView(long invoiceId)
        {
            string sql = @"SELECT CASE
                                  WHEN il.invoiceQuantity < 0 THEN 'Deposit offset (credit)'
                                  ELSE 'Customer deposit (prepayment)'
                               END AS 'Description',
                               il.invoiceQuantity AS 'Qty',
                               il.amount AS 'Amount',
                               il.amount AS 'Line Total'
                           FROM InvoiceLine il
                           INNER JOIN Product p ON il.productID = p.productID
                           WHERE il.invoiceID = @id
                           ORDER BY il.invoiceQuantity DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", invoiceId) });
        }

        private DataTable GetInvoiceLinesInternal(long invoiceId, bool forView)
        {
            if (!forView)
            {
                string sql = @"SELECT il.deliveryNoteID AS 'Delivery Note ID',
                                      p.productCode AS 'Product Code',
                                      p.category AS 'Category',
                                      p.styleNumber AS 'Style Number',
                                      p.size AS 'Size',
                                      p.color AS 'Color',
                                      p.unit AS 'Unit',
                                      il.invoiceQuantity AS 'Qty',
                                      il.amount AS 'Amount',
                                      il.amount AS 'Line Total'
                               FROM InvoiceLine il
                               INNER JOIN Product p ON il.productID = p.productID
                               WHERE il.invoiceID = @id
                               ORDER BY p.productCode";
                return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", invoiceId) });
            }

            string viewSql = @"SELECT CASE
                                      WHEN p.productCode = 'DEPOSIT' OR dn.deliveryNoteCode = @depositDnCode
                                          THEN 'N/A (Deposit)'
                                      ELSE COALESCE(dn.deliveryNoteCode, CONCAT('DN-', il.deliveryNoteID))
                                   END AS 'Delivery Note',
                                   CASE
                                      WHEN p.productCode = 'DEPOSIT' THEN
                                          CASE WHEN il.invoiceQuantity < 0 THEN 'Deposit offset' ELSE 'Customer deposit' END
                                      ELSE p.productCode
                                   END AS 'Product',
                                   CASE WHEN p.productCode = 'DEPOSIT' THEN '' ELSE p.category END AS 'Category',
                                   CASE WHEN p.productCode = 'DEPOSIT' THEN '' ELSE p.styleNumber END AS 'Style Number',
                                   CASE WHEN p.productCode = 'DEPOSIT' THEN '' ELSE p.size END AS 'Size',
                                   CASE WHEN p.productCode = 'DEPOSIT' THEN '' ELSE p.color END AS 'Color',
                                   CASE WHEN p.productCode = 'DEPOSIT' THEN '' ELSE p.unit END AS 'Unit',
                                   il.invoiceQuantity AS 'Qty',
                                   il.amount AS 'Amount',
                                   il.amount AS 'Line Total'
                               FROM InvoiceLine il
                               INNER JOIN Product p ON il.productID = p.productID
                               LEFT JOIN DeliveryNote dn ON il.deliveryNoteID = dn.deliveryNoteID
                               WHERE il.invoiceID = @id
                               ORDER BY CASE WHEN p.productCode = 'DEPOSIT' THEN 1 ELSE 0 END, p.productCode";
            return DatabaseConnect.ExecuteQuery(viewSql, new[]
            {
                new MySqlParameter("@id", invoiceId),
                new MySqlParameter("@depositDnCode", DeliveryNoteController.DepositDeliveryNoteCode)
            });
        }

        public DataTable GetHeaderDetail(long invoiceId)
        {
            string sql = @"SELECT i.invoiceCode AS 'Invoice Code',
                                  c.customerName AS 'Customer',
                                  c.billingAddress AS 'Billing Address',
                                  c.paymentTerm AS 'Payment Terms',
                                  so.salesOrderCode AS 'Sales Order',
                                  so.customerRefNumber AS 'Customer Ref Number',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  i.invoiceType AS 'Invoice Type',
                                  i.createDate AS 'Create Date',
                                  i.status AS 'Status',
                                  i.remark AS 'Remark',
                                  (SELECT COALESCE(SUM(rvi.receivedAmount), 0)
                                   FROM receiptvoucherinvoice rvi
                                   INNER JOIN receiptvoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                                   WHERE rvi.invoiceID = i.invoiceID AND rvi.invoiceID IS NOT NULL AND rv.status = 1)
                                  - (SELECT COALESCE(SUM(rr.refundAmount), 0)
                                     FROM RefundRequest rr
                                     WHERE rr.InvoiceID = i.invoiceID AND rr.status = 2) AS 'Total Received'
                           FROM Invoice i
                           LEFT JOIN Customer c ON i.customerID = c.customerID
                           LEFT JOIN SalesOrder so ON i.salesOrderID = so.salesOrderID
                           LEFT JOIN Staff st ON i.staffID = st.staffID
                           WHERE i.invoiceID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", invoiceId) });
        }

        public DataTable GetReceiptSettlementsByInvoice(long invoiceId)
        {
            string sql = @"SELECT rv.receiptVoucherCode AS 'Receipt Voucher',
                                  rv.paymentReceivedDate AS 'Received Date',
                                  rv.paymentMethod AS 'Method',
                                  rvi.receivedAmount AS 'Allocated Amount',
                                  rvi.type AS 'Allocation Type',
                                  rv.status AS 'Voucher Status',
                                  'Receipt' AS 'Entry Type'
                           FROM receiptvoucherinvoice rvi
                           INNER JOIN receiptvoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                           WHERE rvi.invoiceID = @id
                           UNION ALL
                           SELECT rr.refundRequestCode,
                                  rr.createDate,
                                  'Refund',
                                  -rr.refundAmount,
                                  0,
                                  rr.status,
                                  'Refund'
                           FROM RefundRequest rr
                           WHERE rr.InvoiceID = @id AND rr.status = 2
                           ORDER BY 2 DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", invoiceId) });
        }

        public DataTable GetInvoicesForCustomerPicker(long customerId)
        {
            string sql = @"SELECT i.invoiceID AS 'Invoice ID',
                                  i.invoiceCode AS 'Invoice Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  (SELECT COALESCE(SUM(il.amount), 0) FROM InvoiceLine il WHERE il.invoiceID = i.invoiceID) AS 'Total',
                                  i.status AS 'Status'
                           FROM Invoice i
                           LEFT JOIN SalesOrder so ON i.salesOrderID = so.salesOrderID
                           WHERE i.customerID = @cid
                           ORDER BY i.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@cid", customerId) });
            if (dt != null && !dt.Columns.Contains("DisplayText"))
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    string code = row["Invoice Code"]?.ToString();
                    string total = row["Total"]?.ToString();
                    row["DisplayText"] = string.IsNullOrWhiteSpace(total) ? code : $"{code} — {total}";
                }
            }
            return dt;
        }

        public int GetCount()
        {
            string sql = "SELECT COUNT(*) FROM Invoice";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql);
            if (dt != null && dt.Rows.Count > 0)
                return System.Convert.ToInt32(dt.Rows[0][0]);
            return 0;
        }

        public Invoice GetByCode(string invoiceCode)
        {
            if (string.IsNullOrWhiteSpace(invoiceCode)) return null;
            string sql = @"SELECT invoiceID, invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark
                           FROM Invoice WHERE invoiceCode = @code LIMIT 1";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@code", invoiceCode.Trim()) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Invoice
            {
                InvoiceID = System.Convert.ToInt64(row["invoiceID"]),
                InvoiceCode = row["invoiceCode"]?.ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                InvoiceType = System.Convert.ToInt32(row["invoiceType"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public DataTable GetInvoicesForPicker()
        {
            string sql = @"SELECT i.invoiceID AS 'Invoice ID',
                                  i.invoiceCode AS 'Invoice Code',
                                  c.customerName AS 'Customer',
                                  so.salesOrderCode AS 'Sales Order',
                                  (SELECT COALESCE(SUM(il.amount), 0) FROM InvoiceLine il WHERE il.invoiceID = i.invoiceID) AS 'Total'
                           FROM Invoice i
                           LEFT JOIN Customer c ON i.customerID = c.customerID
                           LEFT JOIN SalesOrder so ON i.salesOrderID = so.salesOrderID
                           ORDER BY i.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql);
            if (dt != null && !dt.Columns.Contains("DisplayText"))
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    string code = row["Invoice Code"]?.ToString();
                    string customer = row["Customer"]?.ToString();
                    row["DisplayText"] = string.IsNullOrWhiteSpace(customer) ? code : $"{code} — {customer}";
                }
            }
            return dt;
        }

        public Invoice GetById(long invoiceId)
        {
            string sql = @"SELECT invoiceID, invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark
                           FROM Invoice WHERE invoiceID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", invoiceId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Invoice
            {
                InvoiceID = System.Convert.ToInt64(row["invoiceID"]),
                InvoiceCode = row["invoiceCode"]?.ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                InvoiceType = System.Convert.ToInt32(row["invoiceType"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public bool Update(Invoice invoice)
        {
            string sql = @"UPDATE Invoice
                           SET customerID = @customerID,
                               salesOrderID = @soID,
                               staffID = @staffID,
                               invoiceType = @type,
                               status = @status,
                               remark = @remark,
                               lastModifyDate = NOW()
                           WHERE invoiceID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@customerID", invoice.CustomerID),
                new MySqlParameter("@soID", invoice.SalesOrderID),
                new MySqlParameter("@staffID", invoice.StaffID),
                new MySqlParameter("@type", invoice.InvoiceType),
                new MySqlParameter("@status", invoice.Status),
                new MySqlParameter("@remark", invoice.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", invoice.InvoiceID)
            }) > 0;
        }

        public bool UpdateDepositLineAmount(long invoiceId, long depositProductId, decimal amount)
        {
            return DatabaseConnect.ExecuteNonQuery(
                @"UPDATE InvoiceLine SET amount = @amount, invoiceQuantity = 1
                  WHERE invoiceID = @invId AND productID = @pid AND deliveryNoteID = @dnId",
                new[]
                {
                    new MySqlParameter("@amount", amount),
                    new MySqlParameter("@invId", invoiceId),
                    new MySqlParameter("@pid", depositProductId),
                    new MySqlParameter("@dnId", ResolveDepositDeliveryNoteId())
                }) > 0;
        }

        public DataTable GetOpenInvoicesByCustomer(long customerId)
        {
            string sql = @"SELECT i.invoiceID AS 'Invoice ID',
                                  i.invoiceCode AS 'Invoice Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  i.invoiceType AS 'Invoice Type',
                                  i.status AS 'Status',
                                  (SELECT COALESCE(SUM(il.amount), 0) FROM InvoiceLine il WHERE il.invoiceID = i.invoiceID) AS 'Invoice Total'
                           FROM Invoice i
                           LEFT JOIN SalesOrder so ON i.salesOrderID = so.salesOrderID
                           WHERE i.customerID = @cid AND i.status IN (0, 1, 3)
                           ORDER BY i.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@cid", customerId) });
        }

        public decimal GetInvoiceTotal(long invoiceId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COALESCE(SUM(amount), 0) FROM InvoiceLine WHERE invoiceID = @id",
                new[] { new MySqlParameter("@id", invoiceId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public decimal GetDepositLineAmount(long invoiceId, long depositProductId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(amount, 0) FROM InvoiceLine
                  WHERE invoiceID = @invId AND productID = @pid AND deliveryNoteID = @dnId",
                new[]
                {
                    new MySqlParameter("@invId", invoiceId),
                    new MySqlParameter("@pid", depositProductId),
                    new MySqlParameter("@dnId", ResolveDepositDeliveryNoteId())
                });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public long? GetActiveDepositInvoiceIdForSalesOrder(long salesOrderId, long depositProductId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT i.invoiceID
                  FROM Invoice i
                  INNER JOIN InvoiceLine il ON il.invoiceID = i.invoiceID
                  WHERE i.salesOrderID = @soId AND i.invoiceType = @depType
                    AND il.productID = @pid AND il.deliveryNoteID = @dnId AND il.amount > 0
                  ORDER BY i.createDate DESC
                  LIMIT 1",
                new[]
                {
                    new MySqlParameter("@soId", salesOrderId),
                    new MySqlParameter("@depType", InvoiceTypeDeposit),
                    new MySqlParameter("@pid", depositProductId),
                    new MySqlParameter("@dnId", ResolveDepositDeliveryNoteId())
                });
            if (value == null || value == System.DBNull.Value) return null;
            return System.Convert.ToInt64(value);
        }

        public bool HasVerifiedReceiptAllocations(long invoiceId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*)
                  FROM receiptvoucherinvoice rvi
                  INNER JOIN receiptvoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                  WHERE rvi.invoiceID = @id AND rv.status = 1",
                new[] { new MySqlParameter("@id", invoiceId) });
            return value != null && value != System.DBNull.Value && System.Convert.ToInt32(value) > 0;
        }

        public bool HasDepositOffsetOnNormalInvoice(long salesOrderId, long depositProductId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*)
                  FROM Invoice i
                  INNER JOIN InvoiceLine il ON il.invoiceID = i.invoiceID
                  WHERE i.salesOrderID = @soId AND i.invoiceType = @normal
                    AND il.productID = @pid AND il.deliveryNoteID = @dnId AND il.invoiceQuantity < 0",
                new[]
                {
                    new MySqlParameter("@soId", salesOrderId),
                    new MySqlParameter("@normal", InvoiceTypeNormal),
                    new MySqlParameter("@pid", depositProductId),
                    new MySqlParameter("@dnId", ResolveDepositDeliveryNoteId())
                });
            return value != null && value != System.DBNull.Value && System.Convert.ToInt32(value) > 0;
        }
    }
}
