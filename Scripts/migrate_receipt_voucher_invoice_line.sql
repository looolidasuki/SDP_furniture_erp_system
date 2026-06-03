-- Receipt voucher invoice allocations: support exchange-loss lines (invoiceID NULL).
-- Run once on existing furniture_erp_system database.

ALTER TABLE receiptvoucherinvoice DROP FOREIGN KEY fk_rvi_inv;
ALTER TABLE receiptvoucherinvoice DROP PRIMARY KEY;

ALTER TABLE receiptvoucherinvoice
  ADD COLUMN lineNo int(10) NOT NULL DEFAULT 1 COMMENT 'Allocation line sequence per receipt voucher' AFTER receiptVoucherID;

UPDATE receiptvoucherinvoice rvi
INNER JOIN (
  SELECT receiptVoucherID, invoiceID,
         ROW_NUMBER() OVER (PARTITION BY receiptVoucherID ORDER BY invoiceID) AS rn
  FROM receiptvoucherinvoice
) numbered ON rvi.receiptVoucherID = numbered.receiptVoucherID
           AND rvi.invoiceID = numbered.invoiceID
SET rvi.lineNo = numbered.rn;

ALTER TABLE receiptvoucherinvoice
  MODIFY invoiceID bigint(20) NULL COMMENT 'NULL for exchange-loss lines (type=4)';

ALTER TABLE receiptvoucherinvoice
  ADD PRIMARY KEY (receiptVoucherID, lineNo),
  ADD KEY fk_rvi_inv (invoiceID);

ALTER TABLE receiptvoucherinvoice
  ADD CONSTRAINT fk_rvi_inv FOREIGN KEY (invoiceID) REFERENCES invoice (invoiceID) ON UPDATE CASCADE;
