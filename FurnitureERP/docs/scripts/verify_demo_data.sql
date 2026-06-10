-- Premium Living ERP — 演示前数据自检（作业功能对照版）
-- USE furniture_erp_system;
-- SOURCE path/to/FurnitureERP/docs/scripts/verify_demo_data.sql;

SET NAMES utf8mb4;

SELECT '=== [Login] admin 账号 ===' AS section;
SELECT staffID, username, title, status,
       CASE WHEN username = 'admin' AND status = 1 THEN 'OK' ELSE 'FAIL' END AS check_result
FROM staff WHERE username = 'admin';

SELECT '=== [Master Data] 客户 / 供应商 / 员工 ===' AS section;
SELECT 'customer' AS entity, COUNT(*) AS cnt FROM customer
UNION ALL SELECT 'supplier (active)', COUNT(*) FROM supplier WHERE status = 1
UNION ALL SELECT 'staff (active)', COUNT(*) FROM staff WHERE status = 1;

SELECT customerID, customerName FROM customer WHERE customerID = 1;
SELECT supplierID, supplierName FROM supplier WHERE supplierID = 1 AND status = 1;

SELECT '=== [Order] 产品与销售单 ===' AS section;
SELECT productID, productCode, status FROM product
WHERE productCode IN ('P-Chair-2001', 'P-Table-2002');

SELECT COUNT(*) AS salesorder_count FROM salesorder;
SELECT COUNT(*) AS confirmed_so_count FROM salesorder WHERE status >= 1;

SELECT '=== [Logistics] 发货单 / 收货单 ===' AS section;
SELECT COUNT(*) AS deliverynote_count FROM deliverynote;
SELECT COUNT(*) AS grn_count FROM goodsreceivednote;
SELECT COUNT(*) AS po_count FROM purchaseorder;

SELECT grn.goodsReceivedNoteID, grn.goodsReceivedNoteCode, grn.status AS grn_status,
       po.purchaseOrderCode
FROM goodsreceivednote grn
LEFT JOIN purchaseorder po ON po.purchaseOrderID = grn.purchaseOrderID
ORDER BY grn.goodsReceivedNoteID DESC
LIMIT 5;

SELECT '=== [Inventory] 仓库与原料库存 ===' AS section;
SELECT warehouseID, warehouseName FROM warehouse WHERE warehouseID IN (1, 5);
SELECT COUNT(*) AS rm_stock_rows FROM rawmaterialwarehouse;
SELECT COUNT(*) AS product_stock_rows FROM warehouseproduct;

SELECT '=== [After-service] 退款 ===' AS section;
SELECT COUNT(*) AS refund_count FROM refundrequest;
SELECT COUNT(*) AS invoice_count FROM invoice;

SELECT '=== [Logistics PO] 供应商报价（建 PO 用）===' AS section;
SELECT s.supplierID, s.supplierName, COUNT(rms.rawMaterialID) AS quotes
FROM supplier s
LEFT JOIN rawmaterialsupplier rms ON rms.supplierID = s.supplierID AND rms.status = 1
WHERE s.supplierID IN (1, 2, 4) AND s.status = 1
GROUP BY s.supplierID, s.supplierName;

SELECT '=== [Database] 业务表存量汇总 ===' AS section;
SELECT 'quotation' AS tbl, COUNT(*) AS rows FROM quotation
UNION ALL SELECT 'salesorder', COUNT(*) FROM salesorder
UNION ALL SELECT 'deliverynote', COUNT(*) FROM deliverynote
UNION ALL SELECT 'goodsreceivednote', COUNT(*) FROM goodsreceivednote
UNION ALL SELECT 'invoice', COUNT(*) FROM invoice
UNION ALL SELECT 'refundrequest', COUNT(*) FROM refundrequest
UNION ALL SELECT 'documentauditlog', COUNT(*) FROM documentauditlog;

SELECT '=== DONE — 对照 DEMO_CHECKLIST 逐项确认 ===' AS section;
