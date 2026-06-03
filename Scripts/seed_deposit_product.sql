-- Virtual product for deposit invoicing (run once if not auto-created by app).
INSERT INTO `product` (`productID`, `productCode`, `category`, `sequenceNumber`, `styleNumber`, `size`, `color`, `basePriceByCurrency`, `currencyID`, `staffID`, `unit`, `createDate`, `status`, `remark`)
SELECT 9999, 'DEPOSIT', 'Service', 9999, 'DEPOSIT', '-', '-', 0.00, 1, 1, 'LOT', NOW(), 1, 'Virtual line for customer deposit invoices'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `product` WHERE `productCode` = 'DEPOSIT');
