-- Merge patches for furniture_erp_system_New.sql (apply after importing the new schema + seed data).
--
-- PRIMARY KEY / AUTO_INCREMENT (run ONCE on existing DB, before or after this file):
--   Scripts/fix_all_primary_keys_once.sql
-- Do NOT re-run fix_purchaseorder_grn_ids.sql or other id=0 patches after that.
--
-- Reply slip: no separate table — paired codes and sign-off live on deliverynote.
--
-- If you get #1146 (rawmaterialrequestnote does not exist), run first:
--   Scripts/migrate_rawmaterialrequestnote_tables.sql

-- 1) Delivery note: reply slip code + customer sign-off (DN status = delivered implies signed)
ALTER TABLE `deliverynote`
  ADD COLUMN `replySlipCode` varchar(30) DEFAULT NULL COMMENT 'Paired customer reply slip RS-+ID' AFTER `deliveryNoteCode`,
  ADD COLUMN `signedBy` varchar(100) DEFAULT NULL COMMENT '客户签收人' AFTER `remark`,
  ADD COLUMN `signedDate` date DEFAULT NULL COMMENT '客户签收日期' AFTER `signedBy`;

-- Unique RS code when present (ignore duplicate key if re-run)
SET @uk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'deliverynote'
    AND INDEX_NAME = 'uk_deliverynote_replyslipcode'
);
SET @sql_uk := IF(@uk_exists = 0,
  'ALTER TABLE `deliverynote` ADD UNIQUE KEY `uk_deliverynote_replyslipcode` (`replySlipCode`)',
  'SELECT 1');
PREPARE stmt FROM @sql_uk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `deliverynote`
SET `replySlipCode` = CONCAT('RS-', SUBSTRING(`deliveryNoteCode`, 4))
WHERE `deliveryNoteCode` LIKE 'DN-%'
  AND `deliveryNoteCode` <> 'DN-DEPOSIT'
  AND (`replySlipCode` IS NULL OR TRIM(`replySlipCode`) = '');

-- 2) Sales order: customer requested delivery date (Case Study 8.3)
ALTER TABLE `salesorder`
  ADD COLUMN `requestedDeliveryDate` date DEFAULT NULL COMMENT '客户期望交付日期' AFTER `deliveryAddress`;

-- 3) Reply slip status dictionary (UI maps DN-centric reply slips; optional if using DELIVERY_STATUS only)
INSERT INTO `systemdictionary` (`dictionaryID`, `category`, `codeValue`, `displayNameEnglish`, `codePrefix`, `sortOrder`)
SELECT 114, 'REPLY_SLIP_STATUS', 0, 'Draft', NULL, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE `category` = 'REPLY_SLIP_STATUS' AND `codeValue` = 0);
INSERT INTO `systemdictionary` (`dictionaryID`, `category`, `codeValue`, `displayNameEnglish`, `codePrefix`, `sortOrder`)
SELECT 115, 'REPLY_SLIP_STATUS', 1, 'Sent', NULL, 2 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE `category` = 'REPLY_SLIP_STATUS' AND `codeValue` = 1);
INSERT INTO `systemdictionary` (`dictionaryID`, `category`, `codeValue`, `displayNameEnglish`, `codePrefix`, `sortOrder`)
SELECT 116, 'REPLY_SLIP_STATUS', 2, 'Signed', NULL, 3 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE `category` = 'REPLY_SLIP_STATUS' AND `codeValue` = 2);
INSERT INTO `systemdictionary` (`dictionaryID`, `category`, `codeValue`, `displayNameEnglish`, `codePrefix`, `sortOrder`)
SELECT 117, 'REPLY_SLIP_STATUS', 3, 'Rejected', NULL, 4 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE `category` = 'REPLY_SLIP_STATUS' AND `codeValue` = 3);

-- 4) Admin account: Super User department (password hashed on app startup via StaffPasswordMigration)
UPDATE `staff`
SET `title` = 'Super User',
    `department` = 'Super User'
WHERE `username` = 'admin';
-- If admin password is still plain text, launch the ERP once; PBKDF2 migration runs automatically.

