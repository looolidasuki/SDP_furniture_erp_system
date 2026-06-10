-- Re-backfill locked exchange rates and HKD amounts on RV/PV
-- when migration or early saves left exchangeRate = 1 for foreign currencies.
--
-- Usage:
--   USE furniture_erp_system;
--   SOURCE path/to/rebackfill_voucher_exchange_rates.sql;

-- Ensure benchmark rates (only updates rows still at 1.00 for non-HKD codes)
UPDATE `currency` SET `rateToBase` = 1.00, `isBaseCurrency` = 1, `isEnabled` = 1 WHERE `currencyID` = 1;
UPDATE `currency` SET `rateToBase` = 7.85 WHERE `currencyCode` = 'USD' AND `rateToBase` = 1.00;
UPDATE `currency` SET `rateToBase` = 8.50 WHERE `currencyCode` = 'EUR' AND `rateToBase` = 1.00;
UPDATE `currency` SET `rateToBase` = 0.05 WHERE `currencyCode` = 'JPY' AND `rateToBase` = 1.00;
UPDATE `currency` SET `rateToBase` = 0.25 WHERE `currencyCode` = 'TWD' AND `rateToBase` = 1.00;
UPDATE `currency` SET `rateToBase` = 1.08 WHERE `currencyCode` = 'CNY' AND `rateToBase` = 1.00;

-- Receipt vouchers: fix rows where HKD base was stored 1:1 with foreign amount
UPDATE `receiptvoucher` rv
INNER JOIN `currency` c ON rv.`currencyID` = c.`currencyID`
SET rv.`exchangeRate` = c.`rateToBase`,
    rv.`paymentAmountBase` = ROUND(rv.`paymentAmount` * c.`rateToBase`, 2)
WHERE c.`currencyID` <> 1
  AND c.`rateToBase` <> 1
  AND (
        rv.`exchangeRate` = 1.0000
        OR ABS(rv.`paymentAmountBase` - rv.`paymentAmount`) < 0.01
      );

-- Payment vouchers: same rule (includes rows forced to HKD by old migration)
UPDATE `paymentvoucher` pv
INNER JOIN `currency` c ON pv.`currencyID` = c.`currencyID`
SET pv.`exchangeRate` = c.`rateToBase`,
    pv.`totalAmountBase` = ROUND(pv.`totalAmount` * c.`rateToBase`, 2)
WHERE c.`currencyID` <> 1
  AND c.`rateToBase` <> 1
  AND (
        pv.`exchangeRate` = 1.0000
        OR ABS(pv.`totalAmountBase` - pv.`totalAmount`) < 0.01
      );

-- HKD rows: keep rate 1, align base with amount
UPDATE `receiptvoucher` rv
INNER JOIN `currency` c ON rv.`currencyID` = c.`currencyID`
SET rv.`exchangeRate` = 1.0000,
    rv.`paymentAmountBase` = ROUND(rv.`paymentAmount`, 2)
WHERE c.`currencyID` = 1
  AND (
        rv.`exchangeRate` <> 1.0000
        OR ABS(rv.`paymentAmountBase` - rv.`paymentAmount`) >= 0.01
      );

UPDATE `paymentvoucher` pv
INNER JOIN `currency` c ON pv.`currencyID` = c.`currencyID`
SET pv.`exchangeRate` = 1.0000,
    pv.`totalAmountBase` = ROUND(pv.`totalAmount`, 2)
WHERE c.`currencyID` = 1
  AND (
        pv.`exchangeRate` <> 1.0000
        OR ABS(pv.`totalAmountBase` - pv.`totalAmount`) >= 0.01
      );

SELECT 'Voucher exchange-rate re-backfill completed.' AS Result;
