-- Fix warehouse stock tables: dedupe rows, add primary keys, enable warehouse AUTO_INCREMENT.
-- Run in database: furniture_erp_system (safe to re-run).

USE `furniture_erp_system`;

-- 1) rawmaterialwarehouse: merge duplicates then add PK
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

-- 2) warehouseproduct: merge duplicates then add PK
DROP TABLE IF EXISTS `_wp_dedup`;
CREATE TABLE `_wp_dedup` AS
SELECT `warehouseID`, `productID`,
       MAX(`physicalQuantity`) AS `physicalQuantity`,
       MAX(`reservedQuantity`) AS `reservedQuantity`,
       MAX(`purchasedQuantity`) AS `purchasedQuantity`
FROM `warehouseproduct`
GROUP BY `warehouseID`, `productID`;

TRUNCATE TABLE `warehouseproduct`;
INSERT INTO `warehouseproduct`
SELECT `warehouseID`, `productID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`
FROM `_wp_dedup`;
DROP TABLE `_wp_dedup`;

SET @wp_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'warehouseproduct'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_wp_pk := IF(@wp_pk_exists = 0,
  'ALTER TABLE `warehouseproduct` ADD PRIMARY KEY (`warehouseID`,`productID`), ADD KEY `fk_wp_product` (`productID`)',
  'SELECT 1');
PREPARE stmt_wp_pk FROM @sql_wp_pk;
EXECUTE stmt_wp_pk;
DEALLOCATE PREPARE stmt_wp_pk;

-- 3) warehouse ID auto-increment for New Warehouse UI
SET @wh_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'warehouse'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_wh_pk := IF(@wh_pk_exists = 0,
  'ALTER TABLE `warehouse` ADD PRIMARY KEY (`warehouseID`)',
  'SELECT 1');
PREPARE stmt_wh_pk FROM @sql_wh_pk;
EXECUTE stmt_wh_pk;
DEALLOCATE PREPARE stmt_wh_pk;

SET @wh_is_ai := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'warehouse'
    AND COLUMN_NAME = 'warehouseID'
    AND EXTRA LIKE '%auto_increment%'
);
SET @sql_wh_ai := IF(@wh_is_ai = 0,
  'ALTER TABLE `warehouse` MODIFY COLUMN `warehouseID` bigint NOT NULL AUTO_INCREMENT',
  'SELECT 1');
PREPARE stmt_wh_ai FROM @sql_wh_ai;
EXECUTE stmt_wh_ai;
DEALLOCATE PREPARE stmt_wh_ai;

SET @wh_next_id := (SELECT COALESCE(MAX(`warehouseID`), 0) + 1 FROM `warehouse`);
SET @sql_wh_ai_val := CONCAT('ALTER TABLE `warehouse` AUTO_INCREMENT = ', @wh_next_id);
PREPARE stmt_wh_ai_val FROM @sql_wh_ai_val;
EXECUTE stmt_wh_ai_val;
DEALLOCATE PREPARE stmt_wh_ai_val;