-- 5) Normalize INV / PV / RV codes to 8-digit zero-padded IDs
UPDATE `invoice`
SET `invoiceCode` = CONCAT('INV-', LPAD(`invoiceID`, 8, '0'))
WHERE `invoiceCode` REGEXP '^INV-[0-9]+$'
  AND `invoiceCode` NOT REGEXP '^INV-[0-9]{8}$';

UPDATE `paymentvoucher`
SET `paymentVoucherCode` = CONCAT('PV-', LPAD(`paymentVoucherID`, 8, '0'))
WHERE `paymentVoucherCode` REGEXP '^PV-[0-9]+$'
  AND `paymentVoucherCode` NOT REGEXP '^PV-[0-9]{8}$';

UPDATE `receiptvoucher`
SET `receiptVoucherCode` = CONCAT('RV-', LPAD(`receiptVoucherID`, 8, '0'))
WHERE `receiptVoucherCode` REGEXP '^RV-[0-9]+$'
  AND `receiptVoucherCode` NOT REGEXP '^RV-[0-9]{8}$';

-- 6) RM request note status for material issue workflow
SET @rmrn_table_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
);
SET @rmrn_status_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND COLUMN_NAME = 'status'
);
SET @sql_rmrn_status := IF(@rmrn_table_exists > 0 AND @rmrn_status_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD COLUMN `status` int NOT NULL DEFAULT 0 COMMENT ''0 Draft, 1 Partially Issued, 2 Completed, 3 Cancelled'' AFTER `requestDate`',
  'SELECT 1');
PREPARE stmt_rmrn FROM @sql_rmrn_status;
EXECUTE stmt_rmrn;
DEALLOCATE PREPARE stmt_rmrn;

SET @sql_rmrn_status_null := IF(@rmrn_table_exists > 0,
  'UPDATE `rawmaterialrequestnote` SET `status` = 0 WHERE `status` IS NULL',
  'SELECT 1');
PREPARE stmt_rmrn_status_null FROM @sql_rmrn_status_null;
EXECUTE stmt_rmrn_status_null;
DEALLOCATE PREPARE stmt_rmrn_status_null;

SET @sql_rmrn_status_mod := IF(@rmrn_table_exists > 0,
  'ALTER TABLE `rawmaterialrequestnote` MODIFY COLUMN `status` int NOT NULL DEFAULT 0 COMMENT ''0 Draft, 1 Partially Issued, 2 Completed, 3 Cancelled''',
  'SELECT 1');
PREPARE stmt_rmrn_status_mod FROM @sql_rmrn_status_mod;
EXECUTE stmt_rmrn_status_mod;
DEALLOCATE PREPARE stmt_rmrn_status_mod;

SET @sql_rmrn_status_old := IF(@rmrn_table_exists > 0,
  'UPDATE `rawmaterialrequestnote` SET `status` = 2 WHERE `status` = 0 AND `createDate` < DATE_SUB(CURDATE(), INTERVAL 30 DAY)',
  'SELECT 1');
PREPARE stmt_rmrn_status_old FROM @sql_rmrn_status_old;
EXECUTE stmt_rmrn_status_old;
DEALLOCATE PREPARE stmt_rmrn_status_old;

