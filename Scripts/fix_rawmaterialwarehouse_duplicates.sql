-- Fix #1062 duplicate entry for rawmaterialwarehouse PRIMARY KEY (rawMaterialID, warehouseID).
-- Run in database: furniture_erp_system

USE `furniture_erp_system`;

-- 1) Show duplicates (optional check)
-- SELECT rawMaterialID, warehouseID, COUNT(*) AS cnt
-- FROM rawmaterialwarehouse
-- GROUP BY rawMaterialID, warehouseID
-- HAVING cnt > 1;

-- 2) Merge duplicates: keep max quantities per (rawMaterialID, warehouseID)
DROP TABLE IF EXISTS `_rmw_dedup`;
CREATE TABLE `_rmw_dedup` AS
SELECT `rawMaterialID`, `warehouseID`,
       MAX(`physicalQuantity`) AS `physicalQuantity`,
       MAX(`reservedQuantity`) AS `reservedQuantity`,
       MAX(`purchasedQuantity`) AS `purchasedQuantity`
FROM `rawmaterialwarehouse`
GROUP BY `rawMaterialID`, `warehouseID`;

TRUNCATE TABLE `rawmaterialwarehouse`;
INSERT INTO `rawmaterialwarehouse`
SELECT `rawMaterialID`, `warehouseID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`
FROM `_rmw_dedup`;
DROP TABLE `_rmw_dedup`;

-- 3) Add primary key if missing
SET @rmw_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialwarehouse'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_rmw_pk := IF(@rmw_pk_exists = 0,
  'ALTER TABLE `rawmaterialwarehouse` ADD PRIMARY KEY (`rawMaterialID`,`warehouseID`), ADD KEY `fk_rmw_warehouse` (`warehouseID`)',
  'SELECT 1');
PREPARE stmt_rmw_pk FROM @sql_rmw_pk;
EXECUTE stmt_rmw_pk;
DEALLOCATE PREPARE stmt_rmw_pk;
