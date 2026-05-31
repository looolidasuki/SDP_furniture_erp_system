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

        private TabControl _tabs;

        public ProcurementPanel(string module = "Purchase Orders")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            // Select the correct tab based on module
            if (module == "Goods Received") _tabs.SelectedIndex = 2;
            else if (module == "Suppliers") _tabs.SelectedIndex = 3;
            else if (module == "Raw Materials") _tabs.SelectedIndex = 0;
            else _tabs.SelectedIndex = 1; // Purchase Orders
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            _tabs.TabPages.Add(BuildRawMaterialsTab());
            _tabs.TabPages.Add(BuildPurchaseOrdersTab());
            _tabs.TabPages.Add(BuildGoodsReceivedTab());
            _tabs.TabPages.Add(BuildSuppliersTab());

            Controls.Add(_tabs);
        }

        private TabPage BuildRawMaterialsTab()
        {
            var tab = new TabPage("Raw Materials") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Raw Material", () => ShowRawMaterialDialog(), grid, () => {
                try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterials(); GridHelper.ApplyStyle(grid); } catch { }
            }, () =>
            {
                if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a raw material first."); return; }
                ShowRowDetail(grid.CurrentRow, "Raw Material Details");
            });
            AttachSearchAndFilter(toolbar, grid);
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idObj = grid.Rows[e.RowIndex].Cells[0].Value;
                if (idObj == null) return;
                var entity = _rawMaterialCtrl.GetById(Convert.ToInt64(idObj));
                if (entity != null) ShowRawMaterialDialog(entity);
            };

            try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterials(); GridHelper.ApplyStyle(grid); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildPurchaseOrdersTab()
        {
            var tab = new TabPage("Purchase Orders") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Purchase Order", () => ShowPurchaseOrderDialog(), grid, () => {
                try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { }
            }, () =>
            {
                if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a purchase order first."); return; }
                ShowPurchaseOrderTableDialog(Convert.ToInt64(grid.CurrentRow.Cells[0].Value), grid.CurrentRow);
            });
            AttachSearchAndFilter(toolbar, grid);
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idObj = grid.Rows[e.RowIndex].Cells[0].Value;
                if (idObj == null) return;
                ShowPurchaseOrderDetailDialog(Convert.ToInt64(idObj));
            };

            try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildGoodsReceivedTab()
        {
            var tab = new TabPage("Goods Received") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New GRN", () => ShowGrnDialog(), grid, () => {
                try { grid.DataSource = _grnCtrl.GetAllGoodsReceivedNotes(); GridHelper.ApplyStyle(grid); } catch { }
            }, () =>
            {
                if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a GRN first."); return; }
                ShowGrnTableDialog(Convert.ToInt64(grid.CurrentRow.Cells[0].Value), grid.CurrentRow);
            });
            AttachSearchAndFilter(toolbar, grid);
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idObj = grid.Rows[e.RowIndex].Cells[0].Value;
                if (idObj == null) return;
                ShowGrnDetailDialog(Convert.ToInt64(idObj));
            };

            try { grid.DataSource = _grnCtrl.GetAllGoodsReceivedNotes(); GridHelper.ApplyStyle(grid); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(toolbar);
            return tab;
        }

        private TabPage BuildSuppliersTab()
        {
            var tab = new TabPage("Suppliers") { BackColor = UITheme.Background, Padding = new Padding(8) };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = BuildToolbar("+ New Supplier", () => ShowSupplierDialog(), grid, () => {
                try { grid.DataSource = _supplierCtrl.GetAllSuppliers(); GridHelper.ApplyStyle(grid); } catch { }
            }, () =>
            {
                if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a supplier first."); return; }
                ShowSupplierTableDialog(Convert.ToInt64(grid.CurrentRow.Cells[0].Value), grid.CurrentRow);
            });
            AttachSearchAndFilter(toolbar, grid);
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idObj = grid.Rows[e.RowIndex].Cells[0].Value;
                if (idObj == null) return;
                var entity = _supplierCtrl.GetById(Convert.ToInt64(idObj));
                if (entity != null) ShowSupplierDialog(entity);
            };

            try { grid.DataSource = _supplierCtrl.GetAllSuppliers(); GridHelper.ApplyStyle(grid); } catch { }

            tab.Controls.Add(grid);
            tab.Controls.Add(toolbar);
            return tab;
        }

        private Panel BuildToolbar(string createLabel, Action onCreate, DataGridView grid, Action onRefresh, Action onViewDetail)
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            var btnCreate = UITheme.CreatePrimaryButton(createLabel);
            btnCreate.Location = new Point(0, 8);
            btnCreate.Click += (s, e) => onCreate();

            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnCreate.Width + 10, 8);
            btnRefresh.Click += (s, e) => onRefresh();
            var btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 8);
            btnDetail.Click += (s, e) => onViewDetail?.Invoke();

            toolbar.Controls.Add(btnCreate);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetail);
            return toolbar;
        }

        private void AttachSearchAndFilter(Panel toolbar, DataGridView grid)
        {
            var txtSearch = new TextBox { Width = 180, Height = 28, Location = new Point(390, 10) };
            var cmbStatus = new ComboBox
            {
                Width = 120,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(txtSearch.Right + 10, 10)
            };
            cmbStatus.Items.AddRange(new object[] { "All Status", "0", "1", "2", "3", "4" });
            cmbStatus.SelectedIndex = 0;

            Action applyFilter = () =>
            {
                if (!(grid.DataSource is DataTable dt)) return;
                string keyword = txtSearch.Text.Trim().Replace("'", "''");
                var view = dt.DefaultView;
                var conditions = new System.Collections.Generic.List<string>();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var textColumns = dt.Columns.Cast<DataColumn>()
                        .Where(c => c.DataType == typeof(string))
                        .Select(c => $"[{c.ColumnName}] LIKE '%{keyword}%'");
                    string textFilter = string.Join(" OR ", textColumns);
                    if (!string.IsNullOrWhiteSpace(textFilter)) conditions.Add("(" + textFilter + ")");
                }

                if (cmbStatus.SelectedIndex > 0 && dt.Columns.Contains("Status"))
                {
                    conditions.Add("[Status] = " + (cmbStatus.SelectedIndex - 1));
                }

                view.RowFilter = string.Join(" AND ", conditions);
            };

            txtSearch.TextChanged += (s, e) => applyFilter();
            cmbStatus.SelectedIndexChanged += (s, e) => applyFilter();
            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(cmbStatus);
        }

        private void ShowRowDetail(DataGridViewRow row, string title)
        {
            DetailViewHelper.ShowKeyValueDetail(this, title, row);
        }

        private void ShowPurchaseOrderTableDialog(long id, DataGridViewRow row)
        {
            DataTable lines = null;
            try { lines = _purchaseOrderCtrl.GetLinesByPurchaseOrder(id); } catch { }
            DetailViewHelper.ShowDetail(this, "Purchase Order Detail",
                DetailViewHelper.RowToFieldValueTable(row), lines, $"PurchaseOrder_{id}");
        }

        private void ShowGrnTableDialog(long id, DataGridViewRow row)
        {
            DataTable lines = null;
            try { lines = _grnCtrl.GetReceivedLines(id); } catch { }
            DetailViewHelper.ShowDetail(this, "GRN Detail",
                DetailViewHelper.RowToFieldValueTable(row), lines, $"GRN_{id}");
        }

        private void ShowSupplierTableDialog(long supplierId, DataGridViewRow row)
        {
            DataTable quotes = null;
            try { quotes = _supplierCtrl.GetRawMaterialQuotesBySupplier(supplierId); } catch { }
            DetailViewHelper.ShowDetail(this, "Supplier Detail",
                DetailViewHelper.RowToFieldValueTable(row), quotes, $"Supplier_{supplierId}");
        }

        private void ShowPurchaseOrderDetailDialog(long id)
        {
            var po = _purchaseOrderCtrl.GetById(id);
            if (po == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Purchase Order Details / Edit";
                dlg.Size = new Size(520, 380);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSupplier = new TextBox { Text = po.SupplierID.ToString() };
                var txtStaff = new TextBox { Text = po.StaffID.ToString() };
                var dtpDelivery = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = po.RequestDeliveryDate };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Sent", "2 - Received", "3 - Cancelled" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(po.Status, 3));
                var txtRemark = new TextBox { Text = po.Remark ?? "", Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "PO Code", new Label { Text = po.PurchaseOrderCode, AutoSize = true });
                UITheme.AddFormField(layout, 1, "Supplier ID *", txtSupplier);
                UITheme.AddFormField(layout, 2, "Staff ID *", txtStaff);
                UITheme.AddFormField(layout, 3, "Request Delivery Date", dtpDelivery);
                UITheme.AddFormField(layout, 4, "Status", cmbStatus);
                UITheme.AddFormField(layout, 5, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!long.TryParse(txtSupplier.Text.Trim(), out long supplierId) || !long.TryParse(txtStaff.Text.Trim(), out long staffId))
                    {
                        UITheme.ShowWarning("Valid Supplier ID and Staff ID are required.");
                        return;
                    }
                    po.SupplierID = supplierId;
                    po.StaffID = staffId;
                    po.RequestDeliveryDate = dtpDelivery.Value;
                    po.Status = cmbStatus.SelectedIndex;
                    po.Remark = txtRemark.Text.Trim();
                    if (_purchaseOrderCtrl.Update(po))
                    {
                        UITheme.ShowSuccess("Purchase order updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[1].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }

        private void ShowGrnDetailDialog(long id)
        {
            var grn = _grnCtrl.GetById(id);
            if (grn == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Goods Received Details / Edit";
                dlg.Size = new Size(520, 380);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSupplier = new TextBox { Text = grn.SupplierID.ToString() };
                var txtPO = new TextBox { Text = grn.PurchaseOrderID.ToString() };
                var txtStaff = new TextBox { Text = grn.StaffID.ToString() };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Received", "2 - Verified" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(grn.Status, 2));
                var txtRemark = new TextBox { Text = grn.Remark ?? "", Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "GRN Code", new Label { Text = grn.GoodsReceivedNoteCode, AutoSize = true });
                UITheme.AddFormField(layout, 1, "Supplier ID *", txtSupplier);
                UITheme.AddFormField(layout, 2, "Purchase Order ID *", txtPO);
                UITheme.AddFormField(layout, 3, "Staff ID *", txtStaff);
                UITheme.AddFormField(layout, 4, "Status", cmbStatus);
                UITheme.AddFormField(layout, 5, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!long.TryParse(txtSupplier.Text.Trim(), out long supplierId) ||
                        !long.TryParse(txtPO.Text.Trim(), out long poId) ||
                        !long.TryParse(txtStaff.Text.Trim(), out long staffId))
                    {
                        UITheme.ShowWarning("Valid Supplier ID, Purchase Order ID and Staff ID are required.");
                        return;
                    }
                    grn.SupplierID = supplierId;
                    grn.PurchaseOrderID = poId;
                    grn.StaffID = staffId;
                    grn.Status = cmbStatus.SelectedIndex;
                    grn.Remark = txtRemark.Text.Trim();
                    if (_grnCtrl.Update(grn))
                    {
                        UITheme.ShowSuccess("GRN updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[2].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _grnCtrl.GetAllGoodsReceivedNotes(); GridHelper.ApplyStyle(grid); } catch { } }
                }
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
                dlg.Size = new Size(480, 360);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSupplier = new TextBox();
                var txtStaff = new TextBox();
                var dtpDelivery = new DateTimePicker { Format = DateTimePickerFormat.Short };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Sent", "2 - Received", "3 - Cancelled" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox();

                UITheme.AddFormField(layout, 0, "Supplier ID *", txtSupplier);
                UITheme.AddFormField(layout, 1, "Staff ID *", txtStaff);
                UITheme.AddFormField(layout, 2, "Request Delivery Date *", dtpDelivery);
                UITheme.AddFormField(layout, 3, "Status", cmbStatus);
                UITheme.AddFormField(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!long.TryParse(txtSupplier.Text, out long suppId) || !long.TryParse(txtStaff.Text, out long staffId))
                    { UITheme.ShowWarning("Valid Supplier ID and Staff ID are required."); return; }
                    try
                    {
                        var po = new PurchaseOrder
                        {
                            PurchaseOrderCode = "PO-TEMP",
                            SupplierID = suppId,
                            StaffID = staffId,
                            RequestDeliveryDate = dtpDelivery.Value,
                            Status = cmbStatus.SelectedIndex,
                            Remark = txtRemark.Text.Trim()
                        };
                        long id = _purchaseOrderCtrl.Insert(po);
                        _purchaseOrderCtrl.UpdateCodeAfterInsert(id);
                        UITheme.ShowSuccess($"Purchase Order PO-{id} created.");
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[1].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _purchaseOrderCtrl.GetAllPurchaseOrders(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
        }

        private void ShowGrnDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Goods Received Note";
                dlg.Size = new Size(480, 360);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSupplier = new TextBox();
                var txtPO = new TextBox();
                var txtStaff = new TextBox();
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Draft", "1 - Received", "2 - Verified" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox();

                UITheme.AddFormField(layout, 0, "Supplier ID *", txtSupplier);
                UITheme.AddFormField(layout, 1, "Purchase Order ID *", txtPO);
                UITheme.AddFormField(layout, 2, "Staff ID *", txtStaff);
                UITheme.AddFormField(layout, 3, "Status", cmbStatus);
                UITheme.AddFormField(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!long.TryParse(txtSupplier.Text, out long suppId) || !long.TryParse(txtPO.Text, out long poId) || !long.TryParse(txtStaff.Text, out long staffId))
                    { UITheme.ShowWarning("Valid Supplier ID, PO ID, and Staff ID are required."); return; }
                    try
                    {
                        var grn = new GoodsReceivedNote
                        {
                            GoodsReceivedNoteCode = "GRN-TEMP",
                            SupplierID = suppId,
                            PurchaseOrderID = poId,
                            StaffID = staffId,
                            Status = cmbStatus.SelectedIndex,
                            Remark = txtRemark.Text.Trim()
                        };
                        long id = _grnCtrl.Insert(grn);
                        _grnCtrl.UpdateCodeAfterInsert(id);
                        UITheme.ShowSuccess($"GRN-{id} created.");
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var grid = _tabs.TabPages[2].Controls.OfType<DataGridView>().FirstOrDefault();
                    if (grid != null) { try { grid.DataSource = _grnCtrl.GetAllGoodsReceivedNotes(); GridHelper.ApplyStyle(grid); } catch { } }
                }
            }
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
                var txtTerm = new TextBox { Text = existing?.PaymentTerm ?? "" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Inactive", "1 - Active" });
                cmbStatus.SelectedIndex = existing != null ? existing.Status : 1;

                UITheme.AddFormField(layout, 0, "Supplier Name *", txtName);
                UITheme.AddFormField(layout, 1, "Contact Person", txtContact);
                UITheme.AddFormField(layout, 2, "Phone", txtPhone);
                UITheme.AddFormField(layout, 3, "Email", txtEmail);
                UITheme.AddFormField(layout, 4, "Billing Address", txtAddress);
                UITheme.AddFormField(layout, 5, "Payment Term", txtTerm);
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
                            PaymentTerm = txtTerm.Text.Trim(),
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
