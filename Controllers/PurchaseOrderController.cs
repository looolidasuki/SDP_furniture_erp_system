using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Data;

namespace Sales_user.Controllers
{
    public class PurchaseOrderController
    {
        public DataTable GetAllPurchaseOrders()
        {
            string sql = @"SELECT po.purchaseOrderID AS 'Purchase Order ID',
                                  po.purchaseOrderCode AS 'Purchase Order Code',
                                  s.supplierName AS 'Supplier',
                                  po.createDate AS 'Create Date',
                                  po.requestDeliveryDate AS 'Request Delivery Date',
                                  po.status AS 'Status'
                           FROM PurchaseOrder po
                           LEFT JOIN Supplier s ON po.supplierID = s.supplierID
                           ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetAllPurchaseOrderLines()
        {
            string sql = @"SELECT po.purchaseOrderCode AS 'Purchase Order',
                                  rm.rawMaterialCode AS 'Raw Material',
                                  pol.price AS 'Price',
                                  pol.orderQuantity AS 'Order Qty',
                                  pol.receivedQuantity AS 'Received Qty'
                           FROM PurchaseOrderRawMaterialLine pol
                           INNER JOIN PurchaseOrder po ON pol.purchaseOrderID = po.purchaseOrderID
                           INNER JOIN RawMaterial rm ON pol.rawMaterialID = rm.rawMaterialID
                           ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetLinesByPurchaseOrder(long purchaseOrderId)
        {
            string sql = @"
                SELECT rm.rawMaterialCode AS 'Item',
                       rms.supplierStyleNumber AS 'Style number',
                       pol.orderQuantity AS 'Quantity',
                       pol.price AS 'Unit Price',
                       c.currencyCode AS 'Currency',
                       pol.receivedQuantity AS 'Received Qty',
                       GREATEST(pol.orderQuantity - pol.receivedQuantity, 0) AS 'Remaining Qty',
                       (pol.price * pol.orderQuantity) AS 'Line Total',
                       (pol.price * pol.orderQuantity) AS 'Total',
                       (pol.price * pol.orderQuantity) AS 'Amount'
                FROM PurchaseOrderRawMaterialLine pol
                INNER JOIN PurchaseOrder po ON pol.purchaseOrderID = po.purchaseOrderID
                INNER JOIN RawMaterial rm ON pol.rawMaterialID = rm.rawMaterialID
                LEFT JOIN RawMaterialSupplier rms
                    ON rms.rawMaterialID = pol.rawMaterialID AND rms.supplierID = po.supplierID
                LEFT JOIN Currency c ON rms.currencyID = c.currencyID
                WHERE pol.purchaseOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", purchaseOrderId) });
        }

        public DataTable GetHeaderDetail(long purchaseOrderId)
        {
            string sql = @"
                SELECT po.purchaseOrderCode AS 'Purchase Order Code',
                       s.supplierName AS 'Supplier',
                       s.contactPerson AS 'Supplier Contact Person',
                       s.phone AS 'Supplier Phone',
                       s.billingAddress AS 'Billing Address',
                       s.paymentTerm AS 'Payment Terms',
                       st.firstName AS 'Buyer First Name',
                       st.lastName AS 'Buyer Last Name',
                       st.phone AS 'Buyer Phone',
                       st.email AS 'Buyer Email',
                       po.createDate AS 'Create Date',
                       po.requestDeliveryDate AS 'Request Delivery Date',
                       po.status AS 'Status',
                       po.relatedShortageReport AS 'Shortage Report ID',
                       po.remark AS 'Remark',
                       (SELECT COALESCE(SUM(pvpo.payAmount), 0)
                        FROM paymentvoucherpurchaseorder pvpo
                        WHERE pvpo.purchaseOrderID = po.purchaseOrderID) AS 'Total Settled'
                FROM PurchaseOrder po
                LEFT JOIN Supplier s ON po.supplierID = s.supplierID
                LEFT JOIN Staff st ON po.staffID = st.staffID
                WHERE po.purchaseOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", purchaseOrderId) });
        }

        public bool HasReceiptActivity(long purchaseOrderId)
        {
            object receivedSum = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(receivedQuantity), 0)
                  FROM PurchaseOrderRawMaterialLine WHERE purchaseOrderID = @id",
                new[] { new MySqlParameter("@id", purchaseOrderId) });
            if (receivedSum != null && receivedSum != System.DBNull.Value &&
                System.Convert.ToDecimal(receivedSum) > 0)
                return true;

            object grnCount = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM GoodsReceivedNote WHERE PurchaseOrderID = @id",
                new[] { new MySqlParameter("@id", purchaseOrderId) });
            return grnCount != null && grnCount != System.DBNull.Value && System.Convert.ToInt32(grnCount) > 0;
        }

        public decimal GetTotalAmount(long purchaseOrderId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(price * orderQuantity), 0)
                  FROM PurchaseOrderRawMaterialLine WHERE purchaseOrderID = @id",
                new[] { new MySqlParameter("@id", purchaseOrderId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public long Insert(PurchaseOrder order)
        {
            string sql = @"INSERT INTO PurchaseOrder
                (purchaseOrderCode, supplierID, staffID, relatedShortageReport,
                 requestDeliveryDate, status, remark)
                VALUES (@code, @supplierID, @staffID, @shortageReport,
                        @requestDeliveryDate, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", order.PurchaseOrderCode),
                new MySqlParameter("@supplierID", order.SupplierID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@shortageReport", order.RelatedShortageReport ?? (object)System.DBNull.Value),
                new MySqlParameter("@requestDeliveryDate", order.RequestDeliveryDate),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE PurchaseOrder SET purchaseOrderCode = @code WHERE purchaseOrderID = @id",
                new[] {
                    new MySqlParameter("@code", "PO-" + id),
                    new MySqlParameter("@id", id)
                });
        }

        public bool InsertLine(long purchaseOrderId, long rawMaterialId, decimal price, decimal orderQty)
        {
            string sql = @"INSERT INTO PurchaseOrderRawMaterialLine
                (purchaseOrderID, rawMaterialID, price, orderQuantity)
                VALUES (@poID, @rmID, @price, @qty)";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@poID", purchaseOrderId),
                new MySqlParameter("@rmID", rawMaterialId),
                new MySqlParameter("@price", price),
                new MySqlParameter("@qty", orderQty)
            }) > 0;
        }

