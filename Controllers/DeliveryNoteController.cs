using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class DeliveryNoteController
    {
        public DataTable GetAllDeliveryNotes()
        {
            string sql = @"SELECT deliveryNoteID AS 'Delivery Note ID',
                                  deliveryNoteCode AS 'Delivery Note Code',
                                  customerID AS 'Customer ID',
                                  SalesOrderID AS 'Sales Order ID',
                                  trackingNumber AS 'Tracking Number',
                                  createDate AS 'Create Date',
                                  status AS 'Status'
                           FROM DeliveryNote
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
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
                new MySqlParameter("@shipMethod", note.ShipMethod),
                new MySqlParameter("@tracking", note.TrackingNumber),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)System.DBNull.Value)
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

        public DataTable GetDeliveryLines(long deliveryNoteId)
        {
            string sql = @"SELECT p.productCode AS 'Product', dpl.shipQuantity AS 'Ship Qty'
                           FROM DeliveryProductLine dpl
                           INNER JOIN Product p ON dpl.productID = p.productID
                           WHERE dpl.deliveryNoteID = @id";
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
                DeliveryNoteID = System.Convert.ToInt64(row["deliveryNoteID"]),
                DeliveryNoteCode = row["deliveryNoteCode"]?.ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                WarehouseID = row["warehouseID"] == System.DBNull.Value ? 0 : System.Convert.ToInt64(row["warehouseID"]),
                ShipMethod = row["shipMethod"] == System.DBNull.Value ? null : row["shipMethod"].ToString(),
                TrackingNumber = row["trackingNumber"] == System.DBNull.Value ? null : row["trackingNumber"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
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
                new MySqlParameter("@shipMethod", note.ShipMethod ?? (object)System.DBNull.Value),
                new MySqlParameter("@tracking", note.TrackingNumber ?? (object)System.DBNull.Value),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)System.DBNull.Value),
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
    }
}
