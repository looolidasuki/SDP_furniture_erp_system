using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class RawMaterialRequestNoteController
    {
        public DataTable GetAllRequestNotes()
        {
            string sql = @"SELECT n.rawMaterialRequestNoteID AS 'Request Note ID',
                                  n.rawMaterialRequestNoteCode AS 'Request Code',
                                  n.ProductionOrderID AS 'Production Order ID',
                                  po.productionOrderCode AS 'Production Order',
                                  CASE n.status
                                      WHEN 0 THEN 'Draft'
                                      WHEN 1 THEN 'Partially Issued'
                                      WHEN 2 THEN 'Completed'
                                      WHEN 3 THEN 'Cancelled'
                                      ELSE CAST(n.status AS CHAR)
                                  END AS 'Status',
                                  n.createDate AS 'Create Date',
                                  n.requestDate AS 'Request Date'
                           FROM RawMaterialRequestNote n
                           LEFT JOIN ProductionOrder po ON n.ProductionOrderID = po.productionOrderID
                           ORDER BY n.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetOpenRequestNotesForPicker()
        {
            string sql = @"SELECT n.rawMaterialRequestNoteID AS 'Request Note ID',
                                  n.rawMaterialRequestNoteCode AS 'Request Code',
                                  n.ProductionOrderID AS 'Production Order ID',
                                  po.productionOrderCode AS 'Production Order',
                                  COALESCE(n.status, 0) AS 'Status',
                                  CONCAT(n.rawMaterialRequestNoteCode, ' — ', COALESCE(po.productionOrderCode, ''),
                                         ' [', CASE COALESCE(n.status, 0)
                                                  WHEN 0 THEN 'Draft'
                                                  WHEN 1 THEN 'Partial'
                                                  ELSE CAST(n.status AS CHAR)
                                              END, ']') AS DisplayText
                           FROM RawMaterialRequestNote n
                           LEFT JOIN ProductionOrder po ON n.ProductionOrderID = po.productionOrderID
                           WHERE COALESCE(n.status, 0) NOT IN (@completed, @cancelled)
                             AND EXISTS (
                                 SELECT 1
                                 FROM RawMaterialRequestNoteRawMaterial_line rl
                                 WHERE rl.rawMaterialRequestNoteID = n.rawMaterialRequestNoteID
                             )
                           ORDER BY n.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@completed", RawMaterialRequestNoteConstants.StatusCompleted),
                new MySqlParameter("@cancelled", RawMaterialRequestNoteConstants.StatusCancelled)
            });
        }

        public DataTable GetOpenProductionOrdersForIssuePicker()
        {
            string sql = @"SELECT DISTINCT po.productionOrderID AS 'Production Order ID',
                                  po.productionOrderCode AS 'Production Order Code',
                                  CONCAT(po.productionOrderCode,
                                         CASE WHEN so.salesOrderCode IS NOT NULL AND so.salesOrderCode <> ''
                                              THEN CONCAT(' — ', so.salesOrderCode) ELSE '' END) AS DisplayText
                           FROM RawMaterialRequestNote n
                           INNER JOIN ProductionOrder po ON n.ProductionOrderID = po.productionOrderID
                           LEFT JOIN SalesOrder so ON po.salesOrderID = so.salesOrderID
                           WHERE COALESCE(n.status, 0) NOT IN (@completed, @cancelled)
                             AND EXISTS (
                                 SELECT 1
                                 FROM RawMaterialRequestNoteRawMaterial_line rl
                                 WHERE rl.rawMaterialRequestNoteID = n.rawMaterialRequestNoteID
                             )
                           ORDER BY po.productionOrderID DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@completed", RawMaterialRequestNoteConstants.StatusCompleted),
                new MySqlParameter("@cancelled", RawMaterialRequestNoteConstants.StatusCancelled)
            });
        }

        public RawMaterialRequestNote GetById(long id)
        {
            string sql = @"SELECT rawMaterialRequestNoteID, rawMaterialRequestNoteCode, ProductionOrderID,
                                  staffID, createDate, requestDate, status, remark
                           FROM RawMaterialRequestNote
                           WHERE rawMaterialRequestNoteID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new RawMaterialRequestNote
            {
                RawMaterialRequestNoteID = Convert.ToInt64(row["rawMaterialRequestNoteID"]),
                RawMaterialRequestNoteCode = row["rawMaterialRequestNoteCode"]?.ToString(),
                ProductionOrderID = Convert.ToInt64(row["ProductionOrderID"]),
                StaffID = Convert.ToInt64(row["staffID"]),
                CreateDate = row["createDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["createDate"]),
                RequestDate = row["requestDate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(row["requestDate"]),
                Status = row["status"] == DBNull.Value ? 0 : Convert.ToInt32(row["status"]),
                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public long Insert(RawMaterialRequestNote note)
        {
            long id = DatabaseConnect.ExecuteInTransaction((conn, trans) => InsertInTransaction(conn, trans, note));
            if (id > 0)
                DocumentAuditService.LogCreate(DocumentAuditService.Types.RawMaterialRequest, id, DocumentCodeHelper.Build("SCR", id));
            return id;
        }

        public long InsertInTransaction(MySqlConnection conn, MySqlTransaction trans, RawMaterialRequestNote note)
        {
            long nextId = DatabaseConnect.AllocateNextId("rawmaterialrequestnote", "rawMaterialRequestNoteID", conn, trans);
            if (nextId <= 0)
                throw new InvalidOperationException("Unable to allocate RM request note ID.");

            var parameters = BuildInsertParameters(note);
            var paramList = new List<MySqlParameter>(parameters) { new MySqlParameter("@id", nextId) };
            DatabaseConnect.ExecuteNonQuery(conn, trans, BuildInsertSql(), paramList.ToArray());
            return nextId;
        }

        public long FindOpenIdByCode(string code)
        {
            code = DocumentCodeHelper.NormalizeScrCode(code);
            if (string.IsNullOrWhiteSpace(code)) return 0;

            string sql = @"SELECT rawMaterialRequestNoteID
                           FROM RawMaterialRequestNote
                           WHERE rawMaterialRequestNoteCode = @code
                             AND COALESCE(status, 0) NOT IN (@completed, @cancelled)
                           LIMIT 1";
            var scalar = DatabaseConnect.ExecuteScalar(sql, new[]
            {
                new MySqlParameter("@code", code),
                new MySqlParameter("@completed", RawMaterialRequestNoteConstants.StatusCompleted),
                new MySqlParameter("@cancelled", RawMaterialRequestNoteConstants.StatusCancelled)
            });
            if (scalar == null || scalar == DBNull.Value) return 0;
            return Convert.ToInt64(scalar);
        }

        private static long AllocateNextId(MySqlConnection conn, MySqlTransaction trans)
        {
            var scalar = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COALESCE(MAX(rawMaterialRequestNoteID), 0) + 1 FROM RawMaterialRequestNote");
            return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);
        }

        private static string BuildInsertSql() =>
            @"INSERT INTO RawMaterialRequestNote
              (rawMaterialRequestNoteID, rawMaterialRequestNoteCode, ProductionOrderID, staffID, requestDate, remark, status)
              VALUES (@id, @code, @poID, @staffID, @requestDate, @remark, @noteStatus)";

        private static MySqlParameter[] BuildInsertParameters(RawMaterialRequestNote note) =>
            new[]
            {
                new MySqlParameter("@code", note.RawMaterialRequestNoteCode ?? "SCR-TEMP"),
                new MySqlParameter("@poID", note.ProductionOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@requestDate", note.RequestDate == default ? DateTime.Today : note.RequestDate),
                new MySqlParameter("@remark", string.IsNullOrWhiteSpace(note.Remark) ? (object)DBNull.Value : note.Remark.Trim()),
                CreateStatusParameter(ResolveStatus(note.Status))
            };

        private static int ResolveStatus(int status) =>
            status >= RawMaterialRequestNoteConstants.StatusDraft
                && status <= RawMaterialRequestNoteConstants.StatusCancelled
                ? status
                : RawMaterialRequestNoteConstants.StatusDraft;

        private static MySqlParameter CreateStatusParameter(int status) =>
            new MySqlParameter("@noteStatus", MySqlDbType.Int32) { Value = status };

        public bool InsertLine(long noteId, long productId, long rawMaterialId, decimal quantity)
        {
            if (noteId <= 0 || productId <= 0 || rawMaterialId <= 0 || quantity <= 0) return false;
            return DatabaseConnect.ExecuteNonQuery(
                @"INSERT INTO RawMaterialRequestNoteRawMaterial_line
                  (rawMaterialRequestNoteID, productID, rawMaterialID, rawMaterialRequestQuantity)
                  VALUES (@noteId, @productId, @rmId, @qty)",
                new[]
                {
                    new MySqlParameter("@noteId", noteId),
                    new MySqlParameter("@productId", productId),
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@qty", quantity)
                }) > 0;
        }

        public bool UpdateStatus(long noteId, int status)
        {
            bool ok = DatabaseConnect.ExecuteNonQuery(
                "UPDATE RawMaterialRequestNote SET status = @noteStatus WHERE rawMaterialRequestNoteID = @id",
                new[]
                {
                    CreateStatusParameter(ResolveStatus(status)),
                    new MySqlParameter("@id", noteId)
                }) > 0;
            if (ok)
                DocumentAuditService.LogStatus(DocumentAuditService.Types.RawMaterialRequest, noteId,
                    DocumentCodeHelper.Build("SCR", noteId), status);
            return ok;
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE RawMaterialRequestNote SET rawMaterialRequestNoteCode = @code WHERE rawMaterialRequestNoteID = @id",
                new[] {
                    new MySqlParameter("@code", DocumentCodeHelper.Build("SCR", id)),
                    new MySqlParameter("@id", id)
                });
        }

        public DataTable GetAggregatedRequestQuantities(long noteId)
        {
            string sql = @"SELECT rl.rawMaterialID,
                                  rm.rawMaterialCode,
                                  SUM(rl.rawMaterialRequestQuantity) AS totalQty
                           FROM RawMaterialRequestNoteRawMaterial_line rl
                           INNER JOIN RawMaterial rm ON rl.rawMaterialID = rm.rawMaterialID
                           WHERE rl.rawMaterialRequestNoteID = @id
                           GROUP BY rl.rawMaterialID, rm.rawMaterialCode
                           ORDER BY rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", noteId) });
        }

        public DataTable GetRequestLines(long noteId)
        {
            string sql = @"SELECT p.productCode AS 'Product', rm.rawMaterialCode AS 'Raw Material',
                                  rl.rawMaterialRequestQuantity AS 'Request Qty'
                           FROM RawMaterialRequestNoteRawMaterial_line rl
                           INNER JOIN RawMaterial rm ON rl.rawMaterialID = rm.rawMaterialID
                           INNER JOIN Product p ON rl.productID = p.productID
                           WHERE rl.rawMaterialRequestNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", noteId) });
        }

        public DataTable GetHeaderDetail(long noteId)
        {
            string sql = @"SELECT n.rawMaterialRequestNoteCode AS 'Request Code',
                                  po.productionOrderCode AS 'Production Order',
                                  CASE n.status
                                      WHEN 0 THEN 'Draft'
                                      WHEN 1 THEN 'Partially Issued'
                                      WHEN 2 THEN 'Completed'
                                      WHEN 3 THEN 'Cancelled'
                                      ELSE CAST(n.status AS CHAR)
                                  END AS 'Status',
                                  n.createDate AS 'Create Date',
                                  n.requestDate AS 'Request Date',
                                  n.remark AS 'Remark'
                           FROM RawMaterialRequestNote n
                           LEFT JOIN ProductionOrder po ON n.ProductionOrderID = po.productionOrderID
                           WHERE n.rawMaterialRequestNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", noteId) });
        }
    }
}