        public bool DeleteLines(long purchaseOrderId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "DELETE FROM PurchaseOrderRawMaterialLine WHERE purchaseOrderID = @id",
                new[] { new MySqlParameter("@id", purchaseOrderId) });
            return true;
        }

        public bool ReplaceLines(long purchaseOrderId, System.Collections.Generic.IEnumerable<(long RawMaterialID, decimal Price, decimal OrderQty, decimal ReceivedQty)> lines)
        {
            DeleteLines(purchaseOrderId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                if (line.RawMaterialID <= 0) continue;
                string sql = @"INSERT INTO PurchaseOrderRawMaterialLine
                               (purchaseOrderID, rawMaterialID, price, orderQuantity, receivedQuantity)
                               VALUES (@poID, @rmID, @price, @qty, @received)";
                DatabaseConnect.ExecuteNonQuery(sql, new[]
                {
                    new MySqlParameter("@poID", purchaseOrderId),
                    new MySqlParameter("@rmID", line.RawMaterialID),
                    new MySqlParameter("@price", line.Price),
                    new MySqlParameter("@qty", line.OrderQty),
                    new MySqlParameter("@received", line.ReceivedQty)
                });
                hasAny = true;
            }
            return hasAny;
        }

        public DataTable GetPurchaseOrdersForPicker()
        {
            string sql = @"SELECT po.purchaseOrderID AS 'Purchase Order ID',
                                  po.purchaseOrderCode AS 'Purchase Order Code',
                                  s.supplierName AS 'Supplier',
                                  po.supplierID AS 'Supplier ID',
                                  po.requestDeliveryDate AS 'Request Delivery Date'
                           FROM PurchaseOrder po
                           LEFT JOIN Supplier s ON po.supplierID = s.supplierID
                           ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetPurchaseOrdersForSupplierPicker(long supplierId)
        {
            string sql = @"SELECT po.purchaseOrderID AS 'Purchase Order ID',
                                  po.purchaseOrderCode AS 'Purchase Order Code',
                                  po.requestDeliveryDate AS 'Request Delivery Date',
                                  po.supplierID AS 'Supplier ID'
                           FROM PurchaseOrder po
                           WHERE po.supplierID = @sid
                           ORDER BY po.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@sid", supplierId) });
            if (dt != null && !dt.Columns.Contains("DisplayText"))
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    string code = row["Purchase Order Code"]?.ToString();
                    string reqDate = row["Request Delivery Date"] == DBNull.Value
                        ? ""
                        : Convert.ToDateTime(row["Request Delivery Date"]).ToString("yyyy-MM-dd");
                    row["DisplayText"] = string.IsNullOrEmpty(reqDate) ? code : $"{code} (Req: {reqDate})";
                }
            }
            return dt;
        }

        public PurchaseOrder GetByCode(string purchaseOrderCode)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrderCode)) return null;
            string sql = @"SELECT purchaseOrderID, purchaseOrderCode, supplierID, staffID,
                                  requestDeliveryDate, status, remark, relatedShortageReport
                           FROM PurchaseOrder WHERE purchaseOrderCode = @code";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@code", purchaseOrderCode.Trim())
            });
            return MapPurchaseOrderRow(dt);
        }

        public PurchaseOrder GetById(long id)
        {
            string sql = @"SELECT purchaseOrderID, purchaseOrderCode, supplierID, staffID,
                                  requestDeliveryDate, status, remark, relatedShortageReport
                           FROM PurchaseOrder WHERE purchaseOrderID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            return MapPurchaseOrderRow(dt);
        }

        private static PurchaseOrder MapPurchaseOrderRow(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new PurchaseOrder
            {
                PurchaseOrderID = System.Convert.ToInt64(row["purchaseOrderID"]),
                PurchaseOrderCode = row["purchaseOrderCode"]?.ToString(),
                SupplierID = System.Convert.ToInt64(row["supplierID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                RequestDeliveryDate = System.Convert.ToDateTime(row["requestDeliveryDate"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString(),
                RelatedShortageReport = row["relatedShortageReport"] == System.DBNull.Value ? (long?)null : System.Convert.ToInt64(row["relatedShortageReport"])
            };
        }

        public DataTable GetRawMaterialLinesInternal(long purchaseOrderId)
        {
            string sql = @"SELECT rawMaterialID, price, orderQuantity, receivedQuantity
                           FROM PurchaseOrderRawMaterialLine
                           WHERE purchaseOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", purchaseOrderId) });
        }
        public bool Update(PurchaseOrder order)
        {
            string sql = @"UPDATE PurchaseOrder
                           SET supplierID=@supplierID, staffID=@staffID, requestDeliveryDate=@requestDeliveryDate,
                               status=@status, remark=@remark, relatedShortageReport=@shortage,
                               lastModifyDate=NOW()
                           WHERE purchaseOrderID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@supplierID", order.SupplierID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@requestDeliveryDate", order.RequestDeliveryDate),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@shortage", order.RelatedShortageReport ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", order.PurchaseOrderID)
            }) > 0;
        }
    }
}
