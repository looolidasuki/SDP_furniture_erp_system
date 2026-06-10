# Premium Living ERP — 15 分钟演示指南（作业功能对照版）

| 项目 | 说明 |
|------|------|
| 系统名称 | Premium Living ERP |
| 演示时长 | **约 15 分钟** |
| 演示账号 | `admin` / `123456`（Super User） |
| 对照依据 | Software Prototype: Basic Stage 提交要求 |

---

## 1. 作业功能与系统模块对照

| # | 提交要求 | 本系统实现 | 演示入口（侧栏） |
|---|----------|------------|------------------|
| 1 | **Menu Program / Login** | 登录窗 + 主界面侧栏菜单 | 启动程序 → 登录 → 侧栏导航 |
| 2 | **Order Processing** — Create / Edit / View Order | 销售单（Sales Order） | **Sales Orders** |
| 3 | **Logistics** — Delivery Notes & Reply Slip | 发货单 DN + 配对回单 RS | **Delivery Notes** |
| 4 | **Logistics** — Handling Goods received | 收货单 GRN 确认收货 | **Goods Received** |
| 5 | **Inventory Control** — Record of inward goods | GRN 入库更新原料库存；仓存查询 | **Goods Received** → **Warehouse** |
| 6 | **After-service** — return / replacement / refund | 退款申请（含退货等原因字典） | **Refunds** |
| 7 | **Master Data** — supplier, staff, restaurant… | 供应商、员工、**客户**（本系统为家具 B2B，客户≈作业中的 restaurant） | **Suppliers** / **Staff** / **Customers** |
| 8 | **System Security & Control** | 改密、岗位权限、模块访问控制 | 顶栏 **Change Password**；**Overview → Permissions**；**Staff** |
| 9 | **Database Implementation** | MySQL 持久化各业务表 | 演示收尾说明 + 可选运行自检 SQL |

---

## 2. 演示前准备

- 清单：[`DEMO_CHECKLIST.md`](DEMO_CHECKLIST.md)
- 数据速查：[`DEMO_DATA.md`](DEMO_DATA.md)
- 自检 SQL：[`scripts/verify_demo_data.sql`](scripts/verify_demo_data.sql)

**采购/收货演示额外要求：** 执行 `Scripts/seed_supplier_raw_material_quotes.sql`，建 PO 时选 **Supplier ID = 1（TimberTech）**。

---

## 3. 十五分钟时间轴（按作业条目走）

| 时间 | 作业条目 | 模块 | 操作摘要 |
|------|----------|------|----------|
| 0:00–1:00 | Login / Menu | 登录 + 侧栏 | `admin` 登录，介绍侧栏模块即「菜单程序」 |
| 1:00–2:30 | Master Data | Customers / Suppliers / Staff | 各打开一条记录展示主数据维护 |
| 2:30–5:30 | Order Processing | Sales Orders | **新建** → **编辑**行 → **查看**详情 |
| 5:30–7:30 | Logistics (DN/RS) | Delivery Notes | 从 SO 生成 DN，展示 **Reply Slip** |
| 7:30–10:00 | Logistics (GRN) + Inventory | Goods Received + Warehouse | **确认收货**，对比原料 **入库前后** 库存 |
| 10:00–12:00 | After-service | Refunds | **新建退款申请**（退货等原因）并查看 |
| 12:00–13:30 | Security | 改密 + 权限 | **Change Password** 界面；**Overview → Permissions** |
| 13:30–15:00 | Database + 收尾 | Overview / Activity | 说明 MySQL 表结构；打开单据 **Activity** 审计 |

---

## 4. 分步操作与讲解话术

### 4.1 Menu Program / Login（0:00–1:00）

**操作**

1. 启动 `FurnitureERP.exe`
2. 输入 `admin` / `123456` → Login
3. 进入 **Overview**，沿侧栏快速扫一遍菜单项

**话术**

> 「系统提供登录与主菜单；不同岗位登录后看到的侧栏由权限控制，这就是 Menu Program。」

**侧栏与作业对应（可口头对照）**

- Sales Orders → Order Processing  
- Delivery Notes / Goods Received → Logistics  
- Warehouse → Inventory  
- Refunds → After-service  
- Suppliers / Staff / Customers → Master Data  

---

### 4.2 Master Data Maintenance（1:00–2:30）

**操作（每项约 30 秒，只演示查看 + 说明可增删改）**

| 作业字段 | 系统模块 | 演示动作 |
|----------|----------|----------|
| restaurant（客户） | **Customers** | 打开 `ABC Clothing Ltd`，展示地址、联系人 |
| supplier | **Suppliers** | 打开 `TimberTech International Ltd`（ID 1） |
| staff | **Staff**（仅 admin） | 打开任一员工，展示部门、职位 |

**话术**

> 「作业要求维护餐厅/供应商/员工等主数据；我们是家具 ERP，**客户（Customer）对应 B2B 买方**，供应商与员工同样支持新增、编辑与停用。」

可选：System Admin → **Import CSV**（一句话带过主数据批量导入）。

---

### 4.3 Order Processing Management（2:30–5:30）

**Create Order**

1. **Sales Orders** → **New**
2. 客户：`ABC Clothing Ltd`
3. 添加产品行：`P-Chair-2001`，数量 1
4. 保存（Draft）

**Edit Order**

