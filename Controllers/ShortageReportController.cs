using MySql.Data.MySqlClient;
using System.Data;

namespace Sales_user.Controllers
{
    public class ShortageReportController
    {
        public DataTable GetAllReports()
        {
            string sql = @"SELECT shortageReportID AS 'Shortage Report ID',
                                  shortageReportCode AS 'Shortage Report Code',
                                  date AS 'Date',
                                  sequenceNumber AS 'Sequence',
                                  createDate AS 'Create Date'
                           FROM ShortageReport
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetHeaderDetail(long shortageReportId)
        {
            string sql = @"SELECT sr.shortageReportCode AS 'Shortage Report Code',
                                  sr.date AS 'Date',
                                  sr.sequenceNumber AS 'Sequence',
                                  sr.createDate AS 'Create Date'
                           FROM ShortageReport sr
                           WHERE sr.shortageReportID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", shortageReportId) });
        }

        public DataTable GetLines(long shortageReportId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material Code',
                                  rm.category AS 'Category',
                                  rm.size AS 'Size',
                                  rm.color AS 'Color',
                                  w.warehouseName AS 'Warehouse',
                                  srl.totalShortageQuantity AS 'Total Shortage Qty'
                           FROM RawMaterialShortageReportLine srl
                           INNER JOIN RawMaterial rm ON srl.rawMaterialID = rm.rawMaterialID
                           INNER JOIN Warehouse w ON srl.WarehousewarehouseID = w.warehouseID
                           WHERE srl.shortageReportID = @id
                           ORDER BY rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", shortageReportId) });
        }
    }
}

