using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class WarehousePanel : UserControl
    {
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private DataGridView _grid;

        private TabControl _tabs;
        private DataGridView _deliveryGrid;
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly InventoryWorkflowService _inventoryWorkflow = new InventoryWorkflowService();
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();

        private static readonly string[] DefaultShipMethods =
        {
            "Courier", "Company Truck", "Customer Pickup", "Sea Freight", "Air Freight", "Express"
        };
        private TextBox _warehouseSearchBox;
        private ComboBox _warehouseStatusFilter;
        private TextBox _deliverySearchBox;
        private ComboBox _deliveryStatusFilter;

        public WarehousePanel(string module = "Warehouse")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildTabUI();
            if (module == "Delivery Notes" && _tabs != null) _tabs.SelectedIndex = 1;
        }

        private void BuildTabUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            // Warehouses Tab
            var warehouseTab = new TabPage("🏭 Warehouses") { BackColor = UITheme.Background };

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.Background };
            var btnNew = UITheme.CreatePrimaryButton("+ New Warehouse");
            btnNew.Location = new Point(0, 8);
            btnNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.Warehouse, PermissionAction.Create, this)) ShowCreateDialog(); };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.Warehouse);
            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Width + 10, 8);
            btnRefresh.Click += (s, e) => LoadData();
            var btnDetailWarehouse = UITheme.CreateSecondaryButton("View Detail");
            btnDetailWarehouse.Location = new Point(btnRefresh.Right + 10, 8);
            btnDetailWarehouse.Click += (s, e) =>
            {
                if (_grid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a warehouse first."); return; }
                ShowWarehouseTableDialog(_grid.CurrentRow);
            };
            var btnEditWarehouse = UITheme.CreateSecondaryButton("Edit");
            btnEditWarehouse.Location = new Point(btnDetailWarehouse.Right + 10, 8);
            btnEditWarehouse.Click += (s, e) =>
            {
                if (_grid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a warehouse first."); return; }
                if (!PermissionGuard.Ensure(PermissionModule.Warehouse, PermissionAction.Edit, this)) return;
                ShowEditDialog(Convert.ToInt64(_grid.CurrentRow.Cells[0].Value));
            };
            PermissionGuard.ApplyEditButton(btnEditWarehouse, PermissionModule.Warehouse);

            _warehouseSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnEditWarehouse.Right + 10, 10) };
            _warehouseSearchBox.TextChanged += (s, e) => LoadData();
            _warehouseStatusFilter = new ComboBox
            {
                Width = 140,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(_warehouseSearchBox.Right + 10, 10)
            };
            _warehouseStatusFilter.Items.AddRange(new object[] { "All Status", "Inactive", "Active" });
            _warehouseStatusFilter.SelectedIndex = 0;
            _warehouseStatusFilter.SelectedIndexChanged += (s, e) => LoadData();
            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetailWarehouse);
            toolbar.Controls.Add(btnEditWarehouse);
            toolbar.Controls.Add(_warehouseSearchBox);
            toolbar.Controls.Add(_warehouseStatusFilter);

            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            var warehouseContent = new Panel { Dock = DockStyle.Fill };
            warehouseContent.Controls.Add(_grid);
            warehouseContent.Controls.Add(FilterBlockHelper.CreateFilterBlock(_grid, "Warehouse Filters"));
            warehouseContent.Controls.Add(toolbar);
            warehouseTab.Controls.Add(warehouseContent);

            // Delivery Notes Tab
            var deliveryTab = new TabPage("🚚 Delivery Notes") { BackColor = UITheme.Background };

            var deliveryToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.Background };
            var btnNewDelivery = UITheme.CreatePrimaryButton("+ New Delivery Note");
            btnNewDelivery.Location = new Point(0, 8);
            btnNewDelivery.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.DeliveryNote, PermissionAction.Create, this)) ShowCreateDeliveryDialog(); };
            PermissionGuard.ApplyCreateButton(btnNewDelivery, PermissionModule.DeliveryNote);
            var btnRefreshDelivery = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefreshDelivery.Location = new Point(btnNewDelivery.Width + 10, 8);
            btnRefreshDelivery.Click += (s, e) => LoadDeliveryNotes();
            var btnDetailDelivery = UITheme.CreateSecondaryButton("View Detail");
            btnDetailDelivery.Location = new Point(btnRefreshDelivery.Right + 10, 8);
            btnDetailDelivery.Click += (s, e) =>
            {
                if (_deliveryGrid?.CurrentRow == null)
                {
                    UITheme.ShowWarning("Please select a delivery note first.");
                    return;
                }
                ShowDeliveryTableDialog(_deliveryGrid.CurrentRow);
            };
            var btnConfirmDelivery = UITheme.CreateSecondaryButton("Confirm Delivery");
            btnConfirmDelivery.Location = new Point(btnDetailDelivery.Right + 10, 8);
            btnConfirmDelivery.Click += (s, e) => ConfirmSelectedDelivery();
            PermissionGuard.ApplyEditButton(btnConfirmDelivery, PermissionModule.DeliveryNote);
            var btnEditDelivery = UITheme.CreateSecondaryButton("Edit");
            btnEditDelivery.Location = new Point(btnConfirmDelivery.Right + 10, 8);
            btnEditDelivery.Click += (s, e) =>
            {
                if (_deliveryGrid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a delivery note first."); return; }
                if (!PermissionGuard.Ensure(PermissionModule.DeliveryNote, PermissionAction.Edit, this)) return;
                OpenDeliveryEditDialog(Convert.ToInt64(_deliveryGrid.CurrentRow.Cells[0].Value));
            };
            PermissionGuard.ApplyEditButton(btnEditDelivery, PermissionModule.DeliveryNote);
            _deliverySearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnEditDelivery.Right + 10, 10) };
            _deliverySearchBox.TextChanged += (s, e) => LoadDeliveryNotes();
            _deliveryStatusFilter = new ComboBox
            {
                Width = 140,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(_deliverySearchBox.Right + 10, 10)
            };
            DictionaryUIHelper.BindStatusFilter(_deliveryStatusFilter, DictionaryService.Categories.Delivery);
            _deliveryStatusFilter.SelectedIndexChanged += (s, e) => LoadDeliveryNotes();
            deliveryToolbar.Controls.Add(btnNewDelivery);
            deliveryToolbar.Controls.Add(btnRefreshDelivery);
            deliveryToolbar.Controls.Add(btnDetailDelivery);
            deliveryToolbar.Controls.Add(btnConfirmDelivery);
            deliveryToolbar.Controls.Add(btnEditDelivery);
            deliveryToolbar.Controls.Add(_deliverySearchBox);
            deliveryToolbar.Controls.Add(_deliveryStatusFilter);

            _deliveryGrid = GridHelper.CreateStyledGrid();
            _deliveryGrid.CellDoubleClick += DeliveryGrid_CellDoubleClick;

            var deliveryContent = new Panel { Dock = DockStyle.Fill };
            deliveryContent.Controls.Add(_deliveryGrid);
            deliveryContent.Controls.Add(FilterBlockHelper.CreateFilterBlock(_deliveryGrid, "Delivery Note Filters"));
            deliveryContent.Controls.Add(deliveryToolbar);
            deliveryTab.Controls.Add(deliveryContent);

            if (AppSession.CanView(PermissionModule.Warehouse))
                _tabs.TabPages.Add(warehouseTab);
            if (AppSession.CanView(PermissionModule.DeliveryNote))
                _tabs.TabPages.Add(deliveryTab);

            Controls.Add(_tabs);

            if (AppSession.CanView(PermissionModule.Warehouse))
                LoadData();
            if (AppSession.CanView(PermissionModule.DeliveryNote))
                LoadDeliveryNotes();
        }

        private void LoadData()
        {
            try
            {
                var dt = _warehouseCtrl.GetAllWarehouses();
                if (dt == null) return;

                string keyword = _warehouseSearchBox?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string escaped = keyword.Replace("'", "''");
                    dt.DefaultView.RowFilter = $"[Warehouse Name] LIKE '%{escaped}%' OR [Address] LIKE '%{escaped}%'";
                    dt = dt.DefaultView.ToTable();
                }

                if (_warehouseStatusFilter != null && _warehouseStatusFilter.SelectedIndex > 0 && dt.Columns.Contains("Status"))
                {
                    int status = _warehouseStatusFilter.SelectedIndex - 1;
                    dt.DefaultView.RowFilter = "[Status] = " + status;
                    dt = dt.DefaultView.ToTable();
                }

                _grid.DataSource = dt;
                GridHelper.StyleGrid(_grid);
            }
            catch { }
        }

        private void LoadDeliveryNotes()
        {
            try
            {
                var dt = DictionaryService.DecorateStatusColumn(_deliveryCtrl.GetAllDeliveryNotes(), "Status", DictionaryService.Categories.Delivery);
                if (dt == null) return;

                var filters = new List<string>();
                string keyword = _deliverySearchBox?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string escaped = keyword.Replace("'", "''");
                    var parts = new List<string>
                    {
                        $"[Delivery Note Code] LIKE '%{escaped}%'",
                        $"[Customer] LIKE '%{escaped}%'",
                        $"[Sales Order] LIKE '%{escaped}%'",
                        $"[Ship Method] LIKE '%{escaped}%'",
                        $"[Tracking Number] LIKE '%{escaped}%'"
                    };
                    if (dt.Columns.Contains("Status Label"))
                        parts.Add($"[Status Label] LIKE '%{escaped}%'");
                    filters.Add("(" + string.Join(" OR ", parts) + ")");
                }

                if (_deliveryStatusFilter != null && _deliveryStatusFilter.SelectedIndex > 0)
                {
                    int? status = DictionaryUIHelper.GetFilterStatusCode(_deliveryStatusFilter);
                    if (status.HasValue)
                        filters.Add("[Status] = " + status.Value);
                }

                if (filters.Count > 0)
                {
                    dt.DefaultView.RowFilter = string.Join(" AND ", filters);
                    dt = dt.DefaultView.ToTable();
                }

                _deliveryGrid.DataSource = dt;
                GridHelper.StyleGrid(_deliveryGrid);
                HideDeliveryGridIdColumns();
            }
            catch { }
        }

        private void HideDeliveryGridIdColumns()
        {
            if (_deliveryGrid == null) return;
            if (_deliveryGrid.Columns.Contains("Delivery Note ID"))
                _deliveryGrid.Columns["Delivery Note ID"].Visible = false;
            if (_deliveryGrid.Columns.Contains("Status"))
                _deliveryGrid.Columns["Status"].Visible = false;
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Cells[0].Value == null) return;
            if (AppSession.CanEdit(PermissionModule.Warehouse))
            {
                long id = Convert.ToInt64(row.Cells[0].Value);
                ShowEditDialog(id);
            }
            else
                ShowWarehouseTableDialog(row);
        }

        private void DeliveryGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _deliveryGrid.Rows[e.RowIndex];
            if (row.Cells[0].Value == null) return;

            if (!AppSession.CanEdit(PermissionModule.DeliveryNote))
            {
                ShowDeliveryTableDialog(row);
                return;
            }

            long id = Convert.ToInt64(row.Cells[0].Value);
            OpenDeliveryEditDialog(id);
        }

        private void OpenDeliveryEditDialog(long id) => ShowDeliveryNoteEditorDialog(id);

        private void ShowWarehouseTableDialog(DataGridViewRow row)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Warehouse Detail";
                dlg.Size = new Size(620, 420);
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

        private void ShowDeliveryTableDialog(DataGridViewRow row)
        {
            if (row?.Cells[0].Value == null) return;
            long id = Convert.ToInt64(row.Cells[0].Value);
            DataTable header = null;
            DataTable lines = null;
            try { header = _deliveryCtrl.GetHeaderDetail(id); } catch { }
            try { lines = _deliveryCtrl.GetDeliveryLines(id); } catch { }
            if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Status"))
            {
                int code = Convert.ToInt32(header.Rows[0]["Status"]);
                header.Rows[0]["Status"] = DictionaryService.GetDisplayName(DictionaryService.Categories.Delivery, code);
            }
            string titleCode = header?.Rows.Count > 0 && header.Columns.Contains("Delivery Note Code")
                ? header.Rows[0]["Delivery Note Code"]?.ToString()
                : "DN-" + id;
            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            DetailViewHelper.ShowDetail(this, $"Delivery Note — {titleCode}", fields, lines, $"DeliveryNote_{id}");
        }

        private void ShowCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Warehouse";
                dlg.Size = new Size(460, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox();
                var txtAddr = new TextBox();

                UITheme.AddFormRow(layout, 0, "Warehouse Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Address", txtAddr);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.Warehouse);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.Warehouse, PermissionAction.Create, dlg)) return;
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Warehouse Name is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        _warehouseCtrl.Insert(new Warehouse { WarehouseName = txtName.Text.Trim(), WarehouseAddress = txtAddr.Text.Trim() });
                        MessageBox.Show("Warehouse created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
            }
        }

        private void ShowEditDialog(long id)
        {
            var wh = _warehouseCtrl.GetById(id);
            if (wh == null) { MessageBox.Show("Warehouse not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit Warehouse — {wh.WarehouseName}";
                dlg.Size = new Size(720, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.Sizable;
                dlg.MinimizeBox = false;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 160 };

                var topPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
                var layout = new TableLayoutPanel { Dock = DockStyle.Top, Height = 90, ColumnCount = 2, RowCount = 2 };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox { Text = wh.WarehouseName };
                var txtAddr = new TextBox { Text = wh.WarehouseAddress };

                UITheme.AddFormRow(layout, 0, "Warehouse Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Address", txtAddr);
                topPanel.Controls.Add(layout);

                var stockGrid = GridHelper.CreateStyledGrid();
                stockGrid.Dock = DockStyle.Fill;
                Action loadStock = () =>
                {
                    try
                    {
                        stockGrid.DataSource = _warehouseCtrl.GetWarehouseProducts(id, StockAlertHelper.DefaultProductMinStock);
                        GridHelper.StyleGridWithStockAlert(stockGrid, "Available Qty", "Min Stock Level");
                    }
                    catch { }
                };
                loadStock();

                var stockHeader = new Panel { Dock = DockStyle.Top, Height = 36 };
                stockHeader.Controls.Add(new Label
                {
                    Text = "Product stock in this warehouse",
                    Dock = DockStyle.Left,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = UITheme.TextDark,
                    Padding = new Padding(4, 8, 0, 0)
                });
                stockHeader.Controls.Add(StockAlertHelper.CreateLegendLabel());

                split.Panel1.Controls.Add(topPanel);
                split.Panel2.Controls.Add(stockGrid);
                split.Panel2.Controls.Add(stockHeader);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                PermissionGuard.ApplyEditButton(btnSave, PermissionModule.Warehouse);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.Warehouse, PermissionAction.Edit, dlg)) return;
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Warehouse Name is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        wh.WarehouseName = txtName.Text.Trim();
                        wh.WarehouseAddress = txtAddr.Text.Trim();
                        _warehouseCtrl.Update(wh);
                        MessageBox.Show("Warehouse updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(split);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
            }
        }

        private void ShowCreateDeliveryDialog() => ShowDeliveryNoteEditorDialog(null);

        private void ShowDeliveryNoteEditorDialog(long? deliveryNoteId)
        {
            bool isNew = !deliveryNoteId.HasValue;
            DeliveryNote dn = null;
            if (!isNew)
            {
                dn = _deliveryCtrl.GetById(deliveryNoteId.Value);
                if (dn == null)
                {
                    UITheme.ShowWarning("Delivery note not found.");
                    return;
                }
            }

            long dnId = dn?.DeliveryNoteID ?? 0;
            bool confirmed = dn != null && DeliveryNoteController.IsDeliveryConfirmed(dn.Status);
            bool hasInvoice = dnId > 0 && _deliveryCtrl.HasInvoiceLines(dnId);
            bool linesLocked = confirmed || hasInvoice;

            using (var dlg = new Form())
            {
                dlg.Text = isNew ? "New Delivery Note" : $"Edit Delivery Note — {dn.DeliveryNoteCode}";
                dlg.Size = new Size(920, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(800, 520);
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 260
                };

                var formLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 9,
                    Padding = new Padding(12)
                };
                formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var lblCode = new Label
                {
                    Text = isNew ? "(assigned on save)" : dn.DeliveryNoteCode,
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbCustomer = BuildCustomerCombo(dn?.CustomerID ?? 0);
                var cmbSalesOrder = BuildSalesOrderCombo(dn?.CustomerID ?? 0, dn?.SalesOrderID ?? 0);
                var cmbWarehouse = BuildWarehouseCombo(dn?.WarehouseID ?? 0);
                var cmbShipMethod = BuildShipMethodCombo(dn?.ShipMethod);
                var txtTracking = new TextBox { Text = dn?.TrackingNumber ?? "" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Delivery, dn?.Status ?? 0);
                if (isNew)
                {
                    cmbStatus.Enabled = false;
                    DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Delivery, 0);
                }
                var txtRemark = new TextBox { Text = dn?.Remark ?? "", Multiline = true, Height = 48, ScrollBars = ScrollBars.Vertical };
                string staffLabel = AppSession.CurrentUser != null
                    ? (string.IsNullOrWhiteSpace(AppSession.CurrentUser.FullName)
                        ? AppSession.CurrentUser.Username
                        : AppSession.CurrentUser.FullName)
                    : (dn != null ? dn.StaffID.ToString() : "—");
                var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };

                int row = 0;
                if (!isNew) UITheme.AddFormRow(formLayout, row++, "Delivery Note Code", lblCode);
                UITheme.AddFormRow(formLayout, row++, "Customer *", cmbCustomer);
                UITheme.AddFormRow(formLayout, row++, "Sales Order *", cmbSalesOrder);
                UITheme.AddFormRow(formLayout, row++, "Warehouse *", cmbWarehouse);
                UITheme.AddFormRow(formLayout, row++, "Ship Method *", cmbShipMethod);
                UITheme.AddFormRow(formLayout, row++, "Tracking Number", txtTracking);
                UITheme.AddFormRow(formLayout, row++, "Staff", lblStaff);
                UITheme.AddFormRow(formLayout, row++, "Status", cmbStatus);
                UITheme.AddFormRow(formLayout, row, "Remark", txtRemark);

                var lineGrid = CreateDeliveryLineGrid();
                Action reloadLines = () =>
                {
                    long soId = GetComboLongId(cmbSalesOrder);
                    if (soId <= 0)
                    {
                        lineGrid.DataSource = null;
                        lineGrid.ReadOnly = true;
                        return;
                    }
                    LoadDeliveryLineGrid(lineGrid, soId, isNew ? 0 : dnId, linesLocked);
                };

                cmbCustomer.SelectedIndexChanged += (s, e) =>
                {
                    if (confirmed) return;
                    BindSalesOrderCombo(cmbSalesOrder, GetComboLongId(cmbCustomer), 0);
                    reloadLines();
                };
                cmbSalesOrder.SelectedIndexChanged += (s, e) => reloadLines();

                if (confirmed)
                {
                    cmbCustomer.Enabled = false;
                    cmbSalesOrder.Enabled = false;
                    cmbWarehouse.Enabled = false;
                    cmbShipMethod.Enabled = false;
                }

                split.Panel1.Controls.Add(formLayout);

                string lineHint = linesLocked
                    ? (confirmed
                        ? "Lines locked (confirmed). Tracking/remark/status can still be updated."
                        : "Lines locked (invoiced). Header fields can still be updated.")
                    : isNew
                        ? "Select Sales Order to load lines. Edit Ship Qty only (optional — leave 0 to auto-fill on Confirm Delivery)."
                        : "Edit Ship Qty only (≤ Remaining). Leave blank to auto-fill on Confirm Delivery.";
                var lineHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
                lineHeader.Controls.Add(new Label
                {
                    Text = lineHint,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = UITheme.TextDark
                });
                split.Panel2.Controls.Add(lineGrid);
                split.Panel2.Controls.Add(lineHeader);

                reloadLines();

                var btnSave = UITheme.CreatePrimaryButton(isNew ? "Create" : "Update");
                if (isNew)
                    PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.DeliveryNote);
                else
                    PermissionGuard.ApplyEditButton(btnSave, PermissionModule.DeliveryNote);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                    if (!PermissionGuard.Ensure(PermissionModule.DeliveryNote, action, dlg)) return;

                    long customerId = GetComboLongId(cmbCustomer);
                    long salesOrderId = GetComboLongId(cmbSalesOrder);
                    long warehouseId = GetComboLongId(cmbWarehouse);
                    if (customerId <= 0 || salesOrderId <= 0)
                    {
                        UITheme.ShowWarning("Please select Customer and Sales Order.");
                        return;
                    }
                    if (warehouseId <= 0)
                    {
                        UITheme.ShowWarning("Please select a Warehouse.");
                        return;
                    }
                    string shipMethod = cmbShipMethod.Text.Trim();
                    if (string.IsNullOrWhiteSpace(shipMethod))
                    {
                        UITheme.ShowWarning("Ship Method is required.");
                        return;
                    }

                    long staffId = AppSession.CurrentUser?.StaffID ?? dn?.StaffID ?? 0;
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Current user has no Staff ID; cannot save delivery note.");
                        return;
                    }

                    List<(long ProductId, int ShipQty)> lines = null;
                    if (!linesLocked)
                    {
                        if (!TryReadDeliveryLines(lineGrid, out lines, out string lineError))
                        {
                            UITheme.ShowWarning(lineError);
                            return;
                        }
                    }

                    try
                    {
                        var note = new DeliveryNote
                        {
                            DeliveryNoteID = dnId,
                            DeliveryNoteCode = isNew ? "DN-TEMP" : dn.DeliveryNoteCode,
                            CustomerID = customerId,
                            SalesOrderID = salesOrderId,
                            StaffID = staffId,
                            WarehouseID = warehouseId,
                            ShipMethod = shipMethod,
                            TrackingNumber = txtTracking.Text.Trim(),
                            Status = isNew ? 0 : DictionaryUIHelper.GetSelectedStatusCode(cmbStatus),
                            Remark = txtRemark.Text.Trim()
                        };

                        if (isNew)
                        {
                            long newId = _deliveryCtrl.CreateWithLines(note, lines ?? Enumerable.Empty<(long, int)>());
                            UITheme.ShowSuccess(
                                $"Delivery note DN-{newId} created.\r\nNext: select it in the list and click Confirm Delivery (then Finance → Invoice from Delivery).");
                        }
                        else if (!_deliveryCtrl.UpdateWithLines(note, lines))
                        {
                            UITheme.ShowWarning("Update failed.");
                            return;
                        }
                        else
                            UITheme.ShowSuccess("Delivery note updated.");

                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(split);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadDeliveryNotes();
            }
        }

        private ComboBox BuildCustomerCombo(long selectedCustomerId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dt = _customerCtrl.GetAllCustomers();
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string code = dt.Columns.Contains("Customer Code") ? row["Customer Code"]?.ToString() : "";
                    string name = row["Customer Name"]?.ToString();
                    row["DisplayText"] = string.IsNullOrWhiteSpace(code) ? name : $"{code} — {name}";
                }
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Customer ID";
            if (selectedCustomerId > 0) SetComboLongValue(cmb, selectedCustomerId);
            return cmb;
        }

        private ComboBox BuildSalesOrderCombo(long customerId, long selectedSalesOrderId)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            BindSalesOrderCombo(cmb, customerId, selectedSalesOrderId);
            return cmb;
        }

        private void BindSalesOrderCombo(ComboBox cmb, long customerId, long selectedSalesOrderId)
        {
            DataTable dt;
            if (customerId > 0)
                dt = _salesOrderCtrl.GetSalesOrdersPickerByCustomer(customerId);
            else
            {
                dt = new DataTable();
                dt.Columns.Add("Order ID", typeof(long));
                dt.Columns.Add("DisplayText", typeof(string));
            }
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string code = row.Table.Columns.Contains("Order Code") ? row["Order Code"]?.ToString() : "";
                    string cref = row.Table.Columns.Contains("Customer Ref") ? row["Customer Ref"]?.ToString() : "";
                    row["DisplayText"] = string.IsNullOrWhiteSpace(cref) ? code : $"{code} ({cref})";
                }
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Order ID";
            if (selectedSalesOrderId > 0) SetComboLongValue(cmb, selectedSalesOrderId);
            else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        private ComboBox BuildWarehouseCombo(long selectedWarehouseId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
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
            if (selectedWarehouseId > 0) SetComboLongValue(cmb, selectedWarehouseId);
            else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            return cmb;
        }

        private static ComboBox BuildShipMethodCombo(string current = null)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 340 };
            cmb.Items.AddRange(DefaultShipMethods);
            if (!string.IsNullOrWhiteSpace(current))
                cmb.Text = current;
            else if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
            return cmb;
        }

        private static DataGridView CreateDeliveryLineGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
        }

        private static bool IsShipQtyColumn(DataGridViewColumn col)
        {
            if (col == null) return false;
            string name = (col.DataPropertyName ?? col.Name ?? col.HeaderText ?? "").Trim();
            return string.Equals(name, "Ship Qty", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadDeliveryLineGrid(DataGridView grid, long salesOrderId, long deliveryNoteId, bool readOnly)
        {
            var dt = _deliveryCtrl.GetLineEditorData(salesOrderId, deliveryNoteId);
            if (dt != null && dt.Columns.Contains("Ship Qty"))
                dt.Columns["Ship Qty"].ReadOnly = false;

            grid.ReadOnly = readOnly;
            grid.DataSource = dt;
            GridHelper.StyleGrid(grid);

            if (grid.Columns.Contains("ProductID"))
                grid.Columns["ProductID"].Visible = false;

            var shipQtyStyle = new DataGridViewCellStyle
            {
                BackColor = readOnly ? Color.White : Color.FromArgb(255, 250, 230)
            };

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (IsShipQtyColumn(col))
                {
                    col.ReadOnly = readOnly;
                    col.DefaultCellStyle = shipQtyStyle;
                }
                else
                {
                    col.ReadOnly = true;
                }
            }
        }

        private static int GetRowInt(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return 0;
            return Convert.ToInt32(row[columnName]);
        }

        private static bool TryReadDeliveryLines(DataGridView grid, out List<(long ProductId, int ShipQty)> lines, out string error)
        {
            lines = new List<(long, int)>();
            error = null;
            if (!(grid.DataSource is DataTable dt))
            {
                error = "No product lines for this sales order.";
                return false;
            }
            foreach (DataRow row in dt.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                long productId = Convert.ToInt64(row["ProductID"]);
                int shipQty = GetRowInt(row, "Ship Qty");
                int remaining = GetRowInt(row, "Remaining Qty");
                if (shipQty < 0)
                {
                    error = "Ship quantity cannot be negative.";
                    return false;
                }
                if (shipQty > remaining)
                {
                    string product = row["Product"]?.ToString() ?? productId.ToString();
                    error = $"Ship qty for {product} ({shipQty}) exceeds remaining ({remaining}).";
                    return false;
                }
                if (shipQty > 0)
                    lines.Add((productId, shipQty));
            }
            return true;
        }

        private static long GetComboLongId(ComboBox cmb)
        {
            if (cmb?.SelectedValue == null) return 0;
            long.TryParse(cmb.SelectedValue.ToString(), out long id);
            return id;
        }

        private static void SetComboLongValue(ComboBox cmb, long value)
        {
            try { cmb.SelectedValue = value; }
            catch { }
        }

        private void ConfirmSelectedDelivery()
        {
            if (_deliveryGrid?.CurrentRow?.Cells[0].Value == null)
            {
                UITheme.ShowWarning("Please select a delivery note first.");
                return;
            }
            if (!PermissionGuard.Ensure(PermissionModule.DeliveryNote, PermissionAction.Edit, this)) return;

            long id = Convert.ToInt64(_deliveryGrid.CurrentRow.Cells[0].Value);
            var result = _inventoryWorkflow.ConfirmDelivery(id);
            if (result.Success)
            {
                UITheme.ShowSuccess(result.Message);
                LoadDeliveryNotes();
            }
            else
            {
                UITheme.ShowWarning(result.Message);
            }
        }
    }
}
