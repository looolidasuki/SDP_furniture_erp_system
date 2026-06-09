-- Create RM request note tables when missing (fixes #1146: table does not exist).
-- Run in database: furniture_erp_system
-- Prerequisites: productionorder, staff, product, rawmaterial tables must already exist.

USE `furniture_erp_system`;

CREATE TABLE IF NOT EXISTS `rawmaterialrequestnote` (
  `rawMaterialRequestNoteID` bigint(20) NOT NULL,
  `rawMaterialRequestNoteCode` varchar(30) NOT NULL COMMENT '领料请购单编号格式SCR-XXXXXXXX',
  `ProductionOrderID` bigint(20) NOT NULL COMMENT '车间关联的源头排产生产订单',
  `staffID` bigint(20) NOT NULL COMMENT '申领发起员工',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `requestDate` date NOT NULL COMMENT '期望要求的到料领料日期',
  `status` int(11) NOT NULL DEFAULT 0 COMMENT '0 Draft, 1 Partially Issued, 2 Completed, 3 Cancelled',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='车间向仓储/采购部提报的物料领料申领及请购单';

CREATE TABLE IF NOT EXISTS `rawmaterialrequestnoterawmaterial_line` (
  `rawMaterialRequestNoteID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL COMMENT '要切片对应哪一个成品所需制造的配给',
  `rawMaterialID` bigint(20) NOT NULL COMMENT '具体申领的原材料',
  `rawMaterialRequestQuantity` decimal(10,2) NOT NULL COMMENT '本次申请流转的数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='领料请购单精确物料行明细';

-- status column for databases created before status was added
SET @rmrn_status_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND COLUMN_NAME = 'status'
);
SET @sql_rmrn_status := IF(@rmrn_status_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD COLUMN `status` int NOT NULL DEFAULT 0 COMMENT ''0 Draft, 1 Partially Issued, 2 Completed, 3 Cancelled'' AFTER `requestDate`',
  'SELECT 1');
PREPARE stmt_rmrn_status FROM @sql_rmrn_status;
EXECUTE stmt_rmrn_status;
DEALLOCATE PREPARE stmt_rmrn_status;

-- Primary key (required before AUTO_INCREMENT)
SET @rmrn_pk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
);
SET @sql_rmrn_pk := IF(@rmrn_pk_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD PRIMARY KEY (`rawMaterialRequestNoteID`)',
  'SELECT 1');
PREPARE stmt_rmrn_pk FROM @sql_rmrn_pk;
EXECUTE stmt_rmrn_pk;
DEALLOCATE PREPARE stmt_rmrn_pk;

SET @rmrn_code_uk_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND INDEX_NAME = 'rawMaterialRequestNoteCode'
);
SET @sql_rmrn_code_uk := IF(@rmrn_code_uk_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD UNIQUE KEY `rawMaterialRequestNoteCode` (`rawMaterialRequestNoteCode`)',
  'SELECT 1');
PREPARE stmt_rmrn_code_uk FROM @sql_rmrn_code_uk;
EXECUTE stmt_rmrn_code_uk;
DEALLOCATE PREPARE stmt_rmrn_code_uk;

SET @rmrn_po_idx_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND INDEX_NAME = 'fk_rmreq_po'
);
SET @sql_rmrn_po_idx := IF(@rmrn_po_idx_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD KEY `fk_rmreq_po` (`ProductionOrderID`)',
  'SELECT 1');
PREPARE stmt_rmrn_po_idx FROM @sql_rmrn_po_idx;
EXECUTE stmt_rmrn_po_idx;
DEALLOCATE PREPARE stmt_rmrn_po_idx;

SET @rmrn_staff_idx_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND INDEX_NAME = 'fk_rmreq_staff'
);
SET @sql_rmrn_staff_idx := IF(@rmrn_staff_idx_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD KEY `fk_rmreq_staff` (`staffID`)',
  'SELECT 1');
PREPARE stmt_rmrn_staff_idx FROM @sql_rmrn_staff_idx;
EXECUTE stmt_rmrn_staff_idx;
DEALLOCATE PREPARE stmt_rmrn_staff_idx;

SET @rmrnline_note_idx_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnoterawmaterial_line'
    AND INDEX_NAME = 'fk_rmreqline_note'
);
SET @sql_rmrnline_note_idx := IF(@rmrnline_note_idx_exists = 0,
  'ALTER TABLE `rawmaterialrequestnoterawmaterial_line` ADD KEY `fk_rmreqline_note` (`rawMaterialRequestNoteID`)',
  'SELECT 1');
PREPARE stmt_rmrnline_note_idx FROM @sql_rmrnline_note_idx;
EXECUTE stmt_rmrnline_note_idx;
DEALLOCATE PREPARE stmt_rmrnline_note_idx;

SET @rmrnline_raw_idx_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnoterawmaterial_line'
    AND INDEX_NAME = 'fk_rmreqline_raw'
);
SET @sql_rmrnline_raw_idx := IF(@rmrnline_raw_idx_exists = 0,
  'ALTER TABLE `rawmaterialrequestnoterawmaterial_line` ADD KEY `fk_rmreqline_raw` (`rawMaterialID`)',
  'SELECT 1');
PREPARE stmt_rmrnline_raw_idx FROM @sql_rmrnline_raw_idx;
EXECUTE stmt_rmrnline_raw_idx;
DEALLOCATE PREPARE stmt_rmrnline_raw_idx;

-- Foreign keys (skip if already present)
SET @fk_rmreq_po_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND CONSTRAINT_NAME = 'fk_rmreq_po'
);
SET @sql_fk_rmreq_po := IF(@fk_rmreq_po_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD CONSTRAINT `fk_rmreq_po` FOREIGN KEY (`ProductionOrderID`) REFERENCES `productionorder` (`productionOrderID`) ON UPDATE CASCADE',
  'SELECT 1');
PREPARE stmt_fk_rmreq_po FROM @sql_fk_rmreq_po;
EXECUTE stmt_fk_rmreq_po;
DEALLOCATE PREPARE stmt_fk_rmreq_po;

SET @fk_rmreq_staff_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND CONSTRAINT_NAME = 'fk_rmreq_staff'
);
SET @sql_fk_rmreq_staff := IF(@fk_rmreq_staff_exists = 0,
  'ALTER TABLE `rawmaterialrequestnote` ADD CONSTRAINT `fk_rmreq_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE',
  'SELECT 1');
PREPARE stmt_fk_rmreq_staff FROM @sql_fk_rmreq_staff;
EXECUTE stmt_fk_rmreq_staff;
DEALLOCATE PREPARE stmt_fk_rmreq_staff;

SET @fk_rmreqline_note_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnoterawmaterial_line'
    AND CONSTRAINT_NAME = 'fk_rmreqline_note'
);
SET @sql_fk_rmreqline_note := IF(@fk_rmreqline_note_exists = 0,
  'ALTER TABLE `rawmaterialrequestnoterawmaterial_line` ADD CONSTRAINT `fk_rmreqline_note` FOREIGN KEY (`rawMaterialRequestNoteID`) REFERENCES `rawmaterialrequestnote` (`rawMaterialRequestNoteID`) ON DELETE CASCADE ON UPDATE CASCADE',
  'SELECT 1');
PREPARE stmt_fk_rmreqline_note FROM @sql_fk_rmreqline_note;
EXECUTE stmt_fk_rmreqline_note;
DEALLOCATE PREPARE stmt_fk_rmreqline_note;

SET @fk_rmreqline_raw_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnoterawmaterial_line'
    AND CONSTRAINT_NAME = 'fk_rmreqline_raw'
);
SET @sql_fk_rmreqline_raw := IF(@fk_rmreqline_raw_exists = 0,
  'ALTER TABLE `rawmaterialrequestnoterawmaterial_line` ADD CONSTRAINT `fk_rmreqline_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE',
  'SELECT 1');
