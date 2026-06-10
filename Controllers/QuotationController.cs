using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class QuotationController
    {
        private readonly CurrencyController _currencyCtrl = new CurrencyController();

        public DataTable GetAllQuotations()
        {
            string sql = @"SELECT q.quotationCode AS 'Quotation Code',
                                  c.customerName AS 'Customer',
                                  cur.currencyCode AS 'Currency',
                                  q.totalAmount AS 'Total',
                                  q.totalAmountBase AS 'Total (HKD)',
                                  q.exchangeRate AS 'Rate',
                                  q.createDate AS 'Create Date',
                                  q.status AS 'Status',
                                  q.quotationID AS 'Quotation ID'
                           FROM Quotation q
                           LEFT JOIN Customer c ON q.customerID = c.customerID
                           LEFT JOIN Currency cur ON q.currencyID = cur.currencyID
                           ORDER BY q.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(Quotation quotation)
        {
            if (quotation.CurrencyID <= 0) quotation.CurrencyID = 1;
            if (quotation.ExchangeRate <= 0)
                quotation.ExchangeRate = _currencyCtrl.LockRateForCurrency(quotation.CurrencyID);

            string sql = @"INSERT INTO Quotation
                (quotationID, quotationCode, sequenceNumber, staffID, customerID, currencyID,
                 exchangeRate, totalAmount, totalAmountBase, status, remark)
                VALUES (@id, @code, @seq, @staffID, @customerID, @currencyID,
                        @rate, @total, @totalBase, @quotationStatus, @remark)";
            return DatabaseConnect.InsertWithAllocatedId("quotation", "quotationID", sql, new[] {
                new MySqlParameter("@code", quotation.QuotationCode),
                new MySqlParameter("@seq", quotation.SequenceNumber),
                new MySqlParameter("@staffID", quotation.StaffID),
                new MySqlParameter("@customerID", quotation.CustomerID),
                new MySqlParameter("@currencyID", quotation.CurrencyID),
                new MySqlParameter("@rate", quotation.ExchangeRate),
                new MySqlParameter("@total", quotation.TotalAmount),
                new MySqlParameter("@totalBase", quotation.TotalAmountBase),
                new MySqlParameter("@quotationStatus", quotation.Status),
                new MySqlParameter("@remark", quotation.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long quotationId)
        {
            string code = DocumentCodeHelper.Build("QT", quotationId);
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE Quotation SET quotationCode = @code WHERE quotationID = @id",
                new[] {
                    new MySqlParameter("@code", code),
                    new MySqlParameter("@id", quotationId)
                });
        }

        public bool InsertProductLine(long quotationId, long productId, decimal price, decimal quantity, decimal discount)
        {
            string sql = @"INSERT INTO QuotationProductLine (quotationID, productID, price, quantity, discountAmount)
                           VALUES (@qid, @pid, @price, @qty, @discount)";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@qid", quotationId),
                new MySqlParameter("@pid", productId),
                new MySqlParameter("@price", price),
                new MySqlParameter("@qty", quantity),
                new MySqlParameter("@discount", discount)
            }) > 0;
        }

        public bool DeleteProductLines(long quotationId)
        {
            string sql = "DELETE FROM QuotationProductLine WHERE quotationID = @qid";
            DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@qid", quotationId)
            });
            return true;
        }

        public bool ReplaceProductLines(long quotationId, IEnumerable<(long ProductID, decimal Price, decimal Quantity, decimal Discount)> lines)
        {
            DeleteProductLines(quotationId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                InsertProductLine(quotationId, line.ProductID, line.Price, line.Quantity, line.Discount);
                hasAny = true;
            }
            if (hasAny) RefreshTotals(quotationId);
            return hasAny;
        }

        public void RefreshTotals(long quotationId)
        {
            var quotation = GetById(quotationId);
            if (quotation == null) return;

            decimal total = GetTotalAmount(quotationId);
            decimal rate = quotation.ExchangeRate > 0
                ? quotation.ExchangeRate
                : _currencyCtrl.LockRateForCurrency(quotation.CurrencyID);
            decimal baseTotal = CurrencyConversionService.ToBaseAmount(total, rate);

            DatabaseConnect.ExecuteNonQuery(
                @"UPDATE Quotation
                  SET totalAmount = @total, totalAmountBase = @base, exchangeRate = @rate, lastModifyDate = NOW()
                  WHERE quotationID = @id",
                new[]
                {
                    new MySqlParameter("@total", total),
                    new MySqlParameter("@base", baseTotal),
                    new MySqlParameter("@rate", rate),
                    new MySqlParameter("@id", quotationId)
                });
        }

        public DataTable GetProductLines(long quotationId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code', qpl.price AS 'Price',
                                  qpl.quantity AS 'Quantity', qpl.discountAmount AS 'Discount'
                           FROM QuotationProductLine qpl
                           INNER JOIN Product p ON qpl.productID = p.productID
                           WHERE qpl.quotationID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", quotationId) });
        }

        public DataTable GetProductLinesDetailed(long quotationId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  p.category AS 'Category',
                                  p.styleNumber AS 'Style Number',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  qpl.price AS 'Price',
                                  qpl.quantity AS 'Quantity',
                                  qpl.discountAmount AS 'Discount',
                                  (qpl.price * qpl.quantity - qpl.discountAmount) AS 'Amount'
                           FROM QuotationProductLine qpl
                           INNER JOIN Product p ON qpl.productID = p.productID
                           WHERE qpl.quotationID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", quotationId) });
        }

        public DataTable GetHeaderDetail(long quotationId)
        {
            string sql = @"SELECT q.quotationCode AS 'Quotation Code',
                                  c.customerName AS 'Customer',
                                  q.createDate AS 'Create Date',
                                  q.status AS 'Status',
                                  q.remark AS 'Remark'
                           FROM Quotation q
                           LEFT JOIN Customer c ON q.customerID = c.customerID
                           WHERE q.quotationID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", quotationId) });
        }

        public decimal GetTotalAmount(long quotationId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(price * quantity - discountAmount), 0)
                  FROM QuotationProductLine WHERE quotationID = @id",
                new[] { new MySqlParameter("@id", quotationId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public Quotation GetById(long quotationId)
        {
            string sql = @"SELECT quotationID, quotationCode, sequenceNumber, staffID, customerID,
                                  currencyID, exchangeRate, totalAmount, totalAmountBase, status, remark
                           FROM Quotation WHERE quotationID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", quotationId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Quotation
            {
                QuotationID = System.Convert.ToInt64(row["quotationID"]),
                QuotationCode = row["quotationCode"]?.ToString(),
                SequenceNumber = row["sequenceNumber"] == System.DBNull.Value ? 0 : System.Convert.ToInt32(row["sequenceNumber"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                CurrencyID = System.Convert.ToInt64(row["currencyID"]),
                ExchangeRate = row.Table.Columns.Contains("exchangeRate") && row["exchangeRate"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["exchangeRate"]) : 1m,
                TotalAmount = row.Table.Columns.Contains("totalAmount") && row["totalAmount"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["totalAmount"]) : 0m,
                TotalAmountBase = row.Table.Columns.Contains("totalAmountBase") && row["totalAmountBase"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["totalAmountBase"]) : 0m,
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public DataTable GetProductLinesInternal(long quotationId)
        {
            string sql = @"SELECT qpl.productID, qpl.price, qpl.quantity, qpl.discountAmount
                           FROM QuotationProductLine qpl
                           WHERE qpl.quotationID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", quotationId) });
        }

        public bool UpdateStatus(long quotationId, int status)
        {
            return DatabaseConnect.ExecuteNonQuery(
                "UPDATE Quotation SET status = @quotationStatus, lastModifyDate = NOW() WHERE quotationID = @id",
                new[]
                {
                    new MySqlParameter("@quotationStatus", status),
                    new MySqlParameter("@id", quotationId)
                }) > 0;
        }

        public bool UpdateHeader(Quotation quotation)
        {
            var existing = GetById(quotation.QuotationID);
            long currencyId = quotation.CurrencyID > 0 ? quotation.CurrencyID : 1;
            decimal rate = existing != null && existing.CurrencyID == currencyId && existing.ExchangeRate > 0
                ? existing.ExchangeRate
                : _currencyCtrl.LockRateForCurrency(currencyId);

            bool ok = DatabaseConnect.ExecuteNonQuery(
                @"UPDATE Quotation SET customerID = @customerID, currencyID = @currencyID,
                  exchangeRate = @rate, status = @quotationStatus, remark = @remark, lastModifyDate = NOW()
                  WHERE quotationID = @id",
                new[]
                {
                    new MySqlParameter("@customerID", quotation.CustomerID),
                    new MySqlParameter("@currencyID", currencyId),
                    new MySqlParameter("@rate", rate),
                    new MySqlParameter("@quotationStatus", quotation.Status),
                    new MySqlParameter("@remark", quotation.Remark ?? (object)System.DBNull.Value),
                    new MySqlParameter("@id", quotation.QuotationID)
                }) > 0;
            if (ok) RefreshTotals(quotation.QuotationID);
            return ok;
        }

        public DataTable GetProductionOrdersByQuotationCustomer(long customerId)
        {
            string sql = @"SELECT po.productionOrderCode AS 'Production Code', po.createDate AS 'Create Date', po.status AS 'Status'
                           FROM ProductionOrder po
                           INNER JOIN SalesOrder so ON po.salesOrderID = so.salesOrderID
                           WHERE so.customerID = @cid ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@cid", customerId) });
        }
    }
}