-- 7) RM request note ID auto-increment (AUTO_INCREMENT column must be PRIMARY KEY — #1075 if missing)
SET @sql_rmrn_delete_zero := IF(@rmrn_table_exists > 0,
  'DELETE FROM `rawmaterialrequestnote` WHERE `rawMaterialRequestNoteID` = 0',
  'SELECT 1');
PREPARE stmt_rmrn_delete_zero FROM @sql_rmrn_delete_zero;
EXECUTE stmt_rmrn_delete_zero;
DEALLOCATE PREPARE stmt_rmrn_delete_zero;

SET @rmrn_pk_exists := IF(@rmrn_table_exists = 0, 1, (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
));
SET @sql_rmrn_pk := IF(@rmrn_table_exists > 0 AND @rmrn_pk_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD PRIMARY KEY (`rawMaterialRequestNoteID`)',
  'SELECT 1');
PREPARE stmt_rmrn_pk FROM @sql_rmrn_pk;
EXECUTE stmt_rmrn_pk;
DEALLOCATE PREPARE stmt_rmrn_pk;

SET @rmrn_is_ai := IF(@rmrn_table_exists = 0, 1, (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND COLUMN_NAME = 'rawMaterialRequestNoteID'
    AND EXTRA LIKE '%auto_increment%'
));
SET @sql_rmrn_ai_col := IF(@rmrn_table_exists > 0 AND @rmrn_is_ai = 0,
  'ALTER TABLE `rawmaterialrequestnote` MODIFY COLUMN `rawMaterialRequestNoteID` bigint NOT NULL AUTO_INCREMENT',
  'SELECT 1');
PREPARE stmt_rmrn_ai_col FROM @sql_rmrn_ai_col;
EXECUTE stmt_rmrn_ai_col;
DEALLOCATE PREPARE stmt_rmrn_ai_col;

SET @rmrn_next_id := IF(@rmrn_table_exists = 0, 1, (
  SELECT COALESCE(MAX(`rawMaterialRequestNoteID`), 0) + 1 FROM `rawmaterialrequestnote`
));
SET @sql_rmrn_ai := IF(@rmrn_table_exists > 0,
  CONCAT('ALTER TABLE `rawmaterialrequestnote` AUTO_INCREMENT = ', @rmrn_next_id),
  'SELECT 1');
PREPARE stmt_rmrn_ai FROM @sql_rmrn_ai;
EXECUTE stmt_rmrn_ai;
DEALLOCATE PREPARE stmt_rmrn_ai;

-- 8) Seed warehouse stock (NOT EXISTS — INSERT IGNORE does nothing without PK and causes #1062 duplicates)
INSERT INTO `rawmaterialwarehouse` (`rawMaterialID`, `warehouseID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`)
SELECT rm.`rawMaterialID`, w.`warehouseID`,
       GREATEST(COALESCE(rm.`minimumStockLevel`, 10) * 10, 500.00),
       0.00, 0.00
FROM `rawmaterial` rm
CROSS JOIN `warehouse` w
WHERE w.`warehouseID` BETWEEN 1 AND 4
  AND NOT EXISTS (
      SELECT 1 FROM `rawmaterialwarehouse` x
      WHERE x.`rawMaterialID` = rm.`rawMaterialID` AND x.`warehouseID` = w.`warehouseID`
  );

INSERT INTO `rawmaterialwarehouse` (`rawMaterialID`, `warehouseID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`)
SELECT DISTINCT bom.`rawMaterialID`, w.`warehouseID`,
       120.00, 0.00, 0.00
FROM `productrawmaterialline` bom
CROSS JOIN `warehouse` w
WHERE w.`warehouseID` BETWEEN 5 AND 8
  AND NOT EXISTS (
      SELECT 1 FROM `rawmaterialwarehouse` x
      WHERE x.`rawMaterialID` = bom.`rawMaterialID` AND x.`warehouseID` = w.`warehouseID`
  );

UPDATE `rawmaterialwarehouse` rmw
INNER JOIN (
    SELECT `rawMaterialID`, MAX(2500.00) AS boostQty
    FROM `rawmaterialrequestnoterawmaterial_line`
    GROUP BY `rawMaterialID`
) scr ON scr.`rawMaterialID` = rmw.`rawMaterialID` AND rmw.`warehouseID` = 1
SET rmw.`physicalQuantity` = GREATEST(rmw.`physicalQuantity`, scr.boostQty);

INSERT INTO `rawmaterialwarehouse` (`rawMaterialID`, `warehouseID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`)
SELECT scr.`rawMaterialID`, 1, 2500.00, 0.00, 0.00
FROM (
    SELECT DISTINCT `rawMaterialID` FROM `rawmaterialrequestnoterawmaterial_line`
) scr
WHERE NOT EXISTS (
    SELECT 1 FROM `rawmaterialwarehouse` x
    WHERE x.`rawMaterialID` = scr.`rawMaterialID` AND x.`warehouseID` = 1
);

INSERT INTO `warehouseproduct` (`warehouseID`, `productID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`)
SELECT w.`warehouseID`, p.`productID`,
       CASE w.`warehouseID`
         WHEN 1 THEN 80.00
         WHEN 2 THEN 50.00
         WHEN 3 THEN 40.00
         ELSE 30.00
       END,
       0.00, 0.00
FROM `product` p
CROSS JOIN `warehouse` w
WHERE w.`warehouseID` BETWEEN 1 AND 4
  AND NOT EXISTS (
      SELECT 1 FROM `warehouseproduct` x
      WHERE x.`warehouseID` = w.`warehouseID` AND x.`productID` = p.`productID`
  );

-- Deduplicate before ADD PRIMARY KEY (#1062 duplicate entry '284-1')
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
DROP TABLE IF EXISTS `_rmw_dedup`;

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
DROP TABLE IF EXISTS `_wp_dedup`;

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

-- 9) Re-open seed SCR notes that still have material lines (section 6 may mark them completed)
UPDATE `rawmaterialrequestnote` n
INNER JOIN (
  SELECT DISTINCT `rawMaterialRequestNoteID` FROM `rawmaterialrequestnoterawmaterial_line`
) rl ON rl.`rawMaterialRequestNoteID` = n.`rawMaterialRequestNoteID`
SET n.`status` = 0
WHERE n.`status` = 2;

-- 10) Purchase order / GRN: remove id=0 rows and enable AUTO_INCREMENT (see Scripts/fix_purchaseorder_grn_ids.sql)
DELETE FROM `goodsreceivednoterawmaterialline` WHERE `goodsReceivedNoteID` = 0;
DELETE FROM `goodsreceivednote` WHERE `goodsReceivedNoteID` = 0;
DELETE FROM `paymentvoucherpurchaseorder` WHERE `purchaseOrderID` = 0;
DELETE FROM `purchaseorderrawmaterialline` WHERE `purchaseOrderID` = 0;
DELETE FROM `goodsreceivednote` WHERE `PurchaseOrderID` = 0;
DELETE FROM `purchaseorder` WHERE `purchaseOrderID` = 0;

