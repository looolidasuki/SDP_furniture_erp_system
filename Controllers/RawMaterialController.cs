using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sales_user.Controllers
{
    public class RawMaterialController
    {
        public DataTable GetAllRawMaterials()
        {
            string sql = @"SELECT rawMaterialCode AS 'Raw Material Code',
                                  category AS 'Category',
                                  size AS 'Size',
                                  color AS 'Color',
                                  minimumStockLevel AS 'Min Stock',
                                  status AS 'Status',
                                  rawMaterialID AS 'Raw Material ID'
                           FROM RawMaterial
                           ORDER BY rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetAllRawMaterialsWithStock()
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material Code',
                                  rm.category AS 'Category',
                                  rm.size AS 'Size',
                                  rm.color AS 'Color',
                                  COALESCE(st.totalPhysical, 0) AS 'Current Stock',
                                  rm.minimumStockLevel AS 'Min Stock',
                                  rm.status AS 'Status',
                                  rm.rawMaterialID AS 'Raw Material ID'
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
                (rawMaterialID, rawMaterialCode, category, SequenceNumber, size, color, minimumStockLevel, status)
                VALUES (@id, @code, @category, @seq, @size, @color, @minStock, @status)";
            return DatabaseConnect.InsertWithAllocatedId("rawmaterial", "rawMaterialID", sql, new[] {
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
            string sql = @"SELECT rms.supplierID AS 'Supplier ID',
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
                           INNER JOIN Supplier s ON rms.supplierID = s.supplierID
                           LEFT JOIN Currency c ON rms.currencyID = c.currencyID
                           WHERE rms.rawMaterialID = @id
                           ORDER BY s.supplierName";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", rawMaterialId) });
        }

        public IList<RawMaterialSupplierLine> GetSupplierQuoteLines(long rawMaterialId)
        {
            var lines = new List<RawMaterialSupplierLine>();
            if (rawMaterialId <= 0) return lines;

            string sql = @"SELECT rms.supplierID, rms.supplierStyleNumber, rms.basePrice, rms.currencyID,
                                  rms.unit, rms.minimumOrderQuantity, rms.quoteDate, rms.status
                           FROM RawMaterialSupplier rms
                           WHERE rms.rawMaterialID = @id
                           ORDER BY rms.supplierID";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", rawMaterialId) });
            if (dt == null) return lines;

            foreach (DataRow row in dt.Rows)
            {
                lines.Add(new RawMaterialSupplierLine
                {
                    SupplierId = System.Convert.ToInt64(row["supplierID"]),
                    SupplierStyleNumber = row["supplierStyleNumber"] == System.DBNull.Value
                        ? null : row["supplierStyleNumber"]?.ToString(),
                    BasePrice = System.Convert.ToDecimal(row["basePrice"]),
                    CurrencyId = row["currencyID"] == System.DBNull.Value ? 1 : System.Convert.ToInt64(row["currencyID"]),
                    Unit = row["unit"]?.ToString(),
                    MinimumOrderQuantity = row["minimumOrderQuantity"] == System.DBNull.Value
                        ? 1 : System.Convert.ToInt32(row["minimumOrderQuantity"]),
                    QuoteDate = row["quoteDate"] == System.DBNull.Value
                        ? (DateTime?)null : System.Convert.ToDateTime(row["quoteDate"]),
                    Status = row["status"] == System.DBNull.Value ? 1 : System.Convert.ToInt32(row["status"])
                });
            }

            return lines;
        }

        public long InsertWithSupplierQuotes(RawMaterial material, IList<RawMaterialSupplierLine> lines)
        {
            if (material == null) throw new System.ArgumentNullException(nameof(material));

            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                string sql = @"INSERT INTO RawMaterial
                    (rawMaterialID, rawMaterialCode, category, SequenceNumber, size, color, minimumStockLevel, status)
                    VALUES (@id, @code, @category, @seq, @size, @color, @minStock, @status)";
                long id = DatabaseConnect.InsertWithAllocatedId(conn, trans, "rawmaterial", "rawMaterialID", sql, new[]
                {
                    new MySqlParameter("@code", material.RawMaterialCode),
                    new MySqlParameter("@category", material.Category ?? (object)System.DBNull.Value),
                    new MySqlParameter("@seq", material.SequenceNumber ?? (object)System.DBNull.Value),
                    new MySqlParameter("@size", material.Size ?? (object)System.DBNull.Value),
                    new MySqlParameter("@color", material.Color ?? (object)System.DBNull.Value),
                    new MySqlParameter("@minStock", material.MinimumStockLevel),
                    new MySqlParameter("@status", material.Status)
                });
                ReplaceSupplierQuotesInTransaction(conn, trans, id, lines);
                return id;
            });
        }

        public void UpdateWithSupplierQuotes(RawMaterial material, IList<RawMaterialSupplierLine> lines)
        {
            if (material == null) throw new System.ArgumentNullException(nameof(material));
            if (material.RawMaterialID <= 0) throw new System.ArgumentException("Raw material ID is required.");

            DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                string sql = @"UPDATE RawMaterial SET
                    rawMaterialCode = @code, category = @category, size = @size,
                    color = @color, minimumStockLevel = @minStock, status = @status
                    WHERE rawMaterialID = @id";
                DatabaseConnect.ExecuteNonQuery(conn, trans, sql, new[]
                {
                    new MySqlParameter("@code", material.RawMaterialCode),
                    new MySqlParameter("@category", material.Category ?? (object)System.DBNull.Value),
                    new MySqlParameter("@size", material.Size ?? (object)System.DBNull.Value),
                    new MySqlParameter("@color", material.Color ?? (object)System.DBNull.Value),
                    new MySqlParameter("@minStock", material.MinimumStockLevel),
                    new MySqlParameter("@status", material.Status),
                    new MySqlParameter("@id", material.RawMaterialID)
                });
                ReplaceSupplierQuotesInTransaction(conn, trans, material.RawMaterialID, lines);
                return 0;
            });
        }

        public void ReplaceSupplierQuotes(long rawMaterialId, IList<RawMaterialSupplierLine> lines)
        {
            if (rawMaterialId <= 0) throw new System.ArgumentException("Raw material ID is required.");
            DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                ReplaceSupplierQuotesInTransaction(conn, trans, rawMaterialId, lines);
                return 0;
            });
        }

        private static void ReplaceSupplierQuotesInTransaction(
            MySqlConnection conn,
            MySqlTransaction trans,
            long rawMaterialId,
            IList<RawMaterialSupplierLine> lines)
        {
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "DELETE FROM RawMaterialSupplier WHERE rawMaterialID = @id",
                new[] { new MySqlParameter("@id", rawMaterialId) });

            if (lines == null) return;
            foreach (var line in lines.Where(l => l != null && l.SupplierId > 0))
            {
                InsertSupplierQuoteInTransaction(conn, trans, rawMaterialId, line);
            }
        }

        private static void InsertSupplierQuoteInTransaction(
            MySqlConnection conn,
            MySqlTransaction trans,
            long rawMaterialId,
            RawMaterialSupplierLine line)
        {
            string sql = @"INSERT INTO RawMaterialSupplier
                (rawMaterialID, supplierID, supplierStyleNumber, basePrice, currencyID, unit,
                 minimumOrderQuantity, quoteDate, status)
                VALUES (@rmId, @suppId, @style, @price, @currencyId, @unit, @minQty, @quoteDate, @status)";
            DatabaseConnect.ExecuteNonQuery(conn, trans, sql, new[]
            {
                new MySqlParameter("@rmId", rawMaterialId),
                new MySqlParameter("@suppId", line.SupplierId),
                new MySqlParameter("@style", string.IsNullOrWhiteSpace(line.SupplierStyleNumber)
                    ? (object)System.DBNull.Value : line.SupplierStyleNumber.Trim()),
                new MySqlParameter("@price", line.BasePrice),
                new MySqlParameter("@currencyId", line.CurrencyId > 0 ? line.CurrencyId : 1),
                new MySqlParameter("@unit", string.IsNullOrWhiteSpace(line.Unit) ? "piece" : line.Unit.Trim()),
                new MySqlParameter("@minQty", line.MinimumOrderQuantity > 0 ? line.MinimumOrderQuantity : 1),
                new MySqlParameter("@quoteDate", line.QuoteDate.HasValue
                    ? (object)line.QuoteDate.Value.Date : System.DBNull.Value),
                new MySqlParameter("@status", line.Status)
            });
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

        public RawMaterialWarehouseSnapshot GetWarehouseStockSnapshot(long rawMaterialId, long warehouseId)
        {
            var material = GetById(rawMaterialId);
            var snap = new RawMaterialWarehouseSnapshot
            {
                MinimumStockLevel = material?.MinimumStockLevel ?? 0
            };

            var dt = DatabaseConnect.ExecuteQuery(
                @"SELECT physicalQuantity, reservedQuantity, purchasedQuantity
                  FROM RawMaterialWarehouse
                  WHERE rawMaterialID = @rmId AND warehouseID = @whId",
                new[]
                {
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@whId", warehouseId)
                });

            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                snap.Physical = row["physicalQuantity"] == System.DBNull.Value ? 0 : System.Convert.ToDecimal(row["physicalQuantity"]);
                snap.Reserved = row["reservedQuantity"] == System.DBNull.Value ? 0 : System.Convert.ToDecimal(row["reservedQuantity"]);
                snap.Purchased = row["purchasedQuantity"] == System.DBNull.Value ? 0 : System.Convert.ToDecimal(row["purchasedQuantity"]);
            }

            return snap;
        }

        public RawMaterialPreferredSupplier GetPreferredSupplierQuote(long rawMaterialId)
        {
            if (rawMaterialId <= 0) return null;
            string sql = @"SELECT rms.supplierID, rms.basePrice
                           FROM RawMaterialSupplier rms
                           WHERE rms.rawMaterialID = @id AND rms.status = 1
                           ORDER BY rms.quoteDate DESC, rms.lastModify DESC
                           LIMIT 1";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", rawMaterialId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new RawMaterialPreferredSupplier
            {
                SupplierId = System.Convert.ToInt64(row["supplierID"]),
                BasePrice = System.Convert.ToDecimal(row["basePrice"])
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
