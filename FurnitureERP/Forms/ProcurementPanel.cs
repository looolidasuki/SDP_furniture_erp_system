using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class ProcurementPanel : UserControl
    {
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly PurchaseOrderController _purchaseOrderCtrl = new PurchaseOrderController();
        private readonly GoodsReceivedNoteController _grnCtrl = new GoodsReceivedNoteController();
        private readonly SupplierController _supplierCtrl = new SupplierController();
        private readonly InventoryWorkflowService _inventoryWorkflow = new InventoryWorkflowService();
        private readonly ShortageReportController _shortageCtrl = new ShortageReportController();
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly PaymentVoucherController _paymentVoucherCtrl = new PaymentVoucherController();
        private readonly CurrencyController _currencyCtrl = new CurrencyController();

        private TabControl _tabs;

        public ProcurementPanel(string module = "Purchase Orders")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            Tag = module;
            BuildUI();
            SelectModuleTab(module);
        }

        private void SelectModuleTab(string module)
        {
            if (_tabs == null || _tabs.TabPages.Count == 0) return;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                string text = _tabs.TabPages[i].Text;
                if (module == "Goods Received" && text.Contains("Goods Received")) { _tabs.SelectedIndex = i; return; }
                if (module == "Suppliers" && text.Contains("Supplier")) { _tabs.SelectedIndex = i; return; }
                if (module == "Raw Materials" && text.Contains("Raw Material")) { _tabs.SelectedIndex = i; return; }
                if (module == "Purchase Orders" && text.Contains("Purchase Order")) { _tabs.SelectedIndex = i; return; }
            }
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            if (AppSession.CanView(PermissionModule.RawMaterial))
                _tabs.TabPages.Add(BuildRawMaterialsTab());
            if (AppSession.CanView(PermissionModule.PurchaseOrder))
                _tabs.TabPages.Add(BuildPurchaseOrdersTab());
            if (AppSession.CanView(PermissionModule.GoodsReceivedNote))
                _tabs.TabPages.Add(BuildGoodsReceivedTab());
            if (AppSession.CanView(PermissionModule.Supplier))
                _tabs.TabPages.Add(BuildSuppliersTab());

            Controls.Add(_tabs);
        }

        private static void ReloadRawMaterialGrid(DataGridView grid, RawMaterialController ctrl)
        {
            GridHelper.BindStatusWithStockAlert(
                grid,
                ctrl.GetAllRawMaterialsWithStock(),
                "Status",
                DictionaryService.Categories.RawMaterial,
                "Current Stock",
                "Min Stock");
        }

        private static void ReloadPurchaseOrderGrid(DataGridView grid, PurchaseOrderController ctrl)
        {
            GridHelper.BindStatusData(
                grid,
                ctrl.GetAllPurchaseOrders(),
                "Status",
                DictionaryService.Categories.PurchaseOrder);
        }

        private static void ReloadGrnGrid(DataGridView grid, GoodsReceivedNoteController ctrl)
        {
            GridHelper.BindStatusData(
                grid,
                ctrl.GetAllGoodsReceivedNotes(),
                "Status",
                DictionaryService.Categories.PurchaseOrder);
        }

        private static void ReloadSupplierGrid(DataGridView grid, SupplierController ctrl)
        {
            GridHelper.BindStatusData(
                grid,
                ctrl.GetAllSuppliers(),
                "Status",
                DictionaryService.Categories.Supplier);
        }

        private TabPage BuildRawMaterialsTab()
        {
            var tab = new TabPage("Raw Materials") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Raw Material", PermissionModule.RawMaterial, () => ShowRawMaterialDialog(), grid, () => {
                try { ReloadRawMaterialGrid(grid, _rawMaterialCtrl); } catch { }
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Raw Material ID") <= 0) { UITheme.ShowWarning("Please select a raw material first."); return; }
                ShowRowDetail(grid.CurrentRow, "Raw Material Details");
            });
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long rawMaterialId = GridHelper.TryGetRowLongId(grid, grid.Rows[e.RowIndex], "Raw Material ID");
                if (rawMaterialId <= 0) return;
                var entity = _rawMaterialCtrl.GetById(rawMaterialId);
                if (entity != null && AppSession.CanEdit(PermissionModule.RawMaterial)) ShowRawMaterialDialog(entity);
                else ShowRowDetail(grid.Rows[e.RowIndex], "Raw Material Details");
            };

            try { ReloadRawMaterialGrid(grid, _rawMaterialCtrl); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Raw Material Filters", DictionaryService.Categories.RawMaterial));
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildPurchaseOrdersTab()
        {
            var tab = new TabPage("Purchase Orders") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Purchase Order", PermissionModule.PurchaseOrder, () => ShowPurchaseOrderDialog(), grid, () => {
                try { ReloadPurchaseOrderGrid(grid, _purchaseOrderCtrl); } catch { }
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Purchase Order ID") <= 0) { UITheme.ShowWarning("Please select a purchase order first."); return; }
                ShowPurchaseOrderTableDialog(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Purchase Order ID"), grid.CurrentRow);
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Purchase Order ID") <= 0) { UITheme.ShowWarning("Please select a purchase order first."); return; }
                ShowPurchaseOrderDetailDialog(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Purchase Order ID"));
            });
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long purchaseOrderId = GridHelper.TryGetRowLongId(grid, grid.Rows[e.RowIndex], "Purchase Order ID");
                if (purchaseOrderId <= 0) return;
                if (AppSession.CanEdit(PermissionModule.PurchaseOrder))
                    ShowPurchaseOrderDetailDialog(purchaseOrderId);
                else
                    ShowPurchaseOrderTableDialog(purchaseOrderId, grid.Rows[e.RowIndex]);
            };

            try { ReloadPurchaseOrderGrid(grid, _purchaseOrderCtrl); } catch { }

            var btnUnpaid = UITheme.CreateSecondaryButton("Unpaid POs");
            btnUnpaid.Location = new Point(8, 8);
            int unpaidX = 8;
            foreach (Control ctl in toolbar.Controls)
            {
                if (ctl.Visible && ctl.Right > unpaidX) unpaidX = ctl.Right;
            }
            btnUnpaid.Location = new Point(unpaidX + 10, 8);
            btnUnpaid.Click += (s, e) =>
            {
                try
                {
                    var dt = DictionaryUIHelper.LoadWithStatusLabels(
                        () => DashboardOverviewService.GetUnsettledPurchaseOrders(500),
                        "Status", DictionaryService.Categories.PurchaseOrder);
                    GridHelper.BindStatusData(grid, dt, DictionaryService.Categories.PurchaseOrder);
                    if (grid.Columns.Contains("Purchase Order ID")) grid.Columns["Purchase Order ID"].Visible = false;
                }
                catch (Exception ex) { UITheme.ShowError(ex.Message); }
            };
            var btnPoShowAll = UITheme.CreateSecondaryButton("Show All");
            btnPoShowAll.Location = new Point(btnUnpaid.Right + 10, 8);
            btnPoShowAll.Click += (s, e) =>
            {
                try { ReloadPurchaseOrderGrid(grid, _purchaseOrderCtrl); } catch (Exception ex) { UITheme.ShowError(ex.Message); }
            };
            toolbar.Controls.Add(btnUnpaid);
            toolbar.Controls.Add(btnPoShowAll);

            tab.Controls.Add(grid);
            tab.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Purchase Order Filters", DictionaryService.Categories.PurchaseOrder));
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildGoodsReceivedTab()
        {
            var tab = new TabPage("Goods Received") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New GRN", PermissionModule.GoodsReceivedNote, () => ShowGrnDialog(), grid, () => {
                try { ReloadGrnGrid(grid, _grnCtrl); } catch { }
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID") <= 0) { UITheme.ShowWarning("Please select a GRN first."); return; }
                ShowGrnTableDialog(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID"), grid.CurrentRow);
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID") <= 0) { UITheme.ShowWarning("Please select a GRN first."); return; }
                ShowGrnDetailDialog(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID"));
            });

            var btnConfirm = UITheme.CreateSecondaryButton("Confirm Receipt");
            int confirmX = 8;
            foreach (Control ctl in toolbar.Controls)
            {
                if (ctl.Visible && ctl.Right > confirmX) confirmX = ctl.Right;
            }
            btnConfirm.Location = new Point(confirmX + 10, 8);
            btnConfirm.Click += (s, e) =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID") <= 0) { UITheme.ShowWarning("Please select a GRN first."); return; }
                if (!PermissionGuard.Ensure(PermissionModule.GoodsReceivedNote, PermissionAction.Edit, this)) return;
                long grnId = GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "GRN ID");
                ShowConfirmGrnDialog(grnId, () =>
                {
                    try { ReloadGrnGrid(grid, _grnCtrl); } catch { }
                });
            };
            PermissionGuard.ApplyEditButton(btnConfirm, PermissionModule.GoodsReceivedNote);
            toolbar.Controls.Add(btnConfirm);
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long grnId = GridHelper.TryGetRowLongId(grid, grid.Rows[e.RowIndex], "GRN ID");
                if (grnId <= 0) return;
                if (AppSession.CanEdit(PermissionModule.GoodsReceivedNote))
                    ShowGrnDetailDialog(grnId);
                else
                    ShowGrnTableDialog(grnId, grid.Rows[e.RowIndex]);
            };

            try { ReloadGrnGrid(grid, _grnCtrl); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "GRN Filters", DictionaryService.Categories.PurchaseOrder));
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildSuppliersTab()
        {
            var tab = new TabPage("Suppliers") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Supplier", PermissionModule.Supplier, () => ShowSupplierDialog(), grid, () => {
                try { ReloadSupplierGrid(grid, _supplierCtrl); } catch { }
            }, () =>
            {
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Supplier ID") <= 0) { UITheme.ShowWarning("Please select a supplier first."); return; }
                ShowSupplierTableDialog(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Supplier ID"), grid.CurrentRow);
            }, () =>
            {
                long supplierId = GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Supplier ID");
                if (supplierId <= 0) { UITheme.ShowWarning("Please select a supplier first."); return; }
                var entity = _supplierCtrl.GetById(supplierId);
                if (entity != null) ShowSupplierDialog(entity);
            });
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long supplierId = GridHelper.TryGetRowLongId(grid, grid.Rows[e.RowIndex], "Supplier ID");
                if (supplierId <= 0) return;
                var entity = _supplierCtrl.GetById(supplierId);
                if (entity != null && AppSession.CanEdit(PermissionModule.Supplier)) ShowSupplierDialog(entity);
                else ShowSupplierTableDialog(supplierId, grid.Rows[e.RowIndex]);
            };

            try { ReloadSupplierGrid(grid, _supplierCtrl); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Supplier Filters", DictionaryService.Categories.Supplier));
            tab.Controls.Add(toolbar);
            return tab;
        }

        private Panel BuildToolbar(string createLabel, string permissionModule, Action onCreate, DataGridView grid, Action onRefresh, Action onViewDetail, Action onEdit = null)
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            var btnCreate = UITheme.CreatePrimaryButton(createLabel);
            btnCreate.Location = new Point(0, 8);
            btnCreate.Click += (s, e) => { if (PermissionGuard.Ensure(permissionModule, PermissionAction.Create, this)) onCreate(); };
            PermissionGuard.ApplyCreateButton(btnCreate, permissionModule);

            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnCreate.Width + 10, 8);
            btnRefresh.Click += (s, e) => onRefresh();
            var btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 8);
            btnDetail.Click += (s, e) => onViewDetail?.Invoke();

            toolbar.Controls.Add(btnCreate);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetail);

            if (onEdit != null)
            {
                var btnEdit = UITheme.CreateSecondaryButton("Edit");
                btnEdit.Location = new Point(btnDetail.Right + 10, 8);
                btnEdit.Click += (s, e) =>
                {
                    if (GridHelper.TryGetRowLongId(grid, grid?.CurrentRow) <= 0) { UITheme.ShowWarning("Please select a record first."); return; }
                    if (!PermissionGuard.Ensure(permissionModule, PermissionAction.Edit, this)) return;
                    onEdit();
                };
                PermissionGuard.ApplyEditButton(btnEdit, permissionModule);
                toolbar.Controls.Add(btnEdit);
            }
            return toolbar;
        }

        private void ShowRowDetail(DataGridViewRow row, string title)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(640, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var grid = GridHelper.CreateStyledGrid();
                var dt = new DataTable();
                dt.Columns.Add("Field");
                dt.Columns.Add("Value");

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn == null) continue;
                    dt.Rows.Add(cell.OwningColumn.HeaderText, cell.Value?.ToString() ?? "");
                }
                grid.DataSource = dt;
                GridHelper.StyleGrid(grid);
                dlg.Controls.Add(grid);
                dlg.ShowDialog(this);
            }
        }

        private static string ExtractLabeledValue(string remark, string label)
        {
            if (string.IsNullOrWhiteSpace(remark) || string.IsNullOrWhiteSpace(label)) return "";
            string needle = label + ":";

            int idx = remark.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";

            int start = idx + needle.Length;
            int end = remark.IndexOfAny(new[] { '\n', '\r', ';' }, start);
            if (end < 0) end = remark.Length;

            return (remark.Substring(start, end - start) ?? "").Trim();
        }

        private static string RemoveLabeledLines(string remark, params string[] labels)
        {
            if (string.IsNullOrWhiteSpace(remark)) return "";
            var lines = remark.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                bool skip = false;
                foreach (var label in labels)
                {
                    if (line.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        skip = true;
                        break;
                    }
                }
                if (!skip) kept.Add(line);
            }
            return string.Join("\n", kept).Trim();
        }

        private static string RemovePoAddressLinesFromRemark(string remark)
        {
            return RemoveLabeledLines(remark,
                "Bill-To Address", "Ship-To Address", "Buyer Address", "Delivery Address");
        }

        private const int PurchaseOrderRemarkMaxLength = 255;

        private static string BuildRemarkWithLabels(string billToAddress, string shipToAddress, string otherRemark)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(billToAddress)) parts.Add("Bill-To Address: " + billToAddress.Trim());
            if (!string.IsNullOrWhiteSpace(shipToAddress)) parts.Add("Ship-To Address: " + shipToAddress.Trim());
            if (!string.IsNullOrWhiteSpace(otherRemark)) parts.Add(otherRemark.Trim());
            return string.Join("\n", parts).Trim();
        }

        private static string ExtractPoBillToAddress(string remark)
        {
            string v = ExtractLabeledValue(remark, "Bill-To Address");
            return string.IsNullOrWhiteSpace(v) ? ExtractLabeledValue(remark, "Buyer Address") : v;
        }

        private static string ExtractPoShipToAddress(string remark)
        {
            string v = ExtractLabeledValue(remark, "Ship-To Address");
            return string.IsNullOrWhiteSpace(v) ? ExtractLabeledValue(remark, "Delivery Address") : v;
        }

        private static bool TryBuildPurchaseOrderRemark(string billTo, string shipTo, string otherRemark, out string remark, out string error)
        {
            remark = BuildRemarkWithLabels(billTo, shipTo, otherRemark);
            if (remark != null && remark.Length > PurchaseOrderRemarkMaxLength)
            {
                error = $"Remark and addresses exceed {PurchaseOrderRemarkMaxLength} characters. Please shorten Ship-To / Bill-To or notes.";
                return false;
            }
            error = null;
            return true;
        }

        private void RefreshSupplierBillingDisplay(ComboBox cmbSupplier, TextBox txtBilling)
        {
            long supplierId = ResolveSupplierId(cmbSupplier);
            if (supplierId <= 0)
            {
                txtBilling.Text = "";
                return;
            }
            try
            {
                var supplier = _supplierCtrl.GetById(supplierId);
                txtBilling.Text = supplier != null && !string.IsNullOrWhiteSpace(supplier.BillingAddress)
                    ? supplier.BillingAddress.Trim()
                    : "";
            }
            catch { txtBilling.Text = ""; }
        }

        private void ApplyWarehouseToShipTo(ComboBox cmbWarehouse, TextBox txtShipTo)
        {
            long warehouseId = GetComboLongId(cmbWarehouse);
            if (warehouseId <= 0) return;
            try
            {
                var wh = _warehouseCtrl.GetById(warehouseId);
                if (wh != null && !string.IsNullOrWhiteSpace(wh.WarehouseAddress))
                    txtShipTo.Text = wh.WarehouseAddress.Trim();
            }
            catch { }
        }

        private static void TrySelectWarehouseForShipTo(ComboBox cmbWarehouse, string shipTo)
        {
            if (string.IsNullOrWhiteSpace(shipTo) || !(cmbWarehouse.DataSource is DataTable dt)) return;
            string norm = shipTo.Trim();
            foreach (DataRow row in dt.Rows)
            {
                string addr = row.Table.Columns.Contains("Address") ? row["Address"]?.ToString()?.Trim() : "";
                if (!string.IsNullOrEmpty(addr) && addr.Equals(norm, StringComparison.OrdinalIgnoreCase))
                {
                    SetComboLongValue(cmbWarehouse, Convert.ToInt64(row["Warehouse ID"]));
                    return;
                }
            }
        }

        private TextBox CreateReadOnlyBillingBox()
        {
            return new TextBox
            {
                ReadOnly = true,
                Multiline = true,
                Height = 44,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };
        }

        private void WirePurchaseOrderAddressFields(
            ComboBox cmbSupplier,
            TextBox txtSupplierBilling,
            ComboBox cmbReceivingWarehouse,
            TextBox txtShipTo,
            TextBox txtBillTo,
            Button btnSameAsShipTo)
        {
            txtShipTo.Multiline = true;
            txtShipTo.Height = 48;
            txtBillTo.Multiline = true;
            txtBillTo.Height = 48;

            Action refreshBilling = () => RefreshSupplierBillingDisplay(cmbSupplier, txtSupplierBilling);
            cmbSupplier.SelectedIndexChanged += (s, e) => refreshBilling();
            cmbSupplier.Leave += (s, e) => refreshBilling();
            refreshBilling();

            cmbReceivingWarehouse.SelectedIndexChanged += (s, e) => ApplyWarehouseToShipTo(cmbReceivingWarehouse, txtShipTo);
            btnSameAsShipTo.Click += (s, e) => txtBillTo.Text = txtShipTo.Text?.Trim() ?? "";
        }

        private static bool ValidatePurchaseOrderAddresses(ComboBox cmbWarehouse, TextBox txtShipTo, out string error)
        {
            if (GetComboLongId(cmbWarehouse) <= 0)
            {
                error = "Please select a receiving warehouse.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtShipTo.Text))
            {
                error = "Ship-To address is required.";
                return false;
            }
            error = null;
            return true;
        }

        private string TryResolveReceivingWarehouseName(string shipTo)
        {
            if (string.IsNullOrWhiteSpace(shipTo)) return "";
            try
            {
                var dt = _warehouseCtrl.GetAllWarehouses();
                if (dt == null) return "";
                string norm = shipTo.Trim();
                foreach (DataRow row in dt.Rows)
                {
                    string addr = row.Table.Columns.Contains("Address") ? row["Address"]?.ToString()?.Trim() : "";
                    if (!string.IsNullOrEmpty(addr) && addr.Equals(norm, StringComparison.OrdinalIgnoreCase))
                        return row["Warehouse Name"]?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private DataTable BuildPurchaseOrderViewFields(DataTable header, long purchaseOrderId)
        {
            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            if (fields == null) return fields;

            try
            {
                RemoveFieldRow(fields, "Remark");
                RemoveFieldRow(fields, "Total Settled");
                if (header != null && header.Rows.Count > 0)
                {
                    var statusRow = fields.Select("Field = 'Status'");
                    foreach (DataRow r in statusRow)
                    {
                        if (int.TryParse(r["Value"]?.ToString(), out int code))
                            r["Value"] = DictionaryService.GetDisplayName(DictionaryService.Categories.PurchaseOrder, code);
                    }
                    fields.AcceptChanges();
                }

                string remark = header != null && header.Rows.Count > 0 && header.Columns.Contains("Remark")
                    ? header.Rows[0]["Remark"]?.ToString()
                    : null;
                string billTo = ExtractPoBillToAddress(remark);
                string shipTo = ExtractPoShipToAddress(remark);
                string cleanRemark = RemovePoAddressLinesFromRemark(remark);
                string warehouseName = TryResolveReceivingWarehouseName(shipTo);

                if (!string.IsNullOrWhiteSpace(warehouseName))
                    fields.Rows.Add("Receiving Warehouse", warehouseName);
                if (!string.IsNullOrWhiteSpace(billTo)) fields.Rows.Add("Bill-To Address", billTo);
                if (!string.IsNullOrWhiteSpace(shipTo)) fields.Rows.Add("Ship-To Address", shipTo);
                if (!string.IsNullOrWhiteSpace(cleanRemark)) fields.Rows.Add("Remark", cleanRemark);

                decimal poTotal = _purchaseOrderCtrl.GetTotalAmount(purchaseOrderId);
                decimal settled = 0;
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Total Settled"))
                    decimal.TryParse(header.Rows[0]["Total Settled"]?.ToString(), out settled);
                else
                    settled = _paymentVoucherCtrl.GetSettledTotalByPurchaseOrder(purchaseOrderId);

                fields.Rows.Add("PO Total Amount", poTotal.ToString("0.00"));
                fields.Rows.Add("Total Settled", settled.ToString("0.00"));
                fields.Rows.Add("Outstanding Balance", (poTotal - settled).ToString("0.00"));

                if (header != null && header.Rows.Count > 0)
                {
                    string buyerFirst = header.Columns.Contains("Buyer First Name") ? header.Rows[0]["Buyer First Name"]?.ToString() : "";
                    string buyerLast = header.Columns.Contains("Buyer Last Name") ? header.Rows[0]["Buyer Last Name"]?.ToString() : "";
                    string supplierName = header.Columns.Contains("Supplier") ? header.Rows[0]["Supplier"]?.ToString() : "";
                    string buyerFull = (buyerFirst + " " + buyerLast).Trim();
                    if (!string.IsNullOrWhiteSpace(buyerFull))
                        fields.Rows.Add("Authorized Signature & Company Chop (Buyer)", buyerFull);
                    if (!string.IsNullOrWhiteSpace(supplierName))
                        fields.Rows.Add("Authorized Signature & Company Chop (Supplier)", supplierName);
                }
            }
            catch { }

            return fields;
        }

        private static void RemoveFieldRow(DataTable fields, string fieldName)
        {
            if (fields == null) return;
            var rows = fields.Select($"Field = '{fieldName.Replace("'", "''")}'");
            foreach (DataRow r in rows)
                fields.Rows.Remove(r);
            fields.AcceptChanges();
        }

        private void ShowPurchaseOrderTableDialog(long id, DataGridViewRow row)
        {
            DataTable header = null;
            DataTable lines = null;
            DataTable paymentLines = null;
            DataTable grnLines = null;
            try { header = _purchaseOrderCtrl.GetHeaderDetail(id); } catch { }
            try { lines = _purchaseOrderCtrl.GetLinesByPurchaseOrder(id); } catch { }
            try { paymentLines = _paymentVoucherCtrl.GetSettlementsByPurchaseOrder(id); } catch { }
            try { grnLines = _grnCtrl.GetGrnsByPurchaseOrder(id); } catch { }

            if (paymentLines != null)
            {
                try
                {
                    foreach (DataRow r in paymentLines.Rows)
                    {
                        if (paymentLines.Columns.Contains("Payment Type") &&
                            int.TryParse(r["Payment Type"]?.ToString(), out int code))
                            r["Payment Type"] = DictionaryService.GetDisplayName(DictionaryService.Categories.PoPaymentType, code);
                    }
                    paymentLines.AcceptChanges();
                }
                catch { }
            }

            if (grnLines != null)
            {
                try
                {
                    foreach (DataRow r in grnLines.Rows)
                    {
                        if (grnLines.Columns.Contains("Status") &&
                            int.TryParse(r["Status"]?.ToString(), out int code))
                            r["Status"] = DictionaryService.GetDisplayName(DictionaryService.Categories.PurchaseOrder, code);
                    }
                    grnLines.AcceptChanges();
                }
                catch { }
            }

            var fields = BuildPurchaseOrderViewFields(header, id);

            var srId = 0L;
            try
            {
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Shortage Report ID"))
                    srId = header.Rows[0]["Shortage Report ID"] == DBNull.Value ? 0 : Convert.ToInt64(header.Rows[0]["Shortage Report ID"]);
            }
            catch { srId = 0; }

            string poCode = row?.Cells["Purchase Order Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(poCode) && header != null && header.Rows.Count > 0 && header.Columns.Contains("Purchase Order Code"))
                poCode = header.Rows[0]["Purchase Order Code"]?.ToString();
            string title = string.IsNullOrWhiteSpace(poCode)
                ? $"Purchase Order Detail — ID: {id}"
                : $"Purchase Order Detail — {poCode}";

            if (srId <= 0)
            {
                DetailViewHelper.ShowDetail(this, title, fields, lines, $"PurchaseOrder_{id}", paymentLines, grnLines,
                    auditDocumentType: DocumentAuditService.Types.PurchaseOrder, auditDocumentId: id);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(920, 660);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var btnShortage = new Button
                {
                    Text = "View Shortage Report",
                    Dock = DockStyle.Bottom,
                    Height = 36,
                    BackColor = Color.White,
                    ForeColor = UITheme.Primary,
                    FlatStyle = FlatStyle.Flat
                };
                btnShortage.FlatAppearance.BorderColor = UITheme.Primary;
                btnShortage.FlatAppearance.BorderSize = 1;
                btnShortage.Click += (s, e) =>
                {
                    DataTable srHeader = null;
                    DataTable srLines = null;
                    try { srHeader = _shortageCtrl.GetHeaderDetail(srId); } catch { }
                    try { srLines = _shortageCtrl.GetLines(srId); } catch { }
                    DetailViewHelper.ShowDetail(this, $"Shortage Report Detail — ID: {srId}",
                        DetailViewHelper.SingleRowToFieldValueTable(srHeader),
                        srLines,
                        $"ShortageReport_{srId}");
                };

                var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 280 };
                var headGrid = GridHelper.CreateStyledGrid();
                headGrid.DataSource = fields;
                GridHelper.StyleGrid(headGrid);
                split.Panel1.Controls.Add(headGrid);

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
                var tabLines = new TabPage("Order Lines");
                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.DataSource = lines;
                GridHelper.StyleGrid(lineGrid);
                tabLines.Controls.Add(lineGrid);
                lineGrid.Dock = DockStyle.Fill;
                tabs.TabPages.Add(tabLines);

                if (paymentLines != null)
                {
                    var tabPay = new TabPage("Payment Vouchers");
                    var payGrid = GridHelper.CreateStyledGrid();
                    payGrid.DataSource = paymentLines;
                    GridHelper.StyleGrid(payGrid);
                    tabPay.Controls.Add(payGrid);
                    payGrid.Dock = DockStyle.Fill;
                    tabs.TabPages.Add(tabPay);
                }
                if (grnLines != null)
                {
                    var tabGrn = new TabPage("Goods Received (GRN)");
                    var grnGrid = GridHelper.CreateStyledGrid();
                    grnGrid.DataSource = grnLines;
                    GridHelper.StyleGrid(grnGrid);
                    tabGrn.Controls.Add(grnGrid);
                    grnGrid.Dock = DockStyle.Fill;
                    tabs.TabPages.Add(tabGrn);
                }
                DocumentAuditService.AppendActivityTab(tabs, DocumentAuditService.Types.PurchaseOrder, id);
                split.Panel2.Controls.Add(tabs);

                dlg.Controls.Add(split);
                dlg.Controls.Add(btnShortage);
                DetailViewHelper.AttachPrintToolbar(dlg, () => DetailViewHelper.FromFieldValueTable(dlg.Text, fields, lines, $"PurchaseOrder_{id}"));
                dlg.ShowDialog(this);
            }
        }

        private void ShowGrnTableDialog(long id, DataGridViewRow row)
        {
            DataTable header = null;
            DataTable lines = null;
            try { header = _grnCtrl.GetHeaderDetail(id); } catch { }
            try { lines = _grnCtrl.GetReceivedLinesDetailed(id); } catch { }
            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            if (fields != null)
            {
                try
                {
                    var statusRows = fields.Select("Field = 'GRN Status' OR Field = 'PO Status'");
                    foreach (DataRow r in statusRows)
                    {
                        if (int.TryParse(r["Value"]?.ToString(), out int code))
                            r["Value"] = DictionaryService.GetDisplayName(DictionaryService.Categories.PurchaseOrder, code);
                    }
                    fields.AcceptChanges();
                }
                catch { }
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"GRN Detail — {row?.Cells["GRN Code"]?.Value ?? id.ToString()}";
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 260
                };
                var headGrid = GridHelper.CreateStyledGrid();
                headGrid.DataSource = fields;
                GridHelper.StyleGrid(headGrid);
                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.DataSource = lines;
                GridHelper.StyleGrid(lineGrid);
                split.Panel1.Controls.Add(headGrid);
                split.Panel2.Controls.Add(lineGrid);

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
                var tabDetail = new TabPage("Detail");
                tabDetail.Controls.Add(split);
                split.Dock = DockStyle.Fill;
                tabs.TabPages.Add(tabDetail);
                tabs.TabPages.Add(DocumentAuditService.BuildActivityTab(DocumentAuditService.Types.GoodsReceivedNote, id));
                dlg.Controls.Add(tabs);
                DetailViewHelper.AttachPrintToolbar(dlg, () =>
                    DetailViewHelper.FromFieldValueTable(dlg.Text, fields, lines, $"GRN_{id}"));
                dlg.ShowDialog(this);
            }
        }

        private void ShowSupplierTableDialog(long supplierId, DataGridViewRow row)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Supplier Detail";
                dlg.Size = new Size(760, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 220 };
                var headGrid = GridHelper.CreateStyledGrid();
                var headDt = new DataTable();
                headDt.Columns.Add("Field");
                headDt.Columns.Add("Value");
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn == null) continue;
                    headDt.Rows.Add(cell.OwningColumn.HeaderText, cell.Value?.ToString() ?? "");
                }
                headGrid.DataSource = headDt;
                GridHelper.StyleGrid(headGrid);

                var quoteGrid = GridHelper.CreateStyledGrid();
                try { quoteGrid.DataSource = _supplierCtrl.GetRawMaterialQuotesBySupplier(supplierId); GridHelper.StyleGrid(quoteGrid); } catch { }
                split.Panel1.Controls.Add(headGrid);
                split.Panel2.Controls.Add(quoteGrid);
                dlg.Controls.Add(split);
                dlg.ShowDialog(this);
            }
        }

        private void ShowPurchaseOrderDetailDialog(long id)
        {
            var po = _purchaseOrderCtrl.GetById(id);
            if (po == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Purchase Order Details / Edit";
                dlg.Size = new Size(920, 700);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 13, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                long originalSupplierId = po.SupplierID;
                bool lockSupplier = false;
                try { lockSupplier = _purchaseOrderCtrl.HasReceiptActivity(id); } catch { }

                var cmbSupplier = BuildSupplierCombo(po.SupplierID);
                if (lockSupplier)
                    cmbSupplier.Enabled = false;
                var txtSupplierBilling = CreateReadOnlyBillingBox();
                var cmbReceivingWarehouse = BuildWarehouseCombo();
                var txtShipTo = new TextBox();
                var txtBillTo = new TextBox();
                var btnSameAsShipTo = UITheme.CreateSecondaryButton("Same as Ship-To");
                var billToPanel = BuildBillToAddressPanel(btnSameAsShipTo, txtBillTo);
                WirePurchaseOrderAddressFields(cmbSupplier, txtSupplierBilling, cmbReceivingWarehouse, txtShipTo, txtBillTo, btnSameAsShipTo);
                DataTable poHeader = null;
                try { poHeader = _purchaseOrderCtrl.GetHeaderDetail(id); } catch { }

                string buyerLabel = "—";
                if (poHeader != null && poHeader.Rows.Count > 0)
                {
                    string fn = poHeader.Columns.Contains("Buyer First Name") ? poHeader.Rows[0]["Buyer First Name"]?.ToString() : "";
                    string ln = poHeader.Columns.Contains("Buyer Last Name") ? poHeader.Rows[0]["Buyer Last Name"]?.ToString() : "";
                    buyerLabel = ($"{fn} {ln}").Trim();
                }
                if (string.IsNullOrWhiteSpace(buyerLabel))
                    buyerLabel = "—";
                var lblStaff = new Label { Text = buyerLabel, AutoSize = true, ForeColor = UITheme.TextDark };

                decimal editPoTotal = 0, editSettled = 0;
                try
                {
                    editPoTotal = _purchaseOrderCtrl.GetTotalAmount(id);
                    if (poHeader != null && poHeader.Rows.Count > 0 && poHeader.Columns.Contains("Total Settled"))
                        decimal.TryParse(poHeader.Rows[0]["Total Settled"]?.ToString(), out editSettled);
                    else
                        editSettled = _paymentVoucherCtrl.GetSettledTotalByPurchaseOrder(id);
                }
                catch { }
                var lblFinancialSummary = new Label
                {
                    AutoSize = false,
                    Height = 44,
                    ForeColor = UITheme.TextGray,
                    Text = $"PO Total: {editPoTotal:0.00}   |   Settled: {editSettled:0.00}   |   Outstanding: {(editPoTotal - editSettled):0.00}\r\nPayments are recorded in Finance → Payment Voucher."
                };
                var dtpDelivery = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = po.RequestDeliveryDate };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Sent", "2 - Received", "3 - Cancelled" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(po.Status, 3));
                string existingRemark = po.Remark ?? "";
                txtShipTo.Text = ExtractPoShipToAddress(existingRemark);
                txtBillTo.Text = ExtractPoBillToAddress(existingRemark);
                TrySelectWarehouseForShipTo(cmbReceivingWarehouse, txtShipTo.Text);
                if (GetComboLongId(cmbReceivingWarehouse) <= 0 && cmbReceivingWarehouse.Items.Count > 0)
                    cmbReceivingWarehouse.SelectedIndex = 0;
                var txtRemark = new TextBox { Text = RemovePoAddressLinesFromRemark(existingRemark), Multiline = true, Height = 48 };

                UITheme.AddFormField(layout, 0, "PO Code", new Label { Text = po.PurchaseOrderCode, AutoSize = true });
                UITheme.AddFormField(layout, 1, "Supplier *", cmbSupplier);
                UITheme.AddFormField(layout, 2, "Billing Address", txtSupplierBilling);
                UITheme.AddFormField(layout, 3, "Receiving Warehouse *", cmbReceivingWarehouse);
                UITheme.AddFormField(layout, 4, "Ship-To Address *", txtShipTo);
                UITheme.AddFormField(layout, 5, "Bill-To Address", billToPanel);
                UITheme.AddFormField(layout, 6, "Buyer (Staff)", lblStaff);
                UITheme.AddFormField(layout, 7, "Request Delivery Date", dtpDelivery);
                UITheme.AddFormField(layout, 8, "Status", cmbStatus);
                UITheme.AddFormField(layout, 9, "Remark", txtRemark);
                UITheme.AddFormField(layout, 10, "Financial Summary", lblFinancialSummary);
                if (lockSupplier)
                {
                    var lblSupplierLock = new Label
                    {
                        AutoSize = true,
                        ForeColor = UITheme.TextGray,
                        Text = "Supplier is locked because this PO has GRN or received quantity."
                    };
                    UITheme.AddFormField(layout, 11, "", lblSupplierLock);
                }

                var lineGrid = CreatePurchaseOrderLineGrid(includeReceivedQty: true);
                lineGrid.UserDeletingRow += (s, e) =>
                {
                    if (e.Row == null || e.Row.IsNewRow) return;
                    decimal received = 0;
                    if (lineGrid.Columns.Contains("ReceivedQty"))
                        decimal.TryParse(e.Row.Cells["ReceivedQty"].Value?.ToString(), out received);
                    if (received > 0)
                    {
                        UITheme.ShowWarning("Cannot delete a line that already has received quantity.");
                        e.Cancel = true;
                    }
                };
                WirePurchaseOrderLineGrid(lineGrid, cmbSupplier, clearLinesOnSupplierChange: false);

                try
                {
                    var internalLines = _purchaseOrderCtrl.GetRawMaterialLinesInternal(id);
                    foreach (DataRow ln in internalLines.Rows)
                    {
                        int idx = lineGrid.Rows.Add();
                        lineGrid.Rows[idx].Cells["RawMaterial"].Value = ln["rawMaterialID"];
                        lineGrid.Rows[idx].Cells["Price"].Value = ln.Table.Columns.Contains("price") ? ln["price"] : 0;
                        lineGrid.Rows[idx].Cells["OrderQty"].Value = ln["orderQuantity"];
                        lineGrid.Rows[idx].Cells["ReceivedQty"].Value = ln["receivedQuantity"];
                    }
                }
                catch { }

                RecalculateAllPurchaseOrderLineDerived(lineGrid);

                var existingRmIds = new System.Collections.Generic.List<long>();
                foreach (DataGridViewRow r in lineGrid.Rows)
                {
                    if (r.IsNewRow) continue;
                    try
                    {
                        long rmId = Convert.ToInt64(r.Cells["RawMaterial"].Value);
                        if (rmId > 0) existingRmIds.Add(rmId);
                    }
                    catch { }
                }
                SetPurchaseOrderMaterialComboSource(lineGrid, BuildQuotedPickerForSupplier(po.SupplierID, existingRmIds));

                MountPurchaseOrderDialogBody(dlg, layout, lineGrid);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    long supplierId = ResolveSupplierId(cmbSupplier);
                    if (supplierId <= 0)
                    {
                        UITheme.ShowWarning("Please select or enter a valid supplier name.");
                        return;
                    }
                    if (lockSupplier && supplierId != originalSupplierId)
                    {
                        UITheme.ShowWarning("Cannot change supplier after goods have been received on this PO.");
                        return;
                    }
                    po.SupplierID = supplierId;
                    po.RequestDeliveryDate = dtpDelivery.Value;
                    po.Status = cmbStatus.SelectedIndex;
                    if (!ValidatePurchaseOrderAddresses(cmbReceivingWarehouse, txtShipTo, out string addrError))
                    {
                        UITheme.ShowWarning(addrError);
                        return;
                    }
                    if (!TryBuildPurchaseOrderRemark(txtBillTo.Text, txtShipTo.Text, txtRemark.Text, out string remark, out string remarkError))
                    {
                        UITheme.ShowWarning(remarkError);
                        return;
                    }
                    po.Remark = remark;
                    if (_purchaseOrderCtrl.Update(po))
                    {
                        try
                        {
                            if (!TryValidatePurchaseOrderLines(lineGrid, cmbSupplier, out string lineError))
                            {
                                UITheme.ShowWarning(lineError);
                                return;
                            }
                            var lines = ReadPurchaseOrderLinesFromGrid(lineGrid, includeReceivedQty: true);
                            if (lines.Count == 0) { UITheme.ShowWarning("Please add at least one raw material line."); return; }
                            _purchaseOrderCtrl.ReplaceLines(id, lines);
                        }
                        catch (Exception ex)
                        {
                            UITheme.ShowWarning("Failed to update PO lines: " + ex.Message);
                            return;
                        }
                        UITheme.ShowSuccess("Purchase order updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[1].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }

        private static Panel BuildBillToAddressPanel(Button btnSameAsShipTo, TextBox txtBillTo)
        {
            var panel = new Panel { Height = 76, MinimumSize = new Size(200, 76) };
            btnSameAsShipTo.Location = new Point(0, 0);
            btnSameAsShipTo.AutoSize = true;
            txtBillTo.Location = new Point(0, 34);
            txtBillTo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtBillTo.Width = 380;
            panel.Controls.Add(btnSameAsShipTo);
            panel.Controls.Add(txtBillTo);
            return panel;
        }

        private static void MountPurchaseOrderDialogBody(Form dlg, TableLayoutPanel headerLayout, DataGridView lineGrid)
        {
            headerLayout.AutoSize = true;
            headerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            headerLayout.Dock = DockStyle.Top;
            var headerHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 0, 4, 0) };
            headerHost.Controls.Add(headerLayout);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = UITheme.Background
            };
            split.Panel1.Controls.Add(headerHost);
            split.Panel2.Controls.Add(lineGrid);
            dlg.Controls.Add(split);

            const int panel1Min = 100;
            const int panel2Min = 120;
            const int preferredHeaderHeight = 300;

            void LayoutSplit()
            {
                int available = split.Height - split.SplitterWidth;
                if (available < panel1Min + panel2Min)
                    return;

                split.Panel1MinSize = panel1Min;
                split.Panel2MinSize = panel2Min;

                int maxDistance = available - panel2Min;
                int distance = Math.Max(panel1Min, Math.Min(preferredHeaderHeight, maxDistance));
                if (split.SplitterDistance != distance)
                    split.SplitterDistance = distance;
            }

            dlg.Load += (s, e) => LayoutSplit();
            dlg.Shown += (s, e) => LayoutSplit();
            split.Resize += (s, e) => LayoutSplit();
        }

        public void OpenPurchaseOrderDetail(long id) => ShowPurchaseOrderDetailDialog(id);

        private void ShowGrnDetailDialog(long id)
        {
            var grn = _grnCtrl.GetById(id);
            if (grn == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Goods Received Details / Edit";
                dlg.Size = new Size(900, 600);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbPO = BuildPurchaseOrderCombo(grn.PurchaseOrderID);
                var lblSupplier = new Label { AutoSize = true, ForeColor = UITheme.TextDark };
                Action refreshSupplierLabel = () =>
                {
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    if (poId <= 0) { lblSupplier.Text = "—"; return; }
                    var po = _purchaseOrderCtrl.GetById(poId);
                    if (po == null) { lblSupplier.Text = "—"; return; }
                    var supplier = _supplierCtrl.GetById(po.SupplierID);
                    lblSupplier.Text = supplier == null
                        ? "—"
                        : $"{supplier.SupplierName} | {supplier.ContactPerson} | {supplier.Phone}";
                };
                refreshSupplierLabel();

                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Received", "2 - Verified" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(grn.Status, 2));
                var txtRemark = new TextBox { Text = grn.Remark ?? "", Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "GRN Code", new Label { Text = grn.GoodsReceivedNoteCode, AutoSize = true });
                UITheme.AddFormField(layout, 1, "Purchase Order *", cmbPO);
                UITheme.AddFormField(layout, 2, "Supplier", lblSupplier);
                UITheme.AddFormField(layout, 3, "Status", cmbStatus);
                UITheme.AddFormField(layout, 4, "Remark", txtRemark);

                var lineGrid = CreateGrnReceiptLineGrid(id, grn.Status, out _);
                lineGrid.Tag = grn.PurchaseOrderID;
                LoadGrnLinesForPurchaseOrder(lineGrid, id, grn.PurchaseOrderID, grn.Status);

                cmbPO.SelectedIndexChanged += (s, e) =>
                {
                    refreshSupplierLabel();
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    lineGrid.Tag = poId;
                    if (poId > 0)
                        LoadGrnLinesForPurchaseOrder(lineGrid, id, poId, grn.Status);
                };

                var linePanel = new Panel { Dock = DockStyle.Fill };
                var lineHint = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 28,
                    Text = "Remaining Need = Order Qty − PO Received Qty − This GRN Received Qty",
                    ForeColor = UITheme.TextGray,
                    Font = new Font("Segoe UI", 8.5f),
                    Padding = new Padding(4, 6, 0, 0)
                };
                linePanel.Controls.Add(lineGrid);
                linePanel.Controls.Add(lineHint);

                root.Controls.Add(layout, 0, 0);
                root.Controls.Add(linePanel, 0, 1);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    if (poId <= 0)
                    {
                        UITheme.ShowWarning("Please select a purchase order.");
                        return;
                    }
                    var po = _purchaseOrderCtrl.GetById(poId);
                    if (po == null)
                    {
                        UITheme.ShowWarning("Purchase order not found.");
                        return;
                    }
                    grn.SupplierID = po.SupplierID;
                    grn.PurchaseOrderID = poId;
                    grn.StaffID = AppSession.CurrentUser?.StaffID ?? grn.StaffID;
                    grn.Status = cmbStatus.SelectedIndex;
                    grn.Remark = txtRemark.Text.Trim();
                    if (_grnCtrl.Update(grn))
                    {
                        try
                        {
                            var lines = ReadGrnLinesFromGrid(lineGrid);
                            if (lines.Count == 0) { UITheme.ShowWarning("Please add at least one raw material line."); return; }
                            _grnCtrl.ReplaceLines(id, lines);
                        }
                        catch (Exception ex)
                        {
                            UITheme.ShowWarning("Failed to update GRN lines: " + ex.Message);
                            return;
                        }
                        UITheme.ShowSuccess("GRN updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    RefreshGrnGrid();
            }
        }

        private void ShowRawMaterialDialog(RawMaterial existing = null)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Raw Material" : "New Raw Material";
                dlg.Size = new Size(480, 420);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode = new TextBox { Text = existing?.RawMaterialCode ?? "" };
                var txtCategory = new TextBox { Text = existing?.Category ?? "" };
                var txtSize = new TextBox { Text = existing?.Size ?? "" };
                var txtColor = new TextBox { Text = existing?.Color ?? "" };
                var txtMinStock = new TextBox { Text = existing?.MinimumStockLevel.ToString() ?? "0" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Inactive", "1 - Active" });
                cmbStatus.SelectedIndex = existing != null ? existing.Status : 1;

                UITheme.AddFormField(layout, 0, "Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category", txtCategory);
                UITheme.AddFormField(layout, 2, "Size", txtSize);
                UITheme.AddFormField(layout, 3, "Color", txtColor);
                UITheme.AddFormField(layout, 4, "Min Stock Level", txtMinStock);
                UITheme.AddFormField(layout, 5, "Status", cmbStatus);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text)) { UITheme.ShowWarning("Code is required."); return; }
                    try
                    {
                        var rm = new RawMaterial
                        {
                            RawMaterialCode = txtCode.Text.Trim(),
                            Category = txtCategory.Text.Trim(),
                            Size = txtSize.Text.Trim(),
                            Color = txtColor.Text.Trim(),
                            MinimumStockLevel = int.TryParse(txtMinStock.Text, out int ms) ? ms : 0,
                            Status = cmbStatus.SelectedIndex
                        };
                        if (isEdit) { rm.RawMaterialID = existing.RawMaterialID; _rawMaterialCtrl.Update(rm); }
                        else _rawMaterialCtrl.Insert(rm);
                        UITheme.ShowSuccess(isEdit ? "Raw Material updated." : "Raw Material created.");
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[0].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterials(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }

        private void ShowPurchaseOrderDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Purchase Order";
                dlg.Size = new Size(920, 700);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 10, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbSupplier = BuildSupplierCombo();
                var txtSupplierBilling = CreateReadOnlyBillingBox();
                var cmbReceivingWarehouse = BuildWarehouseCombo();
                var txtShipTo = new TextBox();
                var txtBillTo = new TextBox();
                var btnSameAsShipTo = UITheme.CreateSecondaryButton("Same as Ship-To");
                var billToPanel = BuildBillToAddressPanel(btnSameAsShipTo, txtBillTo);
                WirePurchaseOrderAddressFields(cmbSupplier, txtSupplierBilling, cmbReceivingWarehouse, txtShipTo, txtBillTo, btnSameAsShipTo);
                if (cmbReceivingWarehouse.Items.Count > 0)
                {
                    cmbReceivingWarehouse.SelectedIndex = 0;
                    ApplyWarehouseToShipTo(cmbReceivingWarehouse, txtShipTo);
                    txtBillTo.Text = txtShipTo.Text;
                }
                var staffDisplay = AppSession.CurrentUser;
                var lblStaff = new Label
                {
                    Text = staffDisplay != null && !string.IsNullOrWhiteSpace(staffDisplay.FullName)
                        ? staffDisplay.FullName
                        : staffDisplay?.Username ?? "Current User",
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var dtpDelivery = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };
                var cmbCurrency = BuildCurrencyCombo(1);
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Sent", "2 - Received", "3 - Cancelled" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox { Multiline = true, Height = 48 };

                UITheme.AddFormField(layout, 0, "Supplier *", cmbSupplier);
                UITheme.AddFormField(layout, 1, "Billing Address", txtSupplierBilling);
                UITheme.AddFormField(layout, 2, "Receiving Warehouse *", cmbReceivingWarehouse);
                UITheme.AddFormField(layout, 3, "Ship-To Address *", txtShipTo);
                UITheme.AddFormField(layout, 4, "Bill-To Address", billToPanel);
                UITheme.AddFormField(layout, 5, "Buyer (Staff)", lblStaff);
                UITheme.AddFormField(layout, 6, "Currency *", cmbCurrency);
                UITheme.AddFormField(layout, 7, "Request Delivery Date *", dtpDelivery);
                UITheme.AddFormField(layout, 8, "Status", cmbStatus);
                UITheme.AddFormField(layout, 9, "Remark", txtRemark);
                layout.RowCount = 10;

                var lineGrid = CreatePurchaseOrderLineGrid();
                WirePurchaseOrderLineGrid(lineGrid, cmbSupplier, clearLinesOnSupplierChange: true);

                MountPurchaseOrderDialogBody(dlg, layout, lineGrid);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    long supplierId = ResolveSupplierId(cmbSupplier);
                    if (supplierId <= 0)
                    {
                        UITheme.ShowWarning("Please select or enter a valid supplier name.");
                        return;
                    }
                    long staffId = AppSession.CurrentUser?.StaffID ?? 0;
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Current user staff profile is required to create a purchase order.");
                        return;
                    }
                    if (!ValidatePurchaseOrderAddresses(cmbReceivingWarehouse, txtShipTo, out string addrError))
                    {
                        UITheme.ShowWarning(addrError);
                        return;
                    }
                    if (!TryBuildPurchaseOrderRemark(txtBillTo.Text, txtShipTo.Text, txtRemark.Text, out string remark, out string remarkError))
                    {
                        UITheme.ShowWarning(remarkError);
                        return;
                    }
                    try
                    {
                        long currencyId = GetComboLongId(cmbCurrency) > 0 ? GetComboLongId(cmbCurrency) : 1;
                        var po = new PurchaseOrder
                        {
                            PurchaseOrderCode = "PO-TEMP",
                            SupplierID = supplierId,
                            StaffID = staffId,
                            WarehouseID = GetComboLongId(cmbReceivingWarehouse),
                            CurrencyID = currencyId,
                            ExchangeRate = _currencyCtrl.LockRateForCurrency(currencyId),
                            RequestDeliveryDate = dtpDelivery.Value,
                            Status = cmbStatus.SelectedIndex,
                            Remark = remark
                        };
                        long id = _purchaseOrderCtrl.Insert(po);
                        if (id <= 0)
                        {
                            UITheme.ShowError("Purchase order creation failed: invalid ID.");
                            return;
                        }
                        _purchaseOrderCtrl.UpdateCodeAfterInsert(id);

                        if (!TryValidatePurchaseOrderLines(lineGrid, cmbSupplier, out string lineError))
                        {
                            UITheme.ShowWarning(lineError);
                            return;
                        }
                        var lines = ReadPurchaseOrderLinesFromGrid(lineGrid);
                        if (lines.Count == 0) { UITheme.ShowWarning("Please add at least one raw material line."); return; }
                        _purchaseOrderCtrl.ReplaceLines(id, lines);

                        UITheme.ShowSuccess($"Purchase Order created (PO-{id}).");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[1].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }

        private ComboBox BuildSupplierCombo(long selectedSupplierId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 360 };
            cmb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmb.AutoCompleteSource = AutoCompleteSource.ListItems;
            var dt = _supplierCtrl.GetAllSuppliers();
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                    row["DisplayText"] = row["Supplier Name"]?.ToString();
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Supplier ID";
            if (selectedSupplierId > 0) SetComboLongValue(cmb, selectedSupplierId);
            return cmb;
        }

        private long ResolveSupplierId(ComboBox cmbSupplier)
        {
            long id = GetComboLongId(cmbSupplier);
            if (id > 0) return id;
            string name = (cmbSupplier.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return 0;
            return _supplierCtrl.FindSupplierIdByName(name);
        }

        private static void RecalculatePurchaseOrderLineDerived(DataGridViewRow row, bool includeRemaining)
        {
            if (row == null || row.IsNewRow) return;
            decimal price = 0, orderQty = 0, receivedQty = 0;
            decimal.TryParse(row.Cells["Price"]?.Value?.ToString(), out price);
            decimal.TryParse(row.Cells["OrderQty"]?.Value?.ToString(), out orderQty);
            if (row.DataGridView != null && row.DataGridView.Columns.Contains("ReceivedQty"))
                decimal.TryParse(row.Cells["ReceivedQty"]?.Value?.ToString(), out receivedQty);

            if (row.DataGridView != null && row.DataGridView.Columns.Contains("LineTotal"))
                row.Cells["LineTotal"].Value = (price * orderQty).ToString("0.00");
            if (includeRemaining && row.DataGridView != null && row.DataGridView.Columns.Contains("Remaining"))
                row.Cells["Remaining"].Value = Math.Max(0, orderQty - receivedQty).ToString("0.##");
        }

        private static void RecalculateAllPurchaseOrderLineDerived(DataGridView lineGrid)
        {
            bool includeRemaining = lineGrid.Columns.Contains("Remaining");
            foreach (DataGridViewRow row in lineGrid.Rows)
                RecalculatePurchaseOrderLineDerived(row, includeRemaining);
        }

        private DataGridView CreatePurchaseOrderLineGrid(bool includeReceivedQty = false)
        {
            var lineGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MinimumSize = new Size(200, 120)
            };

            var rmCol = new DataGridViewComboBoxColumn
            {
                Name = "RawMaterial",
                HeaderText = "Raw Material",
                DataSource = RawMaterialController.BuildEmptyQuotedRawMaterialPickerTable(),
                DisplayMember = "DisplayText",
                ValueMember = "Raw Material ID",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            lineGrid.Columns.Add(rmCol);
            lineGrid.Columns.Add("Price", "Unit Price");
            lineGrid.Columns.Add("OrderQty", "Order Qty");
            if (includeReceivedQty)
            {
                lineGrid.Columns.Add("ReceivedQty", "Received Qty");
                lineGrid.Columns["ReceivedQty"].ReadOnly = true;
            }
            lineGrid.Columns.Add("LineTotal", "Line Total");
            lineGrid.Columns["LineTotal"].ReadOnly = true;
            if (includeReceivedQty)
            {
                lineGrid.Columns.Add("Remaining", "Remaining");
                lineGrid.Columns["Remaining"].ReadOnly = true;
            }
            GridHelper.ApplyStyle(lineGrid);

            lineGrid.EditingControlShowing += (s, e) =>
            {
                if (lineGrid.CurrentCell?.OwningColumn?.Name != "RawMaterial") return;
                if (!(e.Control is ComboBox cb)) return;
                cb.DropDownStyle = ComboBoxStyle.DropDown;
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
            };
            lineGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (lineGrid.IsCurrentCellDirty && lineGrid.CurrentCell is DataGridViewComboBoxCell)
                    lineGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            lineGrid.DataError += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && lineGrid.Columns[e.ColumnIndex].Name == "RawMaterial")
                    e.ThrowException = false;
            };
            return lineGrid;
        }

        private static void SetPurchaseOrderMaterialComboSource(DataGridView lineGrid, DataTable pickerSource)
        {
            if (!(lineGrid.Columns["RawMaterial"] is DataGridViewComboBoxColumn rmCol)) return;
            rmCol.DataSource = pickerSource?.Copy() ?? RawMaterialController.BuildEmptyQuotedRawMaterialPickerTable();
            rmCol.DisplayMember = "DisplayText";
            rmCol.ValueMember = "Raw Material ID";
        }

        private static void ClearPurchaseOrderLineDataRows(DataGridView lineGrid)
        {
            for (int i = lineGrid.Rows.Count - 1; i >= 0; i--)
            {
                if (!lineGrid.Rows[i].IsNewRow)
                    lineGrid.Rows.RemoveAt(i);
            }
        }

        private DataTable BuildQuotedPickerForSupplier(long supplierId, System.Collections.Generic.IEnumerable<long> ensureIncludeMaterialIds = null)
        {
            DataTable picker = supplierId > 0
                ? _rawMaterialCtrl.GetQuotedRawMaterialsForSupplier(supplierId)
                : RawMaterialController.BuildEmptyQuotedRawMaterialPickerTable();
            if (ensureIncludeMaterialIds == null) return picker;

            var included = new System.Collections.Generic.HashSet<long>();
            foreach (DataRow row in picker.Rows)
            {
                if (row["Raw Material ID"] != DBNull.Value)
                    included.Add(Convert.ToInt64(row["Raw Material ID"]));
            }
            foreach (long rmId in ensureIncludeMaterialIds)
            {
                if (rmId <= 0 || included.Contains(rmId)) continue;
                var rm = _rawMaterialCtrl.GetById(rmId);
                if (rm == null) continue;
                var newRow = picker.NewRow();
                newRow["Raw Material ID"] = rmId;
                newRow["Raw Material Code"] = rm.RawMaterialCode ?? "";
                newRow["Quote Price"] = 0m;
                newRow["Unit"] = "";
                newRow["Min Order Qty"] = 1;
                newRow["DisplayText"] = (rm.RawMaterialCode ?? "") + " (legacy line)";
                picker.Rows.Add(newRow);
                included.Add(rmId);
            }
            return picker;
        }

        private void WirePurchaseOrderLineGrid(DataGridView lineGrid, ComboBox cmbSupplier, bool clearLinesOnSupplierChange)
        {
            bool applying = false;
            long lastSupplierId = 0;

            long ResolveSupplier() => ResolveSupplierId(cmbSupplier);

            void ReloadMaterialPicker(bool warnIfEmpty, System.Collections.Generic.IEnumerable<long> ensureIncludeIds = null)
            {
                long supplierId = ResolveSupplier();
                var picker = BuildQuotedPickerForSupplier(supplierId, ensureIncludeIds);
                SetPurchaseOrderMaterialComboSource(lineGrid, picker);
                if (warnIfEmpty && supplierId > 0 && picker.Rows.Count == 0)
                {
                    UITheme.ShowWarning(
                        "This supplier has no active raw material quotes. Please add rows in Raw Material Supplier (supplier material catalog) first.");
                }
            }

            void ApplyRow(int rowIndex, bool warnIfMissing)
            {
                if (rowIndex < 0 || rowIndex >= lineGrid.Rows.Count || lineGrid.Rows[rowIndex].IsNewRow)
                    return;
                long supplierId = ResolveSupplier();
                if (supplierId <= 0)
                {
                    if (warnIfMissing)
                        UITheme.ShowWarning("Please select a supplier before choosing raw materials.");
                    return;
                }
                long rmId = 0;
                try { rmId = Convert.ToInt64(lineGrid.Rows[rowIndex].Cells["RawMaterial"].Value); }
                catch { rmId = 0; }
                if (rmId <= 0) return;

                var quote = _rawMaterialCtrl.TryGetSupplierQuote(rmId, supplierId);
                if (quote == null)
                {
                    lineGrid.Rows[rowIndex].Cells["Price"].Value = DBNull.Value;
                    if (warnIfMissing)
                        UITheme.ShowWarning("Selected item is not in this supplier's active quote list.");
                    return;
                }

                lineGrid.Rows[rowIndex].Cells["Price"].Value = quote.BasePrice;
                var qtyCell = lineGrid.Rows[rowIndex].Cells["OrderQty"];
                if (qtyCell.Value == null || qtyCell.Value == DBNull.Value ||
                    (decimal.TryParse(qtyCell.Value?.ToString(), out decimal q) && q <= 0))
                    qtyCell.Value = quote.MinimumOrderQuantity;
                RecalculatePurchaseOrderLineDerived(lineGrid.Rows[rowIndex], lineGrid.Columns.Contains("Remaining"));
            }

            void RefreshAllLinePrices()
            {
                if (applying) return;
                applying = true;
                try
                {
                    for (int i = 0; i < lineGrid.Rows.Count; i++)
                        ApplyRow(i, warnIfMissing: false);
                }
                finally { applying = false; }
            }

            System.Collections.Generic.List<long> CollectLineMaterialIds()
            {
                var ids = new System.Collections.Generic.List<long>();
                foreach (DataGridViewRow row in lineGrid.Rows)
                {
                    if (row.IsNewRow) continue;
                    try
                    {
                        long rmId = Convert.ToInt64(row.Cells["RawMaterial"].Value);
                        if (rmId > 0) ids.Add(rmId);
                    }
                    catch { }
                }
                return ids;
            }

            void OnSupplierChanged(bool warnIfEmpty)
            {
                long supplierId = ResolveSupplier();
                if (supplierId != lastSupplierId)
                {
                    var keepIds = !clearLinesOnSupplierChange ? CollectLineMaterialIds() : null;
                    if (clearLinesOnSupplierChange && lastSupplierId > 0)
                        ClearPurchaseOrderLineDataRows(lineGrid);
                    lastSupplierId = supplierId;
                    ReloadMaterialPicker(warnIfEmpty, keepIds);
                }
                else if (supplierId > 0)
                {
                    ReloadMaterialPicker(false, clearLinesOnSupplierChange ? null : CollectLineMaterialIds());
                }
                RefreshAllLinePrices();
            }

            lineGrid.CellValueChanged += (s, e) =>
            {
                if (applying || e.RowIndex < 0) return;
                string colName = lineGrid.Columns[e.ColumnIndex].Name;
                if (colName == "Price" || colName == "OrderQty")
                {
                    RecalculatePurchaseOrderLineDerived(lineGrid.Rows[e.RowIndex], lineGrid.Columns.Contains("Remaining"));
                    return;
                }
                if (colName != "RawMaterial") return;
                applying = true;
                try { ApplyRow(e.RowIndex, warnIfMissing: true); }
                finally { applying = false; }
            };

            cmbSupplier.SelectedIndexChanged += (s, e) => OnSupplierChanged(warnIfEmpty: true);
            cmbSupplier.Leave += (s, e) =>
            {
                long supplierId = ResolveSupplier();
                if (supplierId != lastSupplierId)
                    OnSupplierChanged(warnIfEmpty: true);
            };

            lastSupplierId = ResolveSupplier();
            if (lastSupplierId > 0)
                ReloadMaterialPicker(warnIfEmpty: false);
        }

        private bool TryValidatePurchaseOrderLines(DataGridView lineGrid, ComboBox cmbSupplier, out string error)
        {
            error = null;
            long supplierId = ResolveSupplierId(cmbSupplier);
            if (supplierId <= 0)
            {
                error = "Please select a valid supplier.";
                return false;
            }

            var seen = new System.Collections.Generic.HashSet<long>();
            foreach (DataGridViewRow r in lineGrid.Rows)
            {
                if (r.IsNewRow) continue;
                long rmId = 0;
                try { rmId = Convert.ToInt64(r.Cells["RawMaterial"].Value); } catch { rmId = 0; }
                if (rmId <= 0) continue;
                if (!seen.Add(rmId))
                {
                    error = "Each raw material can only appear once on a purchase order.";
                    return false;
                }
                decimal orderQty = 0, receivedOnLine = 0;
                decimal.TryParse(r.Cells["OrderQty"].Value?.ToString(), out orderQty);
                if (lineGrid.Columns.Contains("ReceivedQty"))
                    decimal.TryParse(r.Cells["ReceivedQty"].Value?.ToString(), out receivedOnLine);
                if (receivedOnLine > 0 && orderQty < receivedOnLine)
                {
                    error = "Order quantity cannot be less than received quantity on a line.";
                    return false;
                }
                if (_rawMaterialCtrl.TryGetSupplierQuote(rmId, supplierId) == null)
                {
                    decimal received = 0;
                    if (lineGrid.Columns.Contains("ReceivedQty"))
                        decimal.TryParse(r.Cells["ReceivedQty"].Value?.ToString(), out received);
                    if (received > 0)
                        continue;
                    error = "All line items must have an active quote in Raw Material Supplier for the selected supplier.";
                    return false;
                }
            }
            return true;
        }

        private static System.Collections.Generic.List<(long RawMaterialID, decimal Price, decimal OrderQty, decimal ReceivedQty)> ReadPurchaseOrderLinesFromGrid(
            DataGridView lineGrid, bool includeReceivedQty = false)
        {
            var lines = new System.Collections.Generic.List<(long, decimal, decimal, decimal)>();
            foreach (DataGridViewRow r in lineGrid.Rows)
            {
                if (r.IsNewRow) continue;
                long rmId = 0;
                try { rmId = Convert.ToInt64(r.Cells["RawMaterial"].Value); } catch { rmId = 0; }
                if (rmId <= 0) continue;
                decimal.TryParse(r.Cells["Price"].Value?.ToString(), out decimal price);
                decimal.TryParse(r.Cells["OrderQty"].Value?.ToString(), out decimal qty);
                decimal received = 0;
                if (includeReceivedQty && lineGrid.Columns.Contains("ReceivedQty"))
                    decimal.TryParse(r.Cells["ReceivedQty"].Value?.ToString(), out received);
                if (qty <= 0) continue;
                lines.Add((rmId, price, qty, received));
            }
            return lines;
        }

        private void ShowGrnDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Goods Received Note";
                dlg.Size = new Size(920, 600);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(12) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

                var cmbPO = BuildPurchaseOrderCombo(allowTypeToSearch: true);
                var lblSupplier = new Label { AutoSize = true, ForeColor = UITheme.TextDark, Text = "—" };
                var txtRemark = new TextBox { Multiline = true, Dock = DockStyle.Fill };

                Action refreshSupplier = () =>
                {
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    if (poId <= 0) { lblSupplier.Text = "—"; return; }
                    var po = _purchaseOrderCtrl.GetById(poId);
                    if (po == null) { lblSupplier.Text = "—"; return; }
                    var supplier = _supplierCtrl.GetById(po.SupplierID);
                    lblSupplier.Text = supplier == null
                        ? "—"
                        : $"{supplier.SupplierName} | {supplier.ContactPerson} | {supplier.Phone}";
                };

                var lineGrid = CreateGrnReceiptLineGrid(0, 0, out _);

                Action onPurchaseOrderChanged = () =>
                {
                    refreshSupplier();
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    if (poId > 0)
                        LoadPoLinesForNewGrn(lineGrid, poId);
                };

                cmbPO.SelectedIndexChanged += (s, e) => onPurchaseOrderChanged();
                cmbPO.SelectionChangeCommitted += (s, e) => onPurchaseOrderChanged();
                cmbPO.Leave += (s, e) => onPurchaseOrderChanged();

                UITheme.AddFormField(layout, 0, "Purchase Order *", cmbPO);
                UITheme.AddFormField(layout, 1, "Supplier", lblSupplier);
                UITheme.AddFormField(layout, 2, "Remark", txtRemark);
                root.Controls.Add(layout, 0, 0);

                var linePanel = new Panel { Dock = DockStyle.Fill };
                linePanel.Controls.Add(lineGrid);
                linePanel.Controls.Add(new Label
                {
                    Dock = DockStyle.Top,
                    Height = 28,
                    Text = "Select PO, then enter this GRN received qty per line. Remaining Need updates automatically.",
                    ForeColor = UITheme.TextGray,
                    Font = new Font("Segoe UI", 8.5f),
                    Padding = new Padding(4, 6, 0, 0)
                });
                root.Controls.Add(linePanel, 0, 1);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    long poId = ResolvePurchaseOrderId(cmbPO);
                    if (poId <= 0)
                    {
                        UITheme.ShowWarning("Please select a purchase order.");
                        return;
                    }
                    var po = _purchaseOrderCtrl.GetById(poId);
                    if (po == null)
                    {
                        UITheme.ShowWarning("Purchase order not found.");
                        return;
                    }
                    var lines = ReadGrnLinesFromGrid(lineGrid);
                    if (lines.Count == 0)
                    {
                        UITheme.ShowWarning("Please enter at least one received quantity.");
                        return;
                    }
                    try
                    {
                        var grn = new GoodsReceivedNote
                        {
                            GoodsReceivedNoteCode = "GRN-TEMP",
                            SupplierID = po.SupplierID,
                            PurchaseOrderID = poId,
                            StaffID = AppSession.CurrentUser?.StaffID ?? 1,
                            Status = 0,
                            Remark = txtRemark.Text.Trim()
                        };
                        long id = _grnCtrl.Insert(grn);
                        if (id <= 0)
                        {
                            UITheme.ShowError("GRN creation failed: invalid ID.");
                            return;
                        }
                        _grnCtrl.UpdateCodeAfterInsert(id);
                        _grnCtrl.ReplaceLines(id, lines);
                        UITheme.ShowSuccess("GRN " + DocumentCodeHelper.Build("GRN", id) + " created.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);

                if (cmbPO.Items.Count > 0 && cmbPO.SelectedIndex < 0)
                    cmbPO.SelectedIndex = 0;
                refreshSupplier();
                long initialPoId = ResolvePurchaseOrderId(cmbPO);
                if (initialPoId > 0)
                    LoadPoLinesForNewGrn(lineGrid, initialPoId);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    RefreshGrnGrid();
            }
        }

        private void ShowConfirmGrnDialog(long grnId, Action onSuccess)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Confirm Goods Receipt";
                dlg.Size = new Size(480, 200);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbWarehouse = BuildWarehouseCombo();
                UITheme.AddFormField(layout, 0, "Warehouse *", cmbWarehouse);

                var btnConfirm = UITheme.CreatePrimaryButton("Confirm");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnConfirm.Click += (s, e) =>
                {
                    long warehouseId = GetComboLongId(cmbWarehouse);
                    if (warehouseId <= 0)
                    {
                        UITheme.ShowWarning("Please select a warehouse.");
                        return;
                    }
                    var result = _inventoryWorkflow.ConfirmGoodsReceived(grnId, warehouseId);
                    if (result.Success)
                    {
                        UITheme.ShowSuccess(result.Message);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        onSuccess?.Invoke();
                    }
                    else UITheme.ShowWarning(result.Message);
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnConfirm);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private ComboBox BuildWarehouseCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
            var dt = _warehouseCtrl.GetAllWarehouses();
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string name = row["Warehouse Name"]?.ToString();
                    string addr = row["Address"]?.ToString();
                    row["DisplayText"] = string.IsNullOrWhiteSpace(addr) ? name : $"{name} — {addr}";
                }
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Warehouse ID";
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            return cmb;
        }

        private DataGridView CreateGrnReceiptLineGrid(long grnId, int grnStatus, out DataGridViewComboBoxColumn rmComboColumn)
        {
            var lineGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = grnId > 0,
                AllowUserToDeleteRows = grnId > 0,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            rmComboColumn = null;
            if (grnId > 0)
            {
                var rms = _rawMaterialCtrl.GetAllRawMaterials();
                if (!rms.Columns.Contains("DisplayText"))
                    rms.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow r in rms.Rows)
                    r["DisplayText"] = r["Raw Material Code"]?.ToString();

                rmComboColumn = new DataGridViewComboBoxColumn
                {
                    Name = "RawMaterialID",
                    HeaderText = "Raw Material",
                    DataSource = rms,
                    DisplayMember = "DisplayText",
                    ValueMember = "Raw Material ID",
                    FlatStyle = FlatStyle.Flat
                };
                lineGrid.Columns.Add(rmComboColumn);
            }
            else
            {
                lineGrid.Columns.Add("RawMaterialCode", "Raw Material");
                lineGrid.Columns["RawMaterialCode"].ReadOnly = true;
                lineGrid.Columns.Add("RawMaterialID", "RawMaterialID");
                lineGrid.Columns["RawMaterialID"].Visible = false;
            }

            var orderCol = lineGrid.Columns.Add("OrderQty", "Order Qty");
            var poReceivedCol = lineGrid.Columns.Add("PoReceivedQty", "PO Received Qty");
            lineGrid.Columns.Add("ReceivedQty", "Received Qty");
            var remainCol = lineGrid.Columns.Add("RemainingNeed", "Remaining Need");
            lineGrid.Columns.Add("OriginalGrnQty", "OriginalGrnQty");
            lineGrid.Columns[orderCol].ReadOnly = true;
            lineGrid.Columns[poReceivedCol].ReadOnly = true;
            lineGrid.Columns[remainCol].ReadOnly = true;
            lineGrid.Columns["OriginalGrnQty"].Visible = false;
            GridHelper.ApplyStyle(lineGrid);

            lineGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string col = lineGrid.Columns[e.ColumnIndex].Name;
                if (col == "ReceivedQty" || col == "RawMaterialID")
                {
                    long poId = lineGrid.Tag is long taggedPo ? taggedPo : 0;
                    RecalculateGrnLineRemaining(lineGrid.Rows[e.RowIndex], poId, grnStatus);
                }
            };
            lineGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (lineGrid.IsCurrentCellDirty && lineGrid.CurrentCell is DataGridViewComboBoxCell)
                    lineGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            return lineGrid;
        }

        private void LoadPoLinesForNewGrn(DataGridView lineGrid, long purchaseOrderId)
        {
            lineGrid.Tag = purchaseOrderId;
            lineGrid.Rows.Clear();
            if (purchaseOrderId <= 0) return;

            var rmCodes = new System.Collections.Generic.Dictionary<long, string>();
            try
            {
                var rms = _rawMaterialCtrl.GetAllRawMaterials();
                if (rms != null)
                {
                    foreach (DataRow r in rms.Rows)
                        rmCodes[Convert.ToInt64(r["Raw Material ID"])] = r["Raw Material Code"]?.ToString();
                }
            }
            catch { }

            foreach (var entry in LoadPoLineMap(purchaseOrderId))
            {
                long rmId = entry.Key;
                decimal orderQty = entry.Value.OrderQty;
                decimal poReceived = entry.Value.PoReceivedQty;
                decimal suggested = Math.Max(0, orderQty - poReceived);

                int idx = lineGrid.Rows.Add();
                var row = lineGrid.Rows[idx];
                if (lineGrid.Columns.Contains("RawMaterialCode"))
                    row.Cells["RawMaterialCode"].Value = rmCodes.ContainsKey(rmId) ? rmCodes[rmId] : rmId.ToString();
                row.Cells["RawMaterialID"].Value = rmId;
                row.Cells["OrderQty"].Value = orderQty;
                row.Cells["PoReceivedQty"].Value = poReceived;
                row.Cells["ReceivedQty"].Value = suggested;
                row.Cells["OriginalGrnQty"].Value = 0m;
                RecalculateGrnLineRemaining(row, purchaseOrderId, 0);
            }
        }

        private static System.Collections.Generic.List<(long RawMaterialID, decimal ReceivedQty)> ReadGrnLinesFromGrid(DataGridView lineGrid)
        {
            var lines = new System.Collections.Generic.List<(long, decimal)>();
            foreach (DataGridViewRow row in lineGrid.Rows)
            {
                if (row.IsNewRow) continue;
                long rmId = 0;
                try { rmId = Convert.ToInt64(row.Cells["RawMaterialID"].Value); } catch { rmId = 0; }
                if (rmId <= 0) continue;
                decimal.TryParse(row.Cells["ReceivedQty"].Value?.ToString(), out decimal qty);
                if (qty <= 0) continue;
                lines.Add((rmId, qty));
            }
            return lines;
        }

        private void RefreshGrnGrid()
        {
            var grid = _tabs?.TabPages.Cast<TabPage>()
                .FirstOrDefault(p => p.Text.Contains("Goods Received"))
                ?.Controls.OfType<DataGridView>().FirstOrDefault();
            if (grid == null) return;
            try
            {
                ReloadGrnGrid(grid, _grnCtrl);
            }
            catch { }
        }

        private ComboBox BuildPurchaseOrderCombo(long selectedPoId = 0, bool allowTypeToSearch = false)
        {
            var cmb = new ComboBox { Width = 360 };
            if (allowTypeToSearch)
            {
                cmb.DropDownStyle = ComboBoxStyle.DropDown;
                cmb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cmb.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            else
            {
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            var dt = _purchaseOrderCtrl.GetPurchaseOrdersForPicker();
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string code = row["Purchase Order Code"]?.ToString();
                    string supplier = row["Supplier"]?.ToString();
                    row["DisplayText"] = string.IsNullOrWhiteSpace(supplier) ? code : $"{code} — {supplier}";
                }
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Purchase Order ID";
            if (selectedPoId > 0) SetComboLongValue(cmb, selectedPoId);
            return cmb;
        }

        private ComboBox BuildCurrencyCombo(long selectedCurrencyId = 1)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dt = _currencyCtrl.GetAllForCombo();
            cmb.DataSource = dt;
            cmb.DisplayMember = "Code";
            cmb.ValueMember = "Currency ID";
            if (selectedCurrencyId > 0) SetComboLongValue(cmb, selectedCurrencyId);
            return cmb;
        }

        private static long GetComboLongId(ComboBox cmb, string valueMember = null)
        {
            if (cmb == null) return 0;

            string member = valueMember;
            if (string.IsNullOrEmpty(member) && !string.IsNullOrEmpty(cmb.ValueMember))
                member = cmb.ValueMember;

            object selected = cmb.SelectedValue;
            if (selected != null && selected != DBNull.Value)
            {
                if (selected is long longVal) return longVal;
                if (selected is int intVal) return intVal;
                if (long.TryParse(selected.ToString(), out long parsed)) return parsed;
            }

            if (cmb.SelectedItem is DataRowView rowView && !string.IsNullOrEmpty(member)
                && rowView.Row.Table.Columns.Contains(member))
            {
                object val = rowView[member];
                if (val != null && val != DBNull.Value && long.TryParse(val.ToString(), out long id))
                    return id;
            }

            return 0;
        }

        private long ResolvePurchaseOrderId(ComboBox cmb)
        {
            long id = GetComboLongId(cmb);
            if (id > 0) return id;

            string text = (cmb?.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text)) return 0;

            int dash = text.IndexOf('—');
            string codePart = dash > 0 ? text.Substring(0, dash).Trim() : text;
            var po = _purchaseOrderCtrl.GetByCode(codePart);
            return po?.PurchaseOrderID ?? 0;
        }

        private static void SetComboLongValue(ComboBox cmb, long value)
        {
            try { cmb.SelectedValue = value; }
            catch { }
        }

        private System.Collections.Generic.Dictionary<long, (decimal OrderQty, decimal PoReceivedQty)> LoadPoLineMap(long purchaseOrderId)
        {
            var map = new System.Collections.Generic.Dictionary<long, (decimal, decimal)>();
            if (purchaseOrderId <= 0) return map;
            try
            {
                var dt = _purchaseOrderCtrl.GetRawMaterialLinesInternal(purchaseOrderId);
                if (dt == null) return map;
                foreach (DataRow row in dt.Rows)
                {
                    long rmId = Convert.ToInt64(row["rawMaterialID"]);
                    decimal orderQty = Convert.ToDecimal(row["orderQuantity"]);
                    decimal poReceived = row["receivedQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["receivedQuantity"]);
                    map[rmId] = (orderQty, poReceived);
                }
            }
            catch { }
            return map;
        }

        private void LoadGrnLinesForPurchaseOrder(DataGridView lineGrid, long grnId, long purchaseOrderId, int grnStatus)
        {
            lineGrid.Tag = purchaseOrderId;
            lineGrid.Rows.Clear();
            if (purchaseOrderId <= 0) return;

            var poMap = LoadPoLineMap(purchaseOrderId);
            var grnLines = new System.Collections.Generic.Dictionary<long, decimal>();
            try
            {
                var dt = _grnCtrl.GetRawMaterialLinesInternal(grnId);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        long rmId = Convert.ToInt64(row["rawMaterialID"]);
                        decimal qty = Convert.ToDecimal(row["receivedQuantity"]);
                        grnLines[rmId] = qty;
                    }
                }
            }
            catch { }

            foreach (var entry in poMap)
            {
                long rmId = entry.Key;
                decimal orderQty = entry.Value.OrderQty;
                decimal poReceived = entry.Value.PoReceivedQty;
                decimal grnReceived = grnLines.ContainsKey(rmId) ? grnLines[rmId] : 0;
                int idx = lineGrid.Rows.Add();
                var gridRow = lineGrid.Rows[idx];
                gridRow.Cells["RawMaterialID"].Value = rmId;
                gridRow.Cells["OrderQty"].Value = orderQty;
                gridRow.Cells["PoReceivedQty"].Value = poReceived;
                gridRow.Cells["ReceivedQty"].Value = grnReceived;
                gridRow.Cells["OriginalGrnQty"].Value = grnReceived;
                RecalculateGrnLineRemaining(gridRow, purchaseOrderId, grnStatus);
            }

            foreach (var extra in grnLines)
            {
                if (poMap.ContainsKey(extra.Key)) continue;
                int idx = lineGrid.Rows.Add();
                var gridRow = lineGrid.Rows[idx];
                gridRow.Cells["RawMaterialID"].Value = extra.Key;
                gridRow.Cells["OrderQty"].Value = 0m;
                gridRow.Cells["PoReceivedQty"].Value = 0m;
                gridRow.Cells["ReceivedQty"].Value = extra.Value;
                gridRow.Cells["OriginalGrnQty"].Value = extra.Value;
                RecalculateGrnLineRemaining(gridRow, purchaseOrderId, grnStatus);
            }
        }

        private void RecalculateGrnLineRemaining(DataGridViewRow row, long purchaseOrderId, int grnStatus)
        {
            if (row == null || row.IsNewRow) return;
            long rmId = 0;
            try { rmId = Convert.ToInt64(row.Cells["RawMaterialID"].Value); } catch { rmId = 0; }

            decimal orderQty = 0, poReceived = 0, grnReceived = 0, originalGrn = 0;
            decimal.TryParse(row.Cells["OrderQty"].Value?.ToString(), out orderQty);
            decimal.TryParse(row.Cells["PoReceivedQty"].Value?.ToString(), out poReceived);
            decimal.TryParse(row.Cells["ReceivedQty"].Value?.ToString(), out grnReceived);
            decimal.TryParse(row.Cells["OriginalGrnQty"].Value?.ToString(), out originalGrn);

            if (rmId > 0 && purchaseOrderId > 0)
            {
                var map = LoadPoLineMap(purchaseOrderId);
                if (map.TryGetValue(rmId, out var info))
                {
                    orderQty = info.OrderQty;
                    poReceived = info.PoReceivedQty;
                    row.Cells["OrderQty"].Value = orderQty;
                    row.Cells["PoReceivedQty"].Value = poReceived;
                }
            }

            // Draft: PO received does not include this GRN yet.
            // Confirmed: PO received already includes original GRN qty — adjust so edits recalc correctly.
            decimal countedPoReceived = poReceived;
            if (grnStatus >= 2)
                countedPoReceived = Math.Max(0, poReceived - originalGrn);

            decimal remaining = orderQty - countedPoReceived - grnReceived;
            if (remaining < 0) remaining = 0;
            row.Cells["RemainingNeed"].Value = remaining;
        }

        private void ShowSupplierDialog(Supplier existing = null)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Supplier" : "New Supplier";
                dlg.Size = new Size(480, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox { Text = existing?.SupplierName ?? "" };
                var txtContact = new TextBox { Text = existing?.ContactPerson ?? "" };
                var txtPhone = new TextBox { Text = existing?.Phone ?? "" };
                var txtEmail = new TextBox { Text = existing?.Email ?? "" };
                var txtAddress = new TextBox { Text = existing?.BillingAddress ?? "" };
                var cmbTerm = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                DictionaryUIHelper.BindPaymentTermCombo(cmbTerm, existing?.PaymentTerm);
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Inactive", "1 - Active" });
                cmbStatus.SelectedIndex = existing != null ? existing.Status : 1;

                UITheme.AddFormField(layout, 0, "Supplier Name *", txtName);
                UITheme.AddFormField(layout, 1, "Contact Person", txtContact);
                UITheme.AddFormField(layout, 2, "Phone", txtPhone);
                UITheme.AddFormField(layout, 3, "Email", txtEmail);
                UITheme.AddFormField(layout, 4, "Billing Address", txtAddress);
                UITheme.AddFormField(layout, 5, "Payment Term", cmbTerm);
                UITheme.AddFormField(layout, 6, "Status", cmbStatus);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { UITheme.ShowWarning("Supplier Name is required."); return; }
                    try
                    {
                        var sup = new Supplier
                        {
                            SupplierName = txtName.Text.Trim(),
                            ContactPerson = txtContact.Text.Trim(),
                            Phone = txtPhone.Text.Trim(),
                            Email = txtEmail.Text.Trim(),
                            BillingAddress = txtAddress.Text.Trim(),
                            PaymentTerm = DictionaryUIHelper.GetSelectedPaymentTerm(cmbTerm),
                            Status = cmbStatus.SelectedIndex
                        };
                        if (isEdit) { sup.SupplierID = existing.SupplierID; _supplierCtrl.Update(sup); }
                        else _supplierCtrl.Insert(sup);
                        UITheme.ShowSuccess(isEdit ? "Supplier updated." : "Supplier created.");
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[3].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _supplierCtrl.GetAllSuppliers(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }
    }
}
