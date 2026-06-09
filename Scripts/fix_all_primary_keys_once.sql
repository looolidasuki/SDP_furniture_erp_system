-- ONE-TIME FIX: duplicate PRIMARY KEY '0', missing AUTO_INCREMENT, dirty id=0 rows.
-- Run once on existing furniture_erp_system database (safe to re-run).
-- After this script, new inserts work via AUTO_INCREMENT OR app MAX(id)+1.
--
-- Usage (MySQL client):
--   USE furniture_erp_system;
--   SOURCE path/to/fix_all_primary_keys_once.sql;

SET FOREIGN_KEY_CHECKS = 0;

-- ========== 1) Remove id=0 header rows and dependent lines ==========
DELETE FROM `deliveryproductline` WHERE `deliveryNoteID` = 0;
DELETE FROM `goodsreceivednoterawmaterialline` WHERE `goodsReceivedNoteID` = 0;
DELETE FROM `purchaseorderrawmaterialline` WHERE `purchaseOrderID` = 0;
DELETE FROM `paymentvoucherpurchaseorder` WHERE `paymentVoucherID` = 0 OR `purchaseOrderID` = 0;
DELETE FROM `rawmaterialrequestnoterawmaterial_line` WHERE `rawMaterialRequestNoteID` = 0;
DELETE FROM `invoiceline` WHERE `invoiceID` = 0;
DELETE FROM `receiptvoucherinvoice` WHERE `receiptVoucherID` = 0;
DELETE FROM `salesorderproductline` WHERE `salesOrderID` = 0;
DELETE FROM `quotationproductline` WHERE `quotationID` = 0;
DELETE FROM `productionorderproductline` WHERE `productionOrderID` = 0;
DELETE FROM `productrawmaterialline` WHERE `productID` = 0 OR `rawMaterialID` = 0;

DELETE FROM `deliverynote` WHERE `deliveryNoteID` = 0;
DELETE FROM `goodsreceivednote` WHERE `goodsReceivedNoteID` = 0;
DELETE FROM `purchaseorder` WHERE `purchaseOrderID` = 0;
DELETE FROM `rawmaterialrequestnote` WHERE `rawMaterialRequestNoteID` = 0;
DELETE FROM `invoice` WHERE `invoiceID` = 0;
DELETE FROM `receiptvoucher` WHERE `receiptVoucherID` = 0;
DELETE FROM `paymentvoucher` WHERE `paymentVoucherID` = 0;
DELETE FROM `salesorder` WHERE `salesOrderID` = 0;
DELETE FROM `quotation` WHERE `quotationID` = 0;
DELETE FROM `productionorder` WHERE `productionOrderID` = 0;
DELETE FROM `refundrequest` WHERE `refundRequestID` = 0;
DELETE FROM `contactperson` WHERE `contactPersonID` = 0 OR `customerID` = 0;
DELETE FROM `customerdeliveryaddress` WHERE `addressID` = 0 OR `customerID` = 0;
DELETE FROM `customer` WHERE `customerID` = 0;
DELETE FROM `supplier` WHERE `supplierID` = 0;
DELETE FROM `staff` WHERE `staffID` = 0;
DELETE FROM `product` WHERE `productID` = 0;
DELETE FROM `rawmaterial` WHERE `rawMaterialID` = 0;
DELETE FROM `systemdictionary` WHERE `dictionaryID` = 0;

-- Keep reserved virtual rows: deliveryNoteID=999999 (DN-DEPOSIT), etc.

-- ========== 2) Enable AUTO_INCREMENT on all document/master tables ==========
-- Helper: run MODIFY only when column is not already AUTO_INCREMENT.

