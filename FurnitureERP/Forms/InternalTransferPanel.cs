using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class InternalTransferPanel : UserControl
    {
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly InventoryWorkflowService _inventoryWorkflow = new InventoryWorkflowService();
        private readonly ProductionMaterialWorkflowService _materialWorkflow = new ProductionMaterialWorkflowService();
        private readonly RawMaterialRequestNoteController _rmrnCtrl = new RawMaterialRequestNoteController();
        private readonly SupplierController _supplierCtrl = new SupplierController();

        private TabControl _tabs;
        private ComboBox _cmbItemType;
        private ComboBox _cmbFromWarehouse;
        private ComboBox _cmbToWarehouse;
        private DataGridView _lineGrid;
        private DataTable _lineTable;
        private Button _btnTransfer;
        private Button _btnLoad;

        private ComboBox _cmbRequestNote;
        private FilteredComboBinder _requestNoteBinder;
        private ComboBox _cmbProductionOrder;
        private FilteredComboBinder _productionOrderBinder;
        private bool _filterSyncInProgress;
        private bool _issueFiltersLoaded;
        private Timer _issuePreviewTimer;
        private ComboBox _cmbIssueInventoryWh;
        private Label _lblIssueProductionWh;
        private DataGridView _issueGrid;
        private Button _btnIssueMaterials;
        private Button _btnCreateShortagePo;

        public InternalTransferPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            ApplyPendingNavigation();
        }

        private void BuildUI()
        {
            var title = new Label
            {
                Text = "Internal Transfer",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = UITheme.Primary,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(16, 10, 0, 0)
            };

            _tabs = new TabControl { Dock = DockStyle.Fill };
            _tabs.TabPages.Add(BuildIssueRequestTab());
            _tabs.TabPages.Add(BuildFreeTransferTab());
            _tabs.SelectedIndex = 0;
            _tabs.SelectedIndexChanged += (s, e) =>
            {
                if (_tabs.SelectedIndex == 0)
                    EnsureIssueFiltersLoaded();
            };

            VisibleChanged += (s, e) =>
            {
                if (Visible)
                    EnsureIssueFiltersLoaded();
            };

            Controls.Add(_tabs);
            Controls.Add(title);
        }

        private void ApplyPendingNavigation()
        {
            if (!AppSession.PendingRmRequestNoteId.HasValue || AppSession.PendingRmRequestNoteId.Value <= 0)
                return;

            long noteId = AppSession.PendingRmRequestNoteId.Value;
            AppSession.PendingRmRequestNoteId = null;

            var note = _rmrnCtrl.GetById(noteId);
            EnsureIssueFiltersLoaded();
            _tabs.SelectedIndex = 0;
            _filterSyncInProgress = true;
            try
            {
                if (note != null && note.ProductionOrderID > 0)
                    ReloadProductionOrderCombo(note.ProductionOrderID);
                else
                    ReloadProductionOrderCombo(0);
                ReloadRequestNoteCombo(noteId);
            }
            finally
            {
                _filterSyncInProgress = false;
            }
            LoadIssuePreview();
        }

        private TabPage BuildIssueRequestTab()
        {
            var page = new TabPage("Issue RM Request") { BackColor = UITheme.Background };

            var step1 = new Panel { Dock = DockStyle.Top, Height = 150, Padding = new Padding(16, 8, 16, 4) };
            var step1Layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4 };
            step1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            step1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            step1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            step1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _cmbProductionOrder = new ComboBox { Dock = DockStyle.Fill };
            _productionOrderBinder = new FilteredComboBinder(_cmbProductionOrder, "Production Order ID", "DisplayText");
            _productionOrderBinder.SelectionCommitted += (s, e) =>
            {
                if (_filterSyncInProgress) return;
                ReloadRequestNoteCombo();
                ScheduleIssuePreview();
            };

            _cmbRequestNote = new ComboBox { Dock = DockStyle.Fill };
            _requestNoteBinder = new FilteredComboBinder(_cmbRequestNote, "Request Note ID", "DisplayText");
            _requestNoteBinder.SetServerSearch(prefix =>
                _rmrnCtrl.SearchOpenRequestNotesForPicker(prefix, GetSelectedProductionOrderFilterId(), 25));
            _requestNoteBinder.SelectionCommitted += (s, e) =>
            {
                SyncProductionOrderFromRequestNote(GetSelectedRequestNoteId());
                ScheduleIssuePreview();
            };
            _cmbRequestNote.Leave += (s, e) => ResolveTypedRequestNote();
            _cmbRequestNote.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ResolveTypedRequestNote();
                    ScheduleIssuePreview();
                }
            };

            _cmbIssueInventoryWh = BuildInventoryWarehouseCombo();
            if (_cmbIssueInventoryWh.Items.Count > 0)
            {
                try { _cmbIssueInventoryWh.SelectedValue = WarehouseHelper.DefaultInventoryWarehouseId; }
                catch { _cmbIssueInventoryWh.SelectedIndex = 0; }
            }
            _cmbIssueInventoryWh.SelectedIndexChanged += (s, e) =>
            {
                UpdateIssueProductionWarehouseLabel();
                ScheduleIssuePreview();
            };

            _lblIssueProductionWh = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UITheme.TextDark
            };
            UpdateIssueProductionWarehouseLabel();

            step1Layout.Controls.Add(MakeStepLabel("Step 1 — Filters"), 0, 0);
            step1Layout.SetColumnSpan(step1Layout.Controls[step1Layout.Controls.Count - 1], 4);
            step1Layout.Controls.Add(MakeFilterLabel("Production Order"), 0, 1);
            step1Layout.Controls.Add(_cmbProductionOrder, 1, 1);
            step1Layout.Controls.Add(MakeFilterLabel("Inventory WH *"), 2, 1);
            step1Layout.Controls.Add(_cmbIssueInventoryWh, 3, 1);
            step1Layout.Controls.Add(MakeFilterLabel("RM Request *"), 0, 2);
            step1Layout.Controls.Add(_cmbRequestNote, 1, 2);
            step1Layout.SetColumnSpan(_cmbRequestNote, 3);
            step1Layout.Controls.Add(MakeFilterLabel("Production WH"), 0, 3);
            step1Layout.Controls.Add(_lblIssueProductionWh, 1, 3);
            step1Layout.SetColumnSpan(_lblIssueProductionWh, 3);
            step1.Controls.Add(step1Layout);

            _issueGrid = GridHelper.CreateStyledGrid();
            _issueGrid.Dock = DockStyle.Fill;
            _issueGrid.ReadOnly = true;

            var step2Panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 8) };
            step2Panel.Controls.Add(_issueGrid);
            step2Panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "Step 2 — Issue preview (auto-refreshes when request or warehouse changes)\r\n" +
                       "Columns show request qty, inventory availability, production on-hand, and shortage.",
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f)
            });

            var step3 = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(16, 8, 16, 8) };
            var step3Flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnIssueMaterials = UITheme.CreatePrimaryButton("Issue Materials");
            _btnIssueMaterials.Click += (s, e) => ExecuteIssueMaterials();
            PermissionGuard.ApplyCreateButton(_btnIssueMaterials, PermissionModule.InternalTransferForm);

            _btnCreateShortagePo = UITheme.CreateSecondaryButton("Create PO for Shortages");
            _btnCreateShortagePo.Click += (s, e) => ShowShortagePurchaseOrderDialog();
            PermissionGuard.ApplyCreateButton(_btnCreateShortagePo, PermissionModule.PurchaseOrder);

            var lblStep3 = new Label
            {
                AutoSize = true,
                Text = "Step 3 — If shortage exists: Create PO → receive goods → issue materials.",
                ForeColor = UITheme.TextGray,
                Padding = new Padding(0, 8, 16, 0)
            };

            step3Flow.Controls.Add(lblStep3);
            step3Flow.Controls.Add(_btnIssueMaterials);
            step3Flow.Controls.Add(_btnCreateShortagePo);
            step3.Controls.Add(step3Flow);

            page.Controls.Add(step2Panel);
            page.Controls.Add(step3);
            page.Controls.Add(step1);
            return page;
        }

        private TabPage BuildFreeTransferTab()
        {
            var page = new TabPage("Other Transfer") { BackColor = UITheme.Background };

            var info = new Label
            {
                Text = "Manual stock move between any warehouses (not tied to RM request notes). No transfer document is stored.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UITheme.TextGray,
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(16, 8, 16, 0)
            };

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(16, 8, 16, 8) };
            var filterLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 1 };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            _cmbItemType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            _cmbItemType.Items.AddRange(new object[] { "Raw Material", "Product" });
            _cmbItemType.SelectedIndex = 0;

            _cmbFromWarehouse = BuildWarehouseCombo();
            _cmbToWarehouse = BuildWarehouseCombo();

            _btnLoad = UITheme.CreateSecondaryButton("Load Items");
            _btnLoad.Dock = DockStyle.Fill;
            _btnLoad.Click += (s, e) => LoadTransferLines();

            _btnTransfer = UITheme.CreatePrimaryButton("Transfer");
            _btnTransfer.Dock = DockStyle.Fill;
            _btnTransfer.Click += (s, e) => ExecuteTransfer();
            PermissionGuard.ApplyCreateButton(_btnTransfer, PermissionModule.InternalTransferForm);

            filterLayout.Controls.Add(MakeFilterLabel("Item Type"), 0, 0);
            filterLayout.Controls.Add(_cmbItemType, 1, 0);
            filterLayout.Controls.Add(MakeFilterLabel("From Warehouse *"), 2, 0);
            filterLayout.Controls.Add(_cmbFromWarehouse, 3, 0);
            filterLayout.Controls.Add(MakeFilterLabel("To Warehouse *"), 4, 0);
            filterLayout.Controls.Add(_cmbToWarehouse, 5, 0);
            filterLayout.Controls.Add(_btnLoad, 6, 0);
            filterLayout.Controls.Add(_btnTransfer, 7, 0);
            filterPanel.Controls.Add(filterLayout);

            _lineGrid = GridHelper.CreateStyledGrid();
            _lineGrid.ReadOnly = false;
            _lineGrid.Dock = DockStyle.Fill;

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };
            gridPanel.Controls.Add(_lineGrid);
            gridPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Enter Transfer Qty for items to move. Only available stock (physical − reserved) can be transferred.",
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(0, 4, 0, 0)
            });

            page.Controls.Add(gridPanel);
            page.Controls.Add(filterPanel);
            page.Controls.Add(info);
            return page;
        }

        private void EnsureIssueFiltersLoaded()
        {
            if (_issueFiltersLoaded) return;
            _issueFiltersLoaded = true;
            ReloadProductionOrderCombo(0);
            _requestNoteBinder?.ClearSelection();
        }

        private void ScheduleIssuePreview()
        {
            if (_issuePreviewTimer == null)
            {
                _issuePreviewTimer = new Timer { Interval = 300 };
                _issuePreviewTimer.Tick += (s, e) =>
                {
                    _issuePreviewTimer.Stop();
                    LoadIssuePreview();
                };
            }

            _issuePreviewTimer.Stop();
            _issuePreviewTimer.Start();
        }

        private static Label MakeStepLabel(string text) =>
            new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UITheme.Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };

        private void SyncProductionOrderFromRequestNote(long noteId)
        {
            if (_filterSyncInProgress || noteId <= 0 || _productionOrderBinder == null) return;

            var note = _rmrnCtrl.GetById(noteId);
            if (note == null || note.ProductionOrderID <= 0) return;

            _filterSyncInProgress = true;
            try
            {
                _productionOrderBinder.SelectById(note.ProductionOrderID);
            }
            finally
            {
                _filterSyncInProgress = false;
            }
        }

        private void ReloadRequestNoteCombo(long selectId = 0)
        {
            if (_requestNoteBinder == null) return;

            if (selectId > 0)
            {
                _requestNoteBinder.SetSource(_rmrnCtrl.GetOpenRequestNotePickerById(selectId), selectId);
                return;
            }

            _requestNoteBinder.ClearSelection();
        }

        private void ResolveTypedRequestNote()
        {
            if (_requestNoteBinder == null) return;
            if (_requestNoteBinder.GetSelectedId() > 0) return;

            string text = (_cmbRequestNote.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return;

            long id = _rmrnCtrl.FindOpenIdByCode(DocumentCodeHelper.NormalizeScrCode(text));
            if (id > 0)
            {
                ReloadRequestNoteCombo(id);
                SyncProductionOrderFromRequestNote(id);
            }
        }

        private void ReloadProductionOrderCombo(long selectPtoId = 0)
        {
            if (_productionOrderBinder == null) return;

            var dt = _rmrnCtrl.GetOpenProductionOrdersForIssuePicker();
            var combined = dt?.Clone() ?? new DataTable();
            if (combined.Columns.Count == 0)
            {
                combined.Columns.Add("Production Order ID", typeof(long));
                combined.Columns.Add("Production Order Code", typeof(string));
                combined.Columns.Add("DisplayText", typeof(string));
            }

            DataRow allRow = combined.NewRow();
            allRow["Production Order ID"] = 0L;
            allRow["Production Order Code"] = "";
            allRow["DisplayText"] = "(All open production orders)";
            combined.Rows.InsertAt(allRow, 0);

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                    combined.ImportRow(row);
            }

            _productionOrderBinder.SetSource(combined, selectPtoId);
        }

        private long GetSelectedProductionOrderFilterId()
        {
            if (_productionOrderBinder == null) return 0;
            return _productionOrderBinder.GetSelectedId();
        }

        private long GetSelectedRequestNoteId()
        {
            ResolveTypedRequestNote();
            return _requestNoteBinder?.GetSelectedId() ?? 0;
        }

        private void UpdateIssueProductionWarehouseLabel()
        {
            if (_lblIssueProductionWh == null) return;
            long invWh = GetComboLongId(_cmbIssueInventoryWh);
            long prodWh = _warehouseCtrl.GetPairedProductionWarehouseId(invWh);
            string prodName = GetWarehouseName(prodWh);
            _lblIssueProductionWh.Text = prodName + " (ID " + prodWh + ")";
        }

        private string GetWarehouseName(long warehouseId)
        {
            var wh = _warehouseCtrl.GetById(warehouseId);
            return wh?.WarehouseName ?? warehouseId.ToString();
        }

        private void LoadIssuePreview()
        {
            if (_issueGrid == null) return;

            long noteId = GetSelectedRequestNoteId();
            long invWh = GetComboLongId(_cmbIssueInventoryWh);
            if (noteId <= 0 || invWh <= 0)
            {
                _issueGrid.DataSource = null;
                return;
            }

            long prodWh = _warehouseCtrl.GetPairedProductionWarehouseId(invWh);
            if (prodWh <= 0)
            {
                _issueGrid.DataSource = null;
                return;
            }
            _issueGrid.DataSource = _materialWorkflow.BuildIssuePreview(noteId, invWh, prodWh);
            GridHelper.StyleGrid(_issueGrid);
            HighlightShortageRows();
        }

        private void HighlightShortageRows()
        {
            if (_issueGrid?.Columns.Contains("Shortage Qty") != true) return;
            foreach (DataGridViewRow row in _issueGrid.Rows)
            {
                if (row.Cells["Shortage Qty"].Value == null || row.Cells["Shortage Qty"].Value == DBNull.Value) continue;
                if (Convert.ToDecimal(row.Cells["Shortage Qty"].Value) > 0)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 230);
            }
        }

        private void ExecuteIssueMaterials()
        {
            if (!PermissionGuard.Ensure(PermissionModule.InternalTransferForm, PermissionAction.Create, this))
                return;

            long noteId = GetSelectedRequestNoteId();
            long invWh = GetComboLongId(_cmbIssueInventoryWh);
            if (noteId <= 0 || invWh <= 0)
            {
                UITheme.ShowWarning("Enter or select an open RM request code (SCR-XXXXXXXX) and inventory warehouse.");
                return;
            }

            long prodWh = _warehouseCtrl.GetPairedProductionWarehouseId(invWh);
            if (prodWh <= 0)
            {
                UITheme.ShowWarning("No paired production warehouse found for the selected inventory warehouse.");
                return;
            }
            var result = _materialWorkflow.IssueRequestNote(noteId, invWh, prodWh);
            if (!result.Success)
            {
                UITheme.ShowWarning(result.Message);
                return;
            }

            UITheme.ShowSuccess(result.Message);
            ReloadProductionOrderCombo(GetSelectedProductionOrderFilterId());
            ReloadRequestNoteCombo();
            LoadIssuePreview();
        }

        private void ShowShortagePurchaseOrderDialog()
        {
            if (!PermissionGuard.Ensure(PermissionModule.PurchaseOrder, PermissionAction.Create, this))
                return;

            long noteId = GetSelectedRequestNoteId();
            long invWh = GetComboLongId(_cmbIssueInventoryWh);
            if (noteId <= 0 || invWh <= 0)
            {
                UITheme.ShowWarning("Enter or select an open RM request code (SCR-XXXXXXXX) and inventory warehouse.");
                return;
            }

            var shortages = _materialWorkflow.EvaluateShortages(noteId, invWh);
            if (shortages.Count == 0)
            {
                UITheme.ShowSuccess("No material shortages for this request note in the selected warehouse.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "Create Purchase Orders for Shortages";
                dlg.Size = new Size(900, 540);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var supplierNames = new Dictionary<long, string>();
                var supplierDt = _supplierCtrl.GetAllSuppliers();
                if (supplierDt != null)
                {
                    foreach (DataRow row in supplierDt.Rows)
                        supplierNames[Convert.ToInt64(row["Supplier ID"])] = row["Supplier Name"]?.ToString() ?? "";
                }

                var grid = GridHelper.CreateStyledGrid();
                grid.Dock = DockStyle.Fill;
                grid.ReadOnly = false;
                var table = new DataTable();
                table.Columns.Add("Supplier", typeof(string));
                table.Columns.Add("Raw Material", typeof(string));
                table.Columns.Add("Required", typeof(decimal));
                table.Columns.Add("Net Available", typeof(decimal));
                table.Columns.Add("Min Stock", typeof(decimal));
                table.Columns.Add("PO Qty", typeof(decimal));
                table.Columns.Add("Unit Price", typeof(decimal));
                table.Columns.Add("Supplier ID", typeof(long));
                table.Columns.Add("Raw Material ID", typeof(long));
                foreach (var line in shortages.OrderBy(s => s.SupplierId).ThenBy(s => s.RawMaterialCode))
                {
                    string supplierName = line.SupplierId > 0 && supplierNames.TryGetValue(line.SupplierId, out var name)
                        ? name
                        : "(No quote)";
                    table.Rows.Add(supplierName, line.RawMaterialCode, line.RequiredQty, line.NetAvailable,
                        line.MinimumStockLevel, line.PoQty, line.UnitPrice, line.SupplierId, line.RawMaterialId);
                }
                grid.DataSource = table;
                if (grid.Columns.Contains("Supplier ID"))
                    grid.Columns["Supplier ID"].Visible = false;
                if (grid.Columns.Contains("Raw Material ID"))
                    grid.Columns["Raw Material ID"].Visible = false;
                foreach (DataGridViewColumn col in grid.Columns)
                    col.ReadOnly = col.Name != "PO Qty" && col.HeaderText != "PO Qty";

                var dtpDelivery = new DateTimePicker { Value = DateTime.Today.AddDays(14), Width = 220 };
                var txtRemark = new TextBox { Text = "Auto-created for RM request shortage", Width = 420 };

                var top = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 3,
                    Padding = new Padding(16, 12, 16, 8)
                };
                top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                UITheme.AddFormField(top, 0, "Request Delivery *", dtpDelivery);
                UITheme.AddFormField(top, 1, "Remark", txtRemark);
                var lblHint = new Label
                {
                    Text = "One purchase order is created per supplier (grouped by preferred supplier quote).",
                    Dock = DockStyle.Fill,
                    ForeColor = UITheme.TextGray,
                    Font = new Font("Segoe UI", 8.5f),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(0, 4, 0, 0)
                };
                top.Controls.Add(new Label(), 0, 2);
                top.Controls.Add(lblHint, 1, 2);

                var btnCreate = UITheme.CreatePrimaryButton("Create PO(s)");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnCreate.Click += (s, e) =>
                {
                    long staffId = AppSession.CurrentUser?.StaffID ?? 0;
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Current staff profile is required.");
                        return;
                    }

                    var lines = new List<MaterialShortageLine>();
                    foreach (DataRow row in table.Rows)
                    {
                        decimal poQty = Convert.ToDecimal(row["PO Qty"]);
                        if (poQty <= 0) continue;
                        lines.Add(new MaterialShortageLine
                        {
                            RawMaterialId = Convert.ToInt64(row["Raw Material ID"]),
                            RawMaterialCode = row["Raw Material"]?.ToString(),
                            PoQty = poQty,
                            UnitPrice = Convert.ToDecimal(row["Unit Price"]),
                            SupplierId = Convert.ToInt64(row["Supplier ID"])
                        });
                    }
                    if (lines.Count == 0)
                    {
                        UITheme.ShowWarning("Enter at least one PO quantity.");
                        return;
                    }

                    var result = _materialWorkflow.CreatePurchaseOrdersForShortagesBySupplier(
                        staffId, invWh, lines, dtpDelivery.Value.Date, txtRemark.Text.Trim());
                    if (!result.Success)
                    {
                        UITheme.ShowWarning(result.Message);
                        return;
                    }

                    UITheme.ShowSuccess(result.Message);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadIssuePreview();
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

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.Controls.Add(top, 0, 0);
                root.Controls.Add(grid, 0, 1);

                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private static Label MakeFilterLabel(string text) =>
            new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 9f)
            };

        private ComboBox BuildWarehouseCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            var dt = _warehouseCtrl.GetAllWarehouses();
            if (dt != null && dt.Rows.Count > 0)
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                    row["DisplayText"] = $"{row["Warehouse Name"]} — {row["Address"]}";
                cmb.DisplayMember = "DisplayText";
                cmb.ValueMember = "Warehouse ID";
                cmb.DataSource = dt;
            }
            return cmb;
        }

        private ComboBox BuildInventoryWarehouseCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            var dt = _warehouseCtrl.GetAllWarehouses();
            if (dt == null) return cmb;

            var filtered = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                long whId = Convert.ToInt64(row["Warehouse ID"]);
                string whName = row["Warehouse Name"]?.ToString();
                if (WarehouseHelper.IsInventoryWarehouse(whId, whName))
                    filtered.ImportRow(row);
            }

            filtered.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in filtered.Rows)
                row["DisplayText"] = $"{row["Warehouse Name"]} — {row["Address"]}";

            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Warehouse ID";
            cmb.DataSource = filtered;
            return cmb;
        }

        private static long GetComboLongId(ComboBox cmb)
        {
            if (cmb?.SelectedValue == null || cmb.SelectedValue == DBNull.Value) return 0;
            if (cmb.SelectedValue is long l) return l;
            if (cmb.SelectedValue is int i) return i;
            long.TryParse(cmb.SelectedValue.ToString(), out long id);
            return id;
        }

        private bool IsRawMaterialTransfer() => _cmbItemType.SelectedIndex == 0;

        private void LoadTransferLines()
        {
            long fromWhId = GetComboLongId(_cmbFromWarehouse);
            if (fromWhId <= 0)
            {
                UITheme.ShowWarning("Please select a source warehouse.");
                return;
            }

            DataTable source = IsRawMaterialTransfer()
                ? _warehouseCtrl.GetTransferableRawMaterials(fromWhId)
                : _warehouseCtrl.GetTransferableProducts(fromWhId);

            _lineTable = source?.Copy() ?? new DataTable();
            if (!_lineTable.Columns.Contains("Transfer Qty"))
                _lineTable.Columns.Add("Transfer Qty", typeof(decimal));

            foreach (DataRow row in _lineTable.Rows)
                row["Transfer Qty"] = 0m;

            _lineGrid.DataSource = _lineTable;
            GridHelper.StyleGrid(_lineGrid);
            ConfigureLineGridColumns();
        }

        private void ConfigureLineGridColumns()
        {
            if (_lineGrid.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in _lineGrid.Columns)
                col.ReadOnly = col.Name != "Transfer Qty";

            if (_lineGrid.Columns.Contains("Item ID"))
                _lineGrid.Columns["Item ID"].Visible = false;
        }

        private void ExecuteTransfer()
        {
            if (!PermissionGuard.Ensure(PermissionModule.InternalTransferForm, PermissionAction.Create, this))
                return;

            long fromWhId = GetComboLongId(_cmbFromWarehouse);
            long toWhId = GetComboLongId(_cmbToWarehouse);
            if (fromWhId <= 0 || toWhId <= 0)
            {
                UITheme.ShowWarning("Please select both warehouses.");
                return;
            }
            if (fromWhId == toWhId)
            {
                UITheme.ShowWarning("Source and destination warehouses must be different.");
                return;
            }
            if (_lineTable == null || _lineTable.Rows.Count == 0)
            {
                UITheme.ShowWarning("Load items first, then enter transfer quantities.");
                return;
            }

            var lines = new List<StockTransferLine>();
            foreach (DataRow row in _lineTable.Rows)
            {
                decimal qty = row["Transfer Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Transfer Qty"]);
                if (qty <= 0) continue;

                decimal available = row["Available Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Available Qty"]);
                string itemCode = row["Item Code"]?.ToString() ?? "item";
                if (qty > available)
                {
                    UITheme.ShowWarning($"Transfer qty for {itemCode} exceeds available stock ({available:N2}).");
                    return;
                }

                lines.Add(new StockTransferLine
                {
                    ItemId = Convert.ToInt64(row["Item ID"]),
                    Quantity = qty
                });
            }

            if (lines.Count == 0)
            {
                UITheme.ShowWarning("Enter at least one transfer quantity greater than zero.");
                return;
            }

            string fromName = _cmbFromWarehouse.Text;
            string toName = _cmbToWarehouse.Text;
            string typeName = IsRawMaterialTransfer() ? "raw material" : "product";
            if (MessageBox.Show(
                    $"Transfer {lines.Count} {typeName} line(s) from\n{fromName}\nto\n{toName}?",
                    "Confirm Transfer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var itemType = IsRawMaterialTransfer() ? StockTransferItemType.RawMaterial : StockTransferItemType.Product;
            var result = _inventoryWorkflow.TransferBetweenWarehouses(fromWhId, toWhId, itemType, lines);
            if (!result.Success)
            {
                UITheme.ShowError(result.Message);
                return;
            }

            UITheme.ShowSuccess(result.Message);
            LoadTransferLines();
        }
    }
}
