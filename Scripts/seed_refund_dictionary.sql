-- Refund method / reason / status dictionaries (run once; skips duplicates).
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 1, 'Bank Transfer', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 1);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 2, 'FPS', 2 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 2);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 3, 'Cheque', 3 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 3);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 4, 'TT', 4 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 4);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 5, 'PayPal', 5 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 5);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 6, 'Amazon Pay', 6 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 6);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_METHOD', 7, 'Taobao Pay', 7 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_METHOD' AND codeValue = 7);

INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_REASON', 1, 'Damage', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_REASON' AND codeValue = 1);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_REASON', 2, 'Wrong Shipment', 2 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_REASON' AND codeValue = 2);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_REASON', 3, 'Sizing Issue', 3 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_REASON' AND codeValue = 3);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_REASON', 4, 'Order Cancelled', 4 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_REASON' AND codeValue = 4);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_REASON', 5, 'Customer Dissatisfaction', 5 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_REASON' AND codeValue = 5);

INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_STATUS', 0, 'Draft', 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_STATUS' AND codeValue = 0);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_STATUS', 1, 'Approved', 2 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_STATUS' AND codeValue = 1);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_STATUS', 2, 'Paid', 3 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_STATUS' AND codeValue = 2);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_STATUS', 3, 'Rejected', 4 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_STATUS' AND codeValue = 3);
INSERT INTO `systemdictionary` (`category`, `codeValue`, `displayNameEnglish`, `sortOrder`)
SELECT 'REFUND_STATUS', 4, 'Cancelled', 5 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `systemdictionary` WHERE category = 'REFUND_STATUS' AND codeValue = 4);
