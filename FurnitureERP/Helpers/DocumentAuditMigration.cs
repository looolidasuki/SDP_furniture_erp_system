using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class DocumentAuditMigration
    {
        public static void EnsureApplied()
        {
            if (TableExists("documentauditlog")) return;

            DatabaseConnect.ExecuteNonQuery(@"
                CREATE TABLE IF NOT EXISTS `documentauditlog` (
                  `auditLogID` BIGINT(20) NOT NULL AUTO_INCREMENT,
                  `documentType` VARCHAR(50) NOT NULL,
                  `documentId` BIGINT(20) NOT NULL,
                  `documentCode` VARCHAR(50) DEFAULT NULL,
                  `action` VARCHAR(30) NOT NULL,
                  `staffID` BIGINT(20) DEFAULT NULL,
                  `staffName` VARCHAR(100) DEFAULT NULL,
                  `summary` VARCHAR(500) DEFAULT NULL,
                  `actionDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`auditLogID`),
                  KEY `idx_doc_audit` (`documentType`, `documentId`),
                  KEY `idx_doc_audit_date` (`actionDate`)
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
