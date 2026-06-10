using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Ensures dual-currency columns exist (exchangeRate, *AmountBase) on document tables.
    /// Safe to call on every startup; skips statements when columns already exist.
    /// </summary>
    public static class CurrencyDualAmountMigration
    {
        public static void EnsureApplied()
        {
            EnsureColumn("currency", "isBaseCurrency",
                "ALTER TABLE `currency` ADD COLUMN `isBaseCurrency` TINYINT(1) NOT NULL DEFAULT 0");
            EnsureColumn("currency", "decimalPlaces",
                "ALTER TABLE `currency` ADD COLUMN `decimalPlaces` INT NOT NULL DEFAULT 2");
            EnsureColumn("currency", "isEnabled",
                "ALTER TABLE `currency` ADD COLUMN `isEnabled` TINYINT(1) NOT NULL DEFAULT 1");

            EnsureColumn("quotation", "exchangeRate",
                "ALTER TABLE `quotation` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("quotation", "totalAmount",
                "ALTER TABLE `quotation` ADD COLUMN `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00");
            EnsureColumn("quotation", "totalAmountBase",
                "ALTER TABLE `quotation` ADD COLUMN `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            EnsureColumn("salesorder", "exchangeRate",
                "ALTER TABLE `salesorder` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("salesorder", "totalAmount",
                "ALTER TABLE `salesorder` ADD COLUMN `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00");
            EnsureColumn("salesorder", "totalAmountBase",
                "ALTER TABLE `salesorder` ADD COLUMN `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            EnsureColumn("invoice", "currencyID",
                "ALTER TABLE `invoice` ADD COLUMN `currencyID` BIGINT(20) NOT NULL DEFAULT 1");
            EnsureColumn("invoice", "exchangeRate",
                "ALTER TABLE `invoice` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("invoice", "totalAmount",
                "ALTER TABLE `invoice` ADD COLUMN `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00");
            EnsureColumn("invoice", "totalAmountBase",
                "ALTER TABLE `invoice` ADD COLUMN `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            EnsureColumn("receiptvoucher", "exchangeRate",
                "ALTER TABLE `receiptvoucher` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("receiptvoucher", "paymentAmountBase",
                "ALTER TABLE `receiptvoucher` ADD COLUMN `paymentAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            EnsureColumn("paymentvoucher", "currencyID",
                "ALTER TABLE `paymentvoucher` ADD COLUMN `currencyID` BIGINT(20) NOT NULL DEFAULT 1");
            EnsureColumn("paymentvoucher", "exchangeRate",
                "ALTER TABLE `paymentvoucher` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("paymentvoucher", "totalAmountBase",
                "ALTER TABLE `paymentvoucher` ADD COLUMN `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            EnsureColumn("purchaseorder", "currencyID",
                "ALTER TABLE `purchaseorder` ADD COLUMN `currencyID` BIGINT(20) NOT NULL DEFAULT 1");
            EnsureColumn("purchaseorder", "exchangeRate",
                "ALTER TABLE `purchaseorder` ADD COLUMN `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000");
            EnsureColumn("purchaseorder", "totalAmount",
                "ALTER TABLE `purchaseorder` ADD COLUMN `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00");
            EnsureColumn("purchaseorder", "totalAmountBase",
                "ALTER TABLE `purchaseorder` ADD COLUMN `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00");

            RunOptional(@"
                UPDATE `currency` SET `isBaseCurrency` = 1, `decimalPlaces` = 2, `isEnabled` = 1 WHERE `currencyID` = 1;
                UPDATE `currency` SET `isBaseCurrency` = 0, `isEnabled` = 1 WHERE `currencyID` <> 1;");

            BackfillIfNeeded();
        }

        private static void BackfillIfNeeded()
        {
            if (!ColumnExists("salesorder", "exchangeRate")) return;

            RunOptional(@"
                UPDATE `salesorder` so
                INNER JOIN `currency` c ON so.`currencyCurrencyID` = c.`currencyID`
                SET so.`exchangeRate` = c.`rateToBase`
                WHERE so.`exchangeRate` = 1.0000 AND c.`currencyID` <> 1 AND c.`rateToBase` <> 1;");

            RunOptional(@"
                UPDATE `salesorder` so
                SET so.`totalAmount` = (
                  SELECT COALESCE(SUM(spl.`price` * spl.`orderQuantity` - spl.`discountAmount`), 0)
                  FROM `salesorderproductline` spl
                  WHERE spl.`salesOrderID` = so.`salesOrderID`
                )
                WHERE so.`totalAmount` = 0;");

            RunOptional(@"
                UPDATE `salesorder`
                SET `totalAmount` = CASE
                  WHEN `discountType` = 'Percentage' AND `discount` > 0 THEN ROUND(`totalAmount` * (1 - `discount` / 100), 2)
                  WHEN `discountType` = 'Fixed Amount' AND `discount` > 0 THEN GREATEST(0, ROUND(`totalAmount` - `discount`, 2))
                  ELSE `totalAmount`
                END
                WHERE `totalAmountBase` = 0 AND `totalAmount` <> 0;");

            RunOptional(@"
                UPDATE `salesorder`
                SET `totalAmountBase` = ROUND(`totalAmount` * `exchangeRate`, 2)
                WHERE `totalAmountBase` = 0 AND `totalAmount` <> 0;");

            RunOptional(@"
                UPDATE `receiptvoucher` rv
                INNER JOIN `currency` c ON rv.`currencyID` = c.`currencyID`
                SET rv.`exchangeRate` = c.`rateToBase`,
                    rv.`paymentAmountBase` = ROUND(rv.`paymentAmount` * c.`rateToBase`, 2)
                WHERE c.`currencyID` <> 1 AND c.`rateToBase` <> 1
                  AND (rv.`exchangeRate` = 1.0000 OR ABS(rv.`paymentAmountBase` - rv.`paymentAmount`) < 0.01);");

            RunOptional(@"
                UPDATE `paymentvoucher` pv
                INNER JOIN `currency` c ON pv.`currencyID` = c.`currencyID`
                SET pv.`exchangeRate` = c.`rateToBase`,
                    pv.`totalAmountBase` = ROUND(pv.`totalAmount` * c.`rateToBase`, 2)
                WHERE c.`currencyID` <> 1 AND c.`rateToBase` <> 1
                  AND (pv.`exchangeRate` = 1.0000 OR ABS(pv.`totalAmountBase` - pv.`totalAmount`) < 0.01);");
        }

        private static bool ColumnExists(string tableName, string columnName)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = LOWER(@table)
                    AND LOWER(COLUMN_NAME) = LOWER(@column)",
                new[]
                {
                    new MySqlParameter("@table", tableName),
                    new MySqlParameter("@column", columnName)
                });
            return count != null && Convert.ToInt32(count) > 0;
        }

        private static void EnsureColumn(string tableName, string columnName, string alterSql)
        {
            if (ColumnExists(tableName, columnName)) return;
            DatabaseConnect.ExecuteNonQuery(alterSql);
        }

        private static void RunOptional(string sql)
        {
            try
            {
                DatabaseConnect.ExecuteNonQuery(sql);
            }
            catch
            {
                // Backfill is best-effort; schema columns are the critical part.
            }
        }
    }
}
