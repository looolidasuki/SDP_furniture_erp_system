-- Pair each delivery note with a reply slip code (RS-*) for customer sign-off printing.
ALTER TABLE `deliverynote`
  ADD COLUMN `replySlipCode` varchar(30) DEFAULT NULL COMMENT 'Paired customer reply slip RS-+ID' AFTER `deliveryNoteCode`,
  ADD COLUMN `signedBy` varchar(100) DEFAULT NULL COMMENT 'Customer sign-off name' AFTER `remark`,
  ADD COLUMN `signedDate` date DEFAULT NULL COMMENT 'Customer sign-off date' AFTER `signedBy`;

ALTER TABLE `deliverynote`
  ADD UNIQUE KEY `uk_deliverynote_replyslipcode` (`replySlipCode`);

UPDATE `deliverynote`
SET `replySlipCode` = CONCAT('RS-', SUBSTRING(`deliveryNoteCode`, 4))
WHERE `deliveryNoteCode` LIKE 'DN-%'
  AND `deliveryNoteCode` <> 'DN-DEPOSIT'
  AND (`replySlipCode` IS NULL OR `replySlipCode` = '' OR `replySlipCode` <> CONCAT('RS-', SUBSTRING(`deliveryNoteCode`, 4)));
