# Premium Living ERP — 演示前检查清单

**对照：** Software Prototype: Basic Stage 提交功能  
**时长：** 约 15 分钟  
**账号：** `admin` / `123456`

---

## A. 环境与数据库

- [ ] MySQL 已启动，应用无连接错误
- [ ] 已导入 `furniture_erp_system_New.sql`
- [ ] 已执行 `docs/scripts/verify_demo_data.sql`，第 1–3 节为 OK
- [ ] （若演示 GRN 收货）已执行 `Scripts/seed_supplier_raw_material_quotes.sql`
- [ ] `FurnitureERP.exe` 可正常启动，分辨率 ≥ 1366×768

---

## B. 作业功能逐项可演示

| 作业要求 | 检查项 | 通过 |
|----------|--------|------|
| **Login / Menu** | `admin` 能登录，侧栏模块齐全 | [ ] |
| **Order — Create/Edit/View** | Sales Orders 能 New、Edit、打开详情 | [ ] |
| **Logistics — DN & Reply Slip** | 至少 1 张 **Confirmed** SO 可建 DN；DN 详情能看 RS | [ ] |
| **Logistics — Goods received** | Goods Received 列表有 GRN，或能快速从 PO 建 GRN | [ ] |
| **Inventory — inward** | Warehouse 能看 Raw Materials 的 physical 数量 | [ ] |
| **After-service** | Refunds 能 New；列表有或能关联 Invoice | [ ] |
| **Master Data** | Customers / Suppliers / Staff 列表有数据 | [ ] |
| **Security** | 顶栏 Change Password 可打开；Overview 有 Permissions 页 | [ ] |
| **Database** | verify SQL 有 quotation/salesorder 等计数 | [ ] |

---

## C. 推荐演示数据（记在现场便签）

| 用途 | 值 |
|------|-----|
| 客户 | ABC Clothing Ltd（ID 1） |
| 产品 | P-Chair-2001 |
| 供应商（PO/GRN） | TimberTech（ID 1） |
| 仓库（收货/库存） | China - Inventory（ID 1） |

---

## D. 15 分钟彩排顺序（勾选完成即彩排通过）

1. [ ] Login → 扫侧栏菜单  
2. [ ] Customers / Suppliers / Staff 各打开 1 条  
3. [ ] Sales Order：New → Edit → View → Confirm  
4. [ ] Delivery Note + Reply Slip  
5. [ ] GRN Confirm → Warehouse 库存对比  
6. [ ] Refund New + View  
7. [ ] Change Password 对话框 + Permissions 页  
8. [ ] 某单据 Activity + 口头 Database 说明  

---

## E. 风险预案

| 问题 | 处理 |
|------|------|
| 无 Confirmed SO | 现场新建 SO 并 Confirm（约 2 分钟） |
| 无可用 GRN | 用已有 PO 建 GRN，或仅演示 GRN 列表 + View |
| 无 Invoice 做退款 | 先对 SO 开一张小额 INV，或选种子已有发票 |
| 改密误操作 | 彩排只打开对话框，不提交 |

详细步骤见 [`DEMO_GUIDE.md`](DEMO_GUIDE.md)。
