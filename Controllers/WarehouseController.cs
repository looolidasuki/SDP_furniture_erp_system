using System;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;

namespace Sales_user.Controllers
{
    public class WarehouseController
    {
        public DataTable GetAllWarehouses()
        {
            string sql = @"SELECT warehouseID AS 'Warehouse ID',
                                  warehouseName AS 'Warehouse Name',
                                  warehouseAddress AS 'Address'
                           FROM Warehouse
                           ORDER BY warehouseName";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(Warehouse warehouse)
        {
            string sql = @"INSERT INTO Warehouse (warehouseName, warehouseAddress)
                           VALUES (@name, @address)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@name", warehouse.WarehouseName),
                new MySqlParameter("@address", warehouse.WarehouseAddress)
            });
        }

        public Warehouse GetById(long id)
        {
            var dt = DatabaseConnect.ExecuteQuery(
                "SELECT warehouseID, warehouseName, warehouseAddress FROM Warehouse WHERE warehouseID = @id",
                new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Warehouse
            {
                WarehouseID = Convert.ToInt64(row["warehouseID"]),
                WarehouseName = row["warehouseName"]?.ToString(),
                WarehouseAddress = row["warehouseAddress"]?.ToString()
            };
        }

        public void Update(Warehouse warehouse)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE Warehouse SET warehouseName = @name, warehouseAddress = @address WHERE warehouseID = @id",
                new[] {
                    new MySqlParameter("@name", warehouse.WarehouseName),
                    new MySqlParameter("@address", warehouse.WarehouseAddress ?? (object)System.DBNull.Value),
                    new MySqlParameter("@id", warehouse.WarehouseID)
                });
        }

        public DataTable GetWarehouseProducts(long warehouseId, decimal defaultMinStock = 5)
        {
            string sql = @"SELECT p.productCode AS 'Product Code',
                                  wp.physicalQuantity AS 'Physical Qty',
                                  wp.reservedQuantity AS 'Reserved',
                                  GREATEST(wp.physicalQuantity - wp.reservedQuantity, 0) AS 'Available Qty',
                                  @minStock AS 'Min Stock Level',
                                  wp.purchasedQuantity AS 'Purchased'
                           FROM WarehouseProduct wp
                           INNER JOIN Product p ON wp.productID = p.productID
                           WHERE wp.warehouseID = @id
                           ORDER BY GREATEST(wp.physicalQuantity - wp.reservedQuantity, 0) ASC, p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@id", warehouseId),
                new MySqlParameter("@minStock", defaultMinStock)
            });
        }

        public DataTable GetTransferableRawMaterials(long warehouseId)
        {
            string sql = @"SELECT rw.rawMaterialID AS 'Item ID',
                                  rm.rawMaterialCode AS 'Item Code',
                                  rm.category AS 'Category',
                                  rw.physicalQuantity AS 'Physical Qty',
                                  rw.reservedQuantity AS 'Reserved',
                                  GREATEST(rw.physicalQuantity - rw.reservedQuantity, 0) AS 'Available Qty'
                           FROM RawMaterialWarehouse rw
                           INNER JOIN RawMaterial rm ON rw.rawMaterialID = rm.rawMaterialID
                           WHERE rw.warehouseID = @id
                             AND GREATEST(rw.physicalQuantity - rw.reservedQuantity, 0) > 0
                           ORDER BY rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", warehouseId) });
        }

        public DataTable GetTransferableProducts(long warehouseId)
        {
            string sql = @"SELECT wp.productID AS 'Item ID',
                                  p.productCode AS 'Item Code',
                                  p.category AS 'Category',
                                  wp.physicalQuantity AS 'Physical Qty',
                                  wp.reservedQuantity AS 'Reserved',
                                  GREATEST(wp.physicalQuantity - wp.reservedQuantity, 0) AS 'Available Qty'
                           FROM WarehouseProduct wp
                           INNER JOIN Product p ON wp.productID = p.productID
                           WHERE wp.warehouseID = @id
                             AND GREATEST(wp.physicalQuantity - wp.reservedQuantity, 0) > 0
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", warehouseId) });
        }
    }
}
