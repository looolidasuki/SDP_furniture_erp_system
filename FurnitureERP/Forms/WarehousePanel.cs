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
    public class WarehousePanel : UserControl
    {
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private DataGridView _grid;

        private TabControl _tabs;
        private DataGridView _deliveryGrid;
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
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

            _warehouseSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnDetailWarehouse.Right + 10, 10) };
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
            toolbar.Controls.Add(_warehouseSearchBox);
            toolbar.Controls.Add(_warehouseStatusFilter);

            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            var warehouseContent = new Panel { Dock = DockStyle.Fill };
            warehouseContent.Controls.Add(toolbar);
            warehouseContent.Controls.Add(_grid);
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
            _deliverySearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnDetailDelivery.Right + 10, 10) };
            _deliverySearchBox.TextChanged += (s, e) => LoadDeliveryNotes();
            _deliveryStatusFilter = new ComboBox
            {
                Width = 140,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(_deliverySearchBox.Right + 10, 10)
            };
            _deliveryStatusFilter.Items.AddRange(new object[] { "All Status", "Draft", "Dispatched", "Delivered" });
            _deliveryStatusFilter.SelectedIndex = 0;
            _deliveryStatusFilter.SelectedIndexChanged += (s, e) => LoadDeliveryNotes();
            deliveryToolbar.Controls.Add(btnNewDelivery);
            deliveryToolbar.Controls.Add(btnRefreshDelivery);
            deliveryToolbar.Controls.Add(btnDetailDelivery);
            deliveryToolbar.Controls.Add(_deliverySearchBox);
            deliveryToolbar.Controls.Add(_deliveryStatusFilter);

            _deliveryGrid = GridHelper.CreateStyledGrid();
            _deliveryGrid.CellDoubleClick += DeliveryGrid_CellDoubleClick;

            var deliveryContent = new Panel { Dock = DockStyle.Fill };
            deliveryContent.Controls.Add(deliveryToolbar);
            deliveryContent.Controls.Add(_deliveryGrid);
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
                var dt = _deliveryCtrl.GetAllDeliveryNotes();
                if (dt == null) return;

                string keyword = _deliverySearchBox?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string escaped = keyword.Replace("'", "''");
                    dt.DefaultView.RowFilter = $"[Delivery Note Code] LIKE '%{escaped}%' OR Convert([Customer ID], 'System.String') LIKE '%{escaped}%'";
                    dt = dt.DefaultView.ToTable();
                }

                if (_deliveryStatusFilter != null && _deliveryStatusFilter.SelectedIndex > 0)
                {
                    int status = _deliveryStatusFilter.SelectedIndex - 1;
                    dt.DefaultView.RowFilter = "[Status] = " + status;
                    dt = dt.DefaultView.ToTable();
                }

                _deliveryGrid.DataSource = dt;
                GridHelper.StyleGrid(_deliveryGrid);
            }
            catch { }
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
            var dn = _deliveryCtrl.GetById(id);
            if (dn == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Delivery Note Details / Edit";
                dlg.Size = new Size(680, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var formLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 230,
                    ColumnCount = 2,
                    RowCount = 8,
                    Padding = new Padding(12)
                };
                formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
                formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCustomer = new TextBox { Text = dn.CustomerID.ToString() };
                var txtSalesOrder = new TextBox { Text = dn.SalesOrderID.ToString() };
                var txtStaff = new TextBox { Text = dn.StaffID.ToString() };
                var txtWarehouse = new TextBox { Text = dn.WarehouseID.ToString() };
                var txtShipMethod = new TextBox { Text = dn.ShipMethod ?? "" };
                var txtTracking = new TextBox { Text = dn.TrackingNumber ?? "" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Dispatched", "2 - Delivered" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(dn.Status, 2));

                UITheme.AddFormRow(formLayout, 0, "Delivery Note Code", new Label { Text = dn.DeliveryNoteCode, AutoSize = true, ForeColor = UITheme.TextDark });
                UITheme.AddFormRow(formLayout, 1, "Customer ID *", txtCustomer);
                UITheme.AddFormRow(formLayout, 2, "Sales Order ID *", txtSalesOrder);
                UITheme.AddFormRow(formLayout, 3, "Staff ID *", txtStaff);
                UITheme.AddFormRow(formLayout, 4, "Warehouse ID", txtWarehouse);
                UITheme.AddFormRow(formLayout, 5, "Ship Method", txtShipMethod);
                UITheme.AddFormRow(formLayout, 6, "Tracking Number", txtTracking);
                UITheme.AddFormRow(formLayout, 7, "Status", cmbStatus);

                var lineGrid = GridHelper.CreateStyledGrid();
                try
                {
                    lineGrid.DataSource = _deliveryCtrl.GetDeliveryLines(id);
                    GridHelper.StyleGrid(lineGrid);
                }
                catch { }

                var btnUpdate = UITheme.CreatePrimaryButton("Update");
                PermissionGuard.ApplyEditButton(btnUpdate, PermissionModule.DeliveryNote);
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, args) => dlg.Close();
                btnUpdate.Click += (s, args) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.DeliveryNote, PermissionAction.Edit, dlg)) return;
                    if (!long.TryParse(txtCustomer.Text.Trim(), out long customerId) ||
                        !long.TryParse(txtSalesOrder.Text.Trim(), out long salesOrderId) ||
                        !long.TryParse(txtStaff.Text.Trim(), out long staffId))
                    {
                        UITheme.ShowWarning("Valid Customer ID, Sales Order ID and Staff ID are required.");
                        return;
                    }

                    dn.CustomerID = customerId;
                    dn.SalesOrderID = salesOrderId;
                    dn.StaffID = staffId;
                    dn.WarehouseID = long.TryParse(txtWarehouse.Text.Trim(), out long whId) ? whId : 0;
                    dn.ShipMethod = txtShipMethod.Text.Trim();
                    dn.TrackingNumber = txtTracking.Text.Trim();
                    dn.Status = cmbStatus.SelectedIndex;

                    if (_deliveryCtrl.Update(dn))
                    {
                        UITheme.ShowSuccess("Delivery note updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadDeliveryNotes();
                    }
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnUpdate);
                btnPanel.Controls.Add(btnClose);

                dlg.Controls.Add(lineGrid);
                dlg.Controls.Add(formLayout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

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
            using (var dlg = new Form())
            {
                dlg.Text = "Delivery Note Detail";
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

                var lineGrid = GridHelper.CreateStyledGrid();
                try { lineGrid.DataSource = _deliveryCtrl.GetDeliveryLines(id); GridHelper.StyleGrid(lineGrid); } catch { }

                split.Panel1.Controls.Add(headGrid);
                split.Panel2.Controls.Add(lineGrid);
                dlg.Controls.Add(split);
                dlg.ShowDialog(this);
            }
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
                dlg.Size = new Size(460, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox { Text = wh.WarehouseName };
                var txtAddr = new TextBox { Text = wh.WarehouseAddress };

                UITheme.AddFormRow(layout, 0, "Warehouse Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Address", txtAddr);

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

                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
            }
        }

        private void ShowCreateDeliveryDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Delivery Note";
                dlg.Size = new Size(480, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCustomerId = new TextBox();
                var txtSalesOrderId = new TextBox();
                var txtStaffId = new TextBox();
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Dispatched", "2 - Delivered" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox();

                UITheme.AddFormRow(layout, 0, "Customer ID *", txtCustomerId);
                UITheme.AddFormRow(layout, 1, "Sales Order ID *", txtSalesOrderId);
                UITheme.AddFormRow(layout, 2, "Staff ID *", txtStaffId);
                UITheme.AddFormRow(layout, 3, "Status", cmbStatus);
                UITheme.AddFormRow(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.DeliveryNote);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.DeliveryNote, PermissionAction.Create, dlg)) return;
                    if (!long.TryParse(txtCustomerId.Text, out long custId) || !long.TryParse(txtSalesOrderId.Text, out long soId) || !long.TryParse(txtStaffId.Text, out long staffId))
                    { MessageBox.Show("Valid Customer ID, Sales Order ID and Staff ID are required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        var dn = new DeliveryNote
                        {
                            DeliveryNoteCode = "DN-TEMP",
                            CustomerID = custId,
                            SalesOrderID = soId,
                            StaffID = staffId,
                            Status = cmbStatus.SelectedIndex,
                            Remark = txtRemark.Text.Trim()
                        };
                        long newId = _deliveryCtrl.Insert(dn);
                        _deliveryCtrl.UpdateCodeAfterInsert(newId);
                        MessageBox.Show($"Delivery Note DN-{newId} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadDeliveryNotes();
            }
        }
    }
}
