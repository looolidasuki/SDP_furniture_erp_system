-- Virtual delivery note for deposit / offset invoice lines (run once if not auto-created by app).
-- Requires at least one row in customer, salesorder, staff, warehouse.
INSERT INTO `deliverynote` (
  `deliveryNoteID`, `deliveryNoteCode`, `customerID`, `SalesOrderID`, `staffID`,
  `WarehouseID`, `shipMethod`, `trackingNumber`, `status`, `remark`
)
SELECT
  999999, 'DN-DEPOSIT',
  (SELECT MIN(customerID) FROM Customer),
  (SELECT MIN(salesOrderID) FROM SalesOrder),
  (SELECT MIN(staffID) FROM Staff),
  (SELECT MIN(warehouseID) FROM Warehouse),
  'N/A', '', 3, 'Virtual delivery note for deposit / offset invoice lines'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `deliverynote` WHERE `deliveryNoteCode` = 'DN-DEPOSIT');

-- Fix legacy deposit lines that used deliveryNoteID = 0 (if any).
UPDATE `invoiceline` il
INNER JOIN `product` p ON il.productID = p.productID
SET il.deliveryNoteID = 999999
WHERE p.productCode = 'DEPOSIT' AND il.deliveryNoteID = 0;
