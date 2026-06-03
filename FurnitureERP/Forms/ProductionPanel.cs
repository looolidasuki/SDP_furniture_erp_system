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

                listPanel.Controls.Add(grid);
                listPanel.Controls.Add(toolbar);

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

                var btnBom = UITheme.CreateSecondaryButton("View BOM");
                btnBom.Dock = DockStyle.Bottom;
                btnBom.Height = 36;
                btnBom.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select a product first."); return; }
                    long pid = Convert.ToInt64(grid.CurrentRow.Cells[0].Value);
                    DataTable lines = null;
                    try { lines = productCtrl.GetBomLinesDetailed(pid); } catch { }
                    var fields = new DataTable();
                    fields.Columns.Add("Field");
                    fields.Columns.Add("Value");
                    try
                    {
                        var p = productCtrl.GetById(pid);
                        fields.Rows.Add("Product Code", p?.ProductCode ?? "");
                        fields.Rows.Add("Style Number", p?.StyleNumber ?? "");
                        fields.Rows.Add("Category", p?.Category ?? "");
                    }
                    catch { }
                    DetailViewHelper.ShowDetail(dlg, $"Product BOM — ID: {pid}", fields, lines, $"ProductBOM_{pid}");
                };

                loadProducts();

                dlg.Controls.Add(listPanel);
                dlg.Controls.Add(detailPanel);
                dlg.Controls.Add(btnBom);
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
            if (row.DataGridView?.Columns.Contains("ID") == true && row.Cells["ID"].Value != null && row.Cells["ID"].Value != DBNull.Value)
                return Convert.ToInt64(row.Cells["ID"].Value);
            if (row.Cells.Count > 0 && row.Cells[0].Value != null && row.Cells[0].Value != DBNull.Value)
                return Convert.ToInt64(row.Cells[0].Value);
            return null;
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
                        else if (cmbSalesOrder?.SelectedValue != null)
                            lines = _productionCtrl.GetLinesTemplateFromSalesOrder(Convert.ToInt64(cmbSalesOrder.SelectedValue));
                    }
                    catch { }
                    LoadProductionLinesToGrid(lineGrid, lines);
                };
                loadLines();
                if (cmbSalesOrder != null)
                    cmbSalesOrder.SelectedIndexChanged += (s, e) => loadLines();

                root.Controls.Add(form, 0, 0);
                string lineHint = readOnly
                    ? "Production line quantities (read-only)."
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
                                if (cmbSalesOrder?.SelectedValue == null || cmbStaff?.SelectedValue == null)
                                {
                                    UITheme.ShowWarning("Please select sales order and staff.");
                                    return;
                                }
                                long soId = Convert.ToInt64(cmbSalesOrder.SelectedValue);
                                long staffId = Convert.ToInt64(cmbStaff.SelectedValue);
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
                if (!readOnly) btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
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
            if (viewOnly)
                grid.ReadOnly = true;
            return grid;
        }

        private static void LoadProductionLinesToGrid(DataGridView grid, DataTable lines)
        {
            grid.Rows.Clear();
            if (lines == null) return;
            foreach (DataRow row in lines.Rows)
            {
                int prodQty = row["ProductionQty"] == DBNull.Value ? 0 : Convert.ToInt32(Convert.ToDecimal(row["ProductionQty"]));
                grid.Rows.Add(
                    row["ProductCode"]?.ToString() ?? "",
                    row["OrderQty"] == DBNull.Value ? "" : Convert.ToDecimal(row["OrderQty"]).ToString("N0"),
                    row["ReservedQty"] == DBNull.Value ? "" : Convert.ToDecimal(row["ReservedQty"]).ToString("N0"),
                    row["NeedMfgQty"] == DBNull.Value ? "" : Convert.ToDecimal(row["NeedMfgQty"]).ToString("N0"),
                    prodQty,
                    row["ProductID"] == DBNull.Value ? 0 : Convert.ToInt64(row["ProductID"]));
            }
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
                var txtImageUrl = new TextBox { Dock = DockStyle.Fill };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category",       txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number",   txtStyle);
                UITheme.AddFormField(layout, 3, "Size",           txtSize);
                UITheme.AddFormField(layout, 4, "Color",          txtColor);
                UITheme.AddFormField(layout, 5, "Unit",           txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price",     txtPrice);
                UITheme.AddFormField(layout, 7, "Status",         txtStatus);
                UITheme.AddFormField(layout, 8, "Image URL",      txtImageUrl);

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
                UITheme.AddFormField(layout, 9, "Local Image", imgPanel);

                var btnSave   = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text)) { MessageBox.Show("Product Code is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        long productId = _productCtrl.Insert(new Product
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
                        if (!string.IsNullOrWhiteSpace(txtImageUrl.Text))
                            _productCtrl.UpsertProductImageUrl(productId, txtImageUrl.Text.Trim());
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
