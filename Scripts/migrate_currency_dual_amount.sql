-- Dual-currency support: HKD base + locked exchangeRate on documents.
-- Safe to re-run (MariaDB ADD COLUMN IF NOT EXISTS).
--
-- Usage:
--   USE furniture_erp_system;
--   SOURCE path/to/migrate_currency_dual_amount.sql;

-- ========== Currency master ==========
ALTER TABLE `currency`
  ADD COLUMN IF NOT EXISTS `isBaseCurrency` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 = HKD benchmark',
  ADD COLUMN IF NOT EXISTS `decimalPlaces` INT NOT NULL DEFAULT 2,
  ADD COLUMN IF NOT EXISTS `isEnabled` TINYINT(1) NOT NULL DEFAULT 1;

UPDATE `currency` SET `isBaseCurrency` = 1, `decimalPlaces` = 2, `isEnabled` = 1 WHERE `currencyID` = 1;
UPDATE `currency` SET `isBaseCurrency` = 0, `isEnabled` = 1 WHERE `currencyID` <> 1;

-- ========== Quotation ==========
ALTER TABLE `quotation`
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000 COMMENT 'Locked rateToBase at save',
  ADD COLUMN IF NOT EXISTS `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00 COMMENT 'Document currency total',
  ADD COLUMN IF NOT EXISTS `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00 COMMENT 'HKD equivalent';

-- ========== Sales Order ==========
ALTER TABLE `salesorder`
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000,
  ADD COLUMN IF NOT EXISTS `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00;

-- ========== Invoice ==========
ALTER TABLE `invoice`
  ADD COLUMN IF NOT EXISTS `currencyID` BIGINT(20) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000,
  ADD COLUMN IF NOT EXISTS `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00;

-- ========== Receipt Voucher ==========
ALTER TABLE `receiptvoucher`
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000,
  ADD COLUMN IF NOT EXISTS `paymentAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00 COMMENT 'HKD equivalent';

-- ========== Payment Voucher ==========
ALTER TABLE `paymentvoucher`
  ADD COLUMN IF NOT EXISTS `currencyID` BIGINT(20) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000,
  ADD COLUMN IF NOT EXISTS `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00 COMMENT 'HKD equivalent';

-- ========== Purchase Order ==========
ALTER TABLE `purchaseorder`
  ADD COLUMN IF NOT EXISTS `currencyID` BIGINT(20) NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS `exchangeRate` DECIMAL(12,4) NOT NULL DEFAULT 1.0000,
  ADD COLUMN IF NOT EXISTS `totalAmount` DECIMAL(14,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS `totalAmountBase` DECIMAL(14,2) NOT NULL DEFAULT 0.00;

-- ========== Backfill quotation ==========
UPDATE `quotation` q
INNER JOIN `currency` c ON q.`currencyID` = c.`currencyID`
SET q.`exchangeRate` = c.`rateToBase`;

UPDATE `quotation` q
SET q.`totalAmount` = (
  SELECT COALESCE(SUM(qpl.`price` * qpl.`quantity` - qpl.`discountAmount`), 0)
  FROM `quotationproductline` qpl
  WHERE qpl.`quotationID` = q.`quotationID`
);

UPDATE `quotation` SET `totalAmountBase` = ROUND(`totalAmount` * `exchangeRate`, 2);

-- ========== Backfill sales order ==========
UPDATE `salesorder` so
INNER JOIN `currency` c ON so.`currencyCurrencyID` = c.`currencyID`
SET so.`exchangeRate` = c.`rateToBase`;

UPDATE `salesorder` so
SET so.`totalAmount` = (
  SELECT COALESCE(SUM(spl.`price` * spl.`orderQuantity` - spl.`discountAmount`), 0)
  FROM `salesorderproductline` spl
  WHERE spl.`salesOrderID` = so.`salesOrderID`
);

UPDATE `salesorder`
SET `totalAmount` = CASE
  WHEN `discountType` = 'Percentage' AND `discount` > 0 THEN ROUND(`totalAmount` * (1 - `discount` / 100), 2)
  WHEN `discountType` = 'Fixed Amount' AND `discount` > 0 THEN GREATEST(0, ROUND(`totalAmount` - `discount`, 2))
  ELSE `totalAmount`
END;

UPDATE `salesorder` SET `totalAmountBase` = ROUND(`totalAmount` * `exchangeRate`, 2);

-- ========== Backfill invoice (inherit from sales order) ==========
UPDATE `invoice` i
INNER JOIN `salesorder` so ON i.`salesOrderID` = so.`salesOrderID`
SET i.`currencyID` = so.`currencyCurrencyID`,
    i.`exchangeRate` = so.`exchangeRate`;

UPDATE `invoice` i
SET i.`totalAmount` = (
  SELECT COALESCE(SUM(il.`amount`), 0)
  FROM `invoiceline` il
  WHERE il.`invoiceID` = i.`invoiceID`
);

UPDATE `invoice` SET `totalAmountBase` = ROUND(`totalAmount` * `exchangeRate`, 2);

-- ========== Backfill receipt voucher ==========
UPDATE `receiptvoucher` rv
INNER JOIN `currency` c ON rv.`currencyID` = c.`currencyID`
SET rv.`exchangeRate` = c.`rateToBase`,
    rv.`paymentAmountBase` = ROUND(rv.`paymentAmount` * c.`rateToBase`, 2);

-- ========== Backfill payment voucher ==========
UPDATE `paymentvoucher` pv
INNER JOIN `currency` c ON pv.`currencyID` = c.`currencyID`
SET pv.`exchangeRate` = c.`rateToBase`,
    pv.`totalAmountBase` = ROUND(pv.`totalAmount` * c.`rateToBase`, 2);

UPDATE `paymentvoucher` pv
LEFT JOIN `currency` c ON pv.`currencyID` = c.`currencyID`
SET pv.`currencyID` = 1,
    pv.`exchangeRate` = 1.0000,
    pv.`totalAmountBase` = ROUND(pv.`totalAmount`, 2)
WHERE c.`currencyID` IS NULL;

-- ========== Backfill purchase order (default HKD) ==========
UPDATE `purchaseorder` po
SET po.`totalAmount` = (
  SELECT COALESCE(SUM(pol.`price` * pol.`orderQuantity`), 0)
  FROM `purchaseorderrawmaterialline` pol
  WHERE pol.`purchaseOrderID` = po.`purchaseOrderID`
);

UPDATE `purchaseorder`
SET `currencyID` = 1,
    `exchangeRate` = 1.0000,
    `totalAmountBase` = ROUND(`totalAmount` * `exchangeRate`, 2);

SELECT 'Currency dual-amount migration completed.' AS Result;