PREPARE stmt_fk_rmreqline_raw FROM @sql_fk_rmreqline_raw;
EXECUTE stmt_fk_rmreqline_raw;
DEALLOCATE PREPARE stmt_fk_rmreqline_raw;

-- Optional AUTO_INCREMENT (app also allocates IDs via MAX+1)
DELETE FROM `rawmaterialrequestnote` WHERE `rawMaterialRequestNoteID` = 0;

SET @rmrn_is_ai := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'rawmaterialrequestnote'
    AND COLUMN_NAME = 'rawMaterialRequestNoteID'
    AND EXTRA LIKE '%auto_increment%'
);
SET @sql_rmrn_ai_col := IF(@rmrn_is_ai = 0,
  'ALTER TABLE `rawmaterialrequestnote` MODIFY COLUMN `rawMaterialRequestNoteID` bigint NOT NULL AUTO_INCREMENT',
  'SELECT 1');
PREPARE stmt_rmrn_ai_col FROM @sql_rmrn_ai_col;
EXECUTE stmt_rmrn_ai_col;
DEALLOCATE PREPARE stmt_rmrn_ai_col;

SET @rmrn_next_id := (SELECT COALESCE(MAX(`rawMaterialRequestNoteID`), 0) + 1 FROM `rawmaterialrequestnote`);
SET @sql_rmrn_ai := CONCAT('ALTER TABLE `rawmaterialrequestnote` AUTO_INCREMENT = ', @rmrn_next_id);
PREPARE stmt_rmrn_ai FROM @sql_rmrn_ai;
EXECUTE stmt_rmrn_ai;
DEALLOCATE PREPARE stmt_rmrn_ai;
