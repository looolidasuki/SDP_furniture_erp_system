-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- 主机： 127.0.0.1
-- 生成日期： 2026-05-25 22:46:51
-- 服务器版本： 10.4.32-MariaDB
-- PHP 版本： 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- 数据库： `furniture_erp_system`
--

-- --------------------------------------------------------

--
-- 表的结构 `contactperson`
--

CREATE TABLE `contactperson` (
  `contactPersonID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `customerID` bigint(20) NOT NULL COMMENT '关联客户',
  `contactPerson` varchar(100) DEFAULT NULL COMMENT '联系人姓名',
  `title` varchar(30) DEFAULT NULL COMMENT '称谓/职位',
  `phone` varchar(30) DEFAULT NULL COMMENT '电话',
  `email` varchar(255) DEFAULT NULL COMMENT '邮箱'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户联系人明细表';

INSERT INTO `contactperson` (`contactPersonID`, `customerID`, `contactPerson`, `title`, `phone`, `email`) VALUES
(1, 1, 'Alice Wong', 'Purchasing Manager', '2123-4501', 'alice.wong@abcfurniture.hk'),
(2, 2, 'Ben Lee', 'Director', '2987-1200', 'ben.lee@pacifichome.hk'),
(3, 3, 'Cindy Ho', 'Owner', '2655-8800', 'cindy@urbanliving.hk'),
(4, 4, 'David Cheung', 'Procurement', '2398-7701', 'david.cheung@greenoffice.hk'),
(5, 5, 'Emily Ng', 'Sales Contact', '2722-3300', 'emily.ng@eliteinteriors.hk');

-- --------------------------------------------------------

--
-- 表的结构 `currency`
--

CREATE TABLE `currency` (
  `currencyID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `currencyCode` varchar(30) NOT NULL COMMENT '货币代码，如USD, HKD',
  `currencySymbol` varchar(5) NOT NULL COMMENT '货币符号',
  `rateToBase` decimal(10,2) NOT NULL COMMENT '当前币种对基准货币的汇率'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='货币汇率基础表';

--
-- 转存表中的数据 `currency`
--

INSERT INTO `currency` (`currencyID`, `currencyCode`, `currencySymbol`, `rateToBase`) VALUES
(1, 'HKD', '$', 1.00),
(2, 'USD', 'US$', 7.80),
(3, 'CNY', '¥', 1.08),
(4, 'EUR', '€', 8.50),
(5, 'GBP', '£', 9.90);

-- --------------------------------------------------------

--
-- 表的结构 `customer`
--

CREATE TABLE `customer` (
  `customerID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `customerCode` varchar(30) DEFAULT NULL COMMENT '客户编号，供快速查询',
  `customerRefNumber` varchar(50) DEFAULT NULL COMMENT '客户参考编号（格式：PO-PL-#########）',
  `customerName` varchar(255) DEFAULT NULL COMMENT '客户名称',
  `billingAddress` varchar(255) DEFAULT NULL COMMENT '账单地址',
  `paymentTerm` varchar(100) DEFAULT NULL COMMENT '付款条款',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp() COMMENT '创建时间',
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp() COMMENT '最后修改时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户主表';

--
-- 转存表中的数据 `customer`
--

INSERT INTO `customer` (`customerID`, `customerCode`, `customerRefNumber`, `customerName`, `billingAddress`, `paymentTerm`, `createDate`, `lastModifyDate`) VALUES
(1, 'CU-000000001', 'PO-PL-000000001', 'ABC Furniture Ltd', '88 Queensway, Central, Hong Kong', '30 Days', '2026-05-25 17:16:09', '2026-05-25 17:16:09'),
(2, 'CU-000000002', 'PO-PL-000000002', 'Pacific Home Co', '12 Harbour Road, Wanchai, HK', '60 Days', '2026-05-26 09:00:00', '2026-05-26 09:00:00'),
(3, 'CU-000000003', 'PO-PL-000000003', 'Urban Living Studio', '5 Science Park, Shatin, NT', 'Cash', '2026-05-26 10:00:00', '2026-05-26 10:00:00'),
(4, 'CU-000000004', 'PO-PL-000000004', 'Green Office Supplies', '200 Nathan Road, Mongkok, HK', '30 Days', '2026-05-26 11:00:00', '2026-05-26 11:00:00'),
(5, 'CU-000000005', 'PO-PL-000000005', 'Elite Interiors HK', '1 Austin Road, Tsim Sha Tsui, HK', '90 Days', '2026-05-26 12:00:00', '2026-05-26 12:00:00');

-- --------------------------------------------------------

--
-- 表的结构 `customerdeliveryaddress`
--

CREATE TABLE `customerdeliveryaddress` (
  `addressID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `customerID` bigint(20) NOT NULL COMMENT '关联客户',
  `deliveryAddress` varchar(255) DEFAULT NULL COMMENT '收货寄送地址',
  `contactPerson` varchar(100) DEFAULT NULL COMMENT '收货联系人',
  `phone` varchar(30) DEFAULT NULL COMMENT '收货电话',
  `email` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户收货地址表';

INSERT INTO `customerdeliveryaddress` (`addressID`, `customerID`, `deliveryAddress`, `contactPerson`, `phone`, `email`) VALUES
(1, 1, 'Flat B, 5/F, Kwun Tong Industrial Building', 'Alice Wong', '2123-4501', 'alice.wong@abcfurniture.hk'),
(2, 2, 'Warehouse 3, Tuen Mun Logistics Centre', 'Ben Lee', '2987-1200', 'ben.lee@pacifichome.hk'),
(3, 3, 'Shop 12, Festival Walk, Kowloon Tong', 'Cindy Ho', '2655-8800', 'cindy@urbanliving.hk'),
(4, 4, 'Loading Bay, Mongkok Commercial Tower', 'David Cheung', '2398-7701', 'david.cheung@greenoffice.hk'),
(5, 5, '15/F, TST Plaza, Tsim Sha Tsui', 'Emily Ng', '2722-3300', 'emily.ng@eliteinteriors.hk');

-- --------------------------------------------------------

--
-- 表的结构 `deliverynote`
--

CREATE TABLE `deliverynote` (
  `deliveryNoteID` bigint(20) NOT NULL,
  `deliveryNoteCode` varchar(30) NOT NULL,
  `customerID` bigint(20) NOT NULL,
  `SalesOrderID` bigint(20) NOT NULL,
  `staffID` bigint(20) NOT NULL,
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp(),
  `WarehouseID` bigint(20) NOT NULL COMMENT '从哪一个出货物理仓出库',
  `shipMethod` varchar(30) NOT NULL COMMENT '发货运输方式',
  `trackingNumber` varchar(30) NOT NULL COMMENT '快递/物流追踪单号',
  `remark` varchar(255) DEFAULT NULL,
  `status` int(10) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='销售发货/出库流水单';

-- --------------------------------------------------------

--
-- 表的结构 `deliveryproductline`
--

CREATE TABLE `deliveryproductline` (
  `deliveryNoteID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `shipQuantity` int(10) NOT NULL COMMENT '本次包裹实际发货发出数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='发货单货品打包封装明细';

-- --------------------------------------------------------

--
-- 表的结构 `goodsreceivednote`
--

CREATE TABLE `goodsreceivednote` (
  `goodsReceivedNoteID` bigint(20) NOT NULL,
  `goodsReceivedNoteCode` varchar(30) NOT NULL,
  `supplierID` bigint(20) NOT NULL,
  `PurchaseOrderID` bigint(20) NOT NULL COMMENT '关联的采购单源头',
  `staffID` bigint(20) NOT NULL COMMENT '收货仓管验收员',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL,
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='原材料采购到货验收及入库单';

-- --------------------------------------------------------

--
-- 表的结构 `goodsreceivednoterawmaterialline`
--

CREATE TABLE `goodsreceivednoterawmaterialline` (
  `goodsReceivedNoteID` bigint(20) NOT NULL,
  `rawMaterialID` bigint(20) NOT NULL,
  `receivedQuantity` decimal(10,2) NOT NULL COMMENT '本次送达包裹清点合规后实际吃进库存的数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='采购到货入库原料明细账目';

-- --------------------------------------------------------

--
-- 表的结构 `invoice`
--

CREATE TABLE `invoice` (
  `invoiceID` bigint(20) NOT NULL,
  `invoiceCode` varchar(30) NOT NULL,
  `customerID` bigint(20) NOT NULL,
  `salesOrderID` bigint(20) NOT NULL,
  `staffID` bigint(20) NOT NULL,
  `invoiceType` int(10) NOT NULL COMMENT '类型：deposit(定金发票), normal(出货正规发票)',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp(),
  `remark` varchar(255) DEFAULT NULL,
  `status` int(10) NOT NULL COMMENT '开票对账状态'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='应收发票对账主表';

--
-- 转存表中的数据 `invoice`
--

INSERT INTO `invoice` (`invoiceID`, `invoiceCode`, `customerID`, `salesOrderID`, `staffID`, `invoiceType`, `createDate`, `lastModifyDate`, `remark`, `status`) VALUES
(1, 'INV-2026052601', 1, 1, 1, 1, '2026-05-25 20:06:43', '2026-05-25 20:06:43', 'Deposit invoice for SO-1', 0),
(2, 'INV-2026052602', 2, 2, 1, 1, '2026-05-26 09:00:00', '2026-05-26 09:00:00', 'Deposit for Pacific Home', 0),
(3, 'INV-2026052603', 3, 3, 1, 2, '2026-05-26 10:00:00', '2026-05-26 10:00:00', 'Partial shipment billing', 1),
(4, 'INV-2026052604', 4, 4, 1, 2, '2026-05-26 11:00:00', '2026-05-26 11:00:00', 'Normal invoice', 0),
(5, 'INV-2026052605', 5, 5, 1, 1, '2026-05-26 12:00:00', '2026-05-26 12:00:00', 'Draft deposit', 0);

-- --------------------------------------------------------

--
-- 表的结构 `invoiceline`
--

CREATE TABLE `invoiceline` (
  `invoiceID` bigint(20) NOT NULL,
  `deliveryNoteID` bigint(20) NOT NULL COMMENT '多包裹分批出货时，开票需追溯对应的出库单',
  `productID` bigint(20) NOT NULL COMMENT '特殊：如果是deposit定金开票类型，此ID可以写入一个虚拟符号并在明细存负项冲平',
  `invoiceQuantity` int(10) NOT NULL COMMENT '本次开票计费数量',
  `amount` decimal(12,2) NOT NULL COMMENT '本次计费金额（包含负项冲减）'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='发票计费项目明细表（支持定金扣减逻辑）';

-- --------------------------------------------------------

--
-- 表的结构 `paymentvoucher`
--

CREATE TABLE `paymentvoucher` (
  `paymentVoucherID` bigint(20) NOT NULL,
  `paymentVoucherCode` varchar(30) NOT NULL,
  `supplierID` bigint(20) NOT NULL,
  `staffID` bigint(20) NOT NULL COMMENT '财务出纳审签经办人',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `paymentMethod` varchar(50) NOT NULL COMMENT '对公付汇渠道方式',
  `paymentMethodRef` varchar(100) NOT NULL COMMENT '银行付款水单参考号',
  `totalAmount` decimal(12,2) NOT NULL COMMENT '本次实际支付给该供应商的总汇款数',
  `remark` varchar(255) DEFAULT NULL,
  `status` int(10) NOT NULL COMMENT '应付款对账核销状态'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='应付上游供应商采购货款财务出账单';

-- --------------------------------------------------------

--
-- 表的结构 `paymentvoucherpurchaseorder`
--

CREATE TABLE `paymentvoucherpurchaseorder` (
  `paymentVoucherID` bigint(20) NOT NULL,
  `purchaseOrderID` bigint(20) NOT NULL,
  `type` int(10) NOT NULL COMMENT '应付对账阶段划分',
  `payAmount` decimal(12,2) NOT NULL COMMENT '本张水单里的款项中有多少额度被拿去核销了该张采购订单的欠款'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='财务应付账款实际冲账核销关联表';

-- --------------------------------------------------------

--
-- 表的结构 `product`
--

CREATE TABLE `product` (
  `productID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `productCode` varchar(30) NOT NULL COMMENT '模式 P-+category+sequenceNumber',
  `category` varchar(30) NOT NULL COMMENT '类别（如上衣、裤子）',
  `sequenceNumber` int(10) DEFAULT NULL COMMENT '序列编号',
  `styleNumber` varchar(30) NOT NULL COMMENT '衣服款号',
  `size` varchar(30) NOT NULL COMMENT '尺码',
  `color` varchar(30) NOT NULL COMMENT '颜色',
  `basePriceByCurrency` decimal(10,2) NOT NULL COMMENT '基本售价',
  `currencyID` bigint(20) NOT NULL COMMENT '定价币种',
  `staffID` bigint(20) NOT NULL COMMENT '录入员工',
  `unit` varchar(30) NOT NULL COMMENT '计量单位',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL COMMENT '商品状态',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='成品服饰SKU信息表';

--
-- 转存表中的数据 `product`
--

INSERT INTO `product` (`productID`, `productCode`, `category`, `sequenceNumber`, `styleNumber`, `size`, `color`, `basePriceByCurrency`, `currencyID`, `staffID`, `unit`, `createDate`, `lastModifyDate`, `status`, `remark`) VALUES
(1, 'P-Desk-1001', 'Desk', 1001, 'OAK-DESK-01', '160x80cm', 'Natural Oak', 3200.00, 1, 1, 'PCS', '2026-05-25 17:20:01', NULL, 1, 'Solid oak executive desk.'),
(2, 'P-Chair-2001', 'Chair', 2001, 'ERG-CHAIR-01', 'Standard High-Back', 'Matte Black', 1250.00, 1, 1, 'PCS', '2026-05-25 17:26:01', NULL, 1, 'Ergonomic office chair with adjustable mesh armrests and lumbar support.'),
(3, 'P-Sofa-3001', 'Sofa', 3001, 'L-SOFA-01', '3-Seater', 'Grey Fabric', 5800.00, 1, 2, 'PCS', '2026-05-26 09:30:00', NULL, 1, 'L-shaped lounge sofa.'),
(4, 'P-Cabinet-4001', 'Cabinet', 4001, 'FILE-CAB-01', '4-Drawer', 'White', 980.00, 1, 2, 'PCS', '2026-05-26 10:00:00', NULL, 1, 'Metal filing cabinet.'),
(5, 'P-Table-5001', 'Table', 5001, 'DIN-TBL-01', '180x90cm', 'Walnut', 4500.00, 1, 3, 'PCS', '2026-05-26 11:00:00', NULL, 1, 'Dining table with walnut veneer.');

-- --------------------------------------------------------

--
-- 表的结构 `productimage`
--

CREATE TABLE `productimage` (
  `productID` bigint(20) NOT NULL COMMENT '一对一或多对一关联Product',
  `productImageUrl` varchar(255) DEFAULT NULL COMMENT '图片托管URL地址'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='商品图片附表';

-- --------------------------------------------------------

--
-- 表的结构 `productionorder`
--

CREATE TABLE `productionorder` (
  `productionOrderID` bigint(20) NOT NULL,
  `productionOrderCode` varchar(30) NOT NULL COMMENT '模式 PO-+ID',
  `salesOrderID` bigint(20) NOT NULL COMMENT '派生出此工单的销售单',
  `staffID` bigint(20) NOT NULL COMMENT '车间排产跟进员工',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `estFinishDate` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00' COMMENT '预计完工交期',
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL COMMENT '车间生产状态控制',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='车间服装生产工单表';

-- --------------------------------------------------------

--
-- 表的结构 `productionorderproductline`
--

CREATE TABLE `productionorderproductline` (
  `ProductionOrderID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `productionQty` int(10) NOT NULL COMMENT '计算逻辑：salesOrderProductLine.quantity - warehouseReservedQty'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='生产工单货品及数量明细';

-- --------------------------------------------------------

--
-- 表的结构 `productrawmaterialline`
--

CREATE TABLE `productrawmaterialline` (
  `productID` bigint(20) NOT NULL,
  `rawMaterialID` bigint(20) NOT NULL,
  `rawMaterialNeedQty` decimal(10,2) NOT NULL COMMENT '标准工艺下单件成品所需此原料的数量消耗定额',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='商品衣服物料清单配方（BOM表）';

-- --------------------------------------------------------

--
-- 表的结构 `purchaseorder`
--

CREATE TABLE `purchaseorder` (
  `purchaseOrderID` bigint(20) NOT NULL,
  `purchaseOrderCode` varchar(30) NOT NULL COMMENT '模式 PO-+ID',
  `supplierID` bigint(20) NOT NULL COMMENT '向哪家商户买料',
  `staffID` bigint(20) NOT NULL COMMENT '采购员',
  `relatedShortageReport` bigint(20) DEFAULT NULL COMMENT '可选追溯的系统缺货汇总单来源',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `requestDeliveryDate` date NOT NULL COMMENT '约束供货商到料交付的死线日期',
  `status` int(10) NOT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `paymentType` int(10) DEFAULT NULL COMMENT '付款类型（字典 FINANCIAL_CLEARING_TYPE）',
  `payAmount` decimal(12,2) DEFAULT 0.00 COMMENT '本次付款金额'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='上游供应链原材料采购订单';

-- --------------------------------------------------------

--
-- 表的结构 `purchaseorderrawmaterialline`
--

CREATE TABLE `purchaseorderrawmaterialline` (
  `purchaseOrderID` bigint(20) NOT NULL,
  `rawMaterialID` bigint(20) NOT NULL,
  `price` decimal(10,2) NOT NULL COMMENT '采购议定单价',
  `orderQuantity` decimal(10,2) NOT NULL COMMENT '采购面料配件总数',
  `receivedQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '后期累计已完成收货清点的在途转实物入库数'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='供应链原材料采购订单明细行';

-- --------------------------------------------------------

--
-- 表的结构 `quotation`
--

CREATE TABLE `quotation` (
  `quotationID` bigint(20) NOT NULL,
  `quotationCode` varchar(30) NOT NULL COMMENT '模式 QT-+ID',
  `sequenceNumber` int(10) NOT NULL,
  `staffID` bigint(20) NOT NULL COMMENT '经办销售员工',
  `customerID` bigint(20) NOT NULL COMMENT '意向客户',
  `currencyID` bigint(20) NOT NULL COMMENT '报价币种',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL,
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='前期销售报价单';

-- --------------------------------------------------------

--
-- 表的结构 `quotationproductline`
--

CREATE TABLE `quotationproductline` (
  `quotationID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `price` decimal(10,2) NOT NULL COMMENT '报价单价',
  `quantity` decimal(10,2) NOT NULL COMMENT '意向数量',
  `discountAmount` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '折让金额'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报价单成品明细';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterial`
--

CREATE TABLE `rawmaterial` (
  `rawMaterialID` bigint(20) NOT NULL,
  `rawMaterialCode` varchar(30) NOT NULL COMMENT '模式 RM-+category+sequenceNumber',
  `category` varchar(30) NOT NULL COMMENT '原料种类（如面料、纽扣、拉链）',
  `SequenceNumber` int(10) DEFAULT NULL,
  `size` varchar(30) NOT NULL,
  `color` varchar(30) NOT NULL,
  `minimumStockLevel` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '物料安全库存红线',
  `status` int(10) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='生产原材料SKU基础档案表';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterialrequestnote`
--

CREATE TABLE `rawmaterialrequestnote` (
  `rawMaterialRequestNoteID` bigint(20) NOT NULL,
  `rawMaterialRequestNoteCode` varchar(30) NOT NULL,
  `ProductionOrderID` bigint(20) NOT NULL COMMENT '车间关联的源头排产生产订单',
  `staffID` bigint(20) NOT NULL COMMENT '申领发起员工',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `requestDate` date NOT NULL COMMENT '期望要求的到料领料日期',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='车间向仓储/采购部提报的物料领料申领及请购单';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterialrequestnoterawmaterial_line`
--

CREATE TABLE `rawmaterialrequestnoterawmaterial_line` (
  `rawMaterialRequestNoteID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL COMMENT '要切片对应哪一个成品所需制造的配给',
  `rawMaterialID` bigint(20) NOT NULL COMMENT '具体申领的原材料',
  `rawMaterialRequestQuantity` decimal(10,2) NOT NULL COMMENT '本次申请流转的数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='领料请购单精确物料行明细';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterialshortagereportline`
--

CREATE TABLE `rawmaterialshortagereportline` (
  `shortageReportID` bigint(20) NOT NULL,
  `rawMaterialID` bigint(20) NOT NULL,
  `WarehousewarehouseID` bigint(20) NOT NULL,
  `totalShortageQuantity` decimal(10,2) NOT NULL COMMENT '通过盘点自动轧出的真实缺货数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='物料缺货清单明细';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterialsupplier`
--

CREATE TABLE `rawmaterialsupplier` (
  `rawMaterialID` bigint(20) NOT NULL,
  `supplierID` bigint(20) NOT NULL,
  `supplierStyleNumber` varchar(50) DEFAULT NULL COMMENT '供应商在自己厂内对应的物料款号',
  `basePrice` decimal(10,2) NOT NULL COMMENT '供货报价',
  `currencyID` bigint(20) NOT NULL COMMENT '供货结算币种',
  `unit` varchar(30) NOT NULL COMMENT '计量供应单位',
  `minimumOrderQuantity` int(10) NOT NULL DEFAULT 1 COMMENT '最小起订量限制',
  `quoteDate` date DEFAULT NULL COMMENT '报价生效起始日',
  `lastModify` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='供应商原材料价格与起订量名录';

-- --------------------------------------------------------

--
-- 表的结构 `rawmaterialwarehouse`
--

CREATE TABLE `rawmaterialwarehouse` (
  `rawMaterialID` bigint(20) NOT NULL,
  `warehouseID` bigint(20) NOT NULL,
  `physicalQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '面料辅料实际物理库存',
  `reservedQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '已被排产锁定消耗的原料数',
  `purchasedQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '已下采购单等在途的原材料数'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='原材料仓储动态库存对账表';

-- --------------------------------------------------------

--
-- 表的结构 `receiptvoucher`
--

CREATE TABLE `receiptvoucher` (
  `receiptVoucherID` bigint(20) NOT NULL,
  `receiptVoucherCode` varchar(30) NOT NULL,
  `cusomerID` bigint(20) NOT NULL COMMENT '原图拼写为 cusomerID 保持一致',
  `staffID` bigint(20) NOT NULL,
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `paymentMethod` varchar(30) NOT NULL COMMENT '付款通道',
  `paymentMethodRef` varchar(30) NOT NULL COMMENT '支付流水参考凭证号',
  `paymentAmount` decimal(10,2) NOT NULL COMMENT '实收总金额',
  `currencyID` bigint(20) NOT NULL COMMENT '实收币种',
  `paymentReceivedDate` date NOT NULL COMMENT '实际到账日期',
  `status` int(10) NOT NULL,
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户财务收款进账流水单';

-- --------------------------------------------------------

--
-- 表的结构 `receiptvoucherinvoice`
--

CREATE TABLE `receiptvoucherinvoice` (
  `receiptVoucherID` bigint(20) NOT NULL,
  `lineNo` int(10) NOT NULL COMMENT 'Allocation line sequence per receipt voucher',
  `invoiceID` bigint(20) DEFAULT NULL COMMENT 'NULL for exchange-loss lines (type=4)',
  `receivedAmount` decimal(10,2) NOT NULL COMMENT 'Amount allocated on this line; SUM(lines) should equal receiptVoucher.paymentAmount',
  `type` int(10) NOT NULL COMMENT '核销阶段: deposit, partial, final, exchangeLoss'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='财务应收实收关联核销表';

-- --------------------------------------------------------

--
-- 表的结构 `refundrequest`
--

CREATE TABLE `refundrequest` (
  `refundRequestID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `refundRequestCode` varchar(30) NOT NULL COMMENT '固定模式 RF-+ID',
  `staffID` bigint(20) NOT NULL COMMENT '经办员工',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `ReceiptVoucherID` bigint(20) DEFAULT NULL COMMENT '关联收款凭证（可选）',
  `InvoiceID` bigint(20) DEFAULT NULL COMMENT '关联发票（可选）',
  `refundAmount` decimal(19,2) NOT NULL COMMENT '退款金额',
  `refundMethod` tinyint(4) NOT NULL COMMENT '退款方式（固定选择，由字典表控制，1:bank transfer, 2:FPS, 3:cheque等）',
  `refundRef` varchar(100) DEFAULT NULL COMMENT '员工输入的支付网关交易参考号',
  `refundReason` varchar(100) NOT NULL COMMENT '退款原因（固定选择，如 damage, wrong shipment等）',
  `status` int(10) NOT NULL COMMENT '单据状态',
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='退款申请流水表';

--
-- 转存表中的数据 `refundrequest`
--

INSERT INTO `refundrequest` (`refundRequestID`, `refundRequestCode`, `staffID`, `createDate`, `ReceiptVoucherID`, `InvoiceID`, `refundAmount`, `refundMethod`, `refundRef`, `refundReason`, `status`, `lastModifyDate`, `remark`) VALUES
(1, 'RF-2026052601', 1, '2026-05-25 16:25:50', NULL, 1, 250.50, 1, 'REF-XYZ123', 'damage', 1, '2026-05-25 16:25:50', 'Damaged chair refund'),
(2, 'RF-2026052602', 2, '2026-05-26 09:00:00', 2, 2, 500.00, 2, 'REF-FPS001', 'wrong shipment', 1, '2026-05-26 09:00:00', 'Wrong model shipped'),
(3, 'RF-2026052603', 1, '2026-05-26 10:00:00', NULL, 3, 120.00, 1, 'REF-BT003', 'sizing issue', 0, '2026-05-26 10:00:00', 'Size mismatch'),
(4, 'RF-2026052604', 3, '2026-05-26 11:00:00', 4, 4, 80.00, 3, 'REF-CHQ004', 'order cancelled', 1, '2026-05-26 11:00:00', 'Order cancelled'),
(5, 'RF-2026052605', 2, '2026-05-26 12:00:00', 5, 5, 300.00, 1, 'REF-BT005', 'customer dissatisfaction', 0, '2026-05-26 12:00:00', 'Quality complaint');

-- --------------------------------------------------------

--
-- 表的结构 `salesorder`
--

CREATE TABLE `salesorder` (
  `salesOrderID` bigint(20) NOT NULL,
  `salesOrderCode` varchar(30) NOT NULL COMMENT '模式 SO-+ID',
  `customerID` bigint(20) NOT NULL,
  `staffID` bigint(20) NOT NULL,
  `currencyCurrencyID` bigint(20) NOT NULL COMMENT '交易币种',
  `deliveryAddress` varchar(255) NOT NULL COMMENT '原图标记为date属笔误，修正为字符串存放发货地址',
  `requestedDeliveryDate` date DEFAULT NULL COMMENT '客户期望交付日期',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `discountType` varchar(30) DEFAULT NULL COMMENT '折扣类型分类',
  `discount` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '总单减免折扣',
  `status` int(10) NOT NULL COMMENT '状态机控制：草稿、已锁定、生产中、发货完成等',
  `customerRefNumber` varchar(50) DEFAULT NULL COMMENT '客户参考号（格式：PO-PL-#########）',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='核心销售订单表';

--
-- 转存表中的数据 `salesorder`
--

INSERT INTO `salesorder` (`salesOrderID`, `salesOrderCode`, `customerID`, `staffID`, `currencyCurrencyID`, `deliveryAddress`, `requestedDeliveryDate`, `createDate`, `lastModifyDate`, `discountType`, `discount`, `status`, `customerRefNumber`, `remark`) VALUES
(1, 'SO-2026052601', 1, 2, 1, 'Flat B, 5/F, Kwun Tong Industrial Building', NULL, '2026-05-25 17:17:02', '2026-05-25 17:17:02', 'Percentage', 500.00, 1, 'PO-PL-000000001', 'Office fit-out package for ABC Furniture.'),
(2, 'SO-2026052602', 2, 2, 1, 'Warehouse 3, Tuen Mun Logistics Centre', NULL, '2026-05-25 20:05:57', '2026-05-25 20:05:57', NULL, 0.00, 1, 'PO-PL-000000002', 'Pacific Home showroom order.'),
(3, 'SO-2026052603', 3, 2, 1, 'Shop 12, Festival Walk, Kowloon Tong', NULL, '2026-05-26 09:00:00', '2026-05-26 09:00:00', 'Fixed Amount', 200.00, 2, 'PO-PL-000000003', 'Urban Living retail stock.'),
(4, 'SO-2026052604', 4, 2, 1, 'Loading Bay, Mongkok Commercial Tower', NULL, '2026-05-26 10:00:00', '2026-05-26 10:00:00', NULL, 0.00, 1, 'PO-PL-000000004', 'Green Office bulk chairs.'),
(5, 'SO-2026052605', 5, 2, 1, '15/F, TST Plaza, Tsim Sha Tsui', NULL, '2026-05-26 11:00:00', '2026-05-26 11:00:00', NULL, 100.00, 0, 'PO-PL-000000005', 'Elite Interiors draft order.');

-- --------------------------------------------------------

--
-- 表的结构 `salesorderproductline`
--

CREATE TABLE `salesorderproductline` (
  `salesOrderID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `price` decimal(10,2) NOT NULL COMMENT '实际销售单价',
  `orderQuantity` decimal(10,2) NOT NULL COMMENT '定购总数量',
  `discountAmount` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '单品折让',
  `warehouseReservedQty` int(10) NOT NULL DEFAULT 0 COMMENT '已从实体仓库占用的预留配额',
  `shippedQuantity` int(10) NOT NULL DEFAULT 0 COMMENT '已发货交付累计数',
  `invoicedQuantity` int(10) NOT NULL DEFAULT 0 COMMENT '已开具发票累计数'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='销售订单商品货品细项';

-- --------------------------------------------------------

--
-- 表的结构 `replyslip`
--

CREATE TABLE `replyslip` (
  `replySlipID` bigint(20) NOT NULL,
  `replySlipCode` varchar(30) NOT NULL COMMENT '模式 RS-+ID',
  `salesOrderID` bigint(20) NOT NULL COMMENT '来源销售订单',
  `customerID` bigint(20) NOT NULL COMMENT '客户',
  `staffID` bigint(20) NOT NULL COMMENT '经办员工',
  `currencyID` bigint(20) NOT NULL COMMENT '币种',
  `signedBy` varchar(100) DEFAULT NULL COMMENT '签收/回签人',
  `signedDate` date DEFAULT NULL COMMENT '签收/回签日期',
  `createDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `lastModifyDate` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `status` int(10) NOT NULL COMMENT 'Reply Slip 状态',
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户订单回签回条主表';

-- --------------------------------------------------------

--
-- 表的结构 `replyslipproductline`
--

CREATE TABLE `replyslipproductline` (
  `replySlipID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `price` decimal(10,2) NOT NULL COMMENT '回签确认单价',
  `quantity` decimal(10,2) NOT NULL COMMENT '回签确认数量',
  `discountAmount` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '回签确认折让'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='客户订单回签回条明细';

-- --------------------------------------------------------

--
-- 表的结构 `shortagereport`
--

CREATE TABLE `shortagereport` (
  `shortageReportID` bigint(20) NOT NULL,
  `shortageReportCode` varchar(30) NOT NULL COMMENT '模式 SR-+date+sequenceNumber',
  `date` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp() COMMENT '报告引发或计算生成的基准结算时间',
  `sequenceNumber` int(10) NOT NULL,
  `createDate` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='系统自动扫描或手动生成的原料缺货汇总报告';

-- --------------------------------------------------------

--
-- 表的结构 `staff`
--

CREATE TABLE `staff` (
  `staffID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `username` varchar(30) NOT NULL COMMENT '登录用户名',
  `password` varchar(255) NOT NULL COMMENT '加密密码',
  `title` varchar(30) NOT NULL COMMENT '职位职称',
  `department` varchar(30) NOT NULL COMMENT '所属部门',
  `firstName` varchar(30) NOT NULL,
  `lastName` varchar(30) NOT NULL,
  `employDate` date NOT NULL COMMENT '入职日期',
  `phone` varchar(30) NOT NULL,
  `email` varchar(255) NOT NULL,
  `status` int(10) DEFAULT NULL COMMENT '员工状态'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='员工与用户主表';

--
-- 转存表中的数据 `staff`
--

INSERT INTO `staff` (`staffID`, `username`, `password`, `title`, `department`, `firstName`, `lastName`, `employDate`, `phone`, `email`, `status`) VALUES
(1, 'admin', '123456', 'Super User', 'Super User', 'John', 'Doe', '2026-01-15', '21234567', 'john.doe@erp.com', 1),
(2, 'sales01', '123456', 'Officer', 'Sales', 'Mary', 'Chan', '2026-02-01', '29876543', 'mary.chan@erp.com', 1),
(3, 'prod01', '123456', 'Officer', 'Production', 'Peter', 'Lam', '2026-02-15', '25551234', 'peter.lam@erp.com', 1),
(4, 'wh01', '123456', 'Clerk', 'Warehouse', 'Sandy', 'Yuen', '2026-03-01', '26667890', 'sandy.yuen@erp.com', 1),
(5, 'pur01', '123456', 'Officer', 'Purchasing', 'Tom', 'Wong', '2026-03-15', '27773456', 'tom.wong@erp.com', 1);

-- --------------------------------------------------------

--
-- 表的结构 `supplier`
--

CREATE TABLE `supplier` (
  `supplierID` bigint(20) NOT NULL,
  `supplierName` varchar(255) NOT NULL,
  `billingAddress` varchar(255) DEFAULT NULL,
  `contactPerson` varchar(100) DEFAULT NULL,
  `phone` varchar(30) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `paymentTerm` varchar(100) DEFAULT NULL COMMENT '与供应商约定的结算账期条件',
  `bankAccount` varchar(100) DEFAULT NULL COMMENT '供应商收汇对公账户',
  `status` int(10) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='面料及配件上游供应商主体表';

-- --------------------------------------------------------

--
-- 表的结构 `systemdictionary`
--

CREATE TABLE `systemdictionary` (
  `dictionaryID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `category` varchar(60) NOT NULL COMMENT '字典类别，例如 refundMethod, refundReason',
  `codeValue` tinyint(4) NOT NULL COMMENT '状态码值，如 1, 2, 3',
  `displayNameEnglish` varchar(50) NOT NULL COMMENT '英文显示名',
  `sortOrder` int(10) NOT NULL DEFAULT 0 COMMENT '排序权值'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='数据字典配置表';

--
-- 转存表中的数据 `systemdictionary`
--

INSERT INTO `systemdictionary` (`dictionaryID`, `category`, `codeValue`, `displayNameEnglish`, `sortOrder`) VALUES
(1, 'PURCHASE_ORDER_STATUS', 0, 'Draft', 1),
(2, 'PURCHASE_ORDER_STATUS', 1, 'Pending Approval', 2),
(3, 'PURCHASE_ORDER_STATUS', 2, 'Approved', 3),
(4, 'PURCHASE_ORDER_STATUS', 3, 'Rejected', 4),
(5, 'PURCHASE_ORDER_STATUS', 4, 'Ordered', 5),
(6, 'PURCHASE_ORDER_STATUS', 5, 'Receiving', 6),
(7, 'PURCHASE_ORDER_STATUS', 6, 'Completed', 7),
(8, 'PURCHASE_ORDER_STATUS', 7, 'Cancelled', 8),
(9, 'SUPPLIER_STATUS', 1, 'Active', 1),
(10, 'SUPPLIER_STATUS', 0, 'Inactive', 2),
(11, 'PAYMENT_TERM', 1, 'Cash', 1),
(12, 'PAYMENT_TERM', 2, '30 Days', 2),
(13, 'PAYMENT_TERM', 3, '60 Days', 3),
(14, 'PAYMENT_TERM', 4, '90 Days', 4),
(15, 'SALES_ORDER_STATUS', 0, 'Draft', 1),
(16, 'SALES_ORDER_STATUS', 1, 'Confirmed', 2),
(17, 'SALES_ORDER_STATUS', 2, 'Processing', 3),
(18, 'SALES_ORDER_STATUS', 3, 'Shipped', 4),
(19, 'SALES_ORDER_STATUS', 4, 'Completed', 5),
(20, 'SALES_ORDER_STATUS', 5, 'Cancelled', 6),
(21, 'DISCOUNT_TYPE', 1, 'Percentage', 1),
(22, 'DISCOUNT_TYPE', 2, 'Fixed Amount', 2),
(23, 'STAFF_STATUS', 1, 'Active', 1),
(24, 'STAFF_STATUS', 0, 'Inactive', 2),
(25, 'DEPARTMENT', 1, 'Sales', 1),
(26, 'DEPARTMENT', 2, 'Purchasing', 2),
(27, 'DEPARTMENT', 3, 'Finance', 3),
(28, 'DEPARTMENT', 4, 'Production', 4),
(29, 'DEPARTMENT', 5, 'Warehouse', 5),
(30, 'STAFF_TITLE', 1, 'Manager', 1),
(31, 'STAFF_TITLE', 2, 'Officer', 2),
(32, 'STAFF_TITLE', 3, 'Clerk', 3),
(33, 'STAFF_TITLE', 4, 'Director', 4),
(34, 'REFUND_METHOD', 1, 'Bank Transfer', 1),
(35, 'REFUND_METHOD', 2, 'FPS', 2),
(36, 'REFUND_METHOD', 3, 'Cheque', 3),
(37, 'REFUND_METHOD', 4, 'TT', 4),
(38, 'REFUND_METHOD', 5, 'PayPal', 5),
(39, 'REFUND_METHOD', 6, 'Amazon Pay', 6),
(40, 'REFUND_METHOD', 7, 'Taobao Pay', 7),
(41, 'REFUND_REASON', 1, 'Damage', 1),
(42, 'REFUND_REASON', 2, 'Wrong Shipment', 2),
(43, 'REFUND_REASON', 3, 'Sizing Issue', 3),
(44, 'REFUND_REASON', 4, 'Order Cancelled', 4),
(45, 'REFUND_REASON', 5, 'Customer Dissatisfaction', 5),
(46, 'PRODUCT_STATUS', 1, 'Active', 1),
(47, 'PRODUCT_STATUS', 0, 'Inactive', 2),
(48, 'PRODUCT_STATUS', 2, 'Out of Stock', 3),
(49, 'PRODUCT_STATUS', 3, 'Discontinued', 4),
(50, 'PRODUCTION_STATUS', 0, 'Pending Scheduling', 1),
(51, 'PRODUCTION_STATUS', 1, 'In Progress', 2),
(52, 'PRODUCTION_STATUS', 2, 'Quality Checking', 3),
(53, 'PRODUCTION_STATUS', 3, 'Completed', 4),
(54, 'PRODUCTION_STATUS', 4, 'Paused', 5),
(55, 'DELIVERY_STATUS', 0, 'Preparing', 1),
(56, 'DELIVERY_STATUS', 1, 'Packed', 2),
(57, 'DELIVERY_STATUS', 2, 'In Transit', 3),
(58, 'DELIVERY_STATUS', 3, 'Delivered', 4),
(59, 'DELIVERY_STATUS', 4, 'Returned', 5),
(60, 'INVOICE_STATUS', 0, 'Unpaid', 1),
(61, 'INVOICE_STATUS', 1, 'Partially Paid', 2),
(62, 'INVOICE_STATUS', 2, 'Fully Paid', 3),
(63, 'INVOICE_STATUS', 3, 'Overdue', 4),
(64, 'INVOICE_STATUS', 4, 'Voided', 5),
(65, 'RECEIPT_VOUCHER_STATUS', 0, 'Pending Verification', 1),
(66, 'RECEIPT_VOUCHER_STATUS', 1, 'Verified', 2),
(67, 'RECEIPT_VOUCHER_STATUS', 2, 'Rejected', 3),
(68, 'FINANCIAL_CLEARING_TYPE', 1, 'Deposit', 1),
(69, 'FINANCIAL_CLEARING_TYPE', 2, 'Partial Payment', 2),
(70, 'FINANCIAL_CLEARING_TYPE', 3, 'Final Payment', 3),
(71, 'FINANCIAL_CLEARING_TYPE', 4, 'Exchange Loss', 4),
(72, 'RAW_MATERIAL_STATUS', 1, 'Active', 1),
(73, 'RAW_MATERIAL_STATUS', 0, 'Inactive', 2),
(74, 'RAW_MATERIAL_STATUS', 2, 'Below Safety Stock', 3),
(75, 'REPLY_SLIP_STATUS', 0, 'Draft', 1),
(76, 'REPLY_SLIP_STATUS', 1, 'Sent', 2),
(77, 'REPLY_SLIP_STATUS', 2, 'Signed', 3),
(78, 'REPLY_SLIP_STATUS', 3, 'Rejected', 4);

-- --------------------------------------------------------

--
-- 表的结构 `systemdictionary_refundrequest`
--

CREATE TABLE `systemdictionary_refundrequest` (
  `SystemDictionarydictionaryID` bigint(20) NOT NULL,
  `SystemDictionarycategory` varchar(60) NOT NULL,
  `SystemDictionarycodeValue` tinyint(4) NOT NULL,
  `RefundRequestrefundRequestID` bigint(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='字典与退款申请桥接映射表';

-- --------------------------------------------------------

--
-- 表的结构 `warehouse`
--

CREATE TABLE `warehouse` (
  `warehouseID` bigint(20) NOT NULL COMMENT '自增唯一标识',
  `warehouseName` varchar(30) NOT NULL COMMENT '仓库名称',
  `warehouseAddress` varchar(255) NOT NULL COMMENT '仓库地址'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='多仓储区域定义表';

-- --------------------------------------------------------

--
-- 表的结构 `warehouseproduct`
--

CREATE TABLE `warehouseproduct` (
  `warehouseID` bigint(20) NOT NULL,
  `productID` bigint(20) NOT NULL,
  `physicalQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '实物库存',
  `reservedQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '被销售单预留锁定的库存',
  `purchasedQuantity` decimal(10,2) NOT NULL DEFAULT 0.00 COMMENT '已下单采购但尚未入库的数量'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='仓库成品物理与逻辑库存对账表';

--
-- 种子数据：各业务表至少 5 笔（systemdictionary 已有 74 笔字典项）
--

INSERT INTO `warehouse` (`warehouseID`, `warehouseName`, `warehouseAddress`) VALUES
(1, 'Kowloon Main WH', '88 Kwun Tong Road, Kowloon'),
(2, 'NT Raw Material WH', '12 Tai Po Industrial Estate, NT'),
(3, 'HK Island Showroom', '1 Queens Road Central, HK'),
(4, 'Tuen Mun Finished WH', '200 Tuen Mun Heung Sze Wui Road'),
(5, 'Airport Logistics WH', '7 Chek Lap Kok Road, Lantau');

INSERT INTO `supplier` (`supplierID`, `supplierName`, `billingAddress`, `contactPerson`, `phone`, `email`, `paymentTerm`, `bankAccount`, `status`) VALUES
(1, 'Oak Timber Supplies Ltd', '100 Timber Lane, Dongguan', 'Mr. Zhang', '+86-769-1234567', 'zhang@oaktimber.cn', '30 Days', 'CN88-1234-5678', 1),
(2, 'Metal Works HK', '50 San Po Kong, Kowloon', 'Ms. Lau', '2345-6789', 'lau@metalworks.hk', '60 Days', 'HK12-3456-7890', 1),
(3, 'Fabric World Asia', '88 Guangzhou Ave, Guangzhou', 'Mr. Chen', '+86-20-8765432', 'chen@fabricworld.cn', '30 Days', 'CN99-8765-4321', 1),
(4, 'Hardware Components Co', '15 Fanling, NT', 'Mr. Ng', '2654-3210', 'ng@hardware.hk', 'Cash', 'HK98-7654-3210', 1),
(5, 'Foam & Cushion Factory', '200 Foshan Rd, Foshan', 'Ms. Li', '+86-757-1122334', 'li@foamfactory.cn', '90 Days', 'CN55-1122-3344', 1);

INSERT INTO `rawmaterial` (`rawMaterialID`, `rawMaterialCode`, `category`, `SequenceNumber`, `size`, `color`, `minimumStockLevel`, `status`) VALUES
(1, 'RM-Wood-1001', 'Wood', 1001, '2400x600mm', 'Natural Oak', 50.00, 1),
(2, 'RM-Fabric-2001', 'Fabric', 2001, 'Roll 50m', 'Grey', 20.00, 1),
(3, 'RM-Metal-3001', 'Metal', 3001, 'Tube 25mm', 'Chrome', 100.00, 1),
(4, 'RM-Foam-4001', 'Foam', 4001, 'Sheet 50mm', 'White', 30.00, 1),
(5, 'RM-Hardware-5001', 'Hardware', 5001, 'Box M6', 'Silver', 200.00, 1);

INSERT INTO `productimage` (`productID`, `productImageUrl`) VALUES
(1, 'https://cdn.example.com/products/desk-oak-01.jpg'),
(2, 'https://cdn.example.com/products/chair-erg-01.jpg'),
(3, 'https://cdn.example.com/products/sofa-l-01.jpg'),
(4, 'https://cdn.example.com/products/cabinet-file-01.jpg'),
(5, 'https://cdn.example.com/products/table-dining-01.jpg');

INSERT INTO `productrawmaterialline` (`productID`, `rawMaterialID`, `rawMaterialNeedQty`, `createDate`, `lastModifyDate`) VALUES
(1, 1, 2.50, '2026-05-26 08:00:00', '2026-05-26 08:00:00'),
(2, 3, 8.00, '2026-05-26 08:00:00', '2026-05-26 08:00:00'),
(2, 4, 1.20, '2026-05-26 08:00:00', '2026-05-26 08:00:00'),
(3, 2, 12.00, '2026-05-26 08:00:00', '2026-05-26 08:00:00'),
(3, 4, 3.50, '2026-05-26 08:00:00', '2026-05-26 08:00:00');

INSERT INTO `rawmaterialsupplier` (`rawMaterialID`, `supplierID`, `supplierStyleNumber`, `basePrice`, `currencyID`, `unit`, `minimumOrderQuantity`, `quoteDate`, `status`) VALUES
(1, 1, 'OAK-PLT-24', 450.00, 1, 'sheet', 10, '2026-01-01', 1),
(2, 3, 'FAB-GRY-50', 120.00, 1, 'roll', 5, '2026-01-01', 1),
(3, 2, 'CHR-TUBE-25', 35.00, 1, 'pcs', 50, '2026-01-01', 1),
(4, 5, 'FOAM-50-W', 88.00, 1, 'sheet', 20, '2026-01-01', 1),
(5, 4, 'HW-M6-BOX', 15.00, 1, 'box', 100, '2026-01-01', 1);

INSERT INTO `rawmaterialwarehouse` (`rawMaterialID`, `warehouseID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`) VALUES
(1, 2, 120.00, 10.00, 20.00),
(2, 2, 8.00, 2.00, 15.00),
(3, 2, 250.00, 30.00, 0.00),
(4, 2, 45.00, 5.00, 10.00),
(5, 2, 500.00, 50.00, 0.00);

INSERT INTO `warehouseproduct` (`warehouseID`, `productID`, `physicalQuantity`, `reservedQuantity`, `purchasedQuantity`) VALUES
(1, 1, 15.00, 2.00, 5.00),
(1, 2, 80.00, 10.00, 20.00),
(1, 3, 6.00, 1.00, 2.00),
(4, 4, 25.00, 3.00, 0.00),
(4, 5, 10.00, 0.00, 4.00);

INSERT INTO `quotation` (`quotationID`, `quotationCode`, `sequenceNumber`, `staffID`, `customerID`, `currencyID`, `createDate`, `lastModifyDate`, `status`, `remark`) VALUES
(1, 'QT-2026052601', 1, 2, 1, 1, '2026-05-26 08:00:00', '2026-05-26 08:00:00', 0, 'ABC office proposal'),
(2, 'QT-2026052602', 1, 2, 2, 1, '2026-05-26 09:00:00', '2026-05-26 09:00:00', 1, 'Pacific Home quote'),
(3, 'QT-2026052603', 1, 2, 3, 1, '2026-05-26 10:00:00', '2026-05-26 10:00:00', 2, 'Converted to SO-3'),
(4, 'QT-2026052604', 1, 2, 4, 1, '2026-05-26 11:00:00', '2026-05-26 11:00:00', 0, 'Green Office chairs'),
(5, 'QT-2026052605', 1, 2, 5, 1, '2026-05-26 12:00:00', '2026-05-26 12:00:00', 0, 'Elite draft quote');

INSERT INTO `quotationproductline` (`quotationID`, `productID`, `price`, `quantity`, `discountAmount`) VALUES
(1, 1, 3200.00, 2.00, 0.00),
(2, 2, 1250.00, 20.00, 500.00),
(3, 3, 5800.00, 1.00, 0.00),
(4, 2, 1200.00, 50.00, 1000.00),
(5, 5, 4500.00, 2.00, 200.00);

INSERT INTO `salesorderproductline` (`salesOrderID`, `productID`, `price`, `orderQuantity`, `discountAmount`, `warehouseReservedQty`, `shippedQuantity`, `invoicedQuantity`) VALUES
(1, 1, 3200.00, 2.00, 0.00, 2, 0, 0),
(2, 2, 1250.00, 10.00, 0.00, 5, 3, 3),
(3, 3, 5800.00, 1.00, 200.00, 1, 0, 0),
(4, 2, 1250.00, 30.00, 500.00, 10, 10, 5),
(5, 4, 980.00, 5.00, 0.00, 0, 0, 0);

INSERT INTO `replyslip` (`replySlipID`, `replySlipCode`, `salesOrderID`, `customerID`, `staffID`, `currencyID`, `signedBy`, `signedDate`, `createDate`, `lastModifyDate`, `status`, `remark`) VALUES
(1, 'RS-2026052601', 1, 1, 2, 1, 'Alice Wong', '2026-05-26', '2026-05-26 10:30:00', '2026-05-26 10:30:00', 2, 'Customer signed and confirmed.'),
(2, 'RS-2026052602', 2, 2, 2, 1, 'Ben Lee', '2026-05-26', '2026-05-26 11:30:00', '2026-05-26 11:30:00', 1, 'Sent for final signature.'),
(3, 'RS-2026052603', 3, 3, 2, 1, NULL, NULL, '2026-05-26 12:30:00', '2026-05-26 12:30:00', 0, 'Draft reply slip.'),
(4, 'RS-2026052604', 4, 4, 2, 1, 'David Cheung', '2026-05-26', '2026-05-26 13:30:00', '2026-05-26 13:30:00', 3, 'Rejected due to quantity mismatch.'),
(5, 'RS-2026052605', 5, 5, 2, 1, NULL, NULL, '2026-05-26 14:30:00', '2026-05-26 14:30:00', 0, 'Pending customer confirmation.');

INSERT INTO `replyslipproductline` (`replySlipID`, `productID`, `price`, `quantity`, `discountAmount`) VALUES
(1, 1, 3200.00, 2.00, 0.00),
(2, 2, 1250.00, 10.00, 0.00),
(3, 3, 5800.00, 1.00, 200.00),
(4, 2, 1250.00, 25.00, 500.00),
(5, 4, 980.00, 5.00, 0.00);

INSERT INTO `productionorder` (`productionOrderID`, `productionOrderCode`, `salesOrderID`, `staffID`, `createDate`, `estFinishDate`, `lastModifyDate`, `status`, `remark`) VALUES
(1, 'PO-2026052601', 1, 3, '2026-05-26 08:00:00', '2026-06-15 00:00:00', '2026-05-26 08:00:00', 1, 'Desks for ABC'),
(2, 'PO-2026052602', 2, 3, '2026-05-26 09:00:00', '2026-06-20 00:00:00', '2026-05-26 09:00:00', 1, 'Chairs batch 1'),
(3, 'PO-2026052603', 3, 3, '2026-05-26 10:00:00', '2026-06-25 00:00:00', '2026-05-26 10:00:00', 0, 'Sofa order'),
(4, 'PO-2026052604', 4, 3, '2026-05-26 11:00:00', '2026-06-10 00:00:00', '2026-05-26 11:00:00', 2, 'QC in progress'),
(5, 'PO-2026052605', 5, 3, '2026-05-26 12:00:00', '2026-07-01 00:00:00', '2026-05-26 12:00:00', 0, 'Pending scheduling');

INSERT INTO `productionorderproductline` (`ProductionOrderID`, `productID`, `productionQty`) VALUES
(1, 1, 2),
(2, 2, 10),
(3, 3, 1),
(4, 2, 30),
(5, 4, 5);

INSERT INTO `purchaseorder` (`purchaseOrderID`, `purchaseOrderCode`, `supplierID`, `staffID`, `relatedShortageReport`, `createDate`, `lastModifyDate`, `requestDeliveryDate`, `status`, `remark`, `paymentType`, `payAmount`) VALUES
(1, 'PUR-2026052601', 1, 5, NULL, '2026-05-26 08:00:00', '2026-05-26 08:00:00', '2026-06-05', 4, 'Oak panels replenishment', 1, 9000.00),
(2, 'PUR-2026052602', 3, 5, NULL, '2026-05-26 09:00:00', '2026-05-26 09:00:00', '2026-06-08', 4, 'Grey fabric order', 2, 1750.00),
(3, 'PUR-2026052603', 2, 5, 1, '2026-05-26 10:00:00', '2026-05-26 10:00:00', '2026-06-01', 2, 'Chrome tubes urgent', 1, 0.00),
(4, 'PUR-2026052604', 5, 5, NULL, '2026-05-26 11:00:00', '2026-05-26 11:00:00', '2026-06-12', 1, 'Foam sheets', 3, 3000.00),
(5, 'PUR-2026052605', 4, 5, NULL, '2026-05-26 12:00:00', '2026-05-26 12:00:00', '2026-06-15', 0, 'Hardware restock draft', 1, 0.00);

INSERT INTO `purchaseorderrawmaterialline` (`purchaseOrderID`, `rawMaterialID`, `price`, `orderQuantity`, `receivedQuantity`) VALUES
(1, 1, 450.00, 40.00, 20.00),
(2, 2, 120.00, 15.00, 0.00),
(3, 3, 35.00, 100.00, 50.00),
(4, 4, 88.00, 25.00, 0.00),
(5, 5, 15.00, 200.00, 0.00);

INSERT INTO `shortagereport` (`shortageReportID`, `shortageReportCode`, `date`, `sequenceNumber`, `createDate`) VALUES
(1, 'SR-20260526-01', '2026-05-26 07:00:00', 1, '2026-05-26 07:00:00'),
(2, 'SR-20260526-02', '2026-05-26 07:30:00', 2, '2026-05-26 07:30:00'),
(3, 'SR-20260526-03', '2026-05-26 08:00:00', 3, '2026-05-26 08:00:00'),
(4, 'SR-20260526-04', '2026-05-26 08:30:00', 4, '2026-05-26 08:30:00'),
(5, 'SR-20260526-05', '2026-05-26 09:00:00', 5, '2026-05-26 09:00:00');

INSERT INTO `rawmaterialshortagereportline` (`shortageReportID`, `rawMaterialID`, `WarehousewarehouseID`, `totalShortageQuantity`) VALUES
(1, 2, 2, 12.00),
(2, 1, 2, 5.00),
(3, 3, 2, 20.00),
(4, 4, 2, 8.00),
(5, 5, 2, 50.00);

INSERT INTO `rawmaterialrequestnote` (`rawMaterialRequestNoteID`, `rawMaterialRequestNoteCode`, `ProductionOrderID`, `staffID`, `createDate`, `requestDate`, `remark`) VALUES
(1, 'RMRN-2026052601', 1, 3, '2026-05-26 09:00:00', '2026-05-28', 'Oak for desk production'),
(2, 'RMRN-2026052602', 2, 3, '2026-05-26 09:30:00', '2026-05-29', 'Metal and foam for chairs'),
(3, 'RMRN-2026052603', 3, 3, '2026-05-26 10:00:00', '2026-06-01', 'Fabric for sofa'),
(4, 'RMRN-2026052604', 4, 4, '2026-05-26 11:00:00', '2026-05-30', 'Bulk chair materials'),
(5, 'RMRN-2026052605', 5, 3, '2026-05-26 12:00:00', '2026-06-05', 'Cabinet hardware');

INSERT INTO `rawmaterialrequestnoterawmaterial_line` (`rawMaterialRequestNoteID`, `productID`, `rawMaterialID`, `rawMaterialRequestQuantity`) VALUES
(1, 1, 1, 5.00),
(2, 2, 3, 80.00),
(3, 3, 2, 12.00),
(4, 2, 4, 36.00),
(5, 4, 5, 25.00);

INSERT INTO `goodsreceivednote` (`goodsReceivedNoteID`, `goodsReceivedNoteCode`, `supplierID`, `PurchaseOrderID`, `staffID`, `createDate`, `lastModifyDate`, `status`, `remark`) VALUES
(1, 'GRN-2026052601', 1, 1, 4, '2026-05-26 14:00:00', '2026-05-26 14:00:00', 2, 'Partial oak delivery'),
(2, 'GRN-2026052602', 2, 3, 4, '2026-05-26 15:00:00', '2026-05-26 15:00:00', 2, 'Metal tubes received'),
(3, 'GRN-2026052603', 3, 2, 4, '2026-05-26 16:00:00', '2026-05-26 16:00:00', 1, 'Fabric pending QC'),
(4, 'GRN-2026052604', 4, 5, 4, '2026-05-26 17:00:00', '2026-05-26 17:00:00', 0, 'Hardware draft GRN'),
(5, 'GRN-2026052605', 5, 4, 4, '2026-05-26 18:00:00', '2026-05-26 18:00:00', 0, 'Foam awaiting inspection');

INSERT INTO `goodsreceivednoterawmaterialline` (`goodsReceivedNoteID`, `rawMaterialID`, `receivedQuantity`) VALUES
(1, 1, 20.00),
(2, 3, 50.00),
(3, 2, 0.00),
(4, 5, 0.00),
(5, 4, 0.00);

INSERT INTO `deliverynote` (`deliveryNoteID`, `deliveryNoteCode`, `customerID`, `SalesOrderID`, `staffID`, `createDate`, `lastModifyDate`, `WarehouseID`, `shipMethod`, `trackingNumber`, `remark`, `status`) VALUES
(1, 'DN-2026052601', 2, 2, 4, '2026-05-26 14:00:00', '2026-05-26 14:00:00', 1, 'Truck', 'TRK-HK-001', 'Partial chair shipment', 2),
(2, 'DN-2026052602', 4, 4, 4, '2026-05-26 15:00:00', '2026-05-26 15:00:00', 1, 'Courier', 'SF-99887766', 'Full chair delivery', 3),
(3, 'DN-2026052603', 1, 1, 4, '2026-05-26 16:00:00', NULL, 4, 'Truck', 'TRK-HK-002', 'Desk scheduled', 0),
(4, 'DN-2026052604', 3, 3, 4, '2026-05-26 17:00:00', NULL, 4, 'Van', 'VAN-554433', 'Sofa prep', 1),
(5, 'DN-2026052605', 5, 5, 4, '2026-05-26 18:00:00', NULL, 1, 'Courier', 'DHL-112233', 'Draft DN', 0);

INSERT INTO `deliveryproductline` (`deliveryNoteID`, `productID`, `shipQuantity`) VALUES
(1, 2, 3),
(2, 2, 10),
(3, 1, 0),
(4, 3, 0),
(5, 4, 0);

INSERT INTO `invoiceline` (`invoiceID`, `deliveryNoteID`, `productID`, `invoiceQuantity`, `amount`) VALUES
(1, 3, 1, 0, 0.00),
(2, 1, 2, 3, 3750.00),
(3, 4, 3, 0, 0.00),
(4, 2, 2, 10, 12000.00),
(5, 5, 4, 0, 0.00);

INSERT INTO `receiptvoucher` (`receiptVoucherID`, `receiptVoucherCode`, `cusomerID`, `staffID`, `createDate`, `lastModifyDate`, `paymentMethod`, `paymentMethodRef`, `paymentAmount`, `currencyID`, `paymentReceivedDate`, `status`, `remark`) VALUES
(1, 'RV-2026052601', 1, 1, '2026-05-26 10:00:00', '2026-05-26 10:00:00', 'Bank Transfer', 'BT-20260526-001', 5000.00, 1, '2026-05-26', 1, 'ABC deposit'),
(2, 'RV-2026052602', 2, 1, '2026-05-26 11:00:00', '2026-05-26 11:00:00', 'FPS', 'FPS-88776655', 3750.00, 1, '2026-05-26', 1, 'Pacific partial pay'),
(3, 'RV-2026052603', 3, 1, '2026-05-26 12:00:00', '2026-05-26 12:00:00', 'Cheque', 'CHQ-334455', 2000.00, 1, '2026-05-27', 0, 'Urban Living cheque'),
(4, 'RV-2026052604', 4, 1, '2026-05-26 13:00:00', '2026-05-26 13:00:00', 'Bank Transfer', 'BT-20260526-004', 12000.00, 1, '2026-05-26', 1, 'Green Office payment'),
(5, 'RV-2026052605', 5, 1, '2026-05-26 14:00:00', '2026-05-26 14:00:00', 'TT', 'TT-55667788', 1000.00, 1, '2026-05-28', 0, 'Elite deposit pending');

INSERT INTO `receiptvoucherinvoice` (`receiptVoucherID`, `lineNo`, `invoiceID`, `receivedAmount`, `type`) VALUES
(1, 1, 1, 5000.00, 1),
(2, 1, 2, 3750.00, 2),
(3, 1, 3, 2000.00, 2),
(4, 1, 4, 12000.00, 3),
(5, 1, 5, 1000.00, 1);

INSERT INTO `paymentvoucher` (`paymentVoucherID`, `paymentVoucherCode`, `supplierID`, `staffID`, `createDate`, `lastModifyDate`, `paymentMethod`, `paymentMethodRef`, `totalAmount`, `remark`, `status`) VALUES
(1, 'PV-2026052601', 1, 1, '2026-05-26 10:00:00', '2026-05-26 10:00:00', 'Bank Transfer', 'PV-BT-001', 9000.00, 'Oak supplier payment', 1),
(2, 'PV-2026052602', 2, 1, '2026-05-26 11:00:00', '2026-05-26 11:00:00', 'Bank Transfer', 'PV-BT-002', 1750.00, 'Metal Works partial', 1),
(3, 'PV-2026052603', 3, 1, '2026-05-26 12:00:00', '2026-05-26 12:00:00', 'TT', 'PV-TT-003', 0.00, 'Fabric PO pending', 0),
(4, 'PV-2026052604', 4, 1, '2026-05-26 13:00:00', '2026-05-26 13:00:00', 'Cheque', 'PV-CHQ-004', 3000.00, 'Hardware payment', 1),
(5, 'PV-2026052605', 5, 1, '2026-05-26 14:00:00', '2026-05-26 14:00:00', 'Bank Transfer', 'PV-BT-005', 2200.00, 'Foam factory', 0);

INSERT INTO `paymentvoucherpurchaseorder` (`paymentVoucherID`, `purchaseOrderID`, `type`, `payAmount`) VALUES
(1, 1, 1, 9000.00),
(2, 3, 2, 1750.00),
(3, 2, 1, 0.00),
(4, 5, 1, 3000.00),
(5, 4, 1, 2200.00);

INSERT INTO `systemdictionary_refundrequest` (`SystemDictionarydictionaryID`, `SystemDictionarycategory`, `SystemDictionarycodeValue`, `RefundRequestrefundRequestID`) VALUES
(34, 'REFUND_METHOD', 1, 1),
(35, 'REFUND_METHOD', 2, 2),
(41, 'REFUND_REASON', 1, 1),
(42, 'REFUND_REASON', 2, 2),
(43, 'REFUND_REASON', 3, 3);

--
-- 转储表的索引
--

--
-- 表的索引 `contactperson`
--
ALTER TABLE `contactperson`
  ADD PRIMARY KEY (`contactPersonID`),
  ADD KEY `fk_contact_customer` (`customerID`);

--
-- 表的索引 `currency`
--
ALTER TABLE `currency`
  ADD PRIMARY KEY (`currencyID`),
  ADD UNIQUE KEY `currencyCode` (`currencyCode`);

--
-- 表的索引 `customer`
--
ALTER TABLE `customer`
  ADD PRIMARY KEY (`customerID`);

--
-- 表的索引 `customerdeliveryaddress`
--
ALTER TABLE `customerdeliveryaddress`
  ADD PRIMARY KEY (`addressID`),
  ADD KEY `fk_address_customer` (`customerID`);

--
-- 表的索引 `deliverynote`
--
ALTER TABLE `deliverynote`
  ADD PRIMARY KEY (`deliveryNoteID`),
  ADD UNIQUE KEY `deliveryNoteCode` (`deliveryNoteCode`),
  ADD KEY `fk_dn_customer` (`customerID`),
  ADD KEY `fk_dn_so` (`SalesOrderID`),
  ADD KEY `fk_dn_staff` (`staffID`),
  ADD KEY `fk_dn_warehouse` (`WarehouseID`);

--
-- 表的索引 `deliveryproductline`
--
ALTER TABLE `deliveryproductline`
  ADD PRIMARY KEY (`deliveryNoteID`,`productID`),
  ADD KEY `fk_dline_product` (`productID`);

--
-- 表的索引 `goodsreceivednote`
--
ALTER TABLE `goodsreceivednote`
  ADD PRIMARY KEY (`goodsReceivedNoteID`),
  ADD UNIQUE KEY `goodsReceivedNoteCode` (`goodsReceivedNoteCode`),
  ADD KEY `fk_grn_supplier` (`supplierID`),
  ADD KEY `fk_grn_pur` (`PurchaseOrderID`),
  ADD KEY `fk_grn_staff` (`staffID`);

--
-- 表的索引 `goodsreceivednoterawmaterialline`
--
ALTER TABLE `goodsreceivednoterawmaterialline`
  ADD PRIMARY KEY (`goodsReceivedNoteID`,`rawMaterialID`),
  ADD KEY `fk_grnline_raw` (`rawMaterialID`);

--
-- 表的索引 `invoice`
--
ALTER TABLE `invoice`
  ADD PRIMARY KEY (`invoiceID`),
  ADD UNIQUE KEY `invoiceCode` (`invoiceCode`),
  ADD KEY `fk_inv_customer` (`customerID`),
  ADD KEY `fk_inv_so` (`salesOrderID`),
  ADD KEY `fk_inv_staff` (`staffID`);

--
-- 表的索引 `invoiceline`
--
ALTER TABLE `invoiceline`
  ADD PRIMARY KEY (`invoiceID`,`deliveryNoteID`,`productID`),
  ADD KEY `fk_invline_product` (`productID`);

--
-- 表的索引 `paymentvoucher`
--
ALTER TABLE `paymentvoucher`
  ADD PRIMARY KEY (`paymentVoucherID`),
  ADD UNIQUE KEY `paymentVoucherCode` (`paymentVoucherCode`),
  ADD KEY `fk_pv_supplier` (`supplierID`),
  ADD KEY `fk_pv_staff` (`staffID`);

--
-- 表的索引 `paymentvoucherpurchaseorder`
--
ALTER TABLE `paymentvoucherpurchaseorder`
  ADD PRIMARY KEY (`paymentVoucherID`,`purchaseOrderID`),
  ADD KEY `fk_pvpo_po` (`purchaseOrderID`);

--
-- 表的索引 `product`
--
ALTER TABLE `product`
  ADD PRIMARY KEY (`productID`),
  ADD UNIQUE KEY `productCode` (`productCode`),
  ADD KEY `fk_product_currency` (`currencyID`),
  ADD KEY `fk_product_staff` (`staffID`);

--
-- 表的索引 `productimage`
--
ALTER TABLE `productimage`
  ADD PRIMARY KEY (`productID`);

--
-- 表的索引 `productionorder`
--
ALTER TABLE `productionorder`
  ADD PRIMARY KEY (`productionOrderID`),
  ADD UNIQUE KEY `productionOrderCode` (`productionOrderCode`),
  ADD KEY `fk_po_so` (`salesOrderID`),
  ADD KEY `fk_po_staff` (`staffID`);

--
-- 表的索引 `productionorderproductline`
--
ALTER TABLE `productionorderproductline`
  ADD PRIMARY KEY (`ProductionOrderID`,`productID`),
  ADD KEY `fk_poline_product` (`productID`);

--
-- 表的索引 `productrawmaterialline`
--
ALTER TABLE `productrawmaterialline`
  ADD PRIMARY KEY (`productID`,`rawMaterialID`),
  ADD KEY `fk_bom_raw` (`rawMaterialID`);

--
-- 表的索引 `purchaseorder`
--
ALTER TABLE `purchaseorder`
  ADD PRIMARY KEY (`purchaseOrderID`),
  ADD UNIQUE KEY `purchaseOrderCode` (`purchaseOrderCode`),
  ADD KEY `fk_pur_supplier` (`supplierID`),
  ADD KEY `fk_pur_staff` (`staffID`);

--
-- 表的索引 `purchaseorderrawmaterialline`
--
ALTER TABLE `purchaseorderrawmaterialline`
  ADD PRIMARY KEY (`purchaseOrderID`,`rawMaterialID`),
  ADD KEY `fk_purline_raw` (`rawMaterialID`);

--
-- 表的索引 `quotation`
--
ALTER TABLE `quotation`
  ADD PRIMARY KEY (`quotationID`),
  ADD UNIQUE KEY `quotationCode` (`quotationCode`),
  ADD KEY `fk_quote_staff` (`staffID`),
  ADD KEY `fk_quote_customer` (`customerID`),
  ADD KEY `fk_quote_currency` (`currencyID`);

--
-- 表的索引 `quotationproductline`
--
ALTER TABLE `quotationproductline`
  ADD PRIMARY KEY (`quotationID`,`productID`),
  ADD KEY `fk_qline_product` (`productID`);

--
-- 表的索引 `rawmaterial`
--
ALTER TABLE `rawmaterial`
  ADD PRIMARY KEY (`rawMaterialID`),
  ADD UNIQUE KEY `rawMaterialCode` (`rawMaterialCode`);

--
-- 表的索引 `rawmaterialrequestnote`
--
ALTER TABLE `rawmaterialrequestnote`
  ADD PRIMARY KEY (`rawMaterialRequestNoteID`),
  ADD UNIQUE KEY `rawMaterialRequestNoteCode` (`rawMaterialRequestNoteCode`),
  ADD KEY `fk_rmreq_po` (`ProductionOrderID`),
  ADD KEY `fk_rmreq_staff` (`staffID`);

--
-- 表的索引 `rawmaterialrequestnoterawmaterial_line`
--
ALTER TABLE `rawmaterialrequestnoterawmaterial_line`
  ADD PRIMARY KEY (`rawMaterialRequestNoteID`,`productID`,`rawMaterialID`),
  ADD KEY `fk_rmreqline_raw` (`rawMaterialID`);

--
-- 表的索引 `rawmaterialshortagereportline`
--
ALTER TABLE `rawmaterialshortagereportline`
  ADD PRIMARY KEY (`shortageReportID`,`rawMaterialID`,`WarehousewarehouseID`),
  ADD KEY `fk_srline_raw` (`rawMaterialID`),
  ADD KEY `fk_srline_wh` (`WarehousewarehouseID`);

--
-- 表的索引 `rawmaterialsupplier`
--
ALTER TABLE `rawmaterialsupplier`
  ADD PRIMARY KEY (`rawMaterialID`,`supplierID`),
  ADD KEY `fk_rms_sup` (`supplierID`);

--
-- 表的索引 `rawmaterialwarehouse`
--
ALTER TABLE `rawmaterialwarehouse`
  ADD PRIMARY KEY (`rawMaterialID`,`warehouseID`),
  ADD KEY `fk_rmw_warehouse` (`warehouseID`);

--
-- 表的索引 `receiptvoucher`
--
ALTER TABLE `receiptvoucher`
  ADD PRIMARY KEY (`receiptVoucherID`),
  ADD UNIQUE KEY `receiptVoucherCode` (`receiptVoucherCode`),
  ADD KEY `fk_rv_customer` (`cusomerID`),
  ADD KEY `fk_rv_staff` (`staffID`),
  ADD KEY `fk_rv_currency` (`currencyID`);

--
-- 表的索引 `receiptvoucherinvoice`
--
ALTER TABLE `receiptvoucherinvoice`
  ADD PRIMARY KEY (`receiptVoucherID`,`lineNo`),
  ADD KEY `fk_rvi_inv` (`invoiceID`);

--
-- 表的索引 `refundrequest`
--
ALTER TABLE `refundrequest`
  ADD PRIMARY KEY (`refundRequestID`),
  ADD UNIQUE KEY `refundRequestCode` (`refundRequestCode`),
  ADD KEY `fk_refund_staff` (`staffID`);

--
-- 表的索引 `salesorder`
--
ALTER TABLE `salesorder`
  ADD PRIMARY KEY (`salesOrderID`),
  ADD UNIQUE KEY `salesOrderCode` (`salesOrderCode`),
  ADD KEY `fk_so_customer` (`customerID`),
  ADD KEY `fk_so_staff` (`staffID`),
  ADD KEY `fk_so_currency` (`currencyCurrencyID`);

--
-- 表的索引 `salesorderproductline`
--
ALTER TABLE `salesorderproductline`
  ADD PRIMARY KEY (`salesOrderID`,`productID`),
  ADD KEY `fk_soline_product` (`productID`);

--
-- 表的索引 `replyslip`
--
ALTER TABLE `replyslip`
  ADD PRIMARY KEY (`replySlipID`),
  ADD UNIQUE KEY `replySlipCode` (`replySlipCode`),
  ADD KEY `fk_rs_so` (`salesOrderID`),
  ADD KEY `fk_rs_customer` (`customerID`),
  ADD KEY `fk_rs_staff` (`staffID`),
  ADD KEY `fk_rs_currency` (`currencyID`);

--
-- 表的索引 `replyslipproductline`
--
ALTER TABLE `replyslipproductline`
  ADD PRIMARY KEY (`replySlipID`,`productID`),
  ADD KEY `fk_rsline_product` (`productID`);

--
-- 表的索引 `shortagereport`
--
ALTER TABLE `shortagereport`
  ADD PRIMARY KEY (`shortageReportID`),
  ADD UNIQUE KEY `shortageReportCode` (`shortageReportCode`);

--
-- 表的索引 `staff`
--
ALTER TABLE `staff`
  ADD PRIMARY KEY (`staffID`),
  ADD UNIQUE KEY `username` (`username`);

--
-- 表的索引 `supplier`
--
ALTER TABLE `supplier`
  ADD PRIMARY KEY (`supplierID`);

--
-- 表的索引 `systemdictionary`
--
ALTER TABLE `systemdictionary`
  ADD PRIMARY KEY (`dictionaryID`),
  ADD UNIQUE KEY `uk_category_value` (`category`,`codeValue`);

--
-- 表的索引 `systemdictionary_refundrequest`
--
ALTER TABLE `systemdictionary_refundrequest`
  ADD PRIMARY KEY (`SystemDictionarydictionaryID`,`RefundRequestrefundRequestID`),
  ADD KEY `fk_bridge_refund` (`RefundRequestrefundRequestID`);

--
-- 表的索引 `warehouse`
--
ALTER TABLE `warehouse`
  ADD PRIMARY KEY (`warehouseID`);

--
-- 表的索引 `warehouseproduct`
--
ALTER TABLE `warehouseproduct`
  ADD PRIMARY KEY (`warehouseID`,`productID`),
  ADD KEY `fk_wp_product` (`productID`);

--
-- 在导出的表使用AUTO_INCREMENT
--

--
-- 使用表AUTO_INCREMENT `contactperson`
--
ALTER TABLE `contactperson`
  MODIFY `contactPersonID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `currency`
--
ALTER TABLE `currency`
  MODIFY `currencyID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `customer`
--
ALTER TABLE `customer`
  MODIFY `customerID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `customerdeliveryaddress`
--
ALTER TABLE `customerdeliveryaddress`
  MODIFY `addressID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `deliverynote`
--
ALTER TABLE `deliverynote`
  MODIFY `deliveryNoteID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `goodsreceivednote`
--
ALTER TABLE `goodsreceivednote`
  MODIFY `goodsReceivedNoteID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `invoice`
--
ALTER TABLE `invoice`
  MODIFY `invoiceID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `paymentvoucher`
--
ALTER TABLE `paymentvoucher`
  MODIFY `paymentVoucherID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `product`
--
ALTER TABLE `product`
  MODIFY `productID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `productionorder`
--
ALTER TABLE `productionorder`
  MODIFY `productionOrderID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `purchaseorder`
--
ALTER TABLE `purchaseorder`
  MODIFY `purchaseOrderID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `quotation`
--
ALTER TABLE `quotation`
  MODIFY `quotationID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `rawmaterial`
--
ALTER TABLE `rawmaterial`
  MODIFY `rawMaterialID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `rawmaterialrequestnote`
--
ALTER TABLE `rawmaterialrequestnote`
  MODIFY `rawMaterialRequestNoteID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `receiptvoucher`
--
ALTER TABLE `receiptvoucher`
  MODIFY `receiptVoucherID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `refundrequest`
--
ALTER TABLE `refundrequest`
  MODIFY `refundRequestID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `salesorder`
--
ALTER TABLE `salesorder`
  MODIFY `salesOrderID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `replyslip`
--
ALTER TABLE `replyslip`
  MODIFY `replySlipID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `shortagereport`
--
ALTER TABLE `shortagereport`
  MODIFY `shortageReportID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `staff`
--
ALTER TABLE `staff`
  MODIFY `staffID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `supplier`
--
ALTER TABLE `supplier`
  MODIFY `supplierID` bigint(20) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- 使用表AUTO_INCREMENT `systemdictionary`
--
ALTER TABLE `systemdictionary`
  MODIFY `dictionaryID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=79;

--
-- 使用表AUTO_INCREMENT `warehouse`
--
ALTER TABLE `warehouse`
  MODIFY `warehouseID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识', AUTO_INCREMENT=6;

--
-- 限制导出的表
--

--
-- 限制表 `contactperson`
--
ALTER TABLE `contactperson`
  ADD CONSTRAINT `fk_contact_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE;

--
-- 限制表 `customerdeliveryaddress`
--
ALTER TABLE `customerdeliveryaddress`
  ADD CONSTRAINT `fk_address_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE;

--
-- 限制表 `deliverynote`
--
ALTER TABLE `deliverynote`
  ADD CONSTRAINT `fk_dn_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_dn_so` FOREIGN KEY (`SalesOrderID`) REFERENCES `salesorder` (`salesOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_dn_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_dn_warehouse` FOREIGN KEY (`WarehouseID`) REFERENCES `warehouse` (`warehouseID`) ON UPDATE CASCADE;

--
-- 限制表 `deliveryproductline`
--
ALTER TABLE `deliveryproductline`
  ADD CONSTRAINT `fk_dline_dn` FOREIGN KEY (`deliveryNoteID`) REFERENCES `deliverynote` (`deliveryNoteID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_dline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE;

--
-- 限制表 `goodsreceivednote`
--
ALTER TABLE `goodsreceivednote`
  ADD CONSTRAINT `fk_grn_pur` FOREIGN KEY (`PurchaseOrderID`) REFERENCES `purchaseorder` (`purchaseOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_grn_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_grn_supplier` FOREIGN KEY (`supplierID`) REFERENCES `supplier` (`supplierID`) ON UPDATE CASCADE;

--
-- 限制表 `goodsreceivednoterawmaterialline`
--
ALTER TABLE `goodsreceivednoterawmaterialline`
  ADD CONSTRAINT `fk_grnline_grn` FOREIGN KEY (`goodsReceivedNoteID`) REFERENCES `goodsreceivednote` (`goodsReceivedNoteID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_grnline_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE;

--
-- 限制表 `invoice`
--
ALTER TABLE `invoice`
  ADD CONSTRAINT `fk_inv_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_inv_so` FOREIGN KEY (`salesOrderID`) REFERENCES `salesorder` (`salesOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_inv_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `invoiceline`
--
ALTER TABLE `invoiceline`
  ADD CONSTRAINT `fk_invline_inv` FOREIGN KEY (`invoiceID`) REFERENCES `invoice` (`invoiceID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_invline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE;

--
-- 限制表 `paymentvoucher`
--
ALTER TABLE `paymentvoucher`
  ADD CONSTRAINT `fk_pv_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_pv_supplier` FOREIGN KEY (`supplierID`) REFERENCES `supplier` (`supplierID`) ON UPDATE CASCADE;

--
-- 限制表 `paymentvoucherpurchaseorder`
--
ALTER TABLE `paymentvoucherpurchaseorder`
  ADD CONSTRAINT `fk_pvpo_po` FOREIGN KEY (`purchaseOrderID`) REFERENCES `purchaseorder` (`purchaseOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_pvpo_pv` FOREIGN KEY (`paymentVoucherID`) REFERENCES `paymentvoucher` (`paymentVoucherID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `product`
--
ALTER TABLE `product`
  ADD CONSTRAINT `fk_product_currency` FOREIGN KEY (`currencyID`) REFERENCES `currency` (`currencyID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_product_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `productimage`
--
ALTER TABLE `productimage`
  ADD CONSTRAINT `fk_img_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `productionorder`
--
ALTER TABLE `productionorder`
  ADD CONSTRAINT `fk_po_so` FOREIGN KEY (`salesOrderID`) REFERENCES `salesorder` (`salesOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_po_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `productionorderproductline`
--
ALTER TABLE `productionorderproductline`
  ADD CONSTRAINT `fk_poline_po` FOREIGN KEY (`ProductionOrderID`) REFERENCES `productionorder` (`productionOrderID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_poline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE;

--
-- 限制表 `productrawmaterialline`
--
ALTER TABLE `productrawmaterialline`
  ADD CONSTRAINT `fk_bom_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_bom_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE;

--
-- 限制表 `purchaseorder`
--
ALTER TABLE `purchaseorder`
  ADD CONSTRAINT `fk_pur_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_pur_supplier` FOREIGN KEY (`supplierID`) REFERENCES `supplier` (`supplierID`) ON UPDATE CASCADE;

--
-- 限制表 `purchaseorderrawmaterialline`
--
ALTER TABLE `purchaseorderrawmaterialline`
  ADD CONSTRAINT `fk_purline_pur` FOREIGN KEY (`purchaseOrderID`) REFERENCES `purchaseorder` (`purchaseOrderID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_purline_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE;

--
-- 限制表 `quotation`
--
ALTER TABLE `quotation`
  ADD CONSTRAINT `fk_quote_currency` FOREIGN KEY (`currencyID`) REFERENCES `currency` (`currencyID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_quote_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_quote_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `quotationproductline`
--
ALTER TABLE `quotationproductline`
  ADD CONSTRAINT `fk_qline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_qline_quote` FOREIGN KEY (`quotationID`) REFERENCES `quotation` (`quotationID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `rawmaterialrequestnote`
--
ALTER TABLE `rawmaterialrequestnote`
  ADD CONSTRAINT `fk_rmreq_po` FOREIGN KEY (`ProductionOrderID`) REFERENCES `productionorder` (`productionOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rmreq_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `rawmaterialrequestnoterawmaterial_line`
--
ALTER TABLE `rawmaterialrequestnoterawmaterial_line`
  ADD CONSTRAINT `fk_rmreqline_note` FOREIGN KEY (`rawMaterialRequestNoteID`) REFERENCES `rawmaterialrequestnote` (`rawMaterialRequestNoteID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rmreqline_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE;

--
-- 限制表 `rawmaterialshortagereportline`
--
ALTER TABLE `rawmaterialshortagereportline`
  ADD CONSTRAINT `fk_srline_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_srline_sr` FOREIGN KEY (`shortageReportID`) REFERENCES `shortagereport` (`shortageReportID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_srline_wh` FOREIGN KEY (`WarehousewarehouseID`) REFERENCES `warehouse` (`warehouseID`) ON UPDATE CASCADE;

--
-- 限制表 `rawmaterialsupplier`
--
ALTER TABLE `rawmaterialsupplier`
  ADD CONSTRAINT `fk_rms_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rms_sup` FOREIGN KEY (`supplierID`) REFERENCES `supplier` (`supplierID`) ON UPDATE CASCADE;

--
-- 限制表 `rawmaterialwarehouse`
--
ALTER TABLE `rawmaterialwarehouse`
  ADD CONSTRAINT `fk_rmw_raw` FOREIGN KEY (`rawMaterialID`) REFERENCES `rawmaterial` (`rawMaterialID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rmw_warehouse` FOREIGN KEY (`warehouseID`) REFERENCES `warehouse` (`warehouseID`) ON UPDATE CASCADE;

--
-- 限制表 `receiptvoucher`
--
ALTER TABLE `receiptvoucher`
  ADD CONSTRAINT `fk_rv_currency` FOREIGN KEY (`currencyID`) REFERENCES `currency` (`currencyID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rv_customer` FOREIGN KEY (`cusomerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rv_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `receiptvoucherinvoice`
--
ALTER TABLE `receiptvoucherinvoice`
  ADD CONSTRAINT `fk_rvi_inv` FOREIGN KEY (`invoiceID`) REFERENCES `invoice` (`invoiceID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rvi_rv` FOREIGN KEY (`receiptVoucherID`) REFERENCES `receiptvoucher` (`receiptVoucherID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `refundrequest`
--
ALTER TABLE `refundrequest`
  ADD CONSTRAINT `fk_refund_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `salesorder`
--
ALTER TABLE `salesorder`
  ADD CONSTRAINT `fk_so_currency` FOREIGN KEY (`currencyCurrencyID`) REFERENCES `currency` (`currencyID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_so_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_so_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `salesorderproductline`
--
ALTER TABLE `salesorderproductline`
  ADD CONSTRAINT `fk_soline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_soline_so` FOREIGN KEY (`salesOrderID`) REFERENCES `salesorder` (`salesOrderID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `replyslip`
--
ALTER TABLE `replyslip`
  ADD CONSTRAINT `fk_rs_currency` FOREIGN KEY (`currencyID`) REFERENCES `currency` (`currencyID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rs_customer` FOREIGN KEY (`customerID`) REFERENCES `customer` (`customerID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rs_so` FOREIGN KEY (`salesOrderID`) REFERENCES `salesorder` (`salesOrderID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rs_staff` FOREIGN KEY (`staffID`) REFERENCES `staff` (`staffID`) ON UPDATE CASCADE;

--
-- 限制表 `replyslipproductline`
--
ALTER TABLE `replyslipproductline`
  ADD CONSTRAINT `fk_rsline_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_rsline_rs` FOREIGN KEY (`replySlipID`) REFERENCES `replyslip` (`replySlipID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `systemdictionary_refundrequest`
--
ALTER TABLE `systemdictionary_refundrequest`
  ADD CONSTRAINT `fk_bridge_dict` FOREIGN KEY (`SystemDictionarydictionaryID`) REFERENCES `systemdictionary` (`dictionaryID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_bridge_refund` FOREIGN KEY (`RefundRequestrefundRequestID`) REFERENCES `refundrequest` (`refundRequestID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- 限制表 `warehouseproduct`
--
ALTER TABLE `warehouseproduct`
  ADD CONSTRAINT `fk_wp_product` FOREIGN KEY (`productID`) REFERENCES `product` (`productID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_wp_warehouse` FOREIGN KEY (`warehouseID`) REFERENCES `warehouse` (`warehouseID`) ON UPDATE CASCADE;
COMMIT;

-- =========================================================
-- Compatibility migration for older customer/contact schema
-- (run on existing DB if table designer previously saved as integer fields)
-- =========================================================

ALTER TABLE `contactperson`
  MODIFY `contactPerson` varchar(100) DEFAULT NULL COMMENT '联系人姓名',
  MODIFY `title` varchar(30) DEFAULT NULL COMMENT '称谓/职位',
  MODIFY `phone` varchar(30) DEFAULT NULL COMMENT '电话',
  MODIFY `email` varchar(255) DEFAULT NULL COMMENT '邮箱';

ALTER TABLE `customerdeliveryaddress`
  MODIFY `deliveryAddress` varchar(255) DEFAULT NULL COMMENT '收货寄送地址',
  MODIFY `contactPerson` varchar(100) DEFAULT NULL COMMENT '收货联系人',
  MODIFY `phone` varchar(30) DEFAULT NULL COMMENT '收货电话',
  MODIFY `email` varchar(255) DEFAULT NULL;

ALTER TABLE `contactperson`
  MODIFY `contactPersonID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识';

ALTER TABLE `customerdeliveryaddress`
  MODIFY `addressID` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增唯一标识';

-- Normalize customerRefNumber to PO-PL-######### (customer / sales order)
UPDATE `customer`
SET `customerRefNumber` = CONCAT('PO-PL-', LPAD(`customerID`, 9, '0'))
WHERE `customerRefNumber` IS NULL
   OR TRIM(`customerRefNumber`) = ''
   OR `customerRefNumber` LIKE 'CR-%';

UPDATE `salesorder`
SET `customerRefNumber` = CONCAT('PO-PL-', LPAD(`salesOrderID`, 9, '0'))
WHERE `customerRefNumber` IS NULL
   OR TRIM(`customerRefNumber`) = ''
   OR `customerRefNumber` LIKE 'CR-%';

-- Strip "address (contact / phone)" into address-only for legacy SO rows
UPDATE `salesorder`
SET `deliveryAddress` = TRIM(SUBSTRING(`deliveryAddress`, 1, LOCATE('(', `deliveryAddress`) - 1))
WHERE `deliveryAddress` LIKE '%(%/%'
  AND LOCATE('(', `deliveryAddress`) > 0;

ALTER TABLE `purchaseorder`
  ADD COLUMN `paymentType` int(10) DEFAULT NULL COMMENT '付款类型（字典 FINANCIAL_CLEARING_TYPE）',
  ADD COLUMN `payAmount` decimal(12,2) DEFAULT 0.00 COMMENT '本次付款金额';

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
