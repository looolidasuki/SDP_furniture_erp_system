using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Adds lineNo to receiptvoucherinvoice for multi-line RV allocations (incl. exchange-loss rows).
    /// Idempotent — safe on every startup.
    /// </summary>
    public static class ReceiptVoucherInvoiceLineMigration
    {
        private const string TableName = "receiptvoucherinvoice";

        public static void EnsureApplied()
        {
            if (!TableExists(TableName)) return;
            if (ColumnExists(TableName, "lineNo"))
            {
                EnsureInvoiceIdNullable();
                return;
            }

            DropForeignKeyIfExists(TableName, "fk_rvi_inv");
            DropPrimaryKeyIfExists(TableName);

            DatabaseConnect.ExecuteNonQuery($@"
                ALTER TABLE `{TableName}`
                  ADD COLUMN `lineNo` INT(10) NOT NULL DEFAULT 1
                    COMMENT 'Allocation line sequence per receipt voucher'
                  AFTER `receiptVoucherID`");

            RunOptional($@"
                UPDATE `{TableName}` rvi
                INNER JOIN (
                  SELECT `receiptVoucherID`, `invoiceID`,
                         ROW_NUMBER() OVER (PARTITION BY `receiptVoucherID` ORDER BY `invoiceID`) AS rn
                  FROM `{TableName}`
                ) numbered ON rvi.`receiptVoucherID` = numbered.`receiptVoucherID`
                           AND rvi.`invoiceID` = numbered.`invoiceID`
                SET rvi.`lineNo` = numbered.rn");

            EnsureInvoiceIdNullable();

            if (!PrimaryKeyIncludesColumn(TableName, "lineNo"))
            {
                DropPrimaryKeyIfExists(TableName);
                DatabaseConnect.ExecuteNonQuery($@"
                    ALTER TABLE `{TableName}`
                      ADD PRIMARY KEY (`receiptVoucherID`, `lineNo`)");
            }

            if (!IndexExists(TableName, "fk_rvi_inv"))
            {
                DatabaseConnect.ExecuteNonQuery($@"
                    ALTER TABLE `{TableName}`
                      ADD KEY `fk_rvi_inv` (`invoiceID`)");
            }

            DropForeignKeyIfExists(TableName, "fk_rvi_inv");
            RunOptional($@"
                ALTER TABLE `{TableName}`
                  ADD CONSTRAINT `fk_rvi_inv`
                  FOREIGN KEY (`invoiceID`) REFERENCES `invoice` (`invoiceID`) ON UPDATE CASCADE");
        }

        private static void EnsureInvoiceIdNullable()
        {
            if (!ColumnExists(TableName, "invoiceID")) return;
            if (ColumnIsNullable(TableName, "invoiceID")) return;

            RunOptional($@"
                ALTER TABLE `{TableName}`
                  MODIFY `invoiceID` BIGINT(20) NULL
                  COMMENT 'NULL for exchange-loss lines (type=4)'");
        }

        private static bool TableExists(string tableName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.TABLES
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@table)",
                new[] { new MySqlParameter("@table", tableName) });
            return count != null && Convert.ToInt32(count) > 0;
        }

        private static bool ColumnExists(string tableName, string columnName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = LOWER(@table)
                    AND LOWER(COLUMN_NAME) = LOWER(@column)",
                new[] {
                    new MySqlParameter("@table", tableName),
                    new MySqlParameter("@column", columnName)
                });
            return count != null && Convert.ToInt32(count) > 0;
        }

        private static bool ColumnIsNullable(string tableName, string columnName)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT IS_NULLABLE FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = LOWER(@table)
                    AND LOWER(COLUMN_NAME) = LOWER(@column)",
                new[] {
                    new MySqlParameter("@table", tableName),
                    new MySqlParameter("@column", columnName)
                });
            return string.Equals(value?.ToString(), "YES", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IndexExists(string tableName, string indexName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = LOWER(@table)
                    AND LOWER(INDEX_NAME) = LOWER(@index)",
                new[] {
                    new MySqlParameter("@table", tableName),
                    new MySqlParameter("@index", indexName)
                });
            return count != null && Convert.ToInt32(count) > 0;
        }

        private static bool PrimaryKeyIncludesColumn(string tableName, string columnName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = LOWER(@table)
                    AND CONSTRAINT_NAME = 'PRIMARY'
                    AND LOWER(COLUMN_NAME) = LOWER(@column)",
                new[] {
                    new MySqlParameter("@table", tableName),
                    new MySqlParameter("@column", columnName)
                });
            return count != null && Convert.ToInt32(count) > 0;
        }

        private static void DropForeignKeyIfExists(string tableName, string constraintName)
        {
            RunOptional($"ALTER TABLE `{tableName}` DROP FOREIGN KEY `{constraintName}`");
        }

        private static void DropPrimaryKeyIfExists(string tableName)
        {
            if (!IndexExists(tableName, "PRIMARY")) return;
            RunOptional($"ALTER TABLE `{tableName}` DROP PRIMARY KEY");
        }

        private static void RunOptional(string sql)
        {
            try
            {
                DatabaseConnect.ExecuteNonQuery(sql);
            }
            catch
            {
                // Best-effort for idempotent DDL.
            }
        }
    }
}
