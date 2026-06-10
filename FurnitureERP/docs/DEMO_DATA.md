# Premium Living ERP — 演示数据速查（作业功能版）

---

## 1. 登录

| 用户名 | 密码 | 用途 |
|--------|------|------|
| **admin** | **123456** | 全功能演示（库内 PBKDF2 哈希；首次启动应用会自动迁移明文密码） |

---

## 2. 作业功能 → 推荐数据

### Master Data（主数据）

| 作业字段 | 系统实体 | 推荐记录 |
|----------|----------|----------|
| restaurant | **Customer** | ID **1** — ABC Clothing Ltd |
| supplier | **Supplier** | ID **1** — TimberTech International Ltd |
| staff | **Staff** | 任意 `status=1` 员工；改密演示用 admin 即可 |

### Order Processing（订单）

| 字段 | 推荐 |
|------|------|
| 客户 | ABC Clothing Ltd |
| 产品 | **P-Chair-2001**（status=1） |
| 产品（加行） | P-Table-2002 |

### Logistics（物流）

| 单据 | 说明 |
|------|------|
| SO | 须 **Confirmed(1)** 后才能建 DN |
| DN / RS | `DN-xxxxxxxx` 对应 `RS-xxxxxxxx` |
| GRN | 关联 PO；Confirm 时选 **China - Inventory (1)** |

### Inventory（入库）

| 仓库 ID | 名称 | 查看 |
|---------|------|------|
| **1** | China - Inventory | 成品 + 原料 physicalQuantity |
| 5 | China - Production | 生产仓（扩展用） |

### After-service（售后）

| 项目 | 说明 |
|------|------|
| Refund 原因 | Damage、Wrong Shipment、Order Cancelled 等 |
| 前置 | 需有关联 **Invoice**（可对 SO 现场开票） |

### Security（安全）

| 功能 | 入口 |
|------|------|
| 改密 | 顶栏 **Change Password** |
| 权限 | **Overview → Permissions**；**Staff** 编辑权限位 |

---

## 3. 供应商（仅 PO / GRN 需要）

**必须用有报价的供应商：**

| ID | 名称 |
|----|------|
| **1** | TimberTech International Ltd |
| 4 | Prime Lumber Supply Co. |

**避免使用（无种子报价）：** ID 2、12、15、58–100

补救脚本：`Scripts/seed_supplier_raw_material_quotes.sql`

---

## 4. 核心数据库表（收尾讲解用）

| 作业领域 | 主要表 |
|----------|--------|
| Login / Staff | `staff` |
| Order | `salesorder`, `salesorderproductline` |
| Logistics | `deliverynote`, `goodsreceivednote`, `purchaseorder` |
| Inventory | `warehouseproduct`, `rawmaterialwarehouse` |
| After-service | `refundrequest` |
| Master Data | `customer`, `supplier`, `product`, `rawmaterial` |
| 审计 | `documentauditlog` |

---

## 5. 自检脚本

```text
SOURCE path/to/FurnitureERP/docs/scripts/verify_demo_data.sql;
```

详见 [`DEMO_GUIDE.md`](DEMO_GUIDE.md)。
