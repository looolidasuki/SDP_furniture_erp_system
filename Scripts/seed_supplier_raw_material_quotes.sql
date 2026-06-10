-- ONE-TIME / RE-RUNNABLE: link active suppliers to raw materials in RawMaterialSupplier.
-- Fixes PO screen "supplier has no active raw material quotes".
--
-- Usage (phpMyAdmin or MySQL client):
--   USE furniture_erp_system;
--   SOURCE path/to/seed_supplier_raw_material_quotes.sql;
--
-- Safe to re-run: only inserts missing (supplierID, rawMaterialID) pairs.

SET NAMES utf8mb4;

-- ---------------------------------------------------------------------------
-- 1) Suppliers with ZERO quotes -> assign first 12 active raw materials each
-- ---------------------------------------------------------------------------
INSERT INTO `rawmaterialsupplier` (
    `rawMaterialID`,
    `supplierID`,
    `supplierStyleNumber`,
    `basePrice`,
    `currencyID`,
    `unit`,
    `minimumOrderQuantity`,
    `quoteDate`,
    `status`
)
SELECT
    rm.`rawMaterialID`,
    s.`supplierID`,
    CONCAT('AUTO-S', s.`supplierID`, '-', rm.`rawMaterialCode`),
    CASE rm.`category`
        WHEN '0' THEN 750.00 + MOD(rm.`rawMaterialID` * 17 + s.`supplierID` * 13, 450)
        WHEN '1' THEN  48.00 + MOD(rm.`rawMaterialID` * 11 + s.`supplierID` *  7,  75)
        WHEN '2' THEN  65.00 + MOD(rm.`rawMaterialID` *  9 + s.`supplierID` *  5,  90)
        WHEN '3' THEN  18.00 + MOD(rm.`rawMaterialID` *  7 + s.`supplierID` *  3,  35)
        WHEN '4' THEN  28.00 + MOD(rm.`rawMaterialID` *  5 + s.`supplierID` *  3,  55)
        WHEN '5' THEN  32.00 + MOD(rm.`rawMaterialID` *  4 + s.`supplierID` *  2,  40)
        ELSE           15.00 + MOD(rm.`rawMaterialID` *  3 + s.`supplierID` *  2,  25)
    END AS `basePrice`,
    1 AS `currencyID`,
    CASE
        WHEN rm.`category` IN ('0', '1', '2') THEN 'piece'
        WHEN rm.`category` IN ('3', '4') THEN 'meter'
        WHEN rm.`category` = '5' THEN 'can'
        WHEN rm.`category` = '6' THEN 'set'
        ELSE 'unit'
    END AS `unit`,
    CASE
        WHEN rm.`category` = '0' THEN 10
        WHEN rm.`category` = '1' THEN 20
        WHEN rm.`category` IN ('3', '4') THEN 50
        ELSE 10
    END AS `minimumOrderQuantity`,
    CURDATE() AS `quoteDate`,
    1 AS `status`
FROM `supplier` s
INNER JOIN (
    SELECT `rawMaterialID`, `rawMaterialCode`, `category`
    FROM `rawmaterial`
    WHERE `status` = 1
    ORDER BY `rawMaterialID`
    LIMIT 12
) rm ON 1 = 1
WHERE s.`status` = 1
  AND NOT EXISTS (
      SELECT 1
      FROM `rawmaterialsupplier` rms0
      WHERE rms0.`supplierID` = s.`supplierID`
  )
  AND NOT EXISTS (
      SELECT 1
      FROM `rawmaterialsupplier` rms1
      WHERE rms1.`supplierID` = s.`supplierID`
        AND rms1.`rawMaterialID` = rm.`rawMaterialID`
  );

-- ---------------------------------------------------------------------------
-- 2) Broader coverage: ~40% of remaining active RM per supplier (staggered)
-- ---------------------------------------------------------------------------
INSERT INTO `rawmaterialsupplier` (
    `rawMaterialID`,
    `supplierID`,
    `supplierStyleNumber`,
    `basePrice`,
    `currencyID`,
    `unit`,
    `minimumOrderQuantity`,
    `quoteDate`,
    `status`
)
SELECT
    rm.`rawMaterialID`,
    s.`supplierID`,
    CONCAT('AUTO-S', s.`supplierID`, '-', rm.`rawMaterialCode`),
    CASE rm.`category`
        WHEN '0' THEN 780.00 + MOD(rm.`rawMaterialID` * 19 + s.`supplierID` * 11, 420)
        WHEN '1' THEN  50.00 + MOD(rm.`rawMaterialID` * 13 + s.`supplierID` *  9,  70)
        WHEN '2' THEN  68.00 + MOD(rm.`rawMaterialID` * 10 + s.`supplierID` *  6,  85)
        WHEN '3' THEN  20.00 + MOD(rm.`rawMaterialID` *  8 + s.`supplierID` *  4,  32)
        WHEN '4' THEN  30.00 + MOD(rm.`rawMaterialID` *  6 + s.`supplierID` *  4,  50)
        WHEN '5' THEN  35.00 + MOD(rm.`rawMaterialID` *  5 + s.`supplierID` *  3,  38)
        ELSE           18.00 + MOD(rm.`rawMaterialID` *  4 + s.`supplierID` *  3,  22)
    END AS `basePrice`,
    1 AS `currencyID`,
    CASE
        WHEN rm.`category` IN ('0', '1', '2') THEN 'piece'
        WHEN rm.`category` IN ('3', '4') THEN 'meter'
        WHEN rm.`category` = '5' THEN 'can'
        WHEN rm.`category` = '6' THEN 'set'
        ELSE 'unit'
    END AS `unit`,
    CASE
        WHEN rm.`category` = '0' THEN 10
        WHEN rm.`category` = '1' THEN 20
        WHEN rm.`category` IN ('3', '4') THEN 50
        ELSE 10
    END AS `minimumOrderQuantity`,
    CURDATE() AS `quoteDate`,
    1 AS `status`
