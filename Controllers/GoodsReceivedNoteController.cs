using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class GoodsReceivedNoteController
    {
        public DataTable GetGrnsByPurchaseOrder(long purchaseOrderId)
        {
            string sql = @"SELECT grn.goodsReceivedNoteID AS 'GRN ID',
                                  grn.goodsReceivedNoteCode AS 'GRN Code',
                                  grn.createDate AS 'Create Date',
                                  grn.status AS 'Status',
                                  grn.remark AS 'Remark'
                           FROM GoodsReceivedNote grn
                           WHERE grn.PurchaseOrderID = @poId
                           ORDER BY grn.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@poId", purchaseOrderId) });
        }

        public DataTable GetAllGoodsReceivedNotes()
        {
            string sql = @"SELECT grn.goodsReceivedNoteCode AS 'GRN Code',
                                  po.purchaseOrderCode AS 'Purchase Order',
                                  s.supplierName AS 'Supplier',
                                  grn.createDate AS 'Create Date',
                                  grn.status AS 'Status',
                                  grn.remark AS 'Remark',
                                  grn.goodsReceivedNoteID AS 'GRN ID'
                           FROM GoodsReceivedNote grn
                           LEFT JOIN Supplier s ON grn.supplierID = s.supplierID
                           LEFT JOIN PurchaseOrder po ON grn.PurchaseOrderID = po.purchaseOrderID
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
            return GetReceivedLinesDetailed(grnId);
        }

        public DataTable GetReceivedLinesDetailed(long grnId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material',
                                  COALESCE(pol.orderQuantity, 0) AS 'Order Qty',
                                  COALESCE(pol.receivedQuantity, 0) AS 'PO Received Qty',
                                  grl.receivedQuantity AS 'Received Qty',
                                  CASE
                                    WHEN grn.status >= 2 THEN GREATEST(COALESCE(pol.orderQuantity, 0) - COALESCE(pol.receivedQuantity, 0), 0)
                                    ELSE GREATEST(
                                        COALESCE(pol.orderQuantity, 0) - COALESCE(pol.receivedQuantity, 0) - grl.receivedQuantity,
                                        0)
                                  END AS 'Remaining Need'
                           FROM GoodsReceivedNoteRawMaterialLine grl
                           INNER JOIN GoodsReceivedNote grn ON grl.goodsReceivedNoteID = grn.goodsReceivedNoteID
                           INNER JOIN RawMaterial rm ON grl.rawMaterialID = rm.rawMaterialID
                           LEFT JOIN PurchaseOrderRawMaterialLine pol
                                ON pol.purchaseOrderID = grn.PurchaseOrderID
                               AND pol.rawMaterialID = grl.rawMaterialID
                           WHERE grl.goodsReceivedNoteID = @id
                           ORDER BY rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", grnId) });
        }

        public DataTable GetHeaderDetail(long grnId)
        {
            string sql = @"SELECT grn.goodsReceivedNoteCode AS 'GRN Code',
                                  po.purchaseOrderCode AS 'Purchase Order',
                                  po.requestDeliveryDate AS 'PO Request Delivery Date',
                                  po.status AS 'PO Status',
                                  s.supplierName AS 'Supplier',
                                  s.contactPerson AS 'Supplier Contact',
                                  s.phone AS 'Supplier Phone',
                                  s.billingAddress AS 'Supplier Address',
                                  s.paymentTerm AS 'Supplier Payment Terms',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Received By',
                                  grn.createDate AS 'Create Date',
                                  grn.status AS 'GRN Status',
                                  grn.remark AS 'Remark'
                           FROM GoodsReceivedNote grn
                           LEFT JOIN Supplier s ON grn.supplierID = s.supplierID
                           LEFT JOIN PurchaseOrder po ON grn.PurchaseOrderID = po.purchaseOrderID
                           LEFT JOIN Staff st ON grn.staffID = st.staffID
                           WHERE grn.goodsReceivedNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", grnId) });
        }

        public bool ExistsByCode(string code, long excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string sql = @"SELECT COUNT(*) FROM GoodsReceivedNote
                           WHERE goodsReceivedNoteCode = @code AND goodsReceivedNoteID <> @excludeId";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@code", code.Trim()),
                new MySqlParameter("@excludeId", excludeId)
            });
            if (dt == null || dt.Rows.Count == 0) return false;
            return System.Convert.ToInt32(dt.Rows[0][0]) > 0;
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

        public bool DeleteLines(long grnId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "DELETE FROM GoodsReceivedNoteRawMaterialLine WHERE goodsReceivedNoteID = @id",
                new[] { new MySqlParameter("@id", grnId) });
            return true;
        }

        public bool ReplaceLines(long grnId, System.Collections.Generic.IEnumerable<(long RawMaterialID, decimal ReceivedQty)> lines)
        {
            DeleteLines(grnId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                if (line.RawMaterialID <= 0) continue;
                InsertRawMaterialLine(grnId, line.RawMaterialID, line.ReceivedQty);
                hasAny = true;
            }
            return hasAny;
        }
    }
}
