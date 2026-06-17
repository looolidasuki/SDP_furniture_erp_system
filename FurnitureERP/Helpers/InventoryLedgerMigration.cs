using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class InventoryLedgerMigration
    {
        public static void EnsureApplied()
        {
            if (TableExists("inventoryledger")) return;

            DatabaseConnect.ExecuteNonQuery(@"
                CREATE TABLE IF NOT EXISTS `inventoryledger` (
                  `ledgerID` BIGINT(20) NOT NULL AUTO_INCREMENT,
                  `itemType` TINYINT(4) NOT NULL COMMENT '1=RawMaterial 2=Product',
                  `itemID` BIGINT(20) NOT NULL,
                  `warehouseID` BIGINT(20) NOT NULL,
                  `qtyDelta` DECIMAL(14,4) NOT NULL,
                  `balanceAfter` DECIMAL(14,4) DEFAULT NULL,
                  `documentType` VARCHAR(50) DEFAULT NULL,
                  `documentID` BIGINT(20) DEFAULT NULL,
                  `documentCode` VARCHAR(50) DEFAULT NULL,
                  `action` VARCHAR(40) NOT NULL,
                  `staffID` BIGINT(20) DEFAULT NULL,
                  `staffName` VARCHAR(100) DEFAULT NULL,
                  `remark` VARCHAR(255) DEFAULT NULL,
                  `actionDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`ledgerID`),
                  KEY `idx_inv_ledger_item` (`itemType`, `itemID`, `warehouseID`),
                  KEY `idx_inv_ledger_doc` (`documentType`, `documentID`),
                  KEY `idx_inv_ledger_date` (`actionDate`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");
        }

        private static bool TableExists(string tableName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.TABLES
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@table)",
                new[] { new MySqlParameter("@table", tableName) });
            return count != null && Convert.ToInt32(count) > 0;
        }
    }
}
