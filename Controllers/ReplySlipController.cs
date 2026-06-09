using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    /// <summary>
    /// Reply slips are paired delivery notes (replySlipCode on deliverynote). No separate ReplySlip table.
    /// </summary>
    public class ReplySlipController
    {
        private const string DepositCode = DeliveryNoteController.DepositDeliveryNoteCode;

        public DataTable GetAllReplySlips()
        {
            string sql = @"SELECT dn.deliveryNoteID AS 'Reply Slip ID',
                                  dn.replySlipCode AS 'Reply Slip Code',
                                  so.salesOrderCode AS 'Sales Order Code',
                                  c.customerName AS 'Customer',
                                  dn.signedBy AS 'Signed By',
                                  dn.signedDate AS 'Signed Date',
                                  dn.createDate AS 'Create Date',
                                  dn.status AS 'Status'
                           FROM DeliveryNote dn
                           LEFT JOIN SalesOrder so ON dn.SalesOrderID = so.salesOrderID
                           LEFT JOIN Customer c ON dn.customerID = c.customerID
                           WHERE dn.deliveryNoteCode <> @deposit
                             AND dn.replySlipCode IS NOT NULL
                           ORDER BY dn.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@deposit", DepositCode) });
        }

        public long Insert(ReplySlip slip)
        {
            var dn = new DeliveryNote
            {
                DeliveryNoteCode = "DN-TEMP",
                CustomerID = slip.CustomerID,
                SalesOrderID = slip.SalesOrderID,
                StaffID = slip.StaffID > 0 ? slip.StaffID : 1,
                WarehouseID = ResolveDefaultWarehouseId(),
                ShipMethod = "0",
                TrackingNumber = "",
                Status = slip.Status,
                Remark = slip.Remark,
                SignedBy = slip.SignedBy,
                SignedDate = slip.SignedDate
            };
            var ctrl = new DeliveryNoteController();
            long id = ctrl.Insert(dn);
            ctrl.UpdateCodeAfterInsert(id);
            if (!string.IsNullOrWhiteSpace(slip.SignedBy) || slip.SignedDate.HasValue)
                ctrl.UpdateSignOff(id, slip.SignedBy, slip.SignedDate);
            return id;
        }

        public void UpdateCodeAfterInsert(long deliveryNoteId)
        {
            new DeliveryNoteController().UpdateCodeAfterInsert(deliveryNoteId);
        }

        public bool Update(ReplySlip slip)
        {
            string sql = @"UPDATE DeliveryNote
                           SET SalesOrderID = @soID, customerID = @customerID,
                               signedBy = @signedBy, signedDate = @signedDate,
                               status = @status, remark = @remark, lastModifyDate = NOW()
                           WHERE deliveryNoteID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@soID", slip.SalesOrderID),
                new MySqlParameter("@customerID", slip.CustomerID),
                new MySqlParameter("@signedBy", string.IsNullOrWhiteSpace(slip.SignedBy) ? (object)System.DBNull.Value : slip.SignedBy),
                new MySqlParameter("@signedDate", slip.SignedDate.HasValue ? (object)slip.SignedDate.Value : System.DBNull.Value),
                new MySqlParameter("@status", slip.Status),
                new MySqlParameter("@remark", slip.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", slip.ReplySlipID)
            }) > 0;
        }

        public bool InsertProductLine(long deliveryNoteId, long productId, decimal price, decimal quantity, decimal discount)
        {
            string sql = @"INSERT INTO deliveryproductline (deliveryNoteID, productID, lineNumber, shipQuantity)
                           VALUES (@dnID, @productID, 1, @qty)
                           ON DUPLICATE KEY UPDATE shipQuantity = @qty";
            return DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@dnID", deliveryNoteId),
                new MySqlParameter("@productID", productId),
                new MySqlParameter("@qty", (int)quantity)
            }) > 0;
        }

        public bool DeleteProductLines(long deliveryNoteId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "DELETE FROM deliveryproductline WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            return true;
        }

        public bool ReplaceProductLines(long deliveryNoteId, IEnumerable<(long ProductID, decimal Price, decimal Quantity, decimal Discount)> lines)
        {
            DeleteProductLines(deliveryNoteId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                InsertProductLine(deliveryNoteId, line.ProductID, line.Price, line.Quantity, line.Discount);
                hasAny = true;
            }
            return hasAny;
        }

        public DataTable GetProductLines(long deliveryNoteId) => GetProductLinesDetailed(deliveryNoteId);

        public DataTable GetHeaderDetail(long deliveryNoteId)
        {
            string sql = @"SELECT dn.replySlipCode AS 'Reply Slip Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  c.customerName AS 'Customer',
                                  CONCAT(st.firstName, ' ', st.lastName) AS 'Staff',
                                  dn.signedBy AS 'Signed By',
                                  dn.signedDate AS 'Signed Date',
                                  dn.createDate AS 'Create Date',
                                  dn.status AS 'Status',
                                  dn.remark AS 'Remark'
                           FROM DeliveryNote dn
                           LEFT JOIN SalesOrder so ON dn.SalesOrderID = so.salesOrderID
                           LEFT JOIN Customer c ON dn.customerID = c.customerID
                           LEFT JOIN Staff st ON dn.staffID = st.staffID
                           WHERE dn.deliveryNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public DataTable GetProductLinesDetailed(long deliveryNoteId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  p.styleNumber AS 'Style',
                                  p.category AS 'Category',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  spl.price AS 'Unit Price',
                                  dpl.shipQuantity AS 'Qty',
                                  spl.discountAmount AS 'Discount',
                                  (spl.price * dpl.shipQuantity - spl.discountAmount * dpl.shipQuantity / NULLIF(spl.orderQuantity, 0)) AS 'Amount'
                           FROM deliveryproductline dpl
                           INNER JOIN Product p ON dpl.productID = p.productID
                           INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                           INNER JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = dn.SalesOrderID AND spl.productID = dpl.productID
                           WHERE dpl.deliveryNoteID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public decimal GetTotalAmount(long deliveryNoteId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(
                    spl.price * dpl.shipQuantity
                    - spl.discountAmount * dpl.shipQuantity / NULLIF(spl.orderQuantity, 0)
                  ), 0)
                  FROM deliveryproductline dpl
                  INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                  INNER JOIN SalesOrderProductLine spl
                       ON spl.salesOrderID = dn.SalesOrderID AND spl.productID = dpl.productID
                  WHERE dpl.deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            if (value == null || value == System.DBNull.Value) return 0m;

            decimal lineTotal = System.Convert.ToDecimal(value);
            long soId = GetSalesOrderId(deliveryNoteId);
            if (soId <= 0) return lineTotal;

            var so = new SalesOrderController().GetFullById(soId);
            if (so == null) return lineTotal;
            return OrderTotalCalculator.ApplyHeaderDiscount(lineTotal, so.DiscountType, so.Discount);
        }

        public DataTable GetProductLinesInternal(long deliveryNoteId)
        {
            string sql = @"SELECT dpl.productID AS productID,
                                  spl.price AS price,
                                  dpl.shipQuantity AS quantity,
                                  spl.discountAmount AS discountAmount
                           FROM deliveryproductline dpl
                           INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                           INNER JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = dn.SalesOrderID AND spl.productID = dpl.productID
                           WHERE dpl.deliveryNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public ReplySlip GetById(long deliveryNoteId)
        {
            string sql = @"SELECT dn.deliveryNoteID, dn.replySlipCode, dn.SalesOrderID, dn.customerID,
                                  dn.staffID, so.currencyCurrencyID AS currencyID,
                                  dn.signedBy, dn.signedDate, dn.status, dn.remark
                           FROM DeliveryNote dn
                           LEFT JOIN SalesOrder so ON dn.SalesOrderID = so.salesOrderID
                           WHERE dn.deliveryNoteID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new ReplySlip
            {
                ReplySlipID = System.Convert.ToInt64(row["deliveryNoteID"]),
                ReplySlipCode = row["replySlipCode"]?.ToString(),
                SalesOrderID = System.Convert.ToInt64(row["SalesOrderID"]),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                CurrencyID = row["currencyID"] == System.DBNull.Value ? 1L : System.Convert.ToInt64(row["currencyID"]),
                SignedBy = row["signedBy"] == System.DBNull.Value ? null : row["signedBy"].ToString(),
                SignedDate = row["signedDate"] == System.DBNull.Value ? (System.DateTime?)null : System.Convert.ToDateTime(row["signedDate"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        private static long GetSalesOrderId(long deliveryNoteId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT SalesOrderID FROM DeliveryNote WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            if (value == null || value == System.DBNull.Value) return 0;
            return System.Convert.ToInt64(value);
        }

        private static long ResolveDefaultWarehouseId()
        {
            object value = DatabaseConnect.ExecuteScalar("SELECT MIN(warehouseID) FROM Warehouse");
            if (value == null || value == System.DBNull.Value) return 1;
            return System.Convert.ToInt64(value);
        }
    }
}