1. 在同一 SO 上 **Edit**
2. 修改数量或加一行 `P-Table-2002`
3. 保存

**View Order**

1. 双击打开详情 / **View**
2. 展示表头、产品行、状态、金额
3. 点击 **Confirm**（Draft → Confirmed）— 便于后续发货演示

**话术**

> 「销售单覆盖作业的 Create、Edit、View Order；确认后订单进入可执行状态。」

**状态（简记）**：Draft(0) → Confirmed(1) → Processing → Shipped → Completed

---

### 4.4 Logistics — Delivery Notes & Reply Slip（5:30–7:30）

**操作**

1. **Delivery Notes**（或在 Warehouse 面板 Delivery 标签）
2. 从已 **Confirmed** 的 SO **创建 Delivery Note**
3. 填写/确认发货信息 → 保存
4. 在 DN 详情中打开 **Reply Slip**（或打印/查看 RS）
5. 指出编号关系：`DN-00000001` ↔ `RS-00000001`

**话术**

> 「物流模块生成发货单；回单 Reply Slip 与 DN 配对，用于客户签收确认，对应作业 Generate Delivery Notes & Reply Slip。」

---

### 4.5 Logistics — Goods Received + Inventory Inward（7:30–10:00）

> 若时间紧：跳过新建 PO，直接打开种子库中已有 **Draft GRN** 做 Confirm；若无则快速建 PO→GRN（见下方快捷路径）。

**快捷路径（推荐，约 2.5 分钟）**

1. **Goods Received** → 选一条未完成的 GRN → **View**
2. 记下关联原料与数量
3. **Warehouse** → 选收货仓库 → **Raw Materials** 页，记下某原料 `physicalQuantity`
4. 回到 GRN → **Confirm Receipt**，选仓库
5. 再回 **Warehouse** 刷新，展示 **physicalQuantity 增加**

**完整路径（时间充裕时）**

1. **Purchase Orders** → New，供应商选 **TimberTech (ID 1)**
2. 添加有报价的原料行 → Approve → Ordered
3. **Goods Received** → New from PO → **Confirm Receipt**
4. **Warehouse** 对比入库前后

**话术**

> 「Handling Goods received 通过 GRN 确认收货；Inventory 的 inward 体现为原料 physical 库存增加，系统与数据库同步更新。」

---

### 4.6 After-service Management（10:00–12:00）

**操作**

1. **Refunds** → **New Refund Request**
2. 选择客户、关联 **Invoice**（可先对 SO 开一张小额发票，或选用种子已有发票）
3. 退款原因选：**Damage** / **Wrong Shipment** / **Order Cancelled** 等（对应 return / 售后场景）
4. 保存为 Draft → 演示 **View** 详情与状态
5. （可选）推进至 Approved

**话术**

> 「售后模块记录退款申请；原因字典覆盖损坏、错发、取消等，对应作业中的 return、replacement、refund 记录需求。换货可结合新 SO/DN 说明扩展流程。」

**说明**：当前原型以 **Refund Request** 为主流程；换货（replacement）可口述为「退款审批后补发新 DN」。

---

### 4.7 System Security & Control（12:00–13:30）

**操作**

1. 顶栏 **Change Password** → 展示改密对话框（**现场可不真改**，避免锁死 admin）
2. **Overview** → **Permissions** 标签 → 展示当前用户模块权限矩阵
3. （可选 20 秒）**Staff** → 某员工的 Permission 勾选 → 说明数据访问控制

**话术**

> 「Security 包含密码管理与按模块的访问控制；非 Super User 无法看到 Staff / System Admin，且不能越权打开单据。」

---

### 4.8 Database Implementation + 收尾（13:30–15:00）

**操作**

1. 打开任意 SO / GRN / Refund 详情的 **Activity** 标签
2. 说明数据落在 MySQL，例如：`salesorder`、`deliverynote`、`goodsreceivednote`、`rawmaterialwarehouse`、`refundrequest`、`staff`、`supplier`、`customer`
3. 提及演示前可运行 `docs/scripts/verify_demo_data.sql` 验证库内数据

**收尾话术**

> 「本原型在 MySQL 上实现完整单据链路与主数据；界面操作实时读写数据库，满足 Database Implementation 要求。」

---

## 5. 单据编号速查

| 前缀 | 含义 |
|------|------|
| SO- | 销售单（Order） |
| DN- / RS- | 发货单 / 回单 |
| PO- / GRN- | 采购单 / 收货单 |
| INV- / RF- | 发票 / 退款申请 |
| CU- | 客户 |

---

## 6. 常见问题

| 现象 | 处理 |
|------|------|
| PO 无原料可选 | 换 Supplier ID=1，或跑报价补种脚本 |
| 无法建 DN | SO 须 Confirmed，且成品仓有货或允许部分发 |
| GRN Confirm 失败 | 检查 PO 是否 Ordered、是否有可收数量 |
| 侧栏无 Staff | 须 `admin` 登录 |
| 作业写 restaurant | 演示 **Customers**，口头说明行业差异 |

---

## 7. 相关文件

| 文件 | 用途 |
|------|------|
| [`DEMO_CHECKLIST.md`](DEMO_CHECKLIST.md) | 演示前勾选 |
| [`DEMO_DATA.md`](DEMO_DATA.md) | 推荐 ID |
| [`scripts/verify_demo_data.sql`](scripts/verify_demo_data.sql) | 库内自检 |
