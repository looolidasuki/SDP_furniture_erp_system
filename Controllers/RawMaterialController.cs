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
                                  rms.supplierStyleNumber AS 'Supplier Style Number',
                                  rms.basePrice AS 'Base Price',
                                  c.currencyCode AS 'Currency',
                                  rms.unit AS 'Unit',
                                  rms.minimumOrderQuantity AS 'Minimum Order Qty',
                                  rms.quoteDate AS 'Quote Date',
                                  rms.lastModify AS 'Last Modify',
                                  rms.status AS 'Status'
                           FROM RawMaterialSupplier rms
                           INNER JOIN RawMaterial rm ON rms.rawMaterialID = rm.rawMaterialID
                           INNER JOIN Supplier s ON rms.supplierID = s.supplierID
                           LEFT JOIN Currency c ON rms.currencyID = c.currencyID
                           ORDER BY rm.rawMaterialCode, s.supplierName";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        /// <summary>Raw materials with active supplier quotes — for PO line combo.</summary>
        public DataTable GetQuotedRawMaterialsForSupplier(long supplierId)
        {
            if (supplierId <= 0) return BuildEmptyQuotedRawMaterialPickerTable();
            string sql = @"SELECT rm.rawMaterialID AS 'Raw Material ID',
                                  rm.rawMaterialCode AS 'Raw Material Code',
                                  rms.basePrice AS 'Quote Price',
                                  rms.unit AS 'Unit',
                                  rms.minimumOrderQuantity AS 'Min Order Qty'
                           FROM RawMaterialSupplier rms
                           INNER JOIN RawMaterial rm ON rms.rawMaterialID = rm.rawMaterialID
                           WHERE rms.supplierID = @suppId AND rms.status = 1
                           ORDER BY rm.rawMaterialCode";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@suppId", supplierId)
            });
            return DecorateQuotedRawMaterialPickerTable(dt);
        }

        public static DataTable BuildEmptyQuotedRawMaterialPickerTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Raw Material ID", typeof(long));
            dt.Columns.Add("Raw Material Code", typeof(string));
            dt.Columns.Add("Quote Price", typeof(decimal));
            dt.Columns.Add("Unit", typeof(string));
            dt.Columns.Add("Min Order Qty", typeof(int));
            dt.Columns.Add("DisplayText", typeof(string));
            return dt;
        }

        private static DataTable DecorateQuotedRawMaterialPickerTable(DataTable dt)
        {
            if (dt == null) return BuildEmptyQuotedRawMaterialPickerTable();
            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                string code = row["Raw Material Code"]?.ToString() ?? "";
                string unit = row.Table.Columns.Contains("Unit") ? row["Unit"]?.ToString() : "";
                string display = string.IsNullOrWhiteSpace(unit) ? code : $"{code} ({unit})";
                row["DisplayText"] = display;
            }
            return dt;
        }

        /// <summary>Supplier quote for a raw material (RawMaterialSupplier.basePrice).</summary>
        public RawMaterialSupplierQuote TryGetSupplierQuote(long rawMaterialId, long supplierId)
        {
            if (rawMaterialId <= 0 || supplierId <= 0) return null;
            string sql = @"SELECT rms.basePrice, rms.minimumOrderQuantity, rms.unit, c.currencyCode
                           FROM RawMaterialSupplier rms
                           LEFT JOIN Currency c ON rms.currencyID = c.currencyID
                           WHERE rms.rawMaterialID = @rmId AND rms.supplierID = @suppId AND rms.status = 1
                           LIMIT 1";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@rmId", rawMaterialId),
                new MySqlParameter("@suppId", supplierId)
            });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new RawMaterialSupplierQuote
            {
                BasePrice = System.Convert.ToDecimal(row["basePrice"]),
                MinimumOrderQuantity = row["minimumOrderQuantity"] == System.DBNull.Value
                    ? 1 : System.Convert.ToInt32(row["minimumOrderQuantity"]),
                Unit = row["unit"]?.ToString(),
                CurrencyCode = row["currencyCode"]?.ToString()
            };
        }

        public DataTable GetSupplierQuotesByMaterial(long rawMaterialId)
        {
            string sql = @"SELECT s.supplierName AS 'Supplier',
                                  rms.supplierStyleNumber AS 'Supplier Style Number',
                                  rms.basePrice AS 'Base Price',
                                  c.currencyCode AS 'Currency',
                                  rms.unit AS 'Unit',
                                  rms.minimumOrderQuantity AS 'Minimum Order Qty',
                                  rms.quoteDate AS 'Quote Date',
                                  rms.lastModify AS 'Last Modify',
                                  rms.status AS 'Status'
                           FROM RawMaterialSupplier rms
                           INNER JOIN Supplier s ON rms.supplierID = s.supplierID
                           LEFT JOIN Currency c ON rms.currencyID = c.currencyID
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
