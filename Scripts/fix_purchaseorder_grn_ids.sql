-- Fix purchaseorder / goodsreceivednote rows with purchaseOrderID or goodsReceivedNoteID = 0,
-- and enable AUTO_INCREMENT on primary keys (safe to re-run).

-- Remove invalid PO id=0 and dependents
DELETE FROM `goodsreceivednoterawmaterialline` WHERE `goodsReceivedNoteID` = 0;
DELETE FROM `goodsreceivednote` WHERE `goodsReceivedNoteID` = 0;

DELETE FROM `paymentvoucherpurchaseorder` WHERE `purchaseOrderID` = 0;
DELETE FROM `purchaseorderrawmaterialline` WHERE `purchaseOrderID` = 0;
DELETE FROM `goodsreceivednote` WHERE `PurchaseOrderID` = 0;
DELETE FROM `purchaseorder` WHERE `purchaseOrderID` = 0;

-- purchaseorder: ensure PK + AUTO_INCREMENT
SET @po_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'purchaseorder'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_po_pk := IF(@po_pk_exists = 0,
  'ALTER TABLE `purchaseorder` ADD PRIMARY KEY (`purchaseOrderID`)',
  'SELECT 1');
PREPARE stmt_po_pk FROM @sql_po_pk;
EXECUTE stmt_po_pk;
DEALLOCATE PREPARE stmt_po_pk;

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

-- goodsreceivednote: ensure PK + AUTO_INCREMENT
SET @grn_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'goodsreceivednote'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_grn_pk := IF(@grn_pk_exists = 0,
  'ALTER TABLE `goodsreceivednote` ADD PRIMARY KEY (`goodsReceivedNoteID`)',
  'SELECT 1');
PREPARE stmt_grn_pk FROM @sql_grn_pk;
EXECUTE stmt_grn_pk;
DEALLOCATE PREPARE stmt_grn_pk;

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
