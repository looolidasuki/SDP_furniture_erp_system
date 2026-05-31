using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class GoodsReceivedNoteController
    {
        public DataTable GetAllGoodsReceivedNotes()
        {
            string sql = @"SELECT grn.goodsReceivedNoteID AS 'GRN ID',
                                  grn.goodsReceivedNoteCode AS 'GRN Code',
                                  s.supplierName AS 'Supplier',
                                  grn.PurchaseOrderID AS 'Purchase Order ID',
                                  grn.createDate AS 'Create Date',
                                  grn.status AS 'Status'
                           FROM GoodsReceivedNote grn
                           LEFT JOIN Supplier s ON grn.supplierID = s.supplierID
                           ORDER BY grn.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(GoodsReceivedNote note)
        {
            string sql = @"INSERT INTO GoodsReceivedNote
                (goodsReceivedNoteCode, supplierID, PurchaseOrderID, staffID, status, remark)
                VALUES (@code, @supplierID, @poID, @staffID, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", note.GoodsReceivedNoteCode),
                new MySqlParameter("@supplierID", note.SupplierID),
                new MySqlParameter("@poID", note.PurchaseOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE GoodsReceivedNote SET goodsReceivedNoteCode = @code WHERE goodsReceivedNoteID = @id",
                new[] {
                    new MySqlParameter("@code", "GRN-" + id),
                    new MySqlParameter("@id", id)
                });
        }

        public DataTable GetReceivedLines(long grnId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material', grl.receivedQuantity AS 'Received Qty'
                           FROM GoodsReceivedNoteRawMaterialLine grl
                           INNER JOIN RawMaterial rm ON grl.rawMaterialID = rm.rawMaterialID
                           WHERE grl.goodsReceivedNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", grnId) });
        }

        public GoodsReceivedNote GetById(long id)
        {
            string sql = @"SELECT goodsReceivedNoteID, goodsReceivedNoteCode, supplierID, purchaseOrderID, staffID, status, remark
                           FROM GoodsReceivedNote WHERE goodsReceivedNoteID=@id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new GoodsReceivedNote
            {
                GoodsReceivedNoteID = System.Convert.ToInt64(row["goodsReceivedNoteID"]),
                GoodsReceivedNoteCode = row["goodsReceivedNoteCode"]?.ToString(),
                SupplierID = System.Convert.ToInt64(row["supplierID"]),
                PurchaseOrderID = System.Convert.ToInt64(row["purchaseOrderID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                Status = System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public bool Update(GoodsReceivedNote note)
        {
            string sql = @"UPDATE GoodsReceivedNote
                           SET supplierID=@supplierID, purchaseOrderID=@poID, staffID=@staffID, status=@status, remark=@remark, lastModifyDate=NOW()
                           WHERE goodsReceivedNoteID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@supplierID", note.SupplierID),
                new MySqlParameter("@poID", note.PurchaseOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@status", note.Status),
                new MySqlParameter("@remark", note.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", note.GoodsReceivedNoteID)
            }) > 0;
        }

        public bool InsertRawMaterialLine(long grnId, long rawMaterialId, decimal receivedQty)
        {
            return DatabaseConnect.ExecuteNonQuery(
                @"INSERT INTO GoodsReceivedNoteRawMaterialLine (goodsReceivedNoteID, rawMaterialID, receivedQuantity)
                  VALUES (@grnId, @rmId, @qty)",
                new[]
                {
                    new MySqlParameter("@grnId", grnId),
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@qty", receivedQty)
                }) > 0;
        }

        public DataTable GetRawMaterialLinesInternal(long grnId)
        {
            string sql = @"SELECT rawMaterialID, receivedQuantity
                           FROM GoodsReceivedNoteRawMaterialLine
                           WHERE goodsReceivedNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", grnId) });
        }
    }
}
