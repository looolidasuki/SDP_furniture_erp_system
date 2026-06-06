using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class ReplySlipController
    {
        public DataTable GetAllReplySlips()
        {
            string sql = @"SELECT rs.replySlipID AS 'Reply Slip ID',
                                  rs.replySlipCode AS 'Reply Slip Code',
                                  so.salesOrderCode AS 'Sales Order Code',
                                  c.customerName AS 'Customer',
                                  rs.signedBy AS 'Signed By',
                                  rs.signedDate AS 'Signed Date',
                                  rs.createDate AS 'Create Date',
                                  rs.status AS 'Status'
                           FROM ReplySlip rs
                           LEFT JOIN SalesOrder so ON rs.salesOrderID = so.salesOrderID
                           LEFT JOIN Customer c ON rs.customerID = c.customerID
                           ORDER BY rs.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(ReplySlip slip)
        {
            string sql = @"INSERT INTO ReplySlip
                (replySlipCode, salesOrderID, customerID, staffID, currencyID, signedBy, signedDate, status, remark)
                VALUES (@code, @soID, @customerID, @staffID, @currencyID, @signedBy, @signedDate, @replySlipStatus, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[]
            {
                new MySqlParameter("@code", slip.ReplySlipCode),
                new MySqlParameter("@soID", slip.SalesOrderID),
                new MySqlParameter("@customerID", slip.CustomerID),
                new MySqlParameter("@staffID", slip.StaffID),
                new MySqlParameter("@currencyID", slip.CurrencyID),
                new MySqlParameter("@signedBy", string.IsNullOrWhiteSpace(slip.SignedBy) ? (object)System.DBNull.Value : slip.SignedBy),
                new MySqlParameter("@signedDate", slip.SignedDate.HasValue ? (object)slip.SignedDate.Value : System.DBNull.Value),
                new MySqlParameter("@replySlipStatus", slip.Status),
                new MySqlParameter("@remark", slip.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long replySlipId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE ReplySlip SET replySlipCode = @code WHERE replySlipID = @id",
                new[]
                {
                    new MySqlParameter("@code", "RS-" + replySlipId),
                    new MySqlParameter("@id", replySlipId)
                });
        }

        public bool Update(ReplySlip slip)
        {
            string sql = @"UPDATE ReplySlip
                           SET salesOrderID = @soID, customerID = @customerID, signedBy = @signedBy,
                               signedDate = @signedDate, status = @replySlipStatus, remark = @remark, lastModifyDate = NOW()
                           WHERE replySlipID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@soID", slip.SalesOrderID),
                new MySqlParameter("@customerID", slip.CustomerID),
                new MySqlParameter("@signedBy", string.IsNullOrWhiteSpace(slip.SignedBy) ? (object)System.DBNull.Value : slip.SignedBy),
                new MySqlParameter("@signedDate", slip.SignedDate.HasValue ? (object)slip.SignedDate.Value : System.DBNull.Value),
                new MySqlParameter("@replySlipStatus", slip.Status),
                new MySqlParameter("@remark", slip.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", slip.ReplySlipID)
            }) > 0;
        }

        public bool InsertProductLine(long replySlipId, long productId, decimal price, decimal quantity, decimal discount)
        {
            string sql = @"INSERT INTO ReplySlipProductLine
                (replySlipID, productID, price, quantity, discountAmount)
                VALUES (@rsID, @productID, @price, @qty, @discount)";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@rsID", replySlipId),
                new MySqlParameter("@productID", productId),
                new MySqlParameter("@price", price),
                new MySqlParameter("@qty", quantity),
                new MySqlParameter("@discount", discount)
            }) > 0;
        }

        public bool DeleteProductLines(long replySlipId)
        {
            string sql = "DELETE FROM ReplySlipProductLine WHERE replySlipID = @id";
            DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@id", replySlipId)
            });
            return true;
        }

        public bool ReplaceProductLines(long replySlipId, IEnumerable<(long ProductID, decimal Price, decimal Quantity, decimal Discount)> lines)
        {
            DeleteProductLines(replySlipId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                InsertProductLine(replySlipId, line.ProductID, line.Price, line.Quantity, line.Discount);
                hasAny = true;
            }
            return hasAny;
        }

        public DataTable GetProductLines(long replySlipId)
        {
            return GetProductLinesDetailed(replySlipId);
        }

        public DataTable GetHeaderDetail(long replySlipId)
        {
            string sql = @"SELECT rs.replySlipCode AS 'Reply Slip Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  c.customerName AS 'Customer',
                                  CONCAT(st.firstName, ' ', st.lastName) AS 'Staff',
                                  rs.signedBy AS 'Signed By',
                                  rs.signedDate AS 'Signed Date',
                                  rs.createDate AS 'Create Date',
                                  rs.status AS 'Status',
                                  rs.remark AS 'Remark'
                           FROM ReplySlip rs
                           LEFT JOIN SalesOrder so ON rs.salesOrderID = so.salesOrderID
                           LEFT JOIN Customer c ON rs.customerID = c.customerID
                           LEFT JOIN Staff st ON rs.staffID = st.staffID
                           WHERE rs.replySlipID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", replySlipId) });
        }

        public DataTable GetProductLinesDetailed(long replySlipId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  p.styleNumber AS 'Style',
                                  p.category AS 'Category',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  rpl.price AS 'Unit Price',
                                  rpl.quantity AS 'Qty',
                                  rpl.discountAmount AS 'Discount',
                                  (rpl.price * rpl.quantity - rpl.discountAmount) AS 'Amount'
                           FROM ReplySlipProductLine rpl
                           INNER JOIN Product p ON rpl.productID = p.productID
                           WHERE rpl.replySlipID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", replySlipId) });
        }

        public decimal GetTotalAmount(long replySlipId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(price * quantity - discountAmount), 0)
                  FROM ReplySlipProductLine WHERE replySlipID = @id",
                new[] { new MySqlParameter("@id", replySlipId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public DataTable GetProductLinesInternal(long replySlipId)
        {
            string sql = @"SELECT productID, price, quantity, discountAmount
                           FROM ReplySlipProductLine WHERE replySlipID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", replySlipId) });
        }

        public ReplySlip GetById(long replySlipId)
        {
            string sql = @"SELECT replySlipID, replySlipCode, salesOrderID, customerID, staffID, currencyID,
                                  signedBy, signedDate, status, remark
                           FROM ReplySlip WHERE replySlipID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", replySlipId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new ReplySlip
            {
                ReplySlipID = System.Convert.ToInt64(row["replySlipID"]),
                ReplySlipCode = row["replySlipCode"]?.ToString(),
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                CurrencyID = System.Convert.ToInt64(row["currencyID"]),
                SignedBy = row["signedBy"] == System.DBNull.Value ? null : row["signedBy"].ToString(),
                SignedDate = row["signedDate"] == System.DBNull.Value ? (System.DateTime?)null : System.Convert.ToDateTime(row["signedDate"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }
    }
}
