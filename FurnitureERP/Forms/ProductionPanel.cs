using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class ProductionPanel : UserControl
    {
        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly RawMaterialRequestNoteController _rmrnCtrl = new RawMaterialRequestNoteController();
        private readonly ProductController _productCtrl = new ProductController();

        private DataGridView _grid;
        private TextBox _searchBox;
        private ComboBox _statusFilter;
        private TabControl _tabs;

        public ProductionPanel(string module = "Production")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            Tag = module;
            BuildUI();
            if (module == "Raw Materials" && _tabs != null)
            {
                for (int i = 0; i < _tabs.TabPages.Count; i++)
                {
                    if (_tabs.TabPages[i].Text.Contains("Raw Material"))
                    {
                        _tabs.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };
            if (AppSession.CanView(PermissionModule.ProductionOrder)
                || AppSession.CanView(PermissionModule.RawMaterialRequestNote)
                || AppSession.CanView(PermissionModule.Product))
                _tabs.TabPages.Add(BuildProductionTab());
            if (AppSession.CanView(PermissionModule.RawMaterial))
                _tabs.TabPages.Add(BuildRawMaterialsTab());
            Controls.Add(_tabs);
        }

        // ─────────────────────────────────────────
        //  TAB 1: PRODUCTION ORDERS
        // ─────────────────────────────────────────
        private TabPage BuildProductionTab()
        {
            var page = new TabPage("🏭 Production Orders") { BackColor = UITheme.Background };

            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 52 };

            Button btnNew = UITheme.CreatePrimaryButton("+ New Production Order");
            btnNew.Location = new Point(0, 9);
            btnNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Create, this)) ShowCreateDialog(); };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.ProductionOrder);

            Button btnQuickNew = UITheme.CreateSecondaryButton("⚡ Quick Entry");
            btnQuickNew.Location = new Point(btnNew.Width + 10, 9);
            btnQuickNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Create, this)) ShowBatchCreateDialog(); };
            PermissionGuard.ApplyCreateButton(btnQuickNew, PermissionModule.ProductionOrder);

            Button btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Width + btnQuickNew.Width + 20, 9);
            btnRefresh.Click += (s, e) => LoadData();

            Button btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 9);
            btnDetail.Click += (s, e) =>
            {
                if (_grid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a production order first."); return; }
                ShowProductionDetailTableDialog(Convert.ToInt64(_grid.CurrentRow.Cells[0].Value));
            };

            Button btnViewProducts = UITheme.CreateSecondaryButton("📦 View Products");
            btnViewProducts.Location = new Point(btnDetail.Right + 10, 9);
            btnViewProducts.Click += (s, e) => ShowProductsViewer();

            Button btnAddProduct = UITheme.CreateSecondaryButton("🗂 Add Product");
            btnAddProduct.Location = new Point(btnViewProducts.Right + 10, 9);
            btnAddProduct.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.Product, PermissionAction.Create, this)) ShowAddProductDialog(); };
            PermissionGuard.ApplyCreateButton(btnAddProduct, PermissionModule.Product);

            Button btnRMRN = UITheme.CreateSecondaryButton("📋 RM Requests");
            btnRMRN.Location = new Point(btnAddProduct.Right + 10, 9);
            btnRMRN.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.RawMaterialRequestNote, PermissionAction.View, this)) ShowRawMaterialRequestsPanel(); };
            btnRMRN.Visible = AppSession.CanView(PermissionModule.RawMaterialRequestNote);

            _searchBox = new TextBox { Width = 160, Height = 28, Location = new Point(btnRMRN.Right + 10, 12) };
            _searchBox.TextChanged += (s, e) => LoadData(_searchBox.Text.Trim());

            _statusFilter = new ComboBox { Width = 130, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_searchBox.Right + 8, 12) };
            _statusFilter.Items.AddRange(new object[] { "All Status", "Pending", "In Progress", "Completed", "Cancelled" });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => LoadData(_searchBox.Text.Trim());

            toolbar.Controls.AddRange(new Control[] { btnNew, btnQuickNew, btnRefresh, btnDetail, btnViewProducts, btnAddProduct, btnRMRN, _searchBox, _statusFilter });

            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_grid.Rows[e.RowIndex].Cells[0].Value == null) return;
                long id = Convert.ToInt64(_grid.Rows[e.RowIndex].Cells[0].Value);
                if (AppSession.CanEdit(PermissionModule.ProductionOrder))
                    ShowDetailDialog(id);
                else
                    ShowProductionDetailTableDialog(id);
            };

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(toolbar);
            content.Controls.Add(_grid);
            page.Controls.Add(content);

            LoadData();
            return page;
        }

        // ─────────────────────────────────────────
        //  TAB 2: RAW MATERIALS (3 sub-tabs)
        // ─────────────────────────────────────────
        private TabPage BuildRawMaterialsTab()
        {
            var page = new TabPage("🧱 Raw Materials") { BackColor = UITheme.Background };

            var subTabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };
            subTabs.TabPages.Add(BuildRmListSubTab());
            subTabs.TabPages.Add(BuildRmWarehouseSubTab());
            subTabs.TabPages.Add(BuildRmSupplierSubTab());

            page.Controls.Add(subTabs);
            return page;
        }

        private TabPage BuildRmListSubTab()
        {
            var page = new TabPage("📋 Materials") { BackColor = UITheme.Background };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
            var btnNew = UITheme.CreatePrimaryButton("+ New Raw Material");
            btnNew.Location = new Point(0, 9);
            btnNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.RawMaterial, PermissionAction.Create, this)) ShowRawMaterialDialog(); };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.RawMaterial);

            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Width + 10, 9);
            btnRefresh.Click += (s, e) => { try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterialsWithStock(); GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock"); } catch { } };

            var btnEdit = UITheme.CreateSecondaryButton("✏ Edit");
            btnEdit.Location = new Point(btnRefresh.Right + 10, 9);
            btnEdit.Click += (s, e) =>
            {
                if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a raw material first."); return; }
                var rm = _rawMaterialCtrl.GetById(Convert.ToInt64(grid.CurrentRow.Cells[0].Value));
                if (rm != null) { ShowRawMaterialDialog(rm); try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterialsWithStock(); GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock"); } catch { } }
            };

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnEdit);

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idObj = grid.Rows[e.RowIndex].Cells[0].Value;
                if (idObj == null) return;
                var rm = _rawMaterialCtrl.GetById(Convert.ToInt64(idObj));
                if (rm != null) ShowRawMaterialDialog(rm);
            };

            try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterialsWithStock(); GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock"); } catch { }

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(toolbar);
            content.Controls.Add(grid);
            page.Controls.Add(content);
            return page;
        }

        private TabPage BuildRmWarehouseSubTab()
        {
            var page = new TabPage("🏭 Warehouse Stock") { BackColor = UITheme.Background };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(0, 9);

            string sql = @"SELECT rm.rawMaterialCode AS 'Code',
                                  rm.category AS 'Category',
                                  rm.size AS 'Size',
                                  rm.color AS 'Color',
                                  rw.warehouseID AS 'Warehouse ID',
                                  rw.currentStock AS 'Current Stock',
                                  rw.reservedStock AS 'Reserved Stock',
                                  rw.availableStock AS 'Available Stock',
                                  rm.minimumStockLevel AS 'Min Stock Level',
                                  rw.unit AS 'Unit',
                                  rw.lastUpdated AS 'Last Updated'
                           FROM RawMaterialWarehouse rw
                           INNER JOIN RawMaterial rm ON rw.rawMaterialID = rm.rawMaterialID
                           ORDER BY rm.rawMaterialCode";

            Action loadStock = () =>
            {
                try
                {
                    var dt = DatabaseConnect.ExecuteQuery(sql);
                    grid.DataSource = dt;
                    GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock Level");
                }
                catch { }
            };

            btnRefresh.Click += (s, e) => loadStock();
            toolbar.Controls.Add(btnRefresh);

            loadStock();

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(toolbar);
            content.Controls.Add(grid);
            page.Controls.Add(content);
            return page;
        }

        private TabPage BuildRmSupplierSubTab()
        {
            var page = new TabPage("🤝 Supplier Quotes") { BackColor = UITheme.Background };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(0, 9);
            btnRefresh.Click += (s, e) =>
            {
                try { grid.DataSource = _rawMaterialCtrl.GetAllSupplierQuotes(); GridHelper.StyleGrid(grid); } catch { }
            };
            toolbar.Controls.Add(btnRefresh);

            try { grid.DataSource = _rawMaterialCtrl.GetAllSupplierQuotes(); GridHelper.StyleGrid(grid); } catch { }

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(toolbar);
            content.Controls.Add(grid);
            page.Controls.Add(content);
            return page;
        }

        // ─────────────────────────────────────────
        //  PRODUCT VIEWER (shared static method)
        // ─────────────────────────────────────────
        public static void ShowProductsViewerDialog(Control owner)
        {
            var productCtrl = new ProductController();
            using (var dlg = new Form())
            {
                dlg.Text = "Products Catalogue";
                dlg.Size = new Size(1100, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                // Left: product list
                var listPanel = new Panel { Dock = DockStyle.Left, Width = 460, BackColor = UITheme.Background };
                var grid = GridHelper.CreateStyledGrid();
                grid.Dock = DockStyle.Fill;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                var toolbar = new Panel { Dock = DockStyle.Top, Height = 68 };
                var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
                btnRefresh.Location = new Point(0, 9);
                var txtSearch = new TextBox { Width = 200, Height = 28, Location = new Point(btnRefresh.Width + 10, 12) };
                var legend = StockAlertHelper.CreateLegendLabel();
                legend.Location = new Point(0, 38);
                toolbar.Controls.Add(btnRefresh);
                toolbar.Controls.Add(txtSearch);
                toolbar.Controls.Add(legend);

                Action loadProducts = () =>
                {
                    try
                    {
                        grid.DataSource = productCtrl.GetAllProductsWithStock(StockAlertHelper.DefaultProductMinStock);
                        GridHelper.StyleGridWithStockAlert(grid, "Available Stock", "Min Stock Level");
                    }
                    catch { }
                };
                btnRefresh.Click += (s, e) => loadProducts();
                txtSearch.TextChanged += (s, e) =>
                {
                    if (!(grid.DataSource is DataTable dt)) return;
                    string kw = txtSearch.Text.Trim().Replace("'", "''");
                    dt.DefaultView.RowFilter = string.IsNullOrEmpty(kw) ? "" :
                        $"[Product Code] LIKE '%{kw}%' OR [Category] LIKE '%{kw}%' OR [Style Number] LIKE '%{kw}%'";
                };

                listPanel.Controls.Add(toolbar);
                listPanel.Controls.Add(grid);

                // Right: detail panel
                var detailPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };

                var picBox = new PictureBox
                {
                    Size = new Size(220, 180),
                    Location = new Point(16, 16),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.WhiteSmoke
                };

                var infoLayout = new TableLayoutPanel
                {
                    Location = new Point(16, 210),
                    Size = new Size(400, 320),
                    ColumnCount = 2,
                    RowCount = 10,
                    BackColor = Color.White
                };
                infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int i = 0; i < 10; i++) infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

                string[] fieldLabels = { "Product Code", "Category", "Style Number", "Size", "Color", "Unit", "Base Price", "Status", "Remark", "Last Modified" };
                Label[] valueLabels = new Label[fieldLabels.Length];

                for (int i = 0; i < fieldLabels.Length; i++)
                {
                    infoLayout.Controls.Add(new Label { Text = fieldLabels[i] + ":", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UITheme.TextDark, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, i);
                    valueLabels[i] = new Label { Text = "—", AutoSize = true, ForeColor = UITheme.TextGray, Anchor = AnchorStyles.Left | AnchorStyles.Top };
                    infoLayout.Controls.Add(valueLabels[i], 1, i);
                }

                var lblNoSelect = new Label
                {
                    Text = "← Select a product to view details",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = UITheme.TextGray,
                    AutoSize = true,
                    Location = new Point(60, 250)
                };

                detailPanel.Controls.Add(picBox);
                detailPanel.Controls.Add(infoLayout);
                detailPanel.Controls.Add(lblNoSelect);
                infoLayout.Visible = false;
                picBox.Visible = false;

                grid.SelectionChanged += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) return;
                    try
                    {
                        long pid = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                        var p = productCtrl.GetById(pid);
                        if (p == null) return;

                        lblNoSelect.Visible = false;
                        infoLayout.Visible = true;
                        picBox.Visible = true;

                        valueLabels[0].Text = p.ProductCode ?? "—";
                        valueLabels[1].Text = p.Category ?? "—";
                        valueLabels[2].Text = p.StyleNumber ?? "—";
                        valueLabels[3].Text = p.Size ?? "—";
                        valueLabels[4].Text = p.Color ?? "—";
                        valueLabels[5].Text = p.Unit ?? "—";
                        valueLabels[6].Text = p.BasePriceByCurrency.ToString("N2");
                        var stockRow = grid.CurrentRow;
                        if (stockRow != null && grid.Columns.Contains("Available Stock"))
                        {
                            decimal available = Convert.ToDecimal(stockRow.Cells["Available Stock"].Value ?? 0);
                            decimal min = StockAlertHelper.DefaultProductMinStock;
                            if (grid.Columns.Contains("Min Stock Level"))
                                min = Convert.ToDecimal(stockRow.Cells["Min Stock Level"].Value ?? min);
                            string stockText = available.ToString("N0");
                            if (available <= 0)
                                valueLabels[7].Text = "Out of Stock (" + stockText + " available)";
                            else if (available < min)
                                valueLabels[7].Text = "Low Stock (" + stockText + " / min " + min.ToString("N0") + ")";
                            else
                                valueLabels[7].Text = "In Stock (" + stockText + " available)";
                            valueLabels[7].ForeColor = available <= 0 ? StockAlertHelper.CriticalForeColor
                                : available < min ? StockAlertHelper.LowStockForeColor : UITheme.TextGray;
                        }
                        else
                        {
                            valueLabels[7].Text = p.Status == 1 ? "Active" : "Inactive";
                            valueLabels[7].ForeColor = UITheme.TextGray;
                        }
                        valueLabels[8].Text = p.Remark ?? "—";
                        valueLabels[9].Text = p.LastModifyDate.HasValue ? p.LastModifyDate.Value.ToString("yyyy-MM-dd") : "—";

                        if (p.ProductImage != null && p.ProductImage.Length > 0)
                        {
                            try
                            {
                                using (var ms = new System.IO.MemoryStream(p.ProductImage))
                                    picBox.Image = Image.FromStream(ms);
                            }
                            catch { picBox.Image = null; }
                        }
                        else
                        {
                            picBox.Image = null;
                        }
                    }
                    catch { }
                };

                loadProducts();

                dlg.Controls.Add(detailPanel);
                dlg.Controls.Add(listPanel);
                dlg.ShowDialog(owner);
            }
        }

        private void ShowProductsViewer() => ShowProductsViewerDialog(this);

        // ─────────────────────────────────────────
        //  RAW MATERIAL DIALOG (Create / Edit)
        // ─────────────────────────────────────────
        private void ShowRawMaterialDialog(RawMaterial existing = null)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Raw Material" : "New Raw Material";
                dlg.Size = new Size(480, 380);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode     = new TextBox { Text = existing?.RawMaterialCode ?? "" };
                var txtCategory = new TextBox { Text = existing?.Category ?? "" };
                var txtSize     = new TextBox { Text = existing?.Size ?? "" };
                var txtColor    = new TextBox { Text = existing?.Color ?? "" };
                var txtMinStock = new TextBox { Text = existing?.MinimumStockLevel.ToString() ?? "0" };
                var cmbStatus   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0 - Inactive", "1 - Active" });
                cmbStatus.SelectedIndex = existing != null ? Math.Max(0, Math.Min(existing.Status, 1)) : 1;

                UITheme.AddFormField(layout, 0, "Material Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category",        txtCategory);
                UITheme.AddFormField(layout, 2, "Size",            txtSize);
                UITheme.AddFormField(layout, 3, "Color",           txtColor);
                UITheme.AddFormField(layout, 4, "Min Stock Level", txtMinStock);
                UITheme.AddFormField(layout, 5, "Status",          cmbStatus);

                var btnSave   = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text)) { UITheme.ShowWarning("Material Code is required."); return; }
                    if (!decimal.TryParse(txtMinStock.Text.Trim(), out decimal minStock)) { UITheme.ShowWarning("Min Stock Level must be a number."); return; }
                    try
                    {
                        var rm = new RawMaterial
                        {
                            RawMaterialCode   = txtCode.Text.Trim(),
                            Category          = txtCategory.Text.Trim(),
                            Size              = txtSize.Text.Trim(),
                            Color             = txtColor.Text.Trim(),
                            MinimumStockLevel = minStock,
                            Status            = cmbStatus.SelectedIndex
                        };
                        if (isEdit)
                        {
                            rm.RawMaterialID = existing.RawMaterialID;
                            _rawMaterialCtrl.Update(rm);
                            UITheme.ShowSuccess("Raw material updated.");
                        }
                        else
                        {
                            long id = _rawMaterialCtrl.Insert(rm);
                            UITheme.ShowSuccess($"Raw material RM-{id} created.");
                        }
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        // ─────────────────────────────────────────
        //  PRODUCTION ORDER METHODS (unchanged)
        // ─────────────────────────────────────────
        private void LoadData(string keyword = null)
        {
            try
            {
                DataTable dt = string.IsNullOrEmpty(keyword)
                    ? _productionCtrl.GetAllProductionOrders()
                    : _productionCtrl.Search(new SearchFilterCriteria { Keyword = keyword });

                if (dt != null)
                {
                    dt.Columns.Add("Status Label", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Status"] == DBNull.Value) continue;
                        int s = Convert.ToInt32(row["Status"]);
                        row["Status Label"] = s == 0 ? "Pending" : s == 1 ? "In Progress" : s == 2 ? "Completed" : s == 3 ? "Cancelled" : s.ToString();
                    }
                }
                if (dt != null && _statusFilter != null && _statusFilter.SelectedIndex > 0)
                {
                    dt.DefaultView.RowFilter = "[Status] = " + (_statusFilter.SelectedIndex - 1);
                    dt = dt.DefaultView.ToTable();
                }
                _grid.DataSource = dt;
                GridHelper.StyleGrid(_grid);
            }
            catch { }
        }

        private void ShowCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Production Order";
                dlg.Size = new Size(480, 360);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSalesOrderId = new TextBox();
                var txtStaffId = new TextBox();
                var dtpFinishDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0 - Pending", "1 - In Progress", "2 - Completed", "3 - Cancelled" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox { Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "Sales Order ID *", txtSalesOrderId);
                UITheme.AddFormField(layout, 1, "Staff ID *",       txtStaffId);
                UITheme.AddFormField(layout, 2, "Est. Finish Date *", dtpFinishDate);
                UITheme.AddFormField(layout, 3, "Status",           cmbStatus);
                UITheme.AddFormField(layout, 4, "Remark",           txtRemark);

                var btnSave   = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtSalesOrderId.Text) || string.IsNullOrWhiteSpace(txtStaffId.Text))
                    { MessageBox.Show("Sales Order ID and Staff ID are required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        var po = new ProductionOrder
                        {
                            ProductionOrderCode = "PO-TEMP",
                            SalesOrderID  = long.Parse(txtSalesOrderId.Text.Trim()),
                            StaffID       = long.Parse(txtStaffId.Text.Trim()),
                            EstFinishDate = dtpFinishDate.Value,
                            Status        = cmbStatus.SelectedIndex,
                            Remark        = txtRemark.Text.Trim()
                        };
                        long id = _productionCtrl.Insert(po);
                        _productionCtrl.UpdateCodeAfterInsert(id);
                        MessageBox.Show($"Production Order PO-{id} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowBatchCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Quick Create Production Orders";
                dlg.Size = new Size(900, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var info = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10, 10, 0, 0), Text = "Enter multiple orders below. Required: Sales Order ID, Staff ID, Est. Finish Date.", ForeColor = UITheme.TextDark };

                var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                grid.Columns.Add("SalesOrderID", "Sales Order ID *");
                grid.Columns.Add("StaffID", "Staff ID *");
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstFinishDate", HeaderText = "Est. Finish Date * (yyyy-MM-dd)" });
                grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Status", HeaderText = "Status", DataSource = new[] { "Pending", "In Progress", "Completed", "Cancelled" } });
                grid.Columns.Add("Remark", "Remark");

                var btnSave   = UITheme.CreatePrimaryButton("Save All");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    int successCount = 0;
                    for (int i = 0; i < grid.Rows.Count; i++)
                    {
                        var row = grid.Rows[i];
                        if (row.IsNewRow) continue;
                        string soText = row.Cells["SalesOrderID"]?.Value?.ToString();
                        string staffText = row.Cells["StaffID"]?.Value?.ToString();
                        string dateText = row.Cells["EstFinishDate"]?.Value?.ToString();
                        string statusText = row.Cells["Status"]?.Value?.ToString();
                        string remarkText = row.Cells["Remark"]?.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(soText) && string.IsNullOrWhiteSpace(staffText) && string.IsNullOrWhiteSpace(dateText)) continue;
                        if (!long.TryParse(soText, out long soId) || !long.TryParse(staffText, out long staffId) || !DateTime.TryParse(dateText, out DateTime estDate))
                        { MessageBox.Show($"Row {i + 1} has invalid required fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        int status = statusText == "In Progress" ? 1 : statusText == "Completed" ? 2 : statusText == "Cancelled" ? 3 : 0;
                        try
                        {
                            var po = new ProductionOrder { ProductionOrderCode = "PO-TEMP", SalesOrderID = soId, StaffID = staffId, EstFinishDate = estDate, Status = status, Remark = string.IsNullOrWhiteSpace(remarkText) ? null : remarkText.Trim() };
                            long id = _productionCtrl.Insert(po);
                            _productionCtrl.UpdateCodeAfterInsert(id);
                            successCount++;
                        }
                        catch (Exception ex) { MessageBox.Show($"Row {i + 1} failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    }
                    MessageBox.Show($"{successCount} production orders created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadData(_searchBox.Text.Trim());
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(grid); dlg.Controls.Add(info); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowDetailDialog(long id)
        {
            var po = _productionCtrl.GetById(id);
            if (po == null) { MessageBox.Show("Record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            using (var dlg = new Form())
            {
                dlg.Text = $"Production Order - {po.ProductionOrderCode}";
                dlg.Size = new Size(560, 450);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var dtpFinishDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = po.EstFinishDate };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0 - Pending", "1 - In Progress", "2 - Completed", "3 - Cancelled" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(po.Status, 3));
                var txtRemark = new TextBox { Text = po.Remark ?? "", Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "Order Code",      new Label { Text = po.ProductionOrderCode, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
                UITheme.AddFormField(layout, 1, "Est. Finish Date", dtpFinishDate);
                UITheme.AddFormField(layout, 2, "Status",          cmbStatus);
                UITheme.AddFormField(layout, 3, "Remark",          txtRemark);

                var btnUpdate = UITheme.CreatePrimaryButton("Update");
                var btnClose  = UITheme.CreateSecondaryButton("Close");
                btnClose.Click  += (s, e) => dlg.Close();
                btnUpdate.Click += (s, e) =>
                {
                    try
                    {
                        po.EstFinishDate = dtpFinishDate.Value;
                        po.Status = cmbStatus.SelectedIndex;
                        po.Remark = txtRemark.Text.Trim();
                        _productionCtrl.Update(po);
                        MessageBox.Show("Updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnUpdate); btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowProductionDetailTableDialog(long id)
        {
            var po = _productionCtrl.GetById(id);
            if (po == null) { UITheme.ShowWarning("Record not found."); return; }

            using (var dlg = new Form())
            {
                dlg.Text = $"Production Detail - {po.ProductionOrderCode}";
                dlg.Size = new Size(760, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 220 };

                var headerGrid = GridHelper.CreateStyledGrid();
                var headerDt = new DataTable();
                headerDt.Columns.Add("Field"); headerDt.Columns.Add("Value");
                headerDt.Rows.Add("Production Order ID", po.ProductionOrderID);
                headerDt.Rows.Add("Production Order Code", po.ProductionOrderCode ?? "");
                headerDt.Rows.Add("Sales Order ID", po.SalesOrderID);
                headerDt.Rows.Add("Staff ID", po.StaffID);
                headerDt.Rows.Add("Est. Finish Date", po.EstFinishDate.ToString("yyyy-MM-dd"));
                headerDt.Rows.Add("Status", po.Status);
                headerDt.Rows.Add("Remark", po.Remark ?? "");
                headerGrid.DataSource = headerDt;
                GridHelper.StyleGrid(headerGrid);

                var linesGrid = GridHelper.CreateStyledGrid();
                try { linesGrid.DataSource = _productionCtrl.GetProductLines(id); GridHelper.StyleGrid(linesGrid); } catch { }

                split.Panel1.Controls.Add(headerGrid);
                split.Panel2.Controls.Add(linesGrid);
                dlg.Controls.Add(split);
                dlg.ShowDialog(this);
            }
        }

        private void ShowAddProductDialog()
        {
            byte[] selectedImageBytes = null;
            using (var dlg = new Form())
            {
                dlg.Text = "New Product";
                dlg.Size = new Size(560, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode     = new TextBox { Dock = DockStyle.Fill };
                var txtCategory = new TextBox { Dock = DockStyle.Fill };
                var txtStyle    = new TextBox { Dock = DockStyle.Fill };
                var txtSize     = new TextBox { Dock = DockStyle.Fill };
                var txtColor    = new TextBox { Dock = DockStyle.Fill };
                var txtUnit     = new TextBox { Dock = DockStyle.Fill };
                var txtPrice    = new TextBox { Dock = DockStyle.Fill, Text = "0" };
                var txtStatus   = new TextBox { Dock = DockStyle.Fill, Text = "1" };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category",       txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number",   txtStyle);
                UITheme.AddFormField(layout, 3, "Size",           txtSize);
                UITheme.AddFormField(layout, 4, "Color",          txtColor);
                UITheme.AddFormField(layout, 5, "Unit",           txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price",     txtPrice);
                UITheme.AddFormField(layout, 7, "Status",         txtStatus);

                var picBox = new PictureBox { Width = 120, Height = 90, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
                var btnUpload = UITheme.CreateSecondaryButton("Upload Image");
                btnUpload.Click += (s, e) =>
                {
                    using (var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif" })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            selectedImageBytes = System.IO.File.ReadAllBytes(ofd.FileName);
                            picBox.Image = Image.FromFile(ofd.FileName);
                        }
                    }
                };
                var imgPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
                imgPanel.Controls.Add(picBox); imgPanel.Controls.Add(btnUpload);
                UITheme.AddFormField(layout, 8, "Product Image", imgPanel);

                var btnSave   = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text)) { MessageBox.Show("Product Code is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        _productCtrl.Insert(new Product
                        {
                            ProductCode         = txtCode.Text.Trim(),
                            Category            = txtCategory.Text.Trim(),
                            StyleNumber         = txtStyle.Text.Trim(),
                            Size                = txtSize.Text.Trim(),
                            Color               = txtColor.Text.Trim(),
                            Unit                = txtUnit.Text.Trim(),
                            BasePriceByCurrency = string.IsNullOrEmpty(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text),
                            Status              = string.IsNullOrEmpty(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text),
                            ProductImage        = selectedImageBytes
                        });
                        MessageBox.Show("Product added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowRawMaterialRequestsPanel()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Raw Material Request Notes";
                dlg.Size = new Size(800, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var grid = GridHelper.CreateStyledGrid();
                grid.Dock = DockStyle.Fill;
                try { grid.DataSource = _rmrnCtrl.GetAllRequestNotes(); GridHelper.StyleGrid(grid); } catch { }

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Dock = DockStyle.Bottom;
                btnClose.Click += (s, e) => dlg.Close();

                dlg.Controls.Add(grid);
                dlg.Controls.Add(btnClose);
                dlg.ShowDialog(this);
            }
        }
    }
}