FROM `supplier` s
INNER JOIN `rawmaterial` rm ON rm.`status` = 1
WHERE s.`status` = 1
  AND MOD(rm.`rawMaterialID` + s.`supplierID` * 11, 10) < 4
  AND NOT EXISTS (
      SELECT 1
      FROM `rawmaterialsupplier` rms
      WHERE rms.`supplierID` = s.`supplierID`
        AND rms.`rawMaterialID` = rm.`rawMaterialID`
  );

-- ---------------------------------------------------------------------------
-- 3) Ensure every active supplier has at least 5 active quotes
-- ---------------------------------------------------------------------------
INSERT INTO `rawmaterialsupplier` (
    `rawMaterialID`,
    `supplierID`,
    `supplierStyleNumber`,
    `basePrice`,
    `currencyID`,
    `unit`,
    `minimumOrderQuantity`,
    `quoteDate`,
    `status`
)
SELECT
    rm.`rawMaterialID`,
    need.`supplierID`,
    CONCAT('AUTO-S', need.`supplierID`, '-', rm.`rawMaterialCode`),
    CASE rm.`category`
        WHEN '0' THEN 800.00 + MOD(rm.`rawMaterialID` * 23 + need.`supplierID` * 17, 400)
        WHEN '1' THEN  52.00 + MOD(rm.`rawMaterialID` * 15 + need.`supplierID` * 11,  68)
        ELSE           25.00 + MOD(rm.`rawMaterialID` *  7 + need.`supplierID` *  5,  30)
    END AS `basePrice`,
    1 AS `currencyID`,
    CASE
        WHEN rm.`category` IN ('0', '1', '2') THEN 'piece'
        WHEN rm.`category` IN ('3', '4') THEN 'meter'
        WHEN rm.`category` = '5' THEN 'can'
        ELSE 'unit'
    END AS `unit`,
    10 AS `minimumOrderQuantity`,
    CURDATE() AS `quoteDate`,
    1 AS `status`
FROM (
    SELECT s.`supplierID`
    FROM `supplier` s
    LEFT JOIN `rawmaterialsupplier` rms ON rms.`supplierID` = s.`supplierID` AND rms.`status` = 1
    WHERE s.`status` = 1
    GROUP BY s.`supplierID`
    HAVING COUNT(rms.`rawMaterialID`) < 5
) need
INNER JOIN `rawmaterial` rm ON rm.`status` = 1
WHERE NOT EXISTS (
    SELECT 1
    FROM `rawmaterialsupplier` rms2
    WHERE rms2.`supplierID` = need.`supplierID`
      AND rms2.`rawMaterialID` = rm.`rawMaterialID`
)
AND rm.`rawMaterialID` <= (
    SELECT MIN(x.`rawMaterialID`) + 4
    FROM `rawmaterial` x
    WHERE x.`status` = 1
);

-- ---------------------------------------------------------------------------
-- 4) Reactivate accidentally inactive quotes for active suppliers/materials
-- ---------------------------------------------------------------------------
UPDATE `rawmaterialsupplier` rms
INNER JOIN `supplier` s ON s.`supplierID` = rms.`supplierID` AND s.`status` = 1
INNER JOIN `rawmaterial` rm ON rm.`rawMaterialID` = rms.`rawMaterialID` AND rm.`status` = 1
SET rms.`status` = 1
WHERE rms.`status` <> 1;

-- ---------------------------------------------------------------------------
-- Verification
-- ---------------------------------------------------------------------------
SELECT
    s.`supplierID`,
    s.`supplierName`,
    COUNT(rms.`rawMaterialID`) AS `quote_count`,
    SUM(CASE WHEN rms.`status` = 1 THEN 1 ELSE 0 END) AS `active_quote_count`
FROM `supplier` s
LEFT JOIN `rawmaterialsupplier` rms ON rms.`supplierID` = s.`supplierID`
WHERE s.`status` = 1
GROUP BY s.`supplierID`, s.`supplierName`
ORDER BY `active_quote_count` ASC, s.`supplierID`
LIMIT 30;
