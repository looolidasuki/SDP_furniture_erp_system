using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class RawMaterialController
    {
        public DataTable GetAllRawMaterials()
        {
            string sql = @"SELECT rawMaterialID AS 'Raw Material ID',
                                  rawMaterialCode AS 'Raw Material Code',
                                  category AS 'Category',
                                  size AS 'Size',
                                  color AS 'Color',
                                  minimumStockLevel AS 'Min Stock',
                                  status AS 'Status'
                           FROM RawMaterial
                           ORDER BY rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetAllRawMaterialsWithStock()
        {
            string sql = @"SELECT rm.rawMaterialID AS 'Raw Material ID',
                                  rm.rawMaterialCode AS 'Raw Material Code',
                                  rm.category AS 'Category',
                                  rm.size AS 'Size',
                                  rm.color AS 'Color',
                                  COALESCE(st.totalPhysical, 0) AS 'Current Stock',
                                  rm.minimumStockLevel AS 'Min Stock',
                                  rm.status AS 'Status'
                           FROM RawMaterial rm
                           LEFT JOIN (
                               SELECT rawMaterialID, SUM(physicalQuantity) AS totalPhysical
                               FROM RawMaterialWarehouse
                               GROUP BY rawMaterialID
                           ) st ON rm.rawMaterialID = st.rawMaterialID
                           ORDER BY COALESCE(st.totalPhysical, 0) ASC, rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(RawMaterial material)
        {
            string sql = @"INSERT INTO RawMaterial
                (rawMaterialCode, category, SequenceNumber, size, color, minimumStockLevel, status)
                VALUES (@code, @category, @seq, @size, @color, @minStock, @status)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", material.RawMaterialCode),
                new MySqlParameter("@category", material.Category),
                new MySqlParameter("@seq", material.SequenceNumber ?? (object)System.DBNull.Value),
                new MySqlParameter("@size", material.Size),
                new MySqlParameter("@color", material.Color),
                new MySqlParameter("@minStock", material.MinimumStockLevel),
                new MySqlParameter("@status", material.Status)
            });
        }

        public DataTable GetAllSupplierQuotes()
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material',
                                  s.supplierName AS 'Supplier',
                                  rms.basePrice AS 'Base Price',
                                  rms.unit AS 'Unit',
                                  rms.minimumOrderQuantity AS 'MOQ',
                                  rms.quoteDate AS 'Quote Date',
                                  rms.status AS 'Status'
                           FROM RawMaterialSupplier rms
                           INNER JOIN RawMaterial rm ON rms.rawMaterialID = rm.rawMaterialID
                           INNER JOIN Supplier s ON rms.supplierID = s.supplierID
                           ORDER BY rm.rawMaterialCode, s.supplierName";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetSupplierQuotesByMaterial(long rawMaterialId)
        {
            string sql = @"SELECT s.supplierName AS 'Supplier', rms.basePrice AS 'Price',
                                  rms.unit AS 'Unit', rms.status AS 'Status'
                           FROM RawMaterialSupplier rms
                           INNER JOIN Supplier s ON rms.supplierID = s.supplierID
                           WHERE rms.rawMaterialID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", rawMaterialId) });
        }

        public RawMaterial GetById(long id)
        {
            string sql = "SELECT * FROM RawMaterial WHERE rawMaterialID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new RawMaterial
            {
                RawMaterialID = System.Convert.ToInt64(row["rawMaterialID"]),
                RawMaterialCode = row["rawMaterialCode"]?.ToString(),
                Category = row["category"]?.ToString(),
                Size = row["size"]?.ToString(),
                Color = row["color"]?.ToString(),
                MinimumStockLevel = row["minimumStockLevel"] != System.DBNull.Value ? System.Convert.ToInt32(row["minimumStockLevel"]) : 0,
                Status = row["status"] != System.DBNull.Value ? System.Convert.ToInt32(row["status"]) : 0
            };
        }

        public void Update(RawMaterial material)
        {
            string sql = @"UPDATE RawMaterial SET
                rawMaterialCode = @code, category = @category, size = @size,
                color = @color, minimumStockLevel = @minStock, status = @status
                WHERE rawMaterialID = @id";
            DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@code", material.RawMaterialCode),
                new MySqlParameter("@category", material.Category ?? (object)System.DBNull.Value),
                new MySqlParameter("@size", material.Size ?? (object)System.DBNull.Value),
                new MySqlParameter("@color", material.Color ?? (object)System.DBNull.Value),
                new MySqlParameter("@minStock", material.MinimumStockLevel),
                new MySqlParameter("@status", material.Status),
                new MySqlParameter("@id", material.RawMaterialID)
            });
        }
    }
}
