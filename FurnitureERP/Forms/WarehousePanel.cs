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

        private SplitContainer _warehouseSplit;
        private DataGridView _productStockGrid;
        private DataGridView _rmStockGrid;
        private Label _lblSelectedWarehouse;

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
            var btnDetailWarehouse = UITheme.CreateSecondaryButton("View Stock");
            btnDetailWarehouse.Location = new Point(btnRefresh.Right + 10, 8);
            btnDetailWarehouse.Click += (s, e) =>
            {
                long? id = GetSelectedWarehouseId();
                if (!id.HasValue) { UITheme.ShowWarning("Please select a warehouse first."); return; }
                ShowWarehouseStockDialog(id.Value);
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

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetailWarehouse);
            toolbar.Controls.Add(btnEditWarehouse);

            _grid = GridHelper.CreateStyledGrid();
            _grid.SelectionChanged += (s, e) => LoadSelectedWarehouseStock();
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            _warehouseSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 360
            };

            var listPanel = new Panel { Dock = DockStyle.Fill };
            listPanel.Controls.Add(_grid);
            listPanel.Controls.Add(FilterBlockHelper.CreateFilterBlock(_grid, "Warehouse Filters", null));
            _warehouseSplit.Panel1.Controls.Add(listPanel);

            _lblSelectedWarehouse = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Select a warehouse to view stock.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(8, 8, 0, 0)
            };

            var stockTabs = new TabControl { Dock = DockStyle.Fill };
            var productTab = new TabPage("Products") { BackColor = UITheme.Background };
            _productStockGrid = GridHelper.CreateStyledGrid();
            _productStockGrid.Dock = DockStyle.Fill;
            _productStockGrid.ReadOnly = true;
            productTab.Controls.Add(_productStockGrid);

            var rmTab = new TabPage("Raw Materials") { BackColor = UITheme.Background };
            _rmStockGrid = GridHelper.CreateStyledGrid();
            _rmStockGrid.Dock = DockStyle.Fill;
            _rmStockGrid.ReadOnly = true;
            var rmHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            rmHeader.Controls.Add(StockAlertHelper.CreateLegendLabel());
            rmTab.Controls.Add(_rmStockGrid);
            rmTab.Controls.Add(rmHeader);

            stockTabs.TabPages.Add(productTab);
            stockTabs.TabPages.Add(rmTab);

            var stockPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8) };
            stockPanel.Controls.Add(stockTabs);
            stockPanel.Controls.Add(_lblSelectedWarehouse);
            _warehouseSplit.Panel2.Controls.Add(stockPanel);

            var warehouseContent = new Panel { Dock = DockStyle.Fill };
            warehouseContent.Controls.Add(_warehouseSplit);
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
            var btnPrintDelivery = UITheme.CreateSecondaryButton("Print DN PDF");
            btnPrintDelivery.Location = new Point(btnEditDelivery.Right + 10, 8);
            btnPrintDelivery.Click += (s, e) =>
            {
                if (_deliveryGrid?.CurrentRow == null)
                {
                    UITheme.ShowWarning("Please select a delivery note first.");
                    return;
                }
                PrintDeliveryNotePdf(Convert.ToInt64(_deliveryGrid.CurrentRow.Cells[0].Value));
            };
            var btnPrintReplySlip = UITheme.CreateSecondaryButton("Print Reply Slip");
            btnPrintReplySlip.Location = new Point(btnPrintDelivery.Right + 10, 8);
            btnPrintReplySlip.Click += (s, e) =>
            {
                if (_deliveryGrid?.CurrentRow == null)
                {
                    UITheme.ShowWarning("Please select a delivery note first.");
                    return;
                }
                PrintReplySlipPdf(Convert.ToInt64(_deliveryGrid.CurrentRow.Cells[0].Value));
            };
            deliveryToolbar.Controls.Add(btnNewDelivery);
            deliveryToolbar.Controls.Add(btnRefreshDelivery);
            deliveryToolbar.Controls.Add(btnDetailDelivery);
            deliveryToolbar.Controls.Add(btnConfirmDelivery);
            deliveryToolbar.Controls.Add(btnEditDelivery);
            deliveryToolbar.Controls.Add(btnPrintDelivery);
            deliveryToolbar.Controls.Add(btnPrintReplySlip);

            _deliveryGrid = GridHelper.CreateStyledGrid();
            _deliveryGrid.CellDoubleClick += DeliveryGrid_CellDoubleClick;

            var deliveryContent = new Panel { Dock = DockStyle.Fill };
            deliveryContent.Controls.Add(_deliveryGrid);
            deliveryContent.Controls.Add(FilterBlockHelper.CreateFilterBlock(_deliveryGrid, "Delivery Note Filters", DictionaryService.Categories.Delivery));
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
                _grid.DataSource = _warehouseCtrl.GetAllWarehouses();
                GridHelper.StyleGrid(_grid);
                LoadSelectedWarehouseStock();
            }
            catch { }
        }

        private void LoadDeliveryNotes()
        {
            try
            {
                var dt = DictionaryService.DecorateShipMethodColumn(
                    _deliveryCtrl.GetAllDeliveryNotes());
                dt = GridHelper.DecorateStatusTable(dt, "Status", DictionaryService.Categories.Delivery);
                GridHelper.BindStatusData(_deliveryGrid, dt, DictionaryService.Categories.Delivery);
                if (_deliveryGrid.Columns.Contains("Delivery Note ID"))
                    _deliveryGrid.Columns["Delivery Note ID"].Visible = false;
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to load delivery notes: " + ex.Message);
            }
        }

        private long? GetSelectedWarehouseId()
        {
            if (_grid?.CurrentRow == null) return null;
            long id = GridHelper.TryGetRowLongId(_grid, _grid.CurrentRow, "Warehouse ID", "ID");
            return id > 0 ? id : (long?)null;
        }

        private void LoadSelectedWarehouseStock()
        {
            long? id = GetSelectedWarehouseId();
            if (!id.HasValue)
            {
                _lblSelectedWarehouse.Text = "Select a warehouse to view stock.";
                _productStockGrid.DataSource = null;
                _rmStockGrid.DataSource = null;
                return;
            }

            var wh = _warehouseCtrl.GetById(id.Value);
            _lblSelectedWarehouse.Text = wh != null
                ? $"Stock — {wh.WarehouseName} (ID {id.Value})"
                : $"Stock — Warehouse ID {id.Value}";

            try
            {
                _productStockGrid.DataSource = _warehouseCtrl.GetWarehouseProducts(id.Value, StockAlertHelper.DefaultProductMinStock);
                GridHelper.StyleGrid(_productStockGrid);
            }
            catch { _productStockGrid.DataSource = null; }

            try
            {
                _rmStockGrid.DataSource = _warehouseCtrl.GetWarehouseRawMaterials(id.Value);
                GridHelper.StyleGridWithStockAlert(_rmStockGrid, "Available Qty", "Min Stock Level");
            }
            catch { _rmStockGrid.DataSource = null; }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long? id = GetSelectedWarehouseId();
            if (!id.HasValue) return;
            ShowWarehouseStockDialog(id.Value);
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

        private void ShowWarehouseStockDialog(long warehouseId)
        {
            var wh = _warehouseCtrl.GetById(warehouseId);
            using (var dlg = new Form())
            {
                dlg.Text = wh != null ? $"Warehouse Stock — {wh.WarehouseName}" : $"Warehouse Stock — ID {warehouseId}";
                dlg.Size = new Size(960, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var tabs = new TabControl { Dock = DockStyle.Fill };
                var productGrid = GridHelper.CreateStyledGrid();
                productGrid.Dock = DockStyle.Fill;
                productGrid.ReadOnly = true;
                productGrid.DataSource = _warehouseCtrl.GetWarehouseProducts(warehouseId, StockAlertHelper.DefaultProductMinStock);
                GridHelper.StyleGrid(productGrid);

                var rmGrid = GridHelper.CreateStyledGrid();
                rmGrid.Dock = DockStyle.Fill;
                rmGrid.ReadOnly = true;
                rmGrid.DataSource = _warehouseCtrl.GetWarehouseRawMaterials(warehouseId);
                GridHelper.StyleGridWithStockAlert(rmGrid, "Available Qty", "Min Stock Level");

                var productPage = new TabPage("Products");
                productPage.Controls.Add(productGrid);
                var rmPage = new TabPage("Raw Materials");
                var rmLegendPanel = new Panel { Dock = DockStyle.Top, Height = 28 };
                rmLegendPanel.Controls.Add(StockAlertHelper.CreateLegendLabel());
                rmPage.Controls.Add(rmGrid);
                rmPage.Controls.Add(rmLegendPanel);
                tabs.TabPages.Add(productPage);
                tabs.TabPages.Add(rmPage);

                if (wh != null)
                {
                    var header = new Label
                    {
                        Dock = DockStyle.Top,
                        Height = 40,
                        Text = $"{wh.WarehouseName}\r\n{wh.WarehouseAddress}",
                        ForeColor = UITheme.TextGray,
                        Padding = new Padding(16, 8, 16, 0)
                    };
                    dlg.Controls.Add(tabs);
                    dlg.Controls.Add(header);
                }
                else
                {
                    dlg.Controls.Add(tabs);
                }

                dlg.ShowDialog(this);
            }
        }

        private void ShowDeliveryTableDialog(DataGridViewRow row)
        {
            if (row?.Cells[0].Value == null) return;
            ShowDeliveryNoteViewDetailDialog(Convert.ToInt64(row.Cells[0].Value));
        }

        public void OpenDeliveryNoteDetail(long deliveryNoteId) => ShowDeliveryNoteViewDetailDialog(deliveryNoteId);

        private void ShowDeliveryNoteViewDetailDialog(long deliveryNoteId)
        {
            var export = BuildDeliveryNoteExportData(deliveryNoteId);
            if (export == null)
            {
                UITheme.ShowWarning("Delivery note not found.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"Delivery Note — {export.NoteCode}";
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 280
                };

                var headerGrid = GridHelper.CreateStyledGrid();
                headerGrid.DataSource = export.Fields;
                GridHelper.StyleGrid(headerGrid);

                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.DataSource = export.Lines;
                GridHelper.StyleGrid(lineGrid);

                split.Panel1.Controls.Add(headerGrid);
                split.Panel2.Controls.Add(lineGrid);

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
                var tabDetail = new TabPage("Detail");
                tabDetail.Controls.Add(split);
                split.Dock = DockStyle.Fill;
                tabs.TabPages.Add(tabDetail);
                tabs.TabPages.Add(DocumentAuditService.BuildActivityTab(DocumentAuditService.Types.DeliveryNote, deliveryNoteId));
                dlg.Controls.Add(tabs);

                var toolbar = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    Padding = new Padding(8, 8, 16, 8),
                    BackColor = UITheme.Background
                };
                var btnPrintReply = UITheme.CreatePrimaryButton("Print Reply Slip");
                btnPrintReply.Width = 150;
                btnPrintReply.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnPrintReply.Location = new Point(toolbar.Width - btnPrintReply.Width - 16, 8);
                toolbar.Resize += (s, e) => btnPrintReply.Left = Math.Max(8, toolbar.Width - btnPrintReply.Width - 16);
                btnPrintReply.Click += (s, e) =>
                {
                    try
                    {
                        if (!TryExportReplySlipPdf(deliveryNoteId, dlg))
                            UITheme.ShowWarning("No data available to print.");
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError("Failed to export reply slip PDF: " + ex.Message);
                    }
                };

                var btnPrint = UITheme.CreateSecondaryButton("Print DN PDF");
                btnPrint.Width = 130;
                btnPrint.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnPrint.Location = new Point(btnPrintReply.Left - btnPrint.Width - 8, 8);
                toolbar.Resize += (s, e) =>
                {
                    btnPrintReply.Left = Math.Max(8, toolbar.Width - btnPrintReply.Width - 16);
                    btnPrint.Left = Math.Max(8, btnPrintReply.Left - btnPrint.Width - 8);
                };
                btnPrint.Click += (s, e) =>
                {
                    try
                    {
                        if (!TryExportDeliveryNotePdf(deliveryNoteId, dlg))
                            UITheme.ShowWarning("No data available to print.");
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError("Failed to export PDF: " + ex.Message);
                    }
                };
                toolbar.Controls.Add(btnPrint);
                toolbar.Controls.Add(btnPrintReply);
                dlg.Controls.Add(toolbar);
                toolbar.BringToFront();

                dlg.ShowDialog(this);
            }
        }

        private void PrintDeliveryNotePdf(long deliveryNoteId)
        {
            try
            {
                if (!TryExportDeliveryNotePdf(deliveryNoteId, this))
                    UITheme.ShowWarning("Delivery note not found.");
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to export PDF: " + ex.Message);
            }
        }

        private bool TryExportDeliveryNotePdf(long deliveryNoteId, IWin32Window owner)
        {
            if (!TryLoadDeliveryNoteDetail(deliveryNoteId, out DataTable header, out DataTable lines, out decimal total, out int totalShipQty))
                return false;

            var pdfData = DeliveryNotePdfHelper.FromHeaderAndLines(header, lines, total, totalShipQty, $"DeliveryNote_{deliveryNoteId}");
            if (DeliveryNotePdfHelper.ExportToPdf(pdfData, owner))
            {
                UITheme.ShowSuccess("PDF saved successfully.");
                return true;
            }
            return true;
        }

        private void PrintReplySlipPdf(long deliveryNoteId)
        {
            try
            {
                if (!TryExportReplySlipPdf(deliveryNoteId, this))
                    UITheme.ShowWarning("Delivery note not found.");
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to export reply slip PDF: " + ex.Message);
            }
        }

        private bool TryExportReplySlipPdf(long deliveryNoteId, IWin32Window owner)
        {
            if (!TryLoadDeliveryNoteDetail(deliveryNoteId, out DataTable header, out DataTable lines, out decimal total, out _))
                return false;

            string slipCode = header.Rows[0].Table.Columns.Contains("Reply Slip Code")
                ? header.Rows[0]["Reply Slip Code"]?.ToString()
                : null;
            if (string.IsNullOrWhiteSpace(slipCode))
                slipCode = "ReplySlip_" + deliveryNoteId;

            var pdfData = ReplySlipPdfHelper.FromDeliveryNoteHeaderAndLines(header, lines, total, slipCode);
            if (ReplySlipPdfHelper.ExportToPdf(pdfData, owner))
            {
                UITheme.ShowSuccess("Reply slip PDF saved successfully.");
                return true;
            }
            return true;
        }

        private bool TryLoadDeliveryNoteDetail(long deliveryNoteId, out DataTable header, out DataTable lines, out decimal total, out int totalShipQty)
        {
            header = null;
            lines = null;
            total = 0;
            totalShipQty = 0;

            try { header = _deliveryCtrl.GetHeaderDetail(deliveryNoteId); } catch { }
            if (header == null || header.Rows.Count == 0)
                return false;

            try { lines = _deliveryCtrl.GetExportProductLines(deliveryNoteId); } catch { }
            try { total = _deliveryCtrl.GetTotalAmount(deliveryNoteId); } catch { }
            try { totalShipQty = _deliveryCtrl.GetTotalShipQty(deliveryNoteId); } catch { }
            return true;
        }

        private DeliveryNoteExportData BuildDeliveryNoteExportData(long deliveryNoteId)
        {
            if (!TryLoadDeliveryNoteDetail(deliveryNoteId, out DataTable header, out DataTable lines, out decimal total, out int totalShipQty))
                return null;

            AppendDeliveryNoteTotalRow(lines, total);

            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            DecorateDeliveryNoteStatusField(fields, header.Rows[0]);
            DecorateDeliveryNoteShipMethodField(fields, header.Rows[0]);
            try
            {
                fields.Rows.Add("Total Ship Qty", totalShipQty.ToString());
                fields.Rows.Add("Total Amount", total.ToString("0.00"));
            }
            catch { }

            string noteCode = header.Columns.Contains("Delivery Note Code")
                ? header.Rows[0]["Delivery Note Code"]?.ToString()
                : ("DN-" + deliveryNoteId);

            return new DeliveryNoteExportData
            {
                NoteCode = noteCode ?? ("DN-" + deliveryNoteId),
                Fields = fields,
                Lines = lines
            };
        }

        private static void DecorateDeliveryNoteStatusField(DataTable fields, DataRow headerRow)
        {
            if (fields == null || headerRow == null || !headerRow.Table.Columns.Contains("Status")) return;
            if (headerRow["Status"] == DBNull.Value) return;

            int statusCode = Convert.ToInt32(headerRow["Status"]);
            string label = DictionaryService.GetDisplayName(DictionaryService.Categories.Delivery, statusCode);
            foreach (DataRow row in fields.Rows)
            {
                if (string.Equals(row["Field"]?.ToString(), "Status", StringComparison.OrdinalIgnoreCase))
                {
                    row["Value"] = label;
                    break;
                }
            }
        }

        private static void DecorateDeliveryNoteShipMethodField(DataTable fields, DataRow headerRow)
        {
            if (fields == null || headerRow == null || !headerRow.Table.Columns.Contains("Ship Method")) return;
            if (headerRow["Ship Method"] == DBNull.Value) return;

            string label = DictionaryService.FormatShipMethod(headerRow["Ship Method"]?.ToString());
            foreach (DataRow row in fields.Rows)
            {
                if (string.Equals(row["Field"]?.ToString(), "Ship Method", StringComparison.OrdinalIgnoreCase))
                {
                    row["Value"] = label;
                    break;
                }
            }
        }

        private static void AppendDeliveryNoteTotalRow(DataTable lines, decimal total)
        {
            if (lines == null || !lines.Columns.Contains("Amount")) return;

            var totalRow = lines.NewRow();
            foreach (DataColumn col in lines.Columns)
            {
                if (col.ColumnName == "Product Code") totalRow[col] = "Total Amount";
                else if (col.ColumnName == "Amount") totalRow[col] = total;
                else if (col.DataType == typeof(string)) totalRow[col] = "";
                else totalRow[col] = DBNull.Value;
            }
            lines.Rows.Add(totalRow);
        }

        private sealed class DeliveryNoteExportData
        {
            public string NoteCode { get; set; }
            public DataTable Fields { get; set; }
            public DataTable Lines { get; set; }
        }

        private void ShowCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Warehouse";
                dlg.Size = new Size(520, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbType = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                cmbType.Items.AddRange(new object[] { "Inventory", "Production" });
                cmbType.SelectedIndex = 0;

                var txtName = new TextBox { Text = "Region - Inventory" };
                var txtAddr = new TextBox();
                var chkCreatePair = new CheckBox
                {
                    Text = "Also create paired production warehouse (Inventory only)",
                    AutoSize = true,
                    Checked = true
                };

                cmbType.SelectedIndexChanged += (s, e) =>
                {
                    bool inventory = cmbType.SelectedIndex == 0;
                    chkCreatePair.Visible = inventory;
                    if (inventory && (string.IsNullOrWhiteSpace(txtName.Text) || txtName.Text.IndexOf("Production", StringComparison.OrdinalIgnoreCase) >= 0))
                        txtName.Text = "Region - Inventory";
                    if (!inventory && (string.IsNullOrWhiteSpace(txtName.Text) || txtName.Text.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0))
                        txtName.Text = "Region - Production";
                };

                UITheme.AddFormRow(layout, 0, "Warehouse Type *", cmbType);
                UITheme.AddFormRow(layout, 1, "Warehouse Name *", txtName);
                UITheme.AddFormRow(layout, 2, "Address", txtAddr);
                layout.Controls.Add(chkCreatePair, 1, 3);

                var hint = new Label
                {
                    Text = "Use names like \"China - Inventory\" and \"China - Production\" for automatic pairing in Internal Transfer.",
                    Dock = DockStyle.Bottom,
                    Height = 42,
                    ForeColor = UITheme.TextGray,
                    Padding = new Padding(16, 0, 16, 8)
                };

                var btnSave = UITheme.CreatePrimaryButton("Save");
                PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.Warehouse);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.Warehouse, PermissionAction.Create, dlg)) return;
                    string name = txtName.Text.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        UITheme.ShowWarning("Warehouse Name is required.");
                        return;
                    }

                    bool isInventory = cmbType.SelectedIndex == 0;
                    if (isInventory && name.IndexOf("inventory", StringComparison.OrdinalIgnoreCase) < 0)
                        name += " - Inventory";
                    if (!isInventory && name.IndexOf("production", StringComparison.OrdinalIgnoreCase) < 0)
                        name += " - Production";

                    try
                    {
                        string address = txtAddr.Text.Trim();
                        long newId = _warehouseCtrl.Insert(new Warehouse
                        {
                            WarehouseName = name,
                            WarehouseAddress = address
                        });
                        _warehouseCtrl.InitializeStockRecords(newId, name);

                        long pairedId = 0;
                        if (isInventory && chkCreatePair.Checked)
                        {
                            string prodName = WarehouseHelper.BuildPairedProductionName(name);
                            pairedId = _warehouseCtrl.Insert(new Warehouse
                            {
                                WarehouseName = prodName,
                                WarehouseAddress = address
                            });
                            _warehouseCtrl.InitializeStockRecords(pairedId, prodName);
                        }

                        string msg = "Warehouse created (ID " + newId + "). Stock rows initialized.";
                        if (pairedId > 0)
                            msg += " Paired production warehouse ID " + pairedId + " also created.";
                        UITheme.ShowSuccess(msg);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex) { UITheme.ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.Controls.Add(hint);
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
                dlg.Size = new Size(520, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16) };
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

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                    LoadSelectedWarehouseStock();
                }
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
                dlg.Size = new Size(920, 700);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(800, 520);
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 300
                };

                var formLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 12,
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
                var lblReplySlipCode = new Label
                {
                    Text = isNew ? "(assigned on save)" : (string.IsNullOrWhiteSpace(dn.ReplySlipCode)
                        ? DeliveryNoteController.FormatReplySlipCodeFromDeliveryNoteCode(dn.DeliveryNoteCode) ?? "(assigned on save)"
                        : dn.ReplySlipCode),
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbCustomer = BuildCustomerCombo(dn?.CustomerID ?? 0);
                long initialCustomerId = dn?.CustomerID ?? CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);
                var cmbSalesOrder = BuildSalesOrderCombo(initialCustomerId, dn?.SalesOrderID ?? 0);
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
                var txtSignedBy = new TextBox { Text = dn?.SignedBy ?? "" };
                var dtpSignedDate = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    ShowCheckBox = true,
                    Checked = dn?.SignedDate.HasValue == true,
                    Value = dn?.SignedDate ?? DateTime.Today
                };
                string staffLabel = AppSession.CurrentUser != null
                    ? (string.IsNullOrWhiteSpace(AppSession.CurrentUser.FullName)
                        ? AppSession.CurrentUser.Username
                        : AppSession.CurrentUser.FullName)
                    : (dn != null ? dn.StaffID.ToString() : "—");
                var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };

                int row = 0;
                if (!isNew) UITheme.AddFormRow(formLayout, row++, "Delivery Note Code", lblCode);
                if (!isNew) UITheme.AddFormRow(formLayout, row++, "Reply Slip Code", lblReplySlipCode);
                UITheme.AddFormRow(formLayout, row++, "Customer *", cmbCustomer);
                UITheme.AddFormRow(formLayout, row++, "Sales Order *", cmbSalesOrder);
                UITheme.AddFormRow(formLayout, row++, "Warehouse *", cmbWarehouse);
                UITheme.AddFormRow(formLayout, row++, "Ship Method *", cmbShipMethod);
                UITheme.AddFormRow(formLayout, row++, "Tracking Number", txtTracking);
                UITheme.AddFormRow(formLayout, row++, "Staff", lblStaff);
                UITheme.AddFormRow(formLayout, row++, "Status", cmbStatus);
                UITheme.AddFormRow(formLayout, row++, "Signed By", txtSignedBy);
                UITheme.AddFormRow(formLayout, row++, "Signed Date", dtpSignedDate);
                UITheme.AddFormRow(formLayout, row, "Remark", txtRemark);

                var lineGrid = CreateDeliveryLineGrid();
                Action reloadLines = () =>
                {
                    long soId = GetComboLongId(cmbSalesOrder);
                    long warehouseId = GetComboLongId(cmbWarehouse);
                    if (soId <= 0)
                    {
                        lineGrid.DataSource = null;
                        lineGrid.ReadOnly = true;
                        return;
                    }
                    LoadDeliveryLineGrid(lineGrid, soId, isNew ? 0 : dnId, warehouseId, linesLocked);
                };

                CustomerComboHelper.WireCustomerChanged(cmbCustomer, _customerCtrl, customerId =>
                {
                    if (confirmed) return;
                    SalesOrderComboHelper.Rebind(cmbSalesOrder, _salesOrderCtrl, customerId, 0);
                    reloadLines();
                });
                cmbSalesOrder.SelectedIndexChanged += (s, e) => reloadLines();
                cmbWarehouse.SelectedIndexChanged += (s, e) => reloadLines();
                lineGrid.CellValueChanged += (s, e) =>
                {
                    if (linesLocked || e.RowIndex < 0) return;
                    if (IsShipQtyColumn(lineGrid.Columns[e.ColumnIndex]))
                        HighlightDeliveryStockRows(lineGrid);
                };

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
                        ? "Select Sales Order and Inventory Warehouse to load lines with stock. Edit Ship Qty (optional — 0 auto-fills on Confirm). Yellow = exceeds available; red = exceeds physical."
                        : "Edit Ship Qty only (≤ Remaining). Stock columns reflect selected warehouse.";
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

                    long customerId = CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);
                    long salesOrderId = SalesOrderComboHelper.ResolveSalesOrderId(cmbSalesOrder, _salesOrderCtrl);
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
                    string shipMethod = DictionaryUIHelper.GetSelectedShipMethodStoredValue(cmbShipMethod);
                    if (string.IsNullOrWhiteSpace(shipMethod))
                    {
                        UITheme.ShowWarning("Please select a ship method.");
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
                            Remark = txtRemark.Text.Trim(),
                            SignedBy = txtSignedBy.Text.Trim(),
                            SignedDate = dtpSignedDate.Checked ? dtpSignedDate.Value.Date : (DateTime?)null
                        };

                        if (isNew)
                        {
                            long newId = _deliveryCtrl.CreateWithLines(note, lines ?? Enumerable.Empty<(long, int)>());
                            if (newId <= 0)
                            {
                                UITheme.ShowError("Delivery note creation failed: invalid ID.");
                                return;
                            }
                            string dnCode = DeliveryNoteController.FormatDeliveryNoteCode(newId);
                            string rsCode = DeliveryNoteController.FormatReplySlipCode(newId);
                            UITheme.ShowSuccess(
                                $"Delivery note {dnCode} / reply slip {rsCode} created.\r\nPrint DN and Reply Slip for the driver. After customer sign-off, click Confirm Delivery.");
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
            var cmb = new ComboBox { Width = 340 };
            CustomerComboHelper.Attach(cmb, _customerCtrl, selectedCustomerId);
            return cmb;
        }

        private ComboBox BuildSalesOrderCombo(long customerId, long selectedSalesOrderId)
        {
            var cmb = new ComboBox { Width = 340 };
            SalesOrderComboHelper.Attach(cmb, _salesOrderCtrl, customerId, selectedSalesOrderId);
            return cmb;
        }

        private ComboBox BuildWarehouseCombo(long selectedWarehouseId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dt = _warehouseCtrl.GetAllWarehouses();
            if (dt == null)
            {
                cmb.DataSource = null;
                return cmb;
            }

            var filtered = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                long whId = Convert.ToInt64(row["Warehouse ID"]);
                string whName = row["Warehouse Name"]?.ToString();
                if (WarehouseHelper.IsInventoryWarehouse(whId, whName))
                    filtered.ImportRow(row);
            }

            if (!filtered.Columns.Contains("DisplayText"))
                filtered.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in filtered.Rows)
            {
                string name = row["Warehouse Name"]?.ToString();
                string addr = row["Address"]?.ToString();
                row["DisplayText"] = string.IsNullOrWhiteSpace(addr) ? name : $"{name} — {addr}";
            }

            cmb.DataSource = filtered;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Warehouse ID";
            if (selectedWarehouseId > 0)
                SetComboLongValue(cmb, selectedWarehouseId);
            else
            {
                try { cmb.SelectedValue = WarehouseHelper.DefaultInventoryWarehouseId; }
                catch { if (cmb.Items.Count > 0) cmb.SelectedIndex = 0; }
            }
            return cmb;
        }

        private static ComboBox BuildShipMethodCombo(string current = null)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            DictionaryUIHelper.BindShipMethodCombo(cmb, current);
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

        private void LoadDeliveryLineGrid(DataGridView grid, long salesOrderId, long deliveryNoteId, long warehouseId, bool readOnly)
        {
            DataTable dt;
            try
            {
                dt = _deliveryCtrl.GetLineEditorData(salesOrderId, deliveryNoteId, warehouseId);
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                UITheme.ShowError("Failed to load delivery lines: " + ex.Message);
                return;
            }

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

            HighlightDeliveryStockRows(grid);
        }

        private static void HighlightDeliveryStockRows(DataGridView grid)
        {
            if (grid?.Rows == null || grid.Rows.Count == 0) return;
            bool hasAvailable = grid.Columns.Contains("Available Qty");
            bool hasPhysical = grid.Columns.Contains("Physical Qty");

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = UITheme.TextDark;

                int shipQty = 0;
                if (grid.Columns.Contains("Ship Qty") && row.Cells["Ship Qty"].Value != null && row.Cells["Ship Qty"].Value != DBNull.Value)
                    shipQty = Convert.ToInt32(row.Cells["Ship Qty"].Value);
                if (shipQty <= 0) continue;

                int available = hasAvailable && row.Cells["Available Qty"].Value != null && row.Cells["Available Qty"].Value != DBNull.Value
                    ? Convert.ToInt32(row.Cells["Available Qty"].Value) : int.MaxValue;
                int physical = hasPhysical && row.Cells["Physical Qty"].Value != null && row.Cells["Physical Qty"].Value != DBNull.Value
                    ? Convert.ToInt32(row.Cells["Physical Qty"].Value) : int.MaxValue;

                if (shipQty > physical)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
                else if (shipQty > available)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 230);
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
            var dn = _deliveryCtrl.GetById(id);
            if (dn == null)
            {
                UITheme.ShowWarning("Delivery note not found.");
                return;
            }
            if (DeliveryNoteController.IsDeliveryConfirmed(dn.Status))
            {
                UITheme.ShowWarning("This delivery note is already confirmed.");
                return;
            }

            if (!TryPromptDeliverySignOff(dn, out string signedBy, out DateTime? signedDate))
                return;

            _deliveryCtrl.UpdateSignOff(id, signedBy, signedDate);

            var result = _inventoryWorkflow.ConfirmDelivery(id);
            if (result.Success)
            {
                UITheme.ShowSuccess(result.Message + "\r\nStatus set to Delivered.");
                LoadDeliveryNotes();
            }
            else
            {
                UITheme.ShowWarning(result.Message);
            }
        }

        private bool TryPromptDeliverySignOff(DeliveryNote dn, out string signedBy, out DateTime? signedDate)
        {
            string capturedSignedBy = dn.SignedBy;
            DateTime? capturedSignedDate = dn.SignedDate;

            using (var dlg = new Form())
            {
                dlg.Text = "Customer Sign-off";
                dlg.Size = new Size(420, 220);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 3,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSignedBy = new TextBox { Text = dn.SignedBy ?? "", Dock = DockStyle.Fill };
                var dtpSigned = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    ShowCheckBox = true,
                    Checked = dn.SignedDate.HasValue,
                    Value = dn.SignedDate ?? DateTime.Today,
                    Dock = DockStyle.Fill
                };

                UITheme.AddFormRow(layout, 0, "Signed By *", txtSignedBy);
                UITheme.AddFormRow(layout, 1, "Signed Date", dtpSigned);
                layout.Controls.Add(new Label
                {
                    Text = "Record customer sign-off from the returned reply slip, then confirm delivery.",
                    Dock = DockStyle.Fill,
                    ForeColor = UITheme.TextGray,
                    Font = new Font("Segoe UI", 8.5f)
                }, 0, 2);
                layout.SetColumnSpan(layout.GetControlFromPosition(0, 2), 2);

                var btnOk = UITheme.CreatePrimaryButton("Confirm");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnOk.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtSignedBy.Text))
                    {
                        UITheme.ShowWarning("Signed By is required.");
                        return;
                    }
                    capturedSignedBy = txtSignedBy.Text.Trim();
                    capturedSignedDate = dtpSigned.Checked ? dtpSigned.Value.Date : (DateTime?)null;
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnOk);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    signedBy = dn.SignedBy;
                    signedDate = dn.SignedDate;
                    return false;
                }

                signedBy = capturedSignedBy;
                signedDate = capturedSignedDate;
                return true;
            }
        }
    }
}
