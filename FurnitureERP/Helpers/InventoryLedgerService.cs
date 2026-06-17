using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class InventoryLedgerService
    {
        public const int ItemTypeRawMaterial = 1;
        public const int ItemTypeProduct = 2;

        public class Actions
        {
            public const string GrnReceive = "GRN Receive";
            public const string DeliveryShip = "Delivery Ship";
            public const string TransferOut = "Transfer Out";
            public const string TransferIn = "Transfer In";
            public const string ProductionIn = "Production In";
            public const string ProductionOut = "Production Out";
            public const string MaterialIssue = "Material Issue";
            public const string Adjustment = "Adjustment";
        }

        public static void LogPhysicalChange(
            MySqlConnection conn,
            MySqlTransaction trans,
            int itemType,
            long itemId,
            long warehouseId,
            decimal qtyDelta,
            string action,
            string documentType = null,
            long documentId = 0,
            string documentCode = null,
            string remark = null)
        {
            if (itemId <= 0 || warehouseId <= 0 || qtyDelta == 0 || string.IsNullOrWhiteSpace(action))
                return;

            try
            {
                decimal balanceAfter = ReadPhysicalBalance(conn, trans, itemType, itemId, warehouseId);
                long staffId = AppSession.IsLoggedIn && AppSession.CurrentUser != null
                    ? AppSession.CurrentUser.StaffID : 0;
                string staffName = AppSession.IsLoggedIn && AppSession.CurrentUser != null
                    ? AppSession.CurrentUser.FullName : "System";

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO inventoryledger
                      (itemType, itemID, warehouseID, qtyDelta, balanceAfter,
                       documentType, documentID, documentCode, action, staffID, staffName, remark, actionDate)
                      VALUES (@itemType, @itemId, @whId, @delta, @balance,
                              @docType, @docId, @docCode, @action, @staffId, @staffName, @remark, NOW())",
                    new[]
                    {
                        new MySqlParameter("@itemType", itemType),
                        new MySqlParameter("@itemId", itemId),
                        new MySqlParameter("@whId", warehouseId),
                        new MySqlParameter("@delta", qtyDelta),
                        new MySqlParameter("@balance", balanceAfter),
                        new MySqlParameter("@docType", string.IsNullOrWhiteSpace(documentType) ? (object)DBNull.Value : documentType.Trim()),
                        new MySqlParameter("@docId", documentId > 0 ? (object)documentId : DBNull.Value),
                        new MySqlParameter("@docCode", string.IsNullOrWhiteSpace(documentCode) ? (object)DBNull.Value : documentCode.Trim()),
                        new MySqlParameter("@action", action.Trim()),
                        new MySqlParameter("@staffId", staffId > 0 ? (object)staffId : DBNull.Value),
                        new MySqlParameter("@staffName", staffName ?? "System"),
                        new MySqlParameter("@remark", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark.Trim())
                    });
            }
            catch
            {
                // Ledger is best-effort; must not break stock transactions.
            }
        }

        private static decimal ReadPhysicalBalance(MySqlConnection conn, MySqlTransaction trans, int itemType, long itemId, long warehouseId)
        {
            string sql = itemType == ItemTypeProduct
                ? @"SELECT COALESCE(physicalQuantity, 0) FROM WarehouseProduct
                    WHERE productID = @id AND warehouseID = @whId"
                : @"SELECT COALESCE(physicalQuantity, 0) FROM RawMaterialWarehouse
                    WHERE rawMaterialID = @id AND warehouseID = @whId";

            object value = DatabaseConnect.ExecuteScalar(conn, trans, sql,
                new[]
                {
                    new MySqlParameter("@id", itemId),
                    new MySqlParameter("@whId", warehouseId)
                });
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        public static System.Data.DataTable GetRecentEntries(int limit = 200, long warehouseId = 0, int itemType = 0)
        {
            limit = SqlGuard.ClampLimit(limit, 1000);
            var conditions = new System.Collections.Generic.List<string>();
            var parameters = new System.Collections.Generic.List<MySqlParameter>
            {
                new MySqlParameter("@lim", limit)
            };

            if (warehouseId > 0)
            {
                conditions.Add("l.warehouseID = @whId");
                parameters.Add(new MySqlParameter("@whId", warehouseId));
            }
            if (itemType > 0)
            {
                conditions.Add("l.itemType = @itemType");
                parameters.Add(new MySqlParameter("@itemType", itemType));
            }

            string where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            string sql = $@"
                SELECT l.actionDate AS 'Date',
                       CASE l.itemType WHEN 1 THEN 'Raw Material' WHEN 2 THEN 'Product' ELSE l.itemType END AS 'Item Type',
                       COALESCE(rm.rawMaterialCode, p.productCode, l.itemID) AS 'Item',
                       w.warehouseName AS 'Warehouse',
                       l.qtyDelta AS 'Qty Change',
                       l.balanceAfter AS 'Balance After',
                       l.action AS 'Action',
                       l.documentType AS 'Document Type',
                       l.documentCode AS 'Document Code',
                       l.staffName AS 'Staff',
                       l.remark AS 'Remark'
                FROM inventoryledger l
                LEFT JOIN RawMaterial rm ON l.itemType = 1 AND rm.rawMaterialID = l.itemID
                LEFT JOIN Product p ON l.itemType = 2 AND p.productID = l.itemID
                LEFT JOIN Warehouse w ON w.warehouseID = l.warehouseID
                {where}
                ORDER BY l.actionDate DESC, l.ledgerID DESC
                LIMIT @lim";
            return DatabaseConnect.ExecuteQuery(sql, parameters.ToArray());
        }
    }
}
