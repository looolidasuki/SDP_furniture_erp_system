using System;
using System.Collections.Generic;
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
            btnNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Create, this)) ShowProductionOrderForm(null, readOnly: false); };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.ProductionOrder);

            Button btnQuickNew = UITheme.CreateSecondaryButton("⚡ Quick Entry");
            btnQuickNew.Location = new Point(btnNew.Width + 10, 9);
            btnQuickNew.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Create, this)) ShowBatchCreateDialog(); };
            PermissionGuard.ApplyCreateButton(btnQuickNew, PermissionModule.ProductionOrder);

            Button btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnQuickNew.Right + 10, 9);
            btnRefresh.Click += (s, e) => LoadData();

            Button btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 9);
            btnDetail.Click += (s, e) =>
            {
                long? id = GetSelectedProductionOrderId();
                if (!id.HasValue) { UITheme.ShowWarning("Please select a production order first."); return; }
                ShowProductionOrderForm(id.Value, readOnly: true);
            };
            Button btnEdit = UITheme.CreateSecondaryButton("Edit");
            btnEdit.Location = new Point(btnDetail.Right + 10, 9);
            btnEdit.Click += (s, e) =>
            {
                long? id = GetSelectedProductionOrderId();
                if (!id.HasValue) { UITheme.ShowWarning("Please select a production order first."); return; }
                if (!PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Edit, this)) return;
                ShowProductionOrderForm(id.Value, readOnly: false);
            };
            PermissionGuard.ApplyEditButton(btnEdit, PermissionModule.ProductionOrder);

            Button btnViewProducts = UITheme.CreateSecondaryButton("📦 View Products");
            btnViewProducts.Location = new Point(btnEdit.Right + 10, 9);
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

            _statusFilter = new ComboBox { Width = 150, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_searchBox.Right + 8, 12) };
            DictionaryUIHelper.BindStatusFilter(_statusFilter, DictionaryService.Categories.Production);
            _statusFilter.SelectedIndexChanged += (s, e) => LoadData(_searchBox.Text.Trim());

            toolbar.Controls.AddRange(new Control[] { btnNew, btnQuickNew, btnRefresh, btnDetail, btnEdit, btnViewProducts, btnAddProduct, btnRMRN, _searchBox, _statusFilter });

            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long? id = GetProductionOrderIdFromRow(_grid.Rows[e.RowIndex]);
                if (!id.HasValue) return;
                if (AppSession.CanEdit(PermissionModule.ProductionOrder))
                    ShowProductionOrderForm(id.Value, readOnly: false);
                else
                    ShowProductionOrderForm(id.Value, readOnly: true);
            };

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(_grid);
            content.Controls.Add(FilterBlockHelper.CreateFilterBlock(_grid, "Production Order Filters"));
            content.Controls.Add(toolbar);
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
                if (GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Raw Material ID") <= 0) { UITheme.ShowWarning("Please select a raw material first."); return; }
                var rm = _rawMaterialCtrl.GetById(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Raw Material ID"));
                if (rm != null) { ShowRawMaterialDialog(rm); try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterialsWithStock(); GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock"); } catch { } }
            };

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnEdit);

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long rawMaterialId = GridHelper.TryGetRowLongId(grid, grid.Rows[e.RowIndex], "Raw Material ID");
                if (rawMaterialId <= 0) return;
                var rm = _rawMaterialCtrl.GetById(rawMaterialId);
                if (rm != null) ShowRawMaterialDialog(rm);
            };

            try { grid.DataSource = _rawMaterialCtrl.GetAllRawMaterialsWithStock(); GridHelper.StyleGridWithStockAlert(grid, "Current Stock", "Min Stock"); } catch { }

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(grid);
            content.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Raw Material Filters"));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            return page;
        }

        private TabPage BuildRmWarehouseSubTab()
        {
            var page = new TabPage("🏭 Warehouse Stock") { BackColor = UITheme.Background };
            var grid = GridHelper.CreateStyledGrid();

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 68 };
            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(0, 9);
            var legend = StockAlertHelper.CreateLegendLabel();
            legend.Location = new Point(0, 38);

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
                    GridHelper.StyleGridWithStockAlert(grid, "Available Stock", "Min Stock Level");
                }
                catch { }
            };

            btnRefresh.Click += (s, e) => loadStock();
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(legend);

            loadStock();

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(grid);
            content.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Warehouse Stock Filters"));
            content.Controls.Add(toolbar);
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
            content.Controls.Add(grid);
            content.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "Supplier Quote Filters"));
            content.Controls.Add(toolbar);
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
                dlg.Size = new Size(1280, 700);
                dlg.MinimumSize = new Size(1020, 560);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    BackColor = UITheme.Background
                };

                var grid = GridHelper.CreateStyledGrid();
                grid.Dock = DockStyle.Fill;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                var toolbar = new Panel { Dock = DockStyle.Top, Height = 68 };
                var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
                btnRefresh.Location = new Point(0, 9);
                var txtSearch = new TextBox { Width = 240, Height = 28, Location = new Point(btnRefresh.Width + 10, 12) };
                var legend = StockAlertHelper.CreateLegendLabel();
                legend.Location = new Point(0, 38);
                toolbar.Controls.Add(btnRefresh);
                toolbar.Controls.Add(txtSearch);
                toolbar.Controls.Add(legend);

                var leftLayout = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Background };
                leftLayout.Controls.Add(grid);
                leftLayout.Controls.Add(toolbar);
                split.Panel1.Controls.Add(leftLayout);

                Action loadProducts = () =>
                {
                    try
                    {
                        grid.DataSource = productCtrl.GetAllProductsWithStock(StockAlertHelper.DefaultProductMinStock);
                        GridHelper.StyleGridWithStockAlert(grid, "Available Stock", "Min Stock Level");
                        GridHelper.ConfigureProductCatalogueGrid(grid);
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

                        picBox.Image = null;
                        try
                        {
                            string imageUrl = productCtrl.GetProductImageUrl(pid);
                            if (!string.IsNullOrWhiteSpace(imageUrl) &&
                                (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                 imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                            {
                                using (var wc = new System.Net.WebClient())
                                using (var ms = new System.IO.MemoryStream(wc.DownloadData(imageUrl)))
                                    picBox.Image = Image.FromStream(ms);
                            }
                        }
                        catch { picBox.Image = null; }
                    }
                    catch { }
                };

                var bomBar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.White };
                var btnViewBom = UITheme.CreateSecondaryButton("View BOM");
                btnViewBom.Size = new Size(110, 32);
                btnViewBom.Location = new Point(16, 6);
                btnViewBom.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a product first."); return; }
                    long pid = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                    ShowProductBomViewDialog(dlg, pid);
                };

                var btnEditBom = UITheme.CreateSecondaryButton("Edit BOM");
                btnEditBom.Size = new Size(110, 32);
                btnEditBom.Location = new Point(btnViewBom.Right + 10, 6);
                btnEditBom.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a product first."); return; }
                    long pid = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                    string code = grid.CurrentRow.Cells["Product Code"]?.Value?.ToString();
                    ShowBomEditorDialog(dlg, pid, code);
                };
                PermissionGuard.ApplyEditButton(btnEditBom, PermissionModule.Product);

                var btnEditProduct = UITheme.CreateSecondaryButton("Edit");
                btnEditProduct.Size = new Size(90, 32);
                btnEditProduct.Location = new Point(btnEditBom.Right + 10, 6);
                btnEditProduct.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a product first."); return; }
                    long pid = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                    if (ShowProductFormDialog(dlg, productCtrl, new RawMaterialController(), pid) == DialogResult.OK)
                        loadProducts();
                };
                PermissionGuard.ApplyEditButton(btnEditProduct, PermissionModule.Product);

                bomBar.Controls.Add(btnViewBom);
                bomBar.Controls.Add(btnEditBom);
                bomBar.Controls.Add(btnEditProduct);
                detailPanel.Controls.Add(bomBar);
                split.Panel2.Controls.Add(detailPanel);
                loadProducts();

                dlg.Controls.Add(split);
                dlg.Shown += (s, e) =>
                {
                    try
                    {
                        int panel2Min = Math.Min(280, Math.Max(180, split.Width / 4));
                        int panel1Min = Math.Min(360, Math.Max(240, split.Width / 3));
                        int maxDistance = split.Width - panel2Min - split.SplitterWidth;
                        int target = (int)(split.Width * 0.58);

                        if (maxDistance >= panel1Min)
                            split.SplitterDistance = Math.Max(panel1Min, Math.Min(maxDistance, target));

                        split.Panel1MinSize = panel1Min;
                        split.Panel2MinSize = panel2Min;

                        GridHelper.ConfigureProductCatalogueGrid(grid);
                    }
                    catch { }
                };
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

                dt = DictionaryUIHelper.LoadWithStatusLabels(() => dt, "Status", DictionaryService.Categories.Production);

                int? statusCode = DictionaryUIHelper.GetFilterStatusCode(_statusFilter);
                if (dt != null && statusCode.HasValue)
                {
                    dt.DefaultView.RowFilter = "[Status] = " + statusCode.Value;
                    dt = dt.DefaultView.ToTable();
                }
                _grid.DataSource = dt;
                GridHelper.StyleGrid(_grid);
            }
            catch { }
        }

        private long? GetSelectedProductionOrderId() =>
            _grid?.CurrentRow == null ? (long?)null : GetProductionOrderIdFromRow(_grid.CurrentRow);

        private static long? GetProductionOrderIdFromRow(DataGridViewRow row)
        {
            if (row == null) return null;
            long id = GridHelper.TryGetRowLongId(row.DataGridView, row, "Production Order ID", "ID");
            return id > 0 ? id : (long?)null;
        }

        private void ShowProductionOrderForm(long? productionOrderId, bool readOnly)
        {
            bool isEdit = productionOrderId.HasValue;
            ProductionOrder existing = isEdit ? _productionCtrl.GetById(productionOrderId.Value) : null;
            if (isEdit && existing == null)
            {
                UITheme.ShowWarning("Production order not found.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = readOnly
                    ? "Production Order — " + (existing?.ProductionOrderCode ?? "")
                    : isEdit ? "Edit Production Order" : "New Production Order";
                dlg.Size = new Size(960, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12) };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5 };
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                ComboBox cmbSalesOrder = null;
                ComboBox cmbStaff = null;
                var lblOrderCode = new Label { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UITheme.TextDark };
                var lblSalesOrder = new Label { AutoSize = true, ForeColor = UITheme.TextDark };
                var lblStaff = new Label { AutoSize = true, ForeColor = UITheme.TextDark };

                if (!isEdit && !readOnly)
                {
                    cmbSalesOrder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 400 };
                    try
                    {
                        cmbSalesOrder.DataSource = _productionCtrl.GetSalesOrdersForProductionPicker();
                        cmbSalesOrder.DisplayMember = "DisplayText";
                        cmbSalesOrder.ValueMember = "Sales Order ID";
                    }
                    catch { }

                    cmbStaff = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
                    try
                    {
                        cmbStaff.DataSource = _productionCtrl.GetStaffForPicker();
                        cmbStaff.DisplayMember = "DisplayText";
                        cmbStaff.ValueMember = "Staff ID";
                        long currentStaff = AppSession.CurrentUser?.StaffID ?? 1;
                        try { cmbStaff.SelectedValue = currentStaff; } catch { }
                    }
                    catch { }
                }
                else
                {
                    lblOrderCode.Text = existing.ProductionOrderCode ?? "";
                    try
                    {
                        var soDt = _productionCtrl.GetHeaderDetail(existing.ProductionOrderID);
                        if (soDt != null && soDt.Rows.Count > 0 && soDt.Columns.Contains("Sales Order"))
                            lblSalesOrder.Text = soDt.Rows[0]["Sales Order"]?.ToString() ?? ("SO-" + existing.SalesOrderID);
                    }
                    catch { lblSalesOrder.Text = "SO-" + existing.SalesOrderID; }
                    try
                    {
                        var staff = new StaffController().GetById(existing.StaffID);
                        lblStaff.Text = staff != null ? staff.Username + " (" + staff.FirstName + " " + staff.LastName + ")" : existing.StaffID.ToString();
                    }
                    catch { lblStaff.Text = existing.StaffID.ToString(); }
                }

                var dtpFinish = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Value = existing?.EstFinishDate ?? DateTime.Today.AddDays(14),
                    Enabled = !readOnly
                };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !readOnly };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Production, existing?.Status ?? 0);
                var txtRemark = new TextBox
                {
                    Multiline = true,
                    Height = 56,
                    Text = existing?.Remark ?? "",
                    ReadOnly = readOnly
                };

                int row = 0;
                if (isEdit)
                    UITheme.AddFormRow(form, row++, "Order Code", lblOrderCode);
                UITheme.AddFormRow(form, row++, "Sales Order *", (!isEdit && !readOnly) ? (Control)cmbSalesOrder : lblSalesOrder);
                UITheme.AddFormRow(form, row++, "Staff *", (!isEdit && !readOnly) ? (Control)cmbStaff : lblStaff);
                UITheme.AddFormRow(form, row++, "Est. Finish Date *", dtpFinish);
                UITheme.AddFormRow(form, row++, "Status", cmbStatus);
                UITheme.AddFormRow(form, row, "Remark", txtRemark);

                var lineGrid = BuildProductionLineGrid(viewOnly: readOnly);
                Action loadLines = () =>
                {
                    DataTable lines = null;
                    try
                    {
                        if (isEdit)
                            lines = _productionCtrl.GetLinesForEditor(existing.ProductionOrderID);
                        else
                        {
                            long soId = GetComboLongId(cmbSalesOrder, "Sales Order ID");
                            if (soId > 0)
                                lines = _productionCtrl.GetLinesTemplateFromSalesOrder(soId);
                        }
                    }
                    catch { }
                    LoadProductionLinesToGrid(lineGrid, lines);
                };
                if (cmbSalesOrder != null)
                    cmbSalesOrder.SelectedIndexChanged += (s, e) => loadLines();
                dlg.Shown += (s, e) => loadLines();

                root.Controls.Add(form, 0, 0);
                string lineHint = readOnly
                    ? "Select a product line and click View BOM to inspect materials."
                    : "Click Production Qty column to edit. Max recommended: Need Mfg per line.";
                root.Controls.Add(WrapEditableGrid(lineGrid, lineHint), 0, 1);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Create");
                var btnClose = UITheme.CreateSecondaryButton(readOnly ? "Close" : "Cancel");
                btnClose.Click += (s, e) => dlg.Close();
                if (readOnly)
                {
                    btnSave.Visible = false;
                }
                else
                {
                    btnSave.Click += (s, e) =>
                    {
                        try
                        {
                            var lineData = ReadProductionLinesFromGrid(lineGrid);
                            if (lineData.Count == 0)
                            {
                                UITheme.ShowWarning("At least one product line with production qty > 0 is required.");
                                return;
                            }

                            if (isEdit)
                            {
                                existing.EstFinishDate = dtpFinish.Value;
                                existing.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                                existing.Remark = txtRemark.Text.Trim();
                                _productionCtrl.UpdateWithLines(existing, lineData);
                                UITheme.ShowSuccess("Production order updated.");
                            }
                            else
                            {
                                long soId = GetComboLongId(cmbSalesOrder, "Sales Order ID");
                                long staffId = GetComboLongId(cmbStaff, "Staff ID");
                                if (soId <= 0 || staffId <= 0)
                                {
                                    UITheme.ShowWarning("Please select sales order and staff.");
                                    return;
                                }
                                var po = new ProductionOrder
                                {
                                    ProductionOrderCode = "PO-TEMP",
                                    SalesOrderID = soId,
                                    StaffID = staffId,
                                    EstFinishDate = dtpFinish.Value,
                                    Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus),
                                    Remark = txtRemark.Text.Trim()
                                };
                                long newId = _productionCtrl.CreateWithLines(po, lineData);
                                UITheme.ShowSuccess("Production order PO-" + newId + " created.");
                            }
                            dlg.DialogResult = DialogResult.OK;
                            dlg.Close();
                            LoadData(_searchBox?.Text?.Trim());
                        }
                        catch (Exception ex) { UITheme.ShowError(ex.Message); }
                    };
                }

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                if (readOnly)
                {
                    var btnViewBom = UITheme.CreateSecondaryButton("View BOM");
                    btnViewBom.Click += (s, e) =>
                    {
                        if (lineGrid.CurrentRow == null || lineGrid.CurrentRow.IsNewRow)
                        {
                            UITheme.ShowWarning("Please select a product line first.");
                            return;
                        }
                        if (!long.TryParse(lineGrid.CurrentRow.Cells["ProductID"].Value?.ToString(), out long pid) || pid <= 0)
                        {
                            UITheme.ShowWarning("Invalid product on selected line.");
                            return;
                        }
                        ShowProductBomViewDialog(dlg, pid);
                    };
                    btnPanel.Controls.Add(btnViewBom);
                }
                if (!readOnly) btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        public static void ShowProductBomViewDialog(Control owner, long productId)
        {
            var productCtrl = new ProductController();
            DataTable lines = null;
            try { lines = productCtrl.GetBomLinesDetailed(productId); } catch { }
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            try
            {
                var p = productCtrl.GetById(productId);
                fields.Rows.Add("Product Code", p?.ProductCode ?? "");
                fields.Rows.Add("Style Number", p?.StyleNumber ?? "");
                fields.Rows.Add("Category", p?.Category ?? "");
            }
            catch { }
            DetailViewHelper.ShowDetail(owner.FindForm() ?? owner, $"Product BOM — ID: {productId}", fields, lines, $"ProductBOM_{productId}");
        }

        private static Panel WrapEditableGrid(DataGridView grid, string hint)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var lbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = hint,
                ForeColor = UITheme.TextGray,
                Padding = new Padding(0, 0, 0, 4)
            };
            grid.Dock = DockStyle.Fill;
            panel.Controls.Add(grid);
            panel.Controls.Add(lbl);
            return panel;
        }

        private static DataGridView BuildProductionLineGrid(bool viewOnly)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                ReadOnly = false,
                EditMode = viewOnly ? DataGridViewEditMode.EditProgrammatically : DataGridViewEditMode.EditOnEnter
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "Product", ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderQty", HeaderText = "Order Qty", ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReservedQty", HeaderText = "Reserved", ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NeedMfgQty", HeaderText = "Need Mfg", ReadOnly = true });
            var qtyCol = new DataGridViewTextBoxColumn
            {
                Name = "ProductionQty",
                HeaderText = "Production Qty",
                ReadOnly = viewOnly,
                ValueType = typeof(int)
            };
            if (!viewOnly)
            {
                qtyCol.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 235);
                qtyCol.DefaultCellStyle.ForeColor = UITheme.TextDark;
            }
            grid.Columns.Add(qtyCol);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", HeaderText = "ProductID", Visible = false, ReadOnly = true });
            GridHelper.ApplyStyle(grid);
            if (!viewOnly)
            {
                grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
                grid.ReadOnly = false;
            }
            else
            {
                grid.ReadOnly = true;
            }
            return grid;
        }

        private static long GetComboLongId(ComboBox cmb, string valueMember)
        {
            if (cmb == null) return 0;

            object selected = cmb.SelectedValue;
            if (selected != null && selected != DBNull.Value)
            {
                if (selected is long longVal) return longVal;
                if (selected is int intVal) return intVal;
                if (long.TryParse(selected.ToString(), out long parsed)) return parsed;
            }

            if (cmb.SelectedItem is DataRowView rowView && !string.IsNullOrEmpty(valueMember)
                && rowView.Row.Table.Columns.Contains(valueMember))
            {
                object val = rowView[valueMember];
                if (val != null && val != DBNull.Value && long.TryParse(val.ToString(), out long id))
                    return id;
            }

            return 0;
        }

        private static void LoadProductionLinesToGrid(DataGridView grid, DataTable lines)
        {
            grid.Rows.Clear();
            if (lines == null || lines.Rows.Count == 0) return;
            foreach (DataRow row in lines.Rows)
            {
                object prodQtyObj = GetDataRowValue(row, "ProductionQty");
                int prodQty = prodQtyObj == null || prodQtyObj == DBNull.Value
                    ? 0
                    : Convert.ToInt32(Convert.ToDecimal(prodQtyObj));
                grid.Rows.Add(
                    GetDataRowValue(row, "ProductCode")?.ToString() ?? "",
                    FormatGridQty(GetDataRowValue(row, "OrderQty")),
                    FormatGridQty(GetDataRowValue(row, "ReservedQty")),
                    FormatGridQty(GetDataRowValue(row, "NeedMfgQty")),
                    prodQty,
                    GetDataRowValue(row, "ProductID") == null || GetDataRowValue(row, "ProductID") == DBNull.Value
                        ? 0
                        : Convert.ToInt64(GetDataRowValue(row, "ProductID")));
            }
        }

        private static object GetDataRowValue(DataRow row, string columnName)
        {
            if (row?.Table == null) return DBNull.Value;
            if (row.Table.Columns.Contains(columnName))
                return row[columnName];
            foreach (DataColumn col in row.Table.Columns)
            {
                if (string.Equals(col.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                    return row[col];
            }
            return DBNull.Value;
        }

        private static string FormatGridQty(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            return Convert.ToDecimal(value).ToString("N0");
        }

        private static List<(long ProductId, int ProductionQty)> ReadProductionLinesFromGrid(DataGridView grid)
        {
            var list = new List<(long ProductId, int ProductionQty)>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (!long.TryParse(row.Cells["ProductID"].Value?.ToString(), out long productId) || productId <= 0) continue;
                if (!int.TryParse(row.Cells["ProductionQty"].Value?.ToString(), out int qty))
                {
                    if (!decimal.TryParse(row.Cells["ProductionQty"].Value?.ToString(), out decimal dQty))
                        continue;
                    qty = (int)Math.Round(dQty);
                }
                if (qty > 0) list.Add((productId, qty));
            }
            return list;
        }

        private void ShowBatchCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Quick Entry — Batch Production Orders";
                dlg.Size = new Size(980, 580);
                dlg.MinimumSize = new Size(820, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1,
                    Padding = new Padding(12)
                };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                var info = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Select confirmed sales orders that still need manufacturing. Each created production order includes product lines (Need Mfg qty).",
                    ForeColor = UITheme.TextDark,
                    Padding = new Padding(0, 8, 0, 0)
                };

                var defaults = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 2,
                    Padding = new Padding(0, 4, 0, 0)
                };
                defaults.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
                defaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
                defaults.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
                defaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));

                var cmbStaff = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                try
                {
                    cmbStaff.DataSource = _productionCtrl.GetStaffForPicker();
                    cmbStaff.DisplayMember = "DisplayText";
                    cmbStaff.ValueMember = "Staff ID";
                    long currentStaff = AppSession.CurrentUser?.StaffID ?? 1;
                    try { cmbStaff.SelectedValue = currentStaff; } catch { }
                }
                catch { }

                var dtpFinish = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Value = DateTime.Today.AddDays(14),
                    Dock = DockStyle.Fill
                };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Production, 0);
                var txtRemark = new TextBox { Dock = DockStyle.Fill, Text = "Quick entry batch" };

                defaults.Controls.Add(MakeFieldLabel("Default staff"), 0, 0);
                defaults.Controls.Add(cmbStaff, 1, 0);
                defaults.Controls.Add(MakeFieldLabel("Est. finish"), 2, 0);
                defaults.Controls.Add(dtpFinish, 3, 0);
                defaults.Controls.Add(MakeFieldLabel("Status"), 0, 1);
                defaults.Controls.Add(cmbStatus, 1, 1);
                defaults.Controls.Add(MakeFieldLabel("Remark"), 2, 1);
                defaults.Controls.Add(txtRemark, 3, 1);

                var gridPanel = new Panel { Dock = DockStyle.Fill };
                var gridToolbar = new Panel { Dock = DockStyle.Top, Height = 40 };

                var queueGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    RowHeadersVisible = false,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                var btnSelectAll = UITheme.CreateSecondaryButton("Select All");
                btnSelectAll.Location = new Point(0, 4);
                var btnClearAll = UITheme.CreateSecondaryButton("Clear All");
                btnClearAll.Location = new Point(btnSelectAll.Right + 8, 4);
                var btnReload = UITheme.CreateSecondaryButton("↻ Reload Queue");
                btnReload.Location = new Point(btnClearAll.Right + 8, 4);

                var cmbAddSo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 320,
                    Location = new Point(btnReload.Right + 16, 6)
                };
                try
                {
                    cmbAddSo.DataSource = _productionCtrl.GetSalesOrdersForProductionPicker();
                    cmbAddSo.DisplayMember = "DisplayText";
                    cmbAddSo.ValueMember = "Sales Order ID";
                }
                catch { }

                var btnAddSo = UITheme.CreateSecondaryButton("+ Add Sales Order");
                btnAddSo.Location = new Point(cmbAddSo.Right + 8, 4);

                gridToolbar.Controls.Add(btnSelectAll);
                gridToolbar.Controls.Add(btnClearAll);
                gridToolbar.Controls.Add(btnReload);
                gridToolbar.Controls.Add(cmbAddSo);
                gridToolbar.Controls.Add(btnAddSo);

                DataTable queueTable = null;

                Action configureGridColumns = () =>
                {
                    if (queueGrid.Columns.Contains("SalesOrderID"))
                    {
                        queueGrid.Columns["SalesOrderID"].Visible = false;
                        queueGrid.Columns["SalesOrderID"].ReadOnly = true;
                    }
                    if (queueGrid.Columns.Contains("SoStatus"))
                    {
                        queueGrid.Columns["SoStatus"].Visible = false;
                        queueGrid.Columns["SoStatus"].ReadOnly = true;
                    }
                    if (queueGrid.Columns.Contains("Select"))
                    {
                        queueGrid.Columns["Select"].HeaderText = "Select";
                        queueGrid.Columns["Select"].FillWeight = 50;
                        queueGrid.Columns["Select"].ReadOnly = false;
                    }
                    if (queueGrid.Columns.Contains("Sales Order"))
                        queueGrid.Columns["Sales Order"].ReadOnly = true;
                    if (queueGrid.Columns.Contains("Customer"))
                        queueGrid.Columns["Customer"].ReadOnly = true;
                    if (queueGrid.Columns.Contains("Lines"))
                        queueGrid.Columns["Lines"].ReadOnly = true;
                    if (queueGrid.Columns.Contains("Need Mfg Qty"))
                        queueGrid.Columns["Need Mfg Qty"].ReadOnly = true;
                };

                Action loadQueue = () =>
                {
                    try
                    {
                        queueTable = _productionCtrl.GetPendingSalesOrdersForQuickEntry();
                        if (!queueTable.Columns.Contains("Select"))
                            queueTable.Columns.Add("Select", typeof(bool));
                        foreach (DataRow row in queueTable.Rows)
                            row["Select"] = true;

                        queueGrid.DataSource = queueTable;
                        GridHelper.ApplyStyle(queueGrid);
                        configureGridColumns();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                btnSelectAll.Click += (s, e) =>
                {
                    if (queueTable == null) return;
                    foreach (DataRow row in queueTable.Rows)
                        row["Select"] = true;
                    queueGrid.Refresh();
                };
                btnClearAll.Click += (s, e) =>
                {
                    if (queueTable == null) return;
                    foreach (DataRow row in queueTable.Rows)
                        row["Select"] = false;
                    queueGrid.Refresh();
                };
                btnReload.Click += (s, e) => loadQueue();

                btnAddSo.Click += (s, e) =>
                {
                    long soId = GetComboLongId(cmbAddSo, "Sales Order ID");
                    if (soId <= 0)
                    {
                        UITheme.ShowWarning("Please select a sales order to add.");
                        return;
                    }
                    if (queueTable == null)
                    {
                        UITheme.ShowWarning("Reload the queue first.");
                        return;
                    }
                    foreach (DataRow existing in queueTable.Rows)
                    {
                        if (Convert.ToInt64(existing["SalesOrderID"]) == soId)
                        {
                            UITheme.ShowWarning("This sales order is already in the list.");
                            return;
                        }
                    }
                    if (_productionCtrl.SalesOrderHasProductionOrder(soId))
                    {
                        UITheme.ShowWarning("This sales order already has a production order.");
                        return;
                    }

                    DataTable lines = null;
                    try { lines = _productionCtrl.GetLinesTemplateFromSalesOrder(soId); } catch { }
                    if (lines == null || lines.Rows.Count == 0)
                    {
                        UITheme.ShowWarning("Sales order has no lines requiring manufacturing.");
                        return;
                    }

                    decimal needTotal = 0;
                    foreach (DataRow line in lines.Rows)
                        needTotal += line["NeedMfgQty"] == DBNull.Value ? 0 : Convert.ToDecimal(line["NeedMfgQty"]);

                    DataTable picker = cmbAddSo.DataSource as DataTable;
                    DataRow pickerRow = null;
                    if (picker != null)
                    {
                        foreach (DataRow pr in picker.Rows)
                        {
                            if (Convert.ToInt64(pr["Sales Order ID"]) == soId)
                            {
                                pickerRow = pr;
                                break;
                            }
                        }
                    }

                    var newRow = queueTable.NewRow();
                    newRow["SalesOrderID"] = soId;
                    newRow["Sales Order"] = pickerRow?["Order Code"]?.ToString() ?? ("SO-" + soId);
                    newRow["Customer"] = pickerRow?["Customer"]?.ToString() ?? "";
                    newRow["SoStatus"] = pickerRow?["Status"] ?? 1;
                    newRow["Lines"] = lines.Rows.Count;
                    newRow["Need Mfg Qty"] = needTotal;
                    newRow["Select"] = true;
                    queueTable.Rows.Add(newRow);
                    configureGridColumns();
                };

                gridPanel.Controls.Add(queueGrid);
                gridPanel.Controls.Add(gridToolbar);

                var btnCreate = UITheme.CreatePrimaryButton("Create Selected");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnCreate.Click += (s, e) =>
                {
                    long staffId = GetComboLongId(cmbStaff, "Staff ID");
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Please select staff.");
                        return;
                    }
                    if (queueTable == null || queueTable.Rows.Count == 0)
                    {
                        UITheme.ShowWarning("No sales orders in queue. Click Reload Queue or add a sales order.");
                        return;
                    }

                    var selected = new List<DataRow>();
                    foreach (DataRow row in queueTable.Rows)
                    {
                        if (row["Select"] != DBNull.Value && Convert.ToBoolean(row["Select"]))
                            selected.Add(row);
                    }
                    if (selected.Count == 0)
                    {
                        UITheme.ShowWarning("Please select at least one sales order.");
                        return;
                    }

                    int status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                    string remark = txtRemark.Text.Trim();
                    DateTime estFinish = dtpFinish.Value.Date;
                    var created = new List<string>();
                    var failed = new List<string>();

                    foreach (DataRow row in selected)
                    {
                        long soId = Convert.ToInt64(row["SalesOrderID"]);
                        string soLabel = row["Sales Order"]?.ToString() ?? ("SO-" + soId);
                        if (_productionCtrl.SalesOrderHasProductionOrder(soId))
                        {
                            failed.Add(soLabel + ": production order already exists.");
                            continue;
                        }

                        int soStatus = row["SoStatus"] == DBNull.Value ? 1 : Convert.ToInt32(row["SoStatus"]);
                        try
                        {
                            long poId = _productionCtrl.CreateFromSalesOrder(
                                soId,
                                staffId,
                                estFinish,
                                string.IsNullOrWhiteSpace(remark) ? null : remark,
                                advanceSalesOrderToProcessing: soStatus == 1,
                                status: status);
                            created.Add(soLabel + " → PO-" + poId);
                        }
                        catch (Exception ex)
                        {
                            failed.Add(soLabel + ": " + ex.Message);
                        }
                    }

                    if (created.Count == 0 && failed.Count > 0)
                    {
                        UITheme.ShowError("No production orders were created.\n\n" + string.Join("\n", failed));
                        return;
                    }

                    string summary = created.Count + " production order(s) created.";
                    if (created.Count > 0)
                        summary += "\n\n" + string.Join("\n", created);
                    if (failed.Count > 0)
                        summary += "\n\nFailed (" + failed.Count + "):\n" + string.Join("\n", failed);

                    UITheme.ShowSuccess(summary);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadData(_searchBox?.Text?.Trim());
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnCreate);
                btnPanel.Controls.Add(btnCancel);

                root.Controls.Add(info, 0, 0);
                root.Controls.Add(defaults, 0, 1);
                root.Controls.Add(gridPanel, 0, 2);

                dlg.Controls.Add(btnPanel);
                dlg.Controls.Add(root);

                loadQueue();
                dlg.ShowDialog(this);
            }
        }

        private static Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 9)
            };
        }

        private void ShowAddProductDialog()
        {
            ShowProductFormDialog(this, _productCtrl, _rawMaterialCtrl, null);
        }

        public static DialogResult ShowProductFormDialog(Control owner, ProductController productCtrl, RawMaterialController rawMaterialCtrl, long? productId)
        {
            bool isEdit = productId.HasValue;
            if (isEdit)
            {
                if (!PermissionGuard.Ensure(PermissionModule.Product, PermissionAction.Edit, owner.FindForm() ?? owner))
                    return DialogResult.Cancel;
            }
            else if (!PermissionGuard.Ensure(PermissionModule.Product, PermissionAction.Create, owner.FindForm() ?? owner))
            {
                return DialogResult.Cancel;
            }

            Product existing = isEdit ? productCtrl.GetById(productId.Value) : null;
            if (isEdit && existing == null)
            {
                UITheme.ShowWarning("Product not found.");
                return DialogResult.Cancel;
            }

            byte[] selectedImageBytes = null;
            DialogResult result = DialogResult.Cancel;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? $"Edit Product — {existing.ProductCode}" : "New Product";
                dlg.Size = new Size(760, 720);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(680, 620);
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 308));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, Padding = new Padding(4, 0, 4, 0) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int i = 0; i < 9; i++)
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

                var txtCode = new TextBox { Dock = DockStyle.Fill };
                var txtCategory = new TextBox { Dock = DockStyle.Fill };
                var txtStyle = new TextBox { Dock = DockStyle.Fill };
                var txtSize = new TextBox { Dock = DockStyle.Fill };
                var txtColor = new TextBox { Dock = DockStyle.Fill };
                var txtUnit = new TextBox { Dock = DockStyle.Fill };
                var txtPrice = new TextBox { Dock = DockStyle.Fill, Text = "0" };
                var txtStatus = new TextBox { Dock = DockStyle.Fill, Text = "1" };
                var txtImageUrl = new TextBox { Dock = DockStyle.Fill };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category", txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number", txtStyle);
                UITheme.AddFormField(layout, 3, "Size", txtSize);
                UITheme.AddFormField(layout, 4, "Color", txtColor);
                UITheme.AddFormField(layout, 5, "Unit", txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price", txtPrice);
                UITheme.AddFormField(layout, 7, "Status", txtStatus);
                UITheme.AddFormField(layout, 8, "Image URL", txtImageUrl);

                if (isEdit)
                {
                    txtCode.Text = existing.ProductCode ?? "";
                    txtCategory.Text = existing.Category ?? "";
                    txtStyle.Text = existing.StyleNumber ?? "";
                    txtSize.Text = existing.Size ?? "";
                    txtColor.Text = existing.Color ?? "";
                    txtUnit.Text = existing.Unit ?? "";
                    txtPrice.Text = existing.BasePriceByCurrency.ToString("0.##");
                    txtStatus.Text = existing.Status.ToString();
                    try { txtImageUrl.Text = productCtrl.GetProductImageUrl(existing.ProductID) ?? ""; } catch { }
                }

                var picBox = new PictureBox
                {
                    Size = new Size(120, 90),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.WhiteSmoke,
                    Margin = new Padding(0)
                };
                var btnUpload = UITheme.CreateSecondaryButton("Upload Image");
                btnUpload.Size = new Size(120, 32);
                btnUpload.Margin = new Padding(8, 0, 0, 0);
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

                var imgRow = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = false,
                    Padding = new Padding(0, 2, 0, 4),
                    Margin = new Padding(0)
                };
                imgRow.Controls.Add(picBox);
                imgRow.Controls.Add(btnUpload);

                var imageSection = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(4, 4, 4, 4),
                    Margin = new Padding(0)
                };
                imageSection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                imageSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                imageSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                var lblImage = new Label
                {
                    Text = "Local Image",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.TopLeft,
                    Padding = new Padding(0, 6, 0, 0),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = UITheme.TextDark
                };
                imageSection.Controls.Add(lblImage, 0, 0);
                imageSection.Controls.Add(imgRow, 1, 0);

                if (isEdit && !string.IsNullOrWhiteSpace(txtImageUrl.Text) &&
                    (txtImageUrl.Text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     txtImageUrl.Text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        using (var wc = new System.Net.WebClient())
                        using (var ms = new System.IO.MemoryStream(wc.DownloadData(txtImageUrl.Text.Trim())))
                            picBox.Image = Image.FromStream(ms);
                    }
                    catch { }
                }

                var sectionGap = new Panel { Dock = DockStyle.Fill, Height = 12 };

                var bomGrid = BuildEditableBomLineGrid(rawMaterialCtrl);
                bomGrid.Dock = DockStyle.Fill;
                if (isEdit)
                    LoadBomLinesToGrid(bomGrid, productCtrl.GetBomLinesInternal(existing.ProductID));
                else if (bomGrid.Rows.Count == 0)
                    bomGrid.Rows.Add();

                var bomSection = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
                var lblBom = new Label
                {
                    Text = "Bill of Materials (optional)",
                    Dock = DockStyle.Top,
                    Height = 26,
                    Padding = new Padding(4, 0, 0, 0),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = UITheme.TextDark
                };
                var bomToolbar = new Panel { Dock = DockStyle.Top, Height = 36 };
                var btnAddBomLine = UITheme.CreateSecondaryButton("+ Material Line");
                btnAddBomLine.Location = new Point(0, 4);
                btnAddBomLine.Click += (s, e) => bomGrid.Rows.Add();
                bomToolbar.Controls.Add(btnAddBomLine);

                bomSection.Controls.Add(bomGrid);
                bomSection.Controls.Add(bomToolbar);
                bomSection.Controls.Add(lblBom);

                root.Controls.Add(layout, 0, 0);
                root.Controls.Add(imageSection, 0, 1);
                root.Controls.Add(sectionGap, 0, 2);
                root.Controls.Add(bomSection, 0, 3);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text))
                    {
                        UITheme.ShowWarning("Product Code is required.");
                        return;
                    }
                    if (!TryReadBomLinesFromGrid(bomGrid, out var bomLines, out string bomError))
                    {
                        UITheme.ShowWarning(bomError);
                        return;
                    }
                    try
                    {
                        if (isEdit)
                        {
                            existing.ProductCode = txtCode.Text.Trim();
                            existing.Category = txtCategory.Text.Trim();
                            existing.StyleNumber = txtStyle.Text.Trim();
                            existing.Size = txtSize.Text.Trim();
                            existing.Color = txtColor.Text.Trim();
                            existing.Unit = txtUnit.Text.Trim();
                            existing.BasePriceByCurrency = string.IsNullOrEmpty(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text);
                            existing.Status = string.IsNullOrEmpty(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text);
                            if (!productCtrl.Update(existing))
                            {
                                UITheme.ShowError("Failed to update product.");
                                return;
                            }
                            productCtrl.UpsertProductImageUrl(existing.ProductID, txtImageUrl.Text.Trim());
                            if (!productCtrl.ReplaceBomLines(existing.ProductID, bomLines))
                            {
                                UITheme.ShowError("Product saved but BOM update failed.");
                                return;
                            }
                            UITheme.ShowSuccess("Product updated successfully.");
                        }
                        else
                        {
                            productCtrl.InsertWithBom(new Product
                            {
                                ProductCode = txtCode.Text.Trim(),
                                Category = txtCategory.Text.Trim(),
                                StyleNumber = txtStyle.Text.Trim(),
                                Size = txtSize.Text.Trim(),
                                Color = txtColor.Text.Trim(),
                                Unit = txtUnit.Text.Trim(),
                                BasePriceByCurrency = string.IsNullOrEmpty(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text),
                                Status = string.IsNullOrEmpty(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text),
                                ProductImage = selectedImageBytes
                            }, bomLines, txtImageUrl.Text.Trim());
                            UITheme.ShowSuccess("Product added successfully.");
                        }
                        result = DialogResult.OK;
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(owner);
            }
            return result;
        }

        public static void ShowBomEditorDialog(Control owner, long productId, string productCode = null)
        {
            if (!PermissionGuard.Ensure(PermissionModule.Product, PermissionAction.Edit, owner.FindForm() ?? owner)) return;

            var productCtrl = new ProductController();
            var rawMaterialCtrl = new RawMaterialController();
            string titleCode = productCode;
            if (string.IsNullOrWhiteSpace(titleCode))
            {
                try { titleCode = productCtrl.GetById(productId)?.ProductCode; } catch { }
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit BOM — {titleCode ?? ("ID " + productId)}";
                dlg.Size = new Size(640, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var lblHint = new Label
                {
                    Text = "Select raw material and enter quantity needed per finished unit.",
                    Dock = DockStyle.Top,
                    Height = 28,
                    Padding = new Padding(12, 8, 12, 0),
                    ForeColor = UITheme.TextGray
                };

                var toolbar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 6, 12, 0) };
                var bomGrid = BuildEditableBomLineGrid(rawMaterialCtrl);
                bomGrid.Dock = DockStyle.Fill;
                LoadBomLinesToGrid(bomGrid, productCtrl.GetBomLinesInternal(productId));

                var btnAddLine = UITheme.CreateSecondaryButton("+ Material Line");
                btnAddLine.Click += (s, e) => bomGrid.Rows.Add();
                toolbar.Controls.Add(btnAddLine);

                var btnSave = UITheme.CreatePrimaryButton("Save BOM");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!TryReadBomLinesFromGrid(bomGrid, out var lines, out string error))
                    {
                        UITheme.ShowWarning(error);
                        return;
                    }
                    try
                    {
                        if (!productCtrl.ReplaceBomLines(productId, lines))
                        {
                            UITheme.ShowError("Failed to save BOM.");
                            return;
                        }
                        UITheme.ShowSuccess("BOM updated successfully.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(bomGrid);
                dlg.Controls.Add(toolbar);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(owner);
            }
        }

        private DataGridView BuildEditableBomLineGrid() => BuildEditableBomLineGrid(_rawMaterialCtrl);

        private static DataGridView BuildEditableBomLineGrid(RawMaterialController rawMaterialCtrl)
        {
            var grid = GridHelper.CreateStyledGrid();
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var rmCol = new DataGridViewComboBoxColumn
            {
                Name = "RawMaterial",
                HeaderText = "Raw Material",
                DisplayMember = "Raw Material Code",
                ValueMember = "Raw Material ID",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            BindBomMaterialCombo(rmCol, rawMaterialCtrl);
            grid.Columns.Add(rmCol);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NeedQty",
                HeaderText = "Need Qty",
                FillWeight = 80
            });
            return grid;
        }

        private static void BindBomMaterialCombo(DataGridViewComboBoxColumn rmCol, RawMaterialController rawMaterialCtrl)
        {
            try { rmCol.DataSource = rawMaterialCtrl.GetAllRawMaterials(); } catch { }
        }

        private static void LoadBomLinesToGrid(DataGridView grid, DataTable lines)
        {
            grid.Rows.Clear();
            if (lines == null) return;
            foreach (DataRow row in lines.Rows)
            {
                if (row["rawMaterialID"] == DBNull.Value) continue;
                long rmId = Convert.ToInt64(row["rawMaterialID"]);
                decimal qty = row["rawMaterialNeedQty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["rawMaterialNeedQty"]);
                int idx = grid.Rows.Add();
                grid.Rows[idx].Cells["RawMaterial"].Value = rmId;
                grid.Rows[idx].Cells["NeedQty"].Value = qty.ToString("0.##");
            }
            if (grid.Rows.Count == 0)
                grid.Rows.Add();
        }

        private static bool TryReadBomLinesFromGrid(DataGridView grid, out List<(long RawMaterialId, decimal NeedQty)> lines, out string error)
        {
            lines = new List<(long, decimal)>();
            error = null;
            var seen = new HashSet<long>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                object rmVal = row.Cells["RawMaterial"]?.Value;
                string qtyText = row.Cells["NeedQty"]?.Value?.ToString()?.Trim();
                if (rmVal == null || rmVal == DBNull.Value)
                {
                    if (string.IsNullOrWhiteSpace(qtyText)) continue;
                    error = "Please select a raw material for each BOM line.";
                    return false;
                }
                if (!long.TryParse(rmVal.ToString(), out long rmId) || rmId <= 0)
                {
                    error = "Invalid raw material selection.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(qtyText) || !decimal.TryParse(qtyText, out decimal qty) || qty <= 0)
                {
                    error = "Need Qty must be greater than zero.";
                    return false;
                }
                if (!seen.Add(rmId))
                {
                    error = "Duplicate raw material in BOM lines.";
                    return false;
                }
                lines.Add((rmId, qty));
            }
            return true;
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

                var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
                var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
                btnRefresh.Location = new Point(8, 9);
                btnRefresh.Click += (s, e) => { try { grid.DataSource = _rmrnCtrl.GetAllRequestNotes(); GridHelper.StyleGrid(grid); } catch { } };

                var btnDetail = UITheme.CreateSecondaryButton("View Detail");
                btnDetail.Location = new Point(btnRefresh.Right + 10, 9);
                btnDetail.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a request note first."); return; }
                    long id = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                    DataTable header = null;
                    DataTable lines = null;
                    try { header = _rmrnCtrl.GetHeaderDetail(id); } catch { }
                    try { lines = _rmrnCtrl.GetRequestLines(id); } catch { }
                    var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
                    DetailViewHelper.ShowDetail(dlg, $"RM Request Note Detail — ID: {id}", fields, lines, $"RMRequest_{id}");
                };

                grid.CellDoubleClick += (s, e) =>
                {
                    if (e.RowIndex < 0) return;
                    if (grid.Rows[e.RowIndex].Cells[0].Value == null) return;
                    grid.CurrentCell = grid.Rows[e.RowIndex].Cells[0];
                    btnDetail.PerformClick();
                };

                toolbar.Controls.Add(btnRefresh);
                toolbar.Controls.Add(btnDetail);

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Dock = DockStyle.Bottom;
                btnClose.Click += (s, e) => dlg.Close();

                dlg.Controls.Add(toolbar);
                dlg.Controls.Add(grid);
                dlg.Controls.Add(FilterBlockHelper.CreateFilterBlock(grid, "RM Request Filters"));
                dlg.Controls.Add(btnClose);
                dlg.ShowDialog(this);
            }
        }
    }
}