SET @po_is_ai := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'purchaseorder'
    AND COLUMN_NAME = 'purchaseOrderID'
    AND EXTRA LIKE '%auto_increment%'
);
SET @sql_po_ai := IF(@po_is_ai = 0,
  'ALTER TABLE `purchaseorder` MODIFY COLUMN `purchaseOrderID` bigint NOT NULL AUTO_INCREMENT',
  'SELECT 1');
PREPARE stmt_po_ai FROM @sql_po_ai;
EXECUTE stmt_po_ai;
DEALLOCATE PREPARE stmt_po_ai;

SET @po_next_id := (SELECT COALESCE(MAX(`purchaseOrderID`), 0) + 1 FROM `purchaseorder`);
SET @sql_po_ai_val := CONCAT('ALTER TABLE `purchaseorder` AUTO_INCREMENT = ', @po_next_id);
PREPARE stmt_po_ai_val FROM @sql_po_ai_val;
EXECUTE stmt_po_ai_val;
DEALLOCATE PREPARE stmt_po_ai_val;

SET @grn_is_ai := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'goodsreceivednote'
    AND COLUMN_NAME = 'goodsReceivedNoteID'
    AND EXTRA LIKE '%auto_increment%'
);
SET @sql_grn_ai := IF(@grn_is_ai = 0,
  'ALTER TABLE `goodsreceivednote` MODIFY COLUMN `goodsReceivedNoteID` bigint NOT NULL AUTO_INCREMENT',
  'SELECT 1');
PREPARE stmt_grn_ai FROM @sql_grn_ai;
EXECUTE stmt_grn_ai;
DEALLOCATE PREPARE stmt_grn_ai;

SET @grn_next_id := (SELECT COALESCE(MAX(`goodsReceivedNoteID`), 0) + 1 FROM `goodsreceivednote`);
SET @sql_grn_ai_val := CONCAT('ALTER TABLE `goodsreceivednote` AUTO_INCREMENT = ', @grn_next_id);
PREPARE stmt_grn_ai_val FROM @sql_grn_ai_val;
EXECUTE stmt_grn_ai_val;
DEALLOCATE PREPARE stmt_grn_ai_val;
