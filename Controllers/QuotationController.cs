using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class QuotationController
    {
        public DataTable GetAllQuotations()
        {
            string sql = @"SELECT q.quotationCode AS 'Quotation Code',
                                  c.customerName AS 'Customer',
                                  q.createDate AS 'Create Date',
                                  q.status AS 'Status',
                                  q.quotationID AS 'Quotation ID'
                           FROM Quotation q
                           LEFT JOIN Customer c ON q.customerID = c.customerID
                           ORDER BY q.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(Quotation quotation)
        {
            string sql = @"INSERT INTO Quotation
                (quotationCode, sequenceNumber, staffID, customerID, currencyID, status, remark)
                VALUES (@code, @seq, @staffID, @customerID, @currencyID, @quotationStatus, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", quotation.QuotationCode),
                new MySqlParameter("@seq", quotation.SequenceNumber),
                new MySqlParameter("@staffID", quotation.StaffID),
                new MySqlParameter("@customerID", quotation.CustomerID),
                new MySqlParameter("@currencyID", quotation.CurrencyID),
                new MySqlParameter("@quotationStatus", quotation.Status),
                new MySqlParameter("@remark", quotation.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long quotationId)
        {
            string code = "QT-" + quotationId;
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
            return hasAny;
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
                                  currencyID, status, remark
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
