using System;
using FurnitureERP.Helpers;
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
            if (warehouse == null || string.IsNullOrWhiteSpace(warehouse.WarehouseName))
                throw new InvalidOperationException("Warehouse name is required.");

            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                long id = AllocateNextWarehouseId(conn, trans);
                if (id <= 0)
                    throw new InvalidOperationException("Unable to allocate warehouse ID.");

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO Warehouse (warehouseID, warehouseName, warehouseAddress)
                      VALUES (@id, @name, @address)",
                    new[]
                    {
                        new MySqlParameter("@id", id),
                        new MySqlParameter("@name", warehouse.WarehouseName.Trim()),
                        new MySqlParameter("@address", string.IsNullOrWhiteSpace(warehouse.WarehouseAddress)
                            ? (object)DBNull.Value
                            : warehouse.WarehouseAddress.Trim())
                    });

                return id;
            });
        }

        public void InitializeStockRecords(long warehouseId, string warehouseName)
        {
            if (warehouseId <= 0) return;

            bool isInventory = WarehouseHelper.IsInventoryWarehouse(warehouseId, warehouseName);
            bool isProduction = WarehouseHelper.IsProductionWarehouse(warehouseId, warehouseName);

            if (isInventory)
            {
                DatabaseConnect.ExecuteNonQuery(
                    @"INSERT INTO RawMaterialWarehouse (rawMaterialID, warehouseID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      SELECT rm.rawMaterialID, @whId, 0, 0, 0
                      FROM RawMaterial rm
                      WHERE NOT EXISTS (
                          SELECT 1 FROM RawMaterialWarehouse x
                          WHERE x.rawMaterialID = rm.rawMaterialID AND x.warehouseID = @whId
                      )",
                    new[] { new MySqlParameter("@whId", warehouseId) });

                DatabaseConnect.ExecuteNonQuery(
                    @"INSERT INTO WarehouseProduct (warehouseID, productID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      SELECT @whId, p.productID, 0, 0, 0
                      FROM Product p
                      WHERE NOT EXISTS (
                          SELECT 1 FROM WarehouseProduct x
                          WHERE x.warehouseID = @whId AND x.productID = p.productID
                      )",
                    new[] { new MySqlParameter("@whId", warehouseId) });
            }
            else if (isProduction)
            {
                DatabaseConnect.ExecuteNonQuery(
                    @"INSERT INTO RawMaterialWarehouse (rawMaterialID, warehouseID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      SELECT DISTINCT bom.rawMaterialID, @whId, 0, 0, 0
                      FROM ProductRawMaterialLine bom
                      WHERE NOT EXISTS (
                          SELECT 1 FROM RawMaterialWarehouse x
                          WHERE x.rawMaterialID = bom.rawMaterialID AND x.warehouseID = @whId
                      )",
                    new[] { new MySqlParameter("@whId", warehouseId) });
            }
        }

        public long GetPairedProductionWarehouseId(long inventoryWarehouseId)
        {
            long legacy = WarehouseHelper.GetPairedProductionWarehouse(inventoryWarehouseId);
            if (legacy > 0)
            {
                var legacyWh = GetById(legacy);
                if (legacyWh != null)
                    return legacy;
            }

            var inv = GetById(inventoryWarehouseId);
            if (inv == null || !WarehouseHelper.IsInventoryWarehouse(inventoryWarehouseId, inv.WarehouseName))
                return 0;

            string prefix = WarehouseHelper.ExtractRegionPrefix(inv.WarehouseName);
            if (string.IsNullOrWhiteSpace(prefix))
                return 0;

            string sql = @"SELECT warehouseID
                           FROM Warehouse
                           WHERE warehouseName LIKE @prefix
                             AND warehouseName LIKE '%Production%'
                           ORDER BY warehouseID
                           LIMIT 1";
            var scalar = DatabaseConnect.ExecuteScalar(sql, new[]
            {
                new MySqlParameter("@prefix", prefix + "%")
            });
            if (scalar == null || scalar == DBNull.Value) return 0;
            return Convert.ToInt64(scalar);
        }

        public long GetPairedInventoryWarehouseId(long productionWarehouseId)
        {
            long legacy = WarehouseHelper.GetPairedInventoryWarehouse(productionWarehouseId);
            if (legacy > 0)
            {
                var legacyWh = GetById(legacy);
                if (legacyWh != null)
                    return legacy;
            }

            var prod = GetById(productionWarehouseId);
            if (prod == null || !WarehouseHelper.IsProductionWarehouse(productionWarehouseId, prod.WarehouseName))
                return 0;

            string prefix = WarehouseHelper.ExtractRegionPrefix(prod.WarehouseName);
            if (string.IsNullOrWhiteSpace(prefix))
                return 0;

            string sql = @"SELECT warehouseID
                           FROM Warehouse
                           WHERE warehouseName LIKE @prefix
                             AND warehouseName LIKE '%Inventory%'
                           ORDER BY warehouseID
                           LIMIT 1";
            var scalar = DatabaseConnect.ExecuteScalar(sql, new[]
            {
                new MySqlParameter("@prefix", prefix + "%")
            });
            if (scalar == null || scalar == DBNull.Value) return 0;
            return Convert.ToInt64(scalar);
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
                                  p.category AS 'Category',
                                  wp.physicalQuantity AS 'Physical Qty',
                                  wp.reservedQuantity AS 'Reserved',
                                  GREATEST(wp.physicalQuantity - wp.reservedQuantity, 0) AS 'Available Qty',
                                  wp.purchasedQuantity AS 'Purchased',
                                  @minStock AS 'Min Stock Level'
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

        public DataTable GetWarehouseRawMaterials(long warehouseId)
        {
            string sql = @"SELECT rm.rawMaterialCode AS 'Raw Material Code',
                                  rm.category AS 'Category',
                                  rm.size AS 'Size',
                                  rm.color AS 'Color',
                                  rw.physicalQuantity AS 'Physical Qty',
                                  rw.reservedQuantity AS 'Reserved',
                                  GREATEST(rw.physicalQuantity - rw.reservedQuantity, 0) AS 'Available Qty',
                                  rw.purchasedQuantity AS 'Purchased',
                                  GREATEST(rw.physicalQuantity - rw.reservedQuantity, 0) + rw.purchasedQuantity AS 'Net Available',
                                  rm.minimumStockLevel AS 'Min Stock Level'
                           FROM RawMaterialWarehouse rw
                           INNER JOIN RawMaterial rm ON rw.rawMaterialID = rm.rawMaterialID
                           WHERE rw.warehouseID = @id
                           ORDER BY GREATEST(rw.physicalQuantity - rw.reservedQuantity, 0) ASC, rm.rawMaterialCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", warehouseId) });
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

        private static long AllocateNextWarehouseId(MySqlConnection conn, MySqlTransaction trans)
        {
            var scalar = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COALESCE(MAX(warehouseID), 0) + 1 FROM Warehouse");
            return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);
        }
    }
}
