using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class DeliveryNoteController
    {
        public DataTable GetAllDeliveryNotes()
        {
            string sql = @"SELECT dn.deliveryNoteID AS 'Delivery Note ID',
                                  dn.deliveryNoteCode AS 'Delivery Note Code',
                                  c.customerName AS 'Customer',
                                  so.salesOrderCode AS 'Sales Order',
                                  dn.shipMethod AS 'Ship Method',
                                  dn.trackingNumber AS 'Tracking Number',
                                  dn.createDate AS 'Create Date',
                                  dn.status AS 'Status',
                                  dn.remark AS 'Remark'
                           FROM DeliveryNote dn
                           LEFT JOIN Customer c ON dn.customerID = c.customerID
                           LEFT JOIN SalesOrder so ON dn.salesOrderID = so.salesOrderID
                           WHERE dn.deliveryNoteCode <> @depositCode
                           ORDER BY dn.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@depositCode", DepositDeliveryNoteCode)
            });
        }

        public const string DepositDeliveryNoteCode = "DN-DEPOSIT";
        public const long DepositDeliveryNoteReservedId = 999999;

        /// <summary>
        /// Ensures a virtual delivery note exists for deposit invoice lines and deposit offsets on invoiceline.
        /// </summary>
        public long EnsureDepositDeliveryNoteId()
        {
            object existing = DatabaseConnect.ExecuteScalar(
                "SELECT deliveryNoteID FROM DeliveryNote WHERE deliveryNoteCode = @code LIMIT 1",
                new[] { new MySqlParameter("@code", DepositDeliveryNoteCode) });
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt64(existing);

            long customerId = GetScalarLong("SELECT MIN(customerID) FROM Customer");
            long salesOrderId = GetScalarLong("SELECT MIN(salesOrderID) FROM SalesOrder");
            long staffId = GetScalarLong("SELECT MIN(staffID) FROM Staff");
            long warehouseId = GetScalarLong("SELECT MIN(warehouseID) FROM Warehouse");
            if (customerId <= 0 || salesOrderId <= 0 || staffId <= 0 || warehouseId <= 0)
                throw new InvalidOperationException(
                    "Cannot create virtual deposit delivery note: seed at least one customer, sales order, staff, and warehouse.");

            object reservedTaken = DatabaseConnect.ExecuteScalar(
                "SELECT deliveryNoteID FROM DeliveryNote WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", DepositDeliveryNoteReservedId) });

            if (reservedTaken == null || reservedTaken == DBNull.Value)
            {
                DatabaseConnect.ExecuteNonQuery(
                    @"INSERT INTO DeliveryNote
                      (deliveryNoteID, deliveryNoteCode, customerID, SalesOrderID, staffID, WarehouseID,
                       shipMethod, trackingNumber, status, remark)
                      VALUES (@id, @code, @customerID, @soID, @staffID, @whID, @shipMethod, @tracking, @status, @remark)",
                    new[]
                    {
                        new MySqlParameter("@id", DepositDeliveryNoteReservedId),
                        new MySqlParameter("@code", DepositDeliveryNoteCode),
                        new MySqlParameter("@customerID", customerId),
                        new MySqlParameter("@soID", salesOrderId),
                        new MySqlParameter("@staffID", staffId),
                        new MySqlParameter("@whID", warehouseId),
                        new MySqlParameter("@shipMethod", "N/A"),
                        new MySqlParameter("@tracking", ""),
                        new MySqlParameter("@status", 3),
                        new MySqlParameter("@remark", "Virtual delivery note for deposit / offset invoice lines")
                    });
                return DepositDeliveryNoteReservedId;
            }

            var note = new DeliveryNote
            {
                DeliveryNoteCode = DepositDeliveryNoteCode,
                CustomerID = customerId,
                SalesOrderID = salesOrderId,
                StaffID = staffId,
                WarehouseID = warehouseId,
                ShipMethod = "N/A",
                TrackingNumber = "",
                Status = 3,
                Remark = "Virtual delivery note for deposit / offset invoice lines"
            };
            return Insert(note);
        }

        private static long GetScalarLong(string sql)
        {
            object value = DatabaseConnect.ExecuteScalar(sql);
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToInt64(value);
        }

        public long Insert(DeliveryNote note)
        {
            string sql = @"INSERT INTO DeliveryNote
                (deliveryNoteCode, customerID, SalesOrderID, staffID, WarehouseID,
                 shipMethod, trackingNumber, status, remark)
                VALUES (@code, @customerID, @soID, @staffID, @whID,
                        @shipMethod, @tracking, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", note.DeliveryNoteCode),
                new MySqlParameter("@customerID", note.CustomerID),
                new MySqlParameter("@soID", note.SalesOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@whID", note.WarehouseID),
                new MySqlParameter("@shipMethod", note.ShipMethod ?? ""),
                new MySqlParameter("@tracking", note.TrackingNumber ?? ""),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE DeliveryNote SET deliveryNoteCode = @code WHERE deliveryNoteID = @id",
                new[] {
                    new MySqlParameter("@code", "DN-" + id),
                    new MySqlParameter("@id", id)
                });
        }

        public long CreateWithLines(DeliveryNote note, IEnumerable<(long ProductId, int ShipQty)> lines)
        {
            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                long newId = DatabaseConnect.ExecuteInsertReturnId(conn, trans,
                    @"INSERT INTO DeliveryNote
                      (deliveryNoteCode, customerID, SalesOrderID, staffID, WarehouseID,
                       shipMethod, trackingNumber, status, remark)
                      VALUES (@code, @customerID, @soID, @staffID, @whID,
                              @shipMethod, @tracking, @status, @remark)",
                    new[]
                    {
                        new MySqlParameter("@code", note.DeliveryNoteCode),
                        new MySqlParameter("@customerID", note.CustomerID),
                        new MySqlParameter("@soID", note.SalesOrderID),
                        new MySqlParameter("@staffID", note.StaffID),
                        new MySqlParameter("@whID", note.WarehouseID),
                        new MySqlParameter("@shipMethod", note.ShipMethod ?? ""),
                        new MySqlParameter("@tracking", note.TrackingNumber ?? ""),
                        new MySqlParameter("@status", note.Status),
                        new MySqlParameter("@remark", note.Remark ?? (object)DBNull.Value)
                    });

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    "UPDATE DeliveryNote SET deliveryNoteCode = @code WHERE deliveryNoteID = @id",
                    new[]
                    {
                        new MySqlParameter("@code", "DN-" + newId),
                        new MySqlParameter("@id", newId)
                    });

                ReplaceLines(conn, trans, newId, lines);
                return newId;
            });
        }

        public bool UpdateWithLines(DeliveryNote note, IEnumerable<(long ProductId, int ShipQty)> lines)
        {
            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        @"UPDATE DeliveryNote
                          SET customerID=@customerID, salesOrderID=@soID, staffID=@staffID, warehouseID=@whID,
                              shipMethod=@shipMethod, trackingNumber=@tracking, status=@status, remark=@remark,
                              lastModifyDate=NOW()
                          WHERE deliveryNoteID=@id",
                        new[]
                        {
                            new MySqlParameter("@customerID", note.CustomerID),
                            new MySqlParameter("@soID", note.SalesOrderID),
                            new MySqlParameter("@staffID", note.StaffID),
                            new MySqlParameter("@whID", note.WarehouseID),
                            new MySqlParameter("@shipMethod", note.ShipMethod ?? ""),
                            new MySqlParameter("@tracking", note.TrackingNumber ?? ""),
                            new MySqlParameter("@status", note.Status),
                            new MySqlParameter("@remark", note.Remark ?? (object)DBNull.Value),
                            new MySqlParameter("@id", note.DeliveryNoteID)
                        });

                    if (lines != null && !IsDeliveryConfirmed(note.Status))
                        ReplaceLines(conn, trans, note.DeliveryNoteID, lines);

                    return 0L;
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsDeliveryConfirmed(int status) => status >= 3;

        public bool HasInvoiceLines(long deliveryNoteId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM InvoiceLine WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            return value != null && value != DBNull.Value && Convert.ToInt32(value) > 0;
        }

        public bool DeleteLines(long deliveryNoteId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "DELETE FROM DeliveryProductLine WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            return true;
        }

        public bool ReplaceLines(long deliveryNoteId, IEnumerable<(long ProductId, int ShipQty)> lines)
        {
            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    ReplaceLines(conn, trans, deliveryNoteId, lines);
                    return 0L;
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceLines(MySqlConnection conn, MySqlTransaction trans, long deliveryNoteId,
            IEnumerable<(long ProductId, int ShipQty)> lines)
        {
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "DELETE FROM DeliveryProductLine WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });

            if (lines == null) return;
            foreach (var line in lines)
            {
                if (line.ProductId <= 0 || line.ShipQty <= 0) continue;
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO DeliveryProductLine (deliveryNoteID, productID, shipQuantity)
                      VALUES (@dnId, @productId, @qty)",
                    new[]
                    {
                        new MySqlParameter("@dnId", deliveryNoteId),
                        new MySqlParameter("@productId", line.ProductId),
                        new MySqlParameter("@qty", line.ShipQty)
                    });
            }
        }

        public DataTable GetDeliveryLines(long deliveryNoteId)
        {
            return GetDeliveryLinesDetailed(deliveryNoteId);
        }

        public DataTable GetDeliveryLinesDetailed(long deliveryNoteId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  p.category AS 'Category',
                                  p.styleNumber AS 'Style Number',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  dpl.shipQuantity AS 'Ship Qty',
                                  spl.orderQuantity AS 'Order Qty',
                                  spl.shippedQuantity AS 'SO Shipped Qty'
                           FROM DeliveryProductLine dpl
                           INNER JOIN Product p ON dpl.productID = p.productID
                           INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                           LEFT JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = dn.salesOrderID AND spl.productID = dpl.productID
                           WHERE dpl.deliveryNoteID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public DataTable GetExportProductLines(long deliveryNoteId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  p.styleNumber AS 'Style',
                                  p.category AS 'Category',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  COALESCE(spl.price, 0) AS 'Unit Price',
                                  dpl.shipQuantity AS 'Ship Qty',
                                  CASE WHEN COALESCE(spl.orderQuantity, 0) > 0
                                       THEN ROUND(spl.discountAmount * dpl.shipQuantity / spl.orderQuantity, 2)
                                       ELSE 0 END AS 'Discount',
                                  (COALESCE(spl.price, 0) * dpl.shipQuantity
                                   - CASE WHEN COALESCE(spl.orderQuantity, 0) > 0
                                          THEN spl.discountAmount * dpl.shipQuantity / spl.orderQuantity
                                          ELSE 0 END) AS 'Amount'
                           FROM DeliveryProductLine dpl
                           INNER JOIN Product p ON dpl.productID = p.productID
                           INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                           LEFT JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = dn.salesOrderID AND spl.productID = dpl.productID
                           WHERE dpl.deliveryNoteID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public decimal GetTotalAmount(long deliveryNoteId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(
                      COALESCE(spl.price, 0) * dpl.shipQuantity
                      - CASE WHEN COALESCE(spl.orderQuantity, 0) > 0
                             THEN spl.discountAmount * dpl.shipQuantity / spl.orderQuantity
                             ELSE 0 END), 0)
                  FROM DeliveryProductLine dpl
                  INNER JOIN DeliveryNote dn ON dpl.deliveryNoteID = dn.deliveryNoteID
                  LEFT JOIN SalesOrderProductLine spl
                       ON spl.salesOrderID = dn.salesOrderID AND spl.productID = dpl.productID
                  WHERE dpl.deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        public int GetTotalShipQty(long deliveryNoteId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COALESCE(SUM(shipQuantity), 0) FROM DeliveryProductLine WHERE deliveryNoteID = @id",
                new[] { new MySqlParameter("@id", deliveryNoteId) });
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        public DataTable GetLineEditorData(long salesOrderId, long deliveryNoteId = 0)
        {
            string sql = @"SELECT spl.productID AS 'ProductID',
                                  p.productCode AS 'Product',
                                  spl.orderQuantity AS 'Order Qty',
                                  spl.shippedQuantity AS 'Shipped Qty',
                                  GREATEST(spl.orderQuantity - spl.shippedQuantity
                                      + CASE WHEN @dnId > 0 THEN COALESCE(dpl.shipQuantity, 0) ELSE 0 END, 0) AS 'Remaining Qty',
                                  COALESCE(dpl.shipQuantity, 0) AS 'Ship Qty'
                           FROM SalesOrderProductLine spl
                           INNER JOIN Product p ON spl.productID = p.productID
                           LEFT JOIN DeliveryProductLine dpl
                                ON dpl.deliveryNoteID = @dnId AND dpl.productID = spl.productID
                           WHERE spl.salesOrderID = @soId
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@soId", salesOrderId),
                new MySqlParameter("@dnId", deliveryNoteId)
            });
        }

        public DataTable GetHeaderDetail(long deliveryNoteId)
        {
            string sql = @"SELECT dn.deliveryNoteCode AS 'Delivery Note Code',
                                  c.customerName AS 'Customer',
                                  so.salesOrderCode AS 'Sales Order',
                                  so.customerRefNumber AS 'Customer Ref Number',
                                  w.warehouseName AS 'Warehouse',
                                  dn.shipMethod AS 'Ship Method',
                                  dn.trackingNumber AS 'Tracking Number',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  dn.createDate AS 'Create Date',
                                  dn.status AS 'Status',
                                  dn.remark AS 'Remark'
                           FROM DeliveryNote dn
                           LEFT JOIN SalesOrder so ON dn.salesOrderID = so.salesOrderID
                           LEFT JOIN Customer c ON dn.customerID = c.customerID
                           LEFT JOIN Warehouse w ON dn.warehouseID = w.warehouseID
                           LEFT JOIN Staff st ON dn.staffID = st.staffID
                           WHERE dn.deliveryNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public DeliveryNote GetById(long id)
        {
            string sql = @"SELECT deliveryNoteID, deliveryNoteCode, customerID, salesOrderID, staffID, warehouseID,
                                  shipMethod, trackingNumber, status, remark
                           FROM DeliveryNote WHERE deliveryNoteID=@id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new DeliveryNote
            {
                DeliveryNoteID = Convert.ToInt64(row["deliveryNoteID"]),
                DeliveryNoteCode = row["deliveryNoteCode"]?.ToString(),
                CustomerID = Convert.ToInt64(row["customerID"]),
                SalesOrderID = Convert.ToInt64(row["salesOrderID"]),
                StaffID = Convert.ToInt64(row["staffID"]),
                WarehouseID = row["warehouseID"] == DBNull.Value ? 0 : Convert.ToInt64(row["warehouseID"]),
                ShipMethod = row["shipMethod"] == DBNull.Value ? null : row["shipMethod"].ToString(),
                TrackingNumber = row["trackingNumber"] == DBNull.Value ? null : row["trackingNumber"].ToString(),
                Status = Convert.ToInt32(row["status"]),
                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public bool Update(DeliveryNote note)
        {
            string sql = @"UPDATE DeliveryNote
                           SET customerID=@customerID, salesOrderID=@soID, staffID=@staffID, warehouseID=@whID,
                               shipMethod=@shipMethod, trackingNumber=@tracking, status=@status, remark=@remark, lastModifyDate=NOW()
                           WHERE deliveryNoteID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@customerID", note.CustomerID),
                new MySqlParameter("@soID", note.SalesOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@whID", note.WarehouseID),
                new MySqlParameter("@shipMethod", note.ShipMethod ?? (object)DBNull.Value),
                new MySqlParameter("@tracking", note.TrackingNumber ?? (object)DBNull.Value),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)DBNull.Value),
                new MySqlParameter("@id", note.DeliveryNoteID)
            }) > 0;
        }

        public bool InsertProductLine(long deliveryNoteId, long productId, int shipQuantity)
        {
            return DatabaseConnect.ExecuteNonQuery(
                @"INSERT INTO DeliveryProductLine (deliveryNoteID, productID, shipQuantity)
                  VALUES (@dnId, @productId, @qty)",
                new[]
                {
                    new MySqlParameter("@dnId", deliveryNoteId),
                    new MySqlParameter("@productId", productId),
                    new MySqlParameter("@qty", shipQuantity)
                }) > 0;
        }

        public DataTable GetProductLinesInternal(long deliveryNoteId)
        {
            string sql = @"SELECT productID, shipQuantity
                           FROM DeliveryProductLine
                           WHERE deliveryNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        public DataTable GetLinesForInvoicing(long deliveryNoteId)
        {
            string sql = @"
                SELECT dpl.productID AS 'Product ID',
                       p.productCode AS 'Product Code',
                       dpl.shipQuantity AS 'Ship Qty',
                       COALESCE((
                            SELECT SUM(il.invoiceQuantity)
                            FROM InvoiceLine il
                            WHERE il.deliveryNoteID = dpl.deliveryNoteID
                              AND il.productID = dpl.productID
                       ), 0) AS 'Already Invoiced Qty',
                       (dpl.shipQuantity - COALESCE((
                            SELECT SUM(il.invoiceQuantity)
                            FROM InvoiceLine il
                            WHERE il.deliveryNoteID = dpl.deliveryNoteID
                              AND il.productID = dpl.productID
                       ), 0)) AS 'Remaining Qty'
                FROM DeliveryProductLine dpl
                INNER JOIN Product p ON dpl.productID = p.productID
                WHERE dpl.deliveryNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", deliveryNoteId) });
        }

        /// <summary>Completed delivery notes that still have quantity available to invoice (for Finance picker).</summary>
        public DataTable GetDeliveryNotesForInvoicingPicker()
        {
            string sql = @"SELECT dn.deliveryNoteID AS 'Delivery Note ID',
                                  dn.deliveryNoteCode AS 'Delivery Note Code',
                                  c.customerName AS 'Customer',
                                  so.salesOrderCode AS 'Sales Order',
                                  dn.createDate AS 'Create Date'
                           FROM DeliveryNote dn
                           LEFT JOIN Customer c ON dn.customerID = c.customerID
                           LEFT JOIN SalesOrder so ON dn.salesOrderID = so.salesOrderID
                           WHERE dn.deliveryNoteCode <> @depositCode
                             AND dn.status >= 3
                             AND EXISTS (
                                 SELECT 1 FROM DeliveryProductLine dpl
                                 WHERE dpl.deliveryNoteID = dn.deliveryNoteID
                                   AND dpl.shipQuantity > COALESCE((
                                       SELECT SUM(il.invoiceQuantity)
                                       FROM InvoiceLine il
                                       WHERE il.deliveryNoteID = dpl.deliveryNoteID
                                         AND il.productID = dpl.productID
                                   ), 0))
                           ORDER BY dn.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@depositCode", DepositDeliveryNoteCode)
            });
            if (dt == null) return dt;
            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                string code = row["Delivery Note Code"]?.ToString() ?? "";
                string customer = row["Customer"]?.ToString() ?? "";
                string so = row["Sales Order"]?.ToString() ?? "";
                string part = string.IsNullOrWhiteSpace(so)
                    ? customer
                    : $"{customer} — {so}";
                row["DisplayText"] = string.IsNullOrWhiteSpace(part) ? code : $"{code} — {part}";
            }
            return dt;
        }
    }
}
