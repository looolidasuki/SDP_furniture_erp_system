using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class ProductController
    {
        public DataTable GetAllProducts()
        {
            string sql = @"SELECT productID AS 'Product ID',
                                  productCode AS 'Product Code',
                                  category AS 'Category',
                                  styleNumber AS 'Style Number',
                                  size AS 'Size',
                                  color AS 'Color',
                                  basePriceByCurrency AS 'Base Price',
                                  unit AS 'Unit',
                                  status AS 'Status'
                           FROM Product
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetAllProductsWithStock(decimal defaultMinStock = 5)
        {
            string sql = @"SELECT p.productID AS 'Product ID',
                                  p.productCode AS 'Product Code',
                                  p.category AS 'Category',
                                  p.styleNumber AS 'Style Number',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.basePriceByCurrency AS 'Base Price',
                                  p.unit AS 'Unit',
                                  p.status AS 'Status',
                                  COALESCE(st.totalPhysical, 0) AS 'Total Stock',
                                  COALESCE(st.totalAvailable, 0) AS 'Available Stock',
                                  @minStock AS 'Min Stock Level'
                           FROM Product p
                           LEFT JOIN (
                               SELECT productID,
                                      SUM(physicalQuantity) AS totalPhysical,
                                      SUM(GREATEST(physicalQuantity - reservedQuantity, 0)) AS totalAvailable
                               FROM WarehouseProduct
                               GROUP BY productID
                           ) st ON p.productID = st.productID
                           ORDER BY COALESCE(st.totalAvailable, 0) ASC, p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@minStock", defaultMinStock)
            });
        }

        public DataTable GetProductsForPicker()
        {
            return GetAllProducts();
        }

        public Product GetById(long productId)
        {
            string sql = @"SELECT productID, productCode, category, styleNumber, size, color, basePriceByCurrency, unit, status, remark, productImage
                           FROM Product WHERE productID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Product
            {
                ProductID = System.Convert.ToInt64(row["productID"]),
                ProductCode = row["productCode"]?.ToString(),
                Category = row["category"]?.ToString(),
                StyleNumber = row["styleNumber"]?.ToString(),
                Size = row["size"]?.ToString(),
                Color = row["color"]?.ToString(),
                BasePriceByCurrency = row["basePriceByCurrency"] == System.DBNull.Value ? 0 : System.Convert.ToDecimal(row["basePriceByCurrency"]),
                Unit = row["unit"]?.ToString(),
                Status = row["status"] == System.DBNull.Value ? 0 : System.Convert.ToInt32(row["status"]),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString(),
                ProductImage = row["productImage"] == System.DBNull.Value ? null : (byte[])row["productImage"]
            };
        }

        public long Insert(Product product)
        {
            // 1. 修正邏輯：確保 currencyID 和 staffID 不為 0
            long validCurrencyID = (product.CurrencyID <= 0) ? 1 : product.CurrencyID;
            long validStaffID = (product.StaffID <= 0) ? 1 : product.StaffID;

            string sql = @"INSERT INTO Product
        (productCode, category, sequenceNumber, styleNumber, size, color,
         basePriceByCurrency, currencyID, staffID, unit, status, remark)
        VALUES (@code, @category, @seq, @style, @size, @color,
                @price, @currencyID, @staffID, @unit, @status, @remark)";

            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
        new MySqlParameter("@code", product.ProductCode),
        new MySqlParameter("@category", product.Category),
        new MySqlParameter("@seq", product.SequenceNumber ?? (object)System.DBNull.Value),
        new MySqlParameter("@style", product.StyleNumber),
        new MySqlParameter("@size", product.Size),
        new MySqlParameter("@color", product.Color),
        new MySqlParameter("@price", product.BasePriceByCurrency),
        new MySqlParameter("@currencyID", validCurrencyID), // 使用修正後的變數
        new MySqlParameter("@staffID", validStaffID),       // 這裡必須改為 validStaffID
        new MySqlParameter("@unit", product.Unit),
        new MySqlParameter("@status", product.Status),
        new MySqlParameter("@remark", product.Remark ?? (object)System.DBNull.Value)
    });
        }

        public bool Update(Product product)
        {
            string sql = @"UPDATE Product SET productCode=@code, category=@category, styleNumber=@style,
                           size=@size, color=@color, basePriceByCurrency=@price, unit=@unit, status=@status, remark=@remark,
                           productImage=@image
                           WHERE productID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@code", product.ProductCode ?? ""),
                new MySqlParameter("@category", product.Category ?? (object)System.DBNull.Value),
                new MySqlParameter("@style", product.StyleNumber ?? (object)System.DBNull.Value),
                new MySqlParameter("@size", product.Size ?? (object)System.DBNull.Value),
                new MySqlParameter("@color", product.Color ?? (object)System.DBNull.Value),
                new MySqlParameter("@price", product.BasePriceByCurrency),
                new MySqlParameter("@unit", product.Unit ?? (object)System.DBNull.Value),
                new MySqlParameter("@status", product.Status),
                new MySqlParameter("@remark", product.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@image", product.ProductImage ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", product.ProductID)
            }) > 0;
        }

        public DataTable GetBomLines(long productId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material',
                                  prml.rawMaterialNeedQty AS 'Need Qty'
                           FROM ProductRawMaterialLine prml
                           INNER JOIN RawMaterial rm ON prml.rawMaterialID = rm.rawMaterialID
                           WHERE prml.productID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productId) });
        }

        public int GetCount()
        {
            string sql = "SELECT COUNT(*) FROM Product";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                return System.Convert.ToInt32(dt.Rows[0][0]);
            }
            return 0;
        }
    }
}