-- customer
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='customer' AND COLUMN_NAME='customerID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `customer` MODIFY COLUMN `customerID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`customerID`),0)+1 FROM `customer`);
SET @sql := CONCAT('ALTER TABLE `customer` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- contactperson
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='contactperson' AND COLUMN_NAME='contactPersonID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `contactperson` MODIFY COLUMN `contactPersonID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`contactPersonID`),0)+1 FROM `contactperson`);
SET @sql := CONCAT('ALTER TABLE `contactperson` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- customerdeliveryaddress
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='customerdeliveryaddress' AND COLUMN_NAME='addressID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `customerdeliveryaddress` MODIFY COLUMN `addressID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`addressID`),0)+1 FROM `customerdeliveryaddress`);
SET @sql := CONCAT('ALTER TABLE `customerdeliveryaddress` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- supplier
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='supplier' AND COLUMN_NAME='supplierID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `supplier` MODIFY COLUMN `supplierID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`supplierID`),0)+1 FROM `supplier`);
SET @sql := CONCAT('ALTER TABLE `supplier` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- staff
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='staff' AND COLUMN_NAME='staffID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `staff` MODIFY COLUMN `staffID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`staffID`),0)+1 FROM `staff`);
SET @sql := CONCAT('ALTER TABLE `staff` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- product
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='product' AND COLUMN_NAME='productID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `product` MODIFY COLUMN `productID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`productID`),0)+1 FROM `product`);
SET @sql := CONCAT('ALTER TABLE `product` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- rawmaterial
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='rawmaterial' AND COLUMN_NAME='rawMaterialID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `rawmaterial` MODIFY COLUMN `rawMaterialID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`rawMaterialID`),0)+1 FROM `rawmaterial`);
SET @sql := CONCAT('ALTER TABLE `rawmaterial` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- warehouse
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='warehouse' AND COLUMN_NAME='warehouseID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `warehouse` MODIFY COLUMN `warehouseID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`warehouseID`),0)+1 FROM `warehouse`);
SET @sql := CONCAT('ALTER TABLE `warehouse` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- quotation
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='quotation' AND COLUMN_NAME='quotationID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `quotation` MODIFY COLUMN `quotationID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`quotationID`),0)+1 FROM `quotation`);
SET @sql := CONCAT('ALTER TABLE `quotation` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- salesorder
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='salesorder' AND COLUMN_NAME='salesOrderID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `salesorder` MODIFY COLUMN `salesOrderID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`salesOrderID`),0)+1 FROM `salesorder`);
SET @sql := CONCAT('ALTER TABLE `salesorder` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- productionorder
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='productionorder' AND COLUMN_NAME='productionOrderID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `productionorder` MODIFY COLUMN `productionOrderID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`productionOrderID`),0)+1 FROM `productionorder`);
SET @sql := CONCAT('ALTER TABLE `productionorder` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- purchaseorder
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='purchaseorder' AND COLUMN_NAME='purchaseOrderID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `purchaseorder` MODIFY COLUMN `purchaseOrderID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`purchaseOrderID`),0)+1 FROM `purchaseorder`);
SET @sql := CONCAT('ALTER TABLE `purchaseorder` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- goodsreceivednote
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='goodsreceivednote' AND COLUMN_NAME='goodsReceivedNoteID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `goodsreceivednote` MODIFY COLUMN `goodsReceivedNoteID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`goodsReceivedNoteID`),0)+1 FROM `goodsreceivednote`);
SET @sql := CONCAT('ALTER TABLE `goodsreceivednote` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- deliverynote
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='deliverynote' AND COLUMN_NAME='deliveryNoteID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `deliverynote` MODIFY COLUMN `deliveryNoteID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`deliveryNoteID`),0)+1 FROM `deliverynote`);
SET @sql := CONCAT('ALTER TABLE `deliverynote` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- invoice
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='invoice' AND COLUMN_NAME='invoiceID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `invoice` MODIFY COLUMN `invoiceID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`invoiceID`),0)+1 FROM `invoice`);
SET @sql := CONCAT('ALTER TABLE `invoice` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- receiptvoucher
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='receiptvoucher' AND COLUMN_NAME='receiptVoucherID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `receiptvoucher` MODIFY COLUMN `receiptVoucherID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`receiptVoucherID`),0)+1 FROM `receiptvoucher`);
SET @sql := CONCAT('ALTER TABLE `receiptvoucher` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- paymentvoucher
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='paymentvoucher' AND COLUMN_NAME='paymentVoucherID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `paymentvoucher` MODIFY COLUMN `paymentVoucherID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`paymentVoucherID`),0)+1 FROM `paymentvoucher`);
SET @sql := CONCAT('ALTER TABLE `paymentvoucher` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- refundrequest
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='refundrequest' AND COLUMN_NAME='refundRequestID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `refundrequest` MODIFY COLUMN `refundRequestID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`refundRequestID`),0)+1 FROM `refundrequest`);
SET @sql := CONCAT('ALTER TABLE `refundrequest` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- rawmaterialrequestnote
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='rawmaterialrequestnote' AND COLUMN_NAME='rawMaterialRequestNoteID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `rawmaterialrequestnote` MODIFY COLUMN `rawMaterialRequestNoteID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`rawMaterialRequestNoteID`),0)+1 FROM `rawmaterialrequestnote`);
SET @sql := CONCAT('ALTER TABLE `rawmaterialrequestnote` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- systemdictionary
SET @ai := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='systemdictionary' AND COLUMN_NAME='dictionaryID' AND EXTRA LIKE '%auto_increment%');
SET @sql := IF(@ai=0, 'ALTER TABLE `systemdictionary` MODIFY COLUMN `dictionaryID` bigint NOT NULL AUTO_INCREMENT', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
SET @n := (SELECT COALESCE(MAX(`dictionaryID`),0)+1 FROM `systemdictionary`);
SET @sql := CONCAT('ALTER TABLE `systemdictionary` AUTO_INCREMENT = ', @n);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET FOREIGN_KEY_CHECKS = 1;

-- Done. Reserved rows preserved: deliveryNoteID 999999 (DN-DEPOSIT), etc.
