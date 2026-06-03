using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class RawMaterialRequestNoteController
    {
        public DataTable GetAllRequestNotes()
        {
            string sql = @"SELECT rawMaterialRequestNoteID AS 'Request Note ID',
                                  rawMaterialRequestNoteCode AS 'Request Code',
                                  ProductionOrderID AS 'Production Order ID',
                                  createDate AS 'Create Date',
                                  requestDate AS 'Request Date'
                           FROM RawMaterialRequestNote
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(RawMaterialRequestNote note)
        {
            string sql = @"INSERT INTO RawMaterialRequestNote
                (rawMaterialRequestNoteCode, ProductionOrderID, staffID, requestDate, remark)
                VALUES (@code, @poID, @staffID, @requestDate, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", note.RawMaterialRequestNoteCode),
                new MySqlParameter("@poID", note.ProductionOrderID),
                new MySqlParameter("@staffID", note.StaffID),
                new MySqlParameter("@requestDate", note.RequestDate),
                new MySqlParameter("@remark", note.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE RawMaterialRequestNote SET rawMaterialRequestNoteCode = @code WHERE rawMaterialRequestNoteID = @id",
                new[] {
                    new MySqlParameter("@code", "RMR-" + id),
                    new MySqlParameter("@id", id)
                });
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
                                  n.createDate AS 'Create Date',
                                  n.requestDate AS 'Request Date',
                                  n.remark AS 'Remark'
                           FROM RawMaterialRequestNote n
                           LEFT JOIN ProductionOrder po ON n.productionOrderID = po.productionOrderID
                           WHERE n.rawMaterialRequestNoteID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", noteId) });
        }
    }
}
