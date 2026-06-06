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
    public class FinanceDeptPanel : UserControl
    {
        private readonly PaymentVoucherController _pvCtrl = new PaymentVoucherController();
        private readonly ReceiptVoucherController _rvCtrl = new ReceiptVoucherController();
        private readonly FinanceWorkflowService _financeWorkflow = new FinanceWorkflowService();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly SupplierController _supplierCtrl = new SupplierController();
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly PurchaseOrderController _poCtrl = new PurchaseOrderController();

        private TabControl _tabs;
        private DataGridView _pvGrid;
        private DataGridView _rvGrid;
        private TextBox _pvSearch;
        private ComboBox _pvStatusFilter;
        private TextBox _rvSearch;
        private ComboBox _rvStatusFilter;
        private ChartControl _incomeChart;
        private ChartControl _expenseChart;
        private PieChartControl _incomePie;
        private PieChartControl _expensePie;
        private Label _lblTotalIncome;
        private Label _lblTotalExpense;
        private Label _lblNetFlow;

        private static readonly string[] MethodNames = { "Cash", "Bank Transfer", "Credit Card", "Cheque" };
        private static readonly string[] PVStatusNames = { "Draft", "Approved", "Paid", "Cancelled" };
        private static readonly string[] RVStatusNames = { "Draft", "Confirmed", "Cancelled" };

        public FinanceDeptPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            LoadAll();
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            var tabDash = new TabPage("📊 Dashboard") { BackColor = UITheme.Background };
            BuildDashboardTab(tabDash);

            var tabPV = new TabPage("💸 Payment Vouchers") { BackColor = UITheme.Background };
            BuildPVTab(tabPV);

            var tabRV = new TabPage("🧾 Receipt Vouchers") { BackColor = UITheme.Background };
            BuildRVTab(tabRV);

            if (AppSession.CanView(PermissionModule.PaymentVoucher) || AppSession.CanView(PermissionModule.ReceiptVoucher))
                _tabs.TabPages.Add(tabDash);
            if (AppSession.CanView(PermissionModule.PaymentVoucher))
                _tabs.TabPages.Add(tabPV);
            if (AppSession.CanView(PermissionModule.ReceiptVoucher))
                _tabs.TabPages.Add(tabRV);
            Controls.Add(_tabs);
        }

        private void BuildDashboardTab(TabPage page)
        {
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // 頂部卡片高度
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 圖表區域高度

            var cardPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            _lblTotalIncome = new Label { Text = "RM 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.DarkGreen, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _lblTotalExpense = new Label { Text = "RM 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.DarkRed, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _lblNetFlow = new Label { Text = "RM 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.Navy, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

            var pnlIn = UITheme.CreateCard("Total Income"); pnlIn.Controls.Add(_lblTotalIncome);
            var pnlOut = UITheme.CreateCard("Total Expenses"); pnlOut.Controls.Add(_lblTotalExpense);
            var pnlNet = UITheme.CreateCard("Net Cash Flow"); pnlNet.Controls.Add(_lblNetFlow);

            var btnReport = UITheme.CreateSecondaryButton("Print Report PDF");
            btnReport.Dock = DockStyle.Bottom;
            btnReport.Height = 34;
            btnReport.Click += (s, e) => ExportDashboardReportPdf();
            pnlNet.Controls.Add(btnReport);

            cardPanel.Controls.Add(pnlIn, 0, 0);
            cardPanel.Controls.Add(pnlOut, 1, 0);
            cardPanel.Controls.Add(pnlNet, 2, 0);
            mainLayout.Controls.Add(cardPanel, 0, 0);

            // 💡 修正這裡：確保圖表網格擁有正確的 50% / 50% 均分行高與列寬
            var chartLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 12, 0, 0) };
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); // 長條圖佔 55% 寬
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // 圓餅圖佔 45% 寬
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));    // 上排收入佔 50% 高
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));    // 下排支出佔 50% 高

            _incomeChart = new ChartControl { Dock = DockStyle.Fill, MinimumSize = new Size(100, 100) };
            _expenseChart = new ChartControl { Dock = DockStyle.Fill, MinimumSize = new Size(100, 100) };
            _incomePie = new PieChartControl { Dock = DockStyle.Fill, MinimumSize = new Size(100, 100) };
            _expensePie = new PieChartControl { Dock = DockStyle.Fill, MinimumSize = new Size(100, 100) };

            chartLayout.Controls.Add(_incomeChart, 0, 0);
            chartLayout.Controls.Add(_incomePie, 1, 0);   // 收入圓餅圖放右上
            chartLayout.Controls.Add(_expenseChart, 0, 1);
            chartLayout.Controls.Add(_expensePie, 1, 1);  // 支出圓餅圖放右下

            mainLayout.Controls.Add(chartLayout, 0, 1); // 💡 修正索引，將圖表佈局放入 mainLayout 的第二行
            page.Controls.Add(mainLayout);
        }

        private void ExportDashboardReportPdf()
        {
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            fields.Rows.Add("Total Income", _lblTotalIncome?.Text ?? "");
            fields.Rows.Add("Total Expenses", _lblTotalExpense?.Text ?? "");
            fields.Rows.Add("Net Cash Flow", _lblNetFlow?.Text ?? "");
            fields.Rows.Add("Report Scope", "Finance dashboard summary with charts");

            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Finance Dashboard Report",
                    fields,
                    null,
                    "Finance_Dashboard_Report");
                data.Charts = BuildDashboardChartImages();
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to export PDF: " + ex.Message);
            }
        }

        private System.Collections.Generic.List<PdfChartImage> BuildDashboardChartImages()
        {
            _incomeChart?.Invalidate();
            _expenseChart?.Invalidate();
            _incomePie?.Invalidate();
            _expensePie?.Invalidate();
            Update();

            return new System.Collections.Generic.List<PdfChartImage>
            {
                new PdfChartImage { Title = "Income Trend", Image = _incomeChart?.ToBitmap(520, 240) },
                new PdfChartImage { Title = "Income by Payment Method", Image = _incomePie?.ToBitmap(520, 240) },
                new PdfChartImage { Title = "Expense Trend", Image = _expenseChart?.ToBitmap(520, 240) },
                new PdfChartImage { Title = "Expense by Payment Method", Image = _expensePie?.ToBitmap(520, 240) }
            };
        }

        private void BuildPVTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Payment Voucher");
            btnNew.Location = new Point(8, 10);
            btnNew.Click += (s, e) => {
                if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, PermissionAction.Create, this)) return;
                using (var dlg = BuildPVForm("Create Payment Voucher", null))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.PaymentVoucher);

            var btnView = UITheme.CreateSecondaryButton("View Detail");
            btnView.Location = new Point(btnNew.Right + 10, 10);
            btnView.Click += (s, e) => ShowSelectedPaymentVoucherDetail();

            var btnUpdateStatus = UITheme.CreateSecondaryButton("Update Status");
            btnUpdateStatus.Location = new Point(btnView.Right + 10, 10);
            btnUpdateStatus.Click += (s, e) => ShowUpdatePaymentVoucherStatusDialog();
            PermissionGuard.ApplyEditButton(btnUpdateStatus, PermissionModule.PaymentVoucher);

            var lblSearch = new Label { Text = "Search Code:", Location = new Point(btnUpdateStatus.Right + 16, 16), AutoSize = true };
            _pvSearch = new TextBox { Location = new Point(lblSearch.Right + 8, 13), Width = 150 };
            _pvSearch.TextChanged += (s, e) => FilterPV();

            var lblFilter = new Label { Text = "Status:", Location = new Point(_pvSearch.Right + 12, 16), AutoSize = true };
            _pvStatusFilter = new ComboBox { Location = new Point(lblFilter.Right + 8, 13), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _pvStatusFilter.Items.Add("All");
            _pvStatusFilter.Items.AddRange(PVStatusNames);
            _pvStatusFilter.SelectedIndex = 0;
            _pvStatusFilter.SelectedIndexChanged += (s, e) => FilterPV();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnView);
            toolbar.Controls.Add(btnUpdateStatus);
            toolbar.Controls.Add(lblSearch);
            toolbar.Controls.Add(_pvSearch);
            toolbar.Controls.Add(lblFilter);
            toolbar.Controls.Add(_pvStatusFilter);
            layout.Controls.Add(toolbar, 0, 0);

            _pvGrid = InitializeCustomGridView();
            _pvGrid.CellDoubleClick += _pvGrid_CellDoubleClick;
            layout.Controls.Add(FilterBlockHelper.CreateFilterBlock(_pvGrid, "Payment Voucher Filters"), 0, 1);
            layout.Controls.Add(_pvGrid, 0, 2);

            page.Controls.Add(layout);
        }

        private void _pvGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long id = Convert.ToInt64(_pvGrid.Rows[e.RowIndex].Cells["ID"].Value);
            if (AppSession.CanEdit(PermissionModule.PaymentVoucher))
            {
                var pv = _pvCtrl.GetById(id);
                if (pv == null) return;
                using (var dlg = BuildPVForm("Edit Payment Voucher", pv))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            }
            else ShowPaymentVoucherDetail(id);
        }

        private void BuildRVTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Receipt Voucher");
            btnNew.Location = new Point(8, 10);
            btnNew.Click += (s, e) => {
                if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, PermissionAction.Create, this)) return;
                using (var dlg = BuildRVForm("Create Receipt Voucher", null))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.ReceiptVoucher);

            var btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnNew.Right + 10, 10);
            btnDetail.Click += (s, e) => ShowSelectedReceiptVoucherDetail();

            var btnUpdateRvStatus = UITheme.CreateSecondaryButton("Update Status");
            btnUpdateRvStatus.Location = new Point(btnDetail.Right + 10, 10);
            btnUpdateRvStatus.Click += (s, e) => ShowUpdateReceiptVoucherStatusDialog();
            PermissionGuard.ApplyEditButton(btnUpdateRvStatus, PermissionModule.ReceiptVoucher);

            var lblSearch = new Label { Text = "Search Code:", Location = new Point(btnUpdateRvStatus.Right + 16, 16), AutoSize = true };
            _rvSearch = new TextBox { Location = new Point(lblSearch.Right + 8, 13), Width = 150 };
            _rvSearch.TextChanged += (s, e) => FilterRV();

            var lblFilter = new Label { Text = "Status:", Location = new Point(_rvSearch.Right + 12, 16), AutoSize = true };
            _rvStatusFilter = new ComboBox { Location = new Point(lblFilter.Right + 8, 13), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _rvStatusFilter.Items.Add("All");
            _rvStatusFilter.Items.AddRange(RVStatusNames);
            _rvStatusFilter.SelectedIndex = 0;
            _rvStatusFilter.SelectedIndexChanged += (s, e) => FilterRV();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnDetail);
            toolbar.Controls.Add(btnUpdateRvStatus);
            toolbar.Controls.Add(lblSearch);
            toolbar.Controls.Add(_rvSearch);
            toolbar.Controls.Add(lblFilter);
            toolbar.Controls.Add(_rvStatusFilter);
            layout.Controls.Add(toolbar, 0, 0);

            _rvGrid = InitializeCustomGridView();
            _rvGrid.CellDoubleClick += _rvGrid_CellDoubleClick;
            layout.Controls.Add(FilterBlockHelper.CreateFilterBlock(_rvGrid, "Receipt Voucher Filters"), 0, 1);
            layout.Controls.Add(_rvGrid, 0, 2);

            page.Controls.Add(layout);
        }

        private void _rvGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long id = Convert.ToInt64(_rvGrid.Rows[e.RowIndex].Cells["ID"].Value);
            if (AppSession.CanEdit(PermissionModule.ReceiptVoucher))
            {
                var rv = _rvCtrl.GetById(id);
                if (rv == null) return;
                using (var dlg = BuildRVForm("Edit Receipt Voucher", rv))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            }
            else ShowReceiptVoucherDetail(id);
        }

        private DataGridView InitializeCustomGridView()
        {
            var gv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };

            gv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            gv.ColumnHeadersHeight = 35;

            return gv;
        }

        private Form BuildPVForm(string title, PaymentVoucher pv)
        {
            bool isNew = pv == null;
            var dlg = new Form
            {
                Text = title,
                Size = new Size(760, 620),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(680, 520),
                BackColor = UITheme.Background
            };

            long defaultStaffId = pv?.StaffID ?? AppSession.CurrentUser?.StaffID ?? 0;
            string staffLabel = AppSession.CurrentUser?.FullName
                ?? (defaultStaffId > 0 ? defaultStaffId.ToString() : "—");

            var txtCode = new TextBox
            {
                Text = pv?.PaymentVoucherCode ?? "",
                Width = 280,
                MaxLength = 30
            };
            var cmbSupplier = BuildSupplierCombo(pv?.SupplierID ?? 0);
            var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };
            var txtAmount = new TextBox { Text = pv?.Amount.ToString("0.##") ?? string.Empty, Width = 280 };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            cmbMethod.Items.AddRange(MethodNames);
            if (pv != null && !string.IsNullOrEmpty(pv.PaymentMethod))
            {
                int idx = Array.IndexOf(MethodNames, pv.PaymentMethod);
                cmbMethod.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else cmbMethod.SelectedIndex = 0;

            var txtRef = new TextBox { Text = pv?.PaymentRef ?? string.Empty, Width = 280 };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            cmbStatus.Items.AddRange(PVStatusNames);
            cmbStatus.SelectedIndex = pv != null ? Math.Max(0, Math.Min(pv.Status, 3)) : 0;
            var txtRemark = new TextBox { Text = pv?.Remark ?? string.Empty, Multiline = true, Height = 44, Width = 280 };

            int headerRows = 8;
            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = headerRows * 34 + 24, Padding = new Padding(12), ColumnCount = 2, RowCount = headerRows };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            UITheme.AddFormField(header, 0, "Voucher Code *", txtCode);
            UITheme.AddFormField(header, 1, "Supplier *", cmbSupplier);
            UITheme.AddFormField(header, 2, "Staff", lblStaff);
            UITheme.AddFormField(header, 3, "Amount *", txtAmount);
            UITheme.AddFormField(header, 4, "Payment Method", cmbMethod);
            UITheme.AddFormField(header, 5, "Method Ref", txtRef);
            UITheme.AddFormField(header, 6, "Status", cmbStatus);
            UITheme.AddFormField(header, 7, "Remark", txtRemark);

            var settlementTable = CreatePoSettlementEditorTable();
            if (pv?.PurchaseOrderLines != null)
            {
                foreach (var line in pv.PurchaseOrderLines)
                {
                    settlementTable.Rows.Add(
                        line.PurchaseOrderID > 0 ? (object)line.PurchaseOrderID : DBNull.Value,
                        line.PurchaseOrderCode ?? "",
                        line.RequestDeliveryDate?.ToString("yyyy-MM-dd") ?? "",
                        line.ClearingType > 0 ? line.ClearingType : 1,
                        line.PayAmount);
                }
            }

            IEnumerable<long> ensurePoIds = pv?.PurchaseOrderLines?
                .Where(l => l.PurchaseOrderID > 0)
                .Select(l => l.PurchaseOrderID);
            DataTable poPicker = BuildPoPickerForSupplier(ResolveSupplierId(cmbSupplier), ensurePoIds);
            DataTable clearingPicker = BuildClearingTypeDataTable();

            var settlementGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DataSource = settlementTable
            };

            var colPo = new DataGridViewComboBoxColumn
            {
                Name = "colPo",
                HeaderText = "Purchase Order",
                DataPropertyName = "Purchase Order ID",
                DisplayMember = "DisplayText",
                ValueMember = "Purchase Order ID",
                Width = 240,
                FlatStyle = FlatStyle.Flat,
                DataSource = poPicker
            };
            var colReqDate = new DataGridViewTextBoxColumn
            {
                Name = "colReqDate",
                HeaderText = "Request Delivery",
                DataPropertyName = "Request Delivery Date",
                ReadOnly = true,
                Width = 120
            };
            var colClearing = new DataGridViewComboBoxColumn
            {
                Name = "colClearing",
                HeaderText = "Payment Type",
                DataPropertyName = "Clearing Type",
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                DataSource = clearingPicker,
                DisplayMember = "Value",
                ValueMember = "Code"
            };
            var colPay = new DataGridViewTextBoxColumn
            {
                Name = "colPay",
                HeaderText = "Pay Amount",
                DataPropertyName = "Pay Amount",
                Width = 110
            };
            settlementGrid.Columns.Add(colPo);
            settlementGrid.Columns.Add(colReqDate);
            settlementGrid.Columns.Add(colClearing);
            settlementGrid.Columns.Add(colPay);

            settlementGrid.DataError += (s, e) => { e.ThrowException = false; };

            settlementGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (settlementGrid.Columns[e.ColumnIndex].Name == "colPo")
                    SyncPoRowFromPicker(settlementGrid.Rows[e.RowIndex], poPicker);
            };
            settlementGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (settlementGrid.IsCurrentCellDirty)
                    settlementGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            cmbSupplier.SelectedIndexChanged += (s, e) =>
            {
                var ids = CollectPoIdsFromGrid(settlementGrid);
                poPicker = BuildPoPickerForSupplier(ResolveSupplierId(cmbSupplier), ids);
                colPo.DataSource = null;
                colPo.DataSource = poPicker;
            };

            var lblSettleHint = new Label
            {
                Text = "PO Settlements (paymentvoucherpurchaseorder) — optional; total pay amount should match voucher amount.",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(12, 6, 0, 0),
                ForeColor = UITheme.TextGray
            };

            var lineToolbar = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(12, 4, 12, 0) };
            var btnAddLine = UITheme.CreateSecondaryButton("+ Add PO Line");
            btnAddLine.Click += (s, e) =>
            {
                settlementTable.Rows.Add(DBNull.Value, "", "", 1, 0m);
            };
            var btnRemoveLine = UITheme.CreateSecondaryButton("Remove Line");
            btnRemoveLine.Location = new Point(btnAddLine.Right + 8, 0);
            btnRemoveLine.Click += (s, e) =>
            {
                if (settlementGrid.CurrentRow != null && !settlementGrid.CurrentRow.IsNewRow)
                    settlementGrid.Rows.Remove(settlementGrid.CurrentRow);
            };
            lineToolbar.Controls.Add(btnAddLine);
            lineToolbar.Controls.Add(btnRemoveLine);

            var settlementPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            settlementPanel.Controls.Add(lblSettleHint);
            settlementPanel.Controls.Add(lineToolbar);
            settlementPanel.Controls.Add(settlementGrid);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.PaymentVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.PaymentVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, action, dlg)) return;

                long supplierId = ResolveSupplierId(cmbSupplier);
                if (supplierId <= 0 || defaultStaffId <= 0 ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    UITheme.ShowWarning("Valid Supplier and Amount are required.");
                    return;
                }

                string voucherCode = txtCode.Text.Trim();
                if (!TryValidateVoucherCode(voucherCode, isNew, pv?.PaymentVoucherID ?? 0, _pvCtrl.ExistsByCode, out string codeError))
                {
                    UITheme.ShowWarning(codeError);
                    return;
                }

                if (!TryBuildPoSettlements(settlementGrid, amount, out var lines, out string settleError))
                {
                    UITheme.ShowWarning(settleError);
                    return;
                }

                try
                {
                    var entity = new PaymentVoucher
                    {
                        PaymentVoucherID = pv?.PaymentVoucherID ?? 0,
                        PaymentVoucherCode = voucherCode,
                        SupplierID = supplierId,
                        StaffID = defaultStaffId,
                        Amount = amount,
                        PaymentMethod = cmbMethod.SelectedItem?.ToString() ?? "Cash",
                        PaymentRef = txtRef.Text.Trim(),
                        Status = cmbStatus.SelectedIndex,
                        Remark = txtRemark.Text.Trim(),
                        PurchaseOrderLines = lines
                    };
                    if (isNew) _pvCtrl.Insert(entity);
                    else _pvCtrl.Update(entity);
                    UITheme.ShowSuccess(isNew ? "Payment Voucher created." : "Payment Voucher updated.");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex) { UITheme.ShowError(ex.Message); }
            };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(btnPanel);
            dlg.Controls.Add(header);
            dlg.Controls.Add(settlementPanel);
            return dlg;
        }

        private Form BuildRVForm(string title, ReceiptVoucher existingRv)
        {
            bool isNew = existingRv == null;
            var dlg = new Form
            {
                Text = title,
                Size = new Size(520, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = UITheme.Background
            };

            long defaultStaffId = existingRv?.StaffID ?? AppSession.CurrentUser?.StaffID ?? 0;
            string staffLabel = AppSession.CurrentUser?.FullName
                ?? (defaultStaffId > 0 ? defaultStaffId.ToString() : "—");

            var txtCode = new TextBox
            {
                Text = existingRv?.ReceiptVoucherCode ?? "",
                Width = 300,
                MaxLength = 30
            };
            var cmbCustomer = BuildCustomerCombo(existingRv?.CusomerID ?? 0);
            var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };
            var txtAmount = new TextBox { Text = existingRv?.PaymentAmount.ToString("0.##") ?? string.Empty, Width = 300 };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            cmbMethod.Items.AddRange(MethodNames);
            string methodName = existingRv?.PaymentMethodName ?? "";
            if (!string.IsNullOrEmpty(methodName))
            {
                int idx = Array.IndexOf(MethodNames, methodName);
                if (idx >= 0) cmbMethod.SelectedIndex = idx;
                else if (int.TryParse(methodName, out int legacyIdx) && legacyIdx >= 0 && legacyIdx < MethodNames.Length)
                    cmbMethod.SelectedIndex = legacyIdx;
                else cmbMethod.SelectedIndex = 0;
            }
            else cmbMethod.SelectedIndex = 0;

            var txtRef = new TextBox { Text = existingRv?.PaymentMethodRef ?? string.Empty, Width = 300 };
            var dtpReceived = new DateTimePicker
            {
                Width = 300,
                Format = DateTimePickerFormat.Short,
                Value = existingRv?.PaymentReceivedDate == default ? DateTime.Today : existingRv.PaymentReceivedDate
            };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            cmbStatus.Items.AddRange(RVStatusNames);
            cmbStatus.SelectedIndex = existingRv != null ? Math.Max(0, Math.Min(existingRv.Status, 2)) : 0;
            var txtRemark = new TextBox { Text = existingRv?.Remark ?? string.Empty, Multiline = true, Height = 44, Width = 300 };

            int rvFieldRows = 9;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = rvFieldRows };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            UITheme.AddFormField(layout, 0, "Voucher Code *", txtCode);
            UITheme.AddFormField(layout, 1, "Customer *", cmbCustomer);
            UITheme.AddFormField(layout, 2, "Staff", lblStaff);
            UITheme.AddFormField(layout, 3, "Amount *", txtAmount);
            UITheme.AddFormField(layout, 4, "Payment Method", cmbMethod);
            UITheme.AddFormField(layout, 5, "Method Ref", txtRef);
            UITheme.AddFormField(layout, 6, "Received Date *", dtpReceived);
            UITheme.AddFormField(layout, 7, "Status", cmbStatus);
            UITheme.AddFormField(layout, 8, "Remark", txtRemark);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.ReceiptVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.ReceiptVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, action, dlg)) return;

                long custId = GetComboLongId(cmbCustomer);
                if (custId <= 0 || defaultStaffId <= 0 ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    UITheme.ShowWarning("Valid Customer and Amount are required.");
                    return;
                }

                string voucherCode = txtCode.Text.Trim();
                if (!TryValidateVoucherCode(voucherCode, isNew, existingRv?.ReceiptVoucherID ?? 0, _rvCtrl.ExistsByCode, out string codeError))
                {
                    UITheme.ShowWarning(codeError);
                    return;
                }

                try
                {
                    var entity = new ReceiptVoucher
                    {
                        ReceiptVoucherID = existingRv?.ReceiptVoucherID ?? 0,
                        ReceiptVoucherCode = voucherCode,
                        CusomerID = custId,
                        StaffID = defaultStaffId,
                        PaymentAmount = amount,
                        PaymentMethodName = cmbMethod.SelectedItem?.ToString() ?? "Cash",
                        PaymentMethodRef = txtRef.Text.Trim(),
                        PaymentReceivedDate = dtpReceived.Value.Date,
                        Status = cmbStatus.SelectedIndex,
                        Remark = txtRemark.Text.Trim(),
                        CurrencyID = existingRv?.CurrencyID ?? 1
                    };
                    if (isNew) _rvCtrl.Insert(entity);
                    else _rvCtrl.Update(entity);
                    UITheme.ShowSuccess(isNew ? "Receipt Voucher created." : "Receipt Voucher updated.");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex) { UITheme.ShowError(ex.Message); }
            };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(btnPanel);
            dlg.Controls.Add(layout);
            return dlg;
        }

        private void FilterPV()
        {
            if (_pvGrid.DataSource is DataTable dt)
            {
                string code = _pvSearch.Text.Trim().Replace("'", "''");
                string status = _pvStatusFilter.SelectedItem?.ToString();
                string expr = "1=1";
                if (!string.IsNullOrEmpty(code)) expr += $" AND [Voucher Code] LIKE '%{code}%'";
                if (status != "All")
                {
                    int idx = Array.IndexOf(PVStatusNames, status);
                    expr += $" AND Status = {idx}";
                }
                dt.DefaultView.RowFilter = expr;
                UpdateSummary();
            }
        }

        private void FilterRV()
        {
            if (_rvGrid.DataSource is DataTable dt)
            {
                string code = _rvSearch.Text.Trim().Replace("'", "''");
                string status = _rvStatusFilter.SelectedItem?.ToString();
                string expr = "1=1";
                if (!string.IsNullOrEmpty(code)) expr += $" AND [Voucher Code] LIKE '%{code}%'";
                if (status != "All")
                {
                    int idx = Array.IndexOf(RVStatusNames, status);
                    expr += $" AND Status = {idx}";
                }
                dt.DefaultView.RowFilter = expr;
                UpdateSummary();
            }
        }

        private void LoadAll()
        {
            _pvGrid.DataSource = _pvCtrl.GetAllPaymentVouchers();
            _rvGrid.DataSource = _rvCtrl.GetAllReceiptVouchers();

            ConfigureVoucherGrid(_pvGrid);
            ConfigureVoucherGrid(_rvGrid);

            UpdateSummary();
            LoadCharts();
        }

        private static void ConfigureVoucherGrid(DataGridView grid)
        {
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
            if (grid.Columns.Contains("Status")) grid.Columns["Status"].Visible = false;
            if (grid.Columns.Contains("Status Label"))
            {
                grid.Columns["Status Label"].DisplayIndex = grid.Columns.Count - 1;
                grid.Columns["Status Label"].HeaderText = "Status";
            }
        }

        private void UpdateSummary()
        {
            decimal totalIn = 0;
            if (_rvGrid.DataSource is DataTable dtIn)
            {
                foreach (DataRowView row in dtIn.DefaultView)
                {
                    if (Convert.ToInt32(row["Status"]) != 2)
                        // 💡 4. 配合數據表回傳的別名進行加總
                        totalIn += Convert.ToDecimal(row["Amount"]);
                }
            }
            decimal totalOut = 0;
            if (_pvGrid.DataSource is DataTable dtOut)
            {
                foreach (DataRowView row in dtOut.DefaultView)
                {
                    if (Convert.ToInt32(row["Status"]) != 3)
                        totalOut += Convert.ToDecimal(row["Amount"]);
                }
            }
            _lblTotalIncome.Text = $"RM {totalIn:N2}";
            _lblTotalExpense.Text = $"RM {totalOut:N2}";
            _lblNetFlow.Text = $"RM {(totalIn - totalOut):N2}";
            _lblNetFlow.ForeColor = (totalIn - totalOut) >= 0 ? Color.DarkGreen : Color.DarkRed;
        }

        private void LoadCharts()
        {
            try
            {
                // ==========================================
                // 1. 載入收入長條圖 (趨勢) 
                // ==========================================
                DataTable dtIncomeTrend = _rvCtrl.GetIncomeTrend();
                if (dtIncomeTrend != null && dtIncomeTrend.Rows.Count > 0)
                {
                    string[] labels = new string[dtIncomeTrend.Rows.Count];
                    decimal[] values = new decimal[dtIncomeTrend.Rows.Count];
                    for (int i = 0; i < dtIncomeTrend.Rows.Count; i++)
                    {
                        labels[i] = dtIncomeTrend.Rows[i]["Month"]?.ToString() ?? "";
                        values[i] = Convert.ToDecimal(dtIncomeTrend.Rows[i]["Total"]);
                    }
                    _incomeChart.SetBarData(labels, values, "RM");
                }
                else
                {
                    _incomeChart.SetBarData(new string[0], new decimal[0]);
                }

                // ==========================================
                // 2. 載入收入圓餅圖 (支付方式佔比) - 💡 安全增強版
                // ==========================================
                DataTable dtIncomeMethod = _rvCtrl.GetIncomeByMethod();
                if (dtIncomeMethod != null && dtIncomeMethod.Rows.Count > 0)
                {
                    string[] labels = new string[dtIncomeMethod.Rows.Count];
                    float[] values = new float[dtIncomeMethod.Rows.Count];
                    for (int i = 0; i < dtIncomeMethod.Rows.Count; i++)
                    {
                        string rawMethod = dtIncomeMethod.Rows[i]["Method"]?.ToString() ?? "";

                        // 💡 彈性解析：如果資料庫存的是數字整數則轉成文字，如果是字串則直接顯示
                        if (int.TryParse(rawMethod, out int methodIdx))
                        {
                            labels[i] = methodIdx >= 0 && methodIdx < MethodNames.Length ? MethodNames[methodIdx] : "Other";
                        }
                        else
                        {
                            labels[i] = string.IsNullOrEmpty(rawMethod) ? "Unknown" : rawMethod;
                        }

                        values[i] = Convert.ToSingle(dtIncomeMethod.Rows[i]["Total"]);
                    }
                    _incomePie.SetData(labels, values);
                    _incomePie.Invalidate(); // 💡 強制元件重新觸發 OnPaint 繪圖
                }
                else
                {
                    _incomePie.SetData(new string[0], new float[0]);
                }

                // ==========================================
                // 3. 載入支出長條圖 (趨勢)
                // ==========================================
                DataTable dtExpenseTrend = _pvCtrl.GetExpenseTrend();
                if (dtExpenseTrend != null && dtExpenseTrend.Rows.Count > 0)
                {
                    string[] labels = new string[dtExpenseTrend.Rows.Count];
                    decimal[] values = new decimal[dtExpenseTrend.Rows.Count];
                    for (int i = 0; i < dtExpenseTrend.Rows.Count; i++)
                    {
                        labels[i] = dtExpenseTrend.Rows[i]["Month"]?.ToString() ?? "";
                        values[i] = Convert.ToDecimal(dtExpenseTrend.Rows[i]["Total"]);
                    }
                    _expenseChart.SetBarData(labels, values, "RM");
                }
                else
                {
                    _expenseChart.SetBarData(new string[0], new decimal[0]);
                }

                // ==========================================
                // 4. 載入支出圓餅圖 (支付方式佔比) - 💡 安全增強版
                // ==========================================
                DataTable dtExpenseMethod = _pvCtrl.GetExpenseByMethod();
                if (dtExpenseMethod != null && dtExpenseMethod.Rows.Count > 0)
                {
                    string[] labels = new string[dtExpenseMethod.Rows.Count];
                    float[] values = new float[dtExpenseMethod.Rows.Count];
                    for (int i = 0; i < dtExpenseMethod.Rows.Count; i++)
                    {
                        string rawMethod = dtExpenseMethod.Rows[i]["Method"]?.ToString() ?? "";

                        if (int.TryParse(rawMethod, out int methodIdx))
                        {
                            labels[i] = methodIdx >= 0 && methodIdx < MethodNames.Length ? MethodNames[methodIdx] : "Other";
                        }
                        else
                        {
                            labels[i] = string.IsNullOrEmpty(rawMethod) ? "Unknown" : rawMethod;
                        }

                        values[i] = Convert.ToSingle(dtExpenseMethod.Rows[i]["Total"]);
                    }
                    _expensePie.SetData(labels, values);
                    _expensePie.Invalidate(); // 💡 強制重新繪製
                }
                else
                {
                    _expensePie.SetData(new string[0], new float[0]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Chart Data Transform Error: " + ex.Message);
            }
        }

        private void VerifySelectedReceipt()
        {
            if (_rvGrid?.CurrentRow == null)
            {
                UITheme.ShowWarning("Please select a receipt voucher first.");
                return;
            }
            if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, PermissionAction.Edit, this)) return;

            long rvId = Convert.ToInt64(_rvGrid.CurrentRow.Cells["ID"].Value);
            var receipt = _rvCtrl.GetById(rvId);
            if (receipt == null) return;

            if (receipt.Status == 1)
            {
                UITheme.ShowWarning("This receipt voucher is already verified.");
                return;
            }

            var allocationTable = CreateAllocationEditorTable();
            try
            {
                var existing = _rvCtrl.GetInvoiceAllocationsForEditor(rvId);
                if (existing != null && existing.Rows.Count > 0)
                {
                    foreach (DataRow row in existing.Rows)
                    {
                        object invId = row["Invoice ID"];
                        string code = row["Invoice Code"]?.ToString() ?? "";
                        if (invId == DBNull.Value || invId == null)
                            code = "(Exchange Loss)";
                        allocationTable.Rows.Add(
                            invId == DBNull.Value ? DBNull.Value : invId,
                            code,
                            row["Allocated Amount"],
                            row["Clearing Type"]);
                    }
                }
                else
                {
                    allocationTable.Rows.Add(DBNull.Value, "", receipt.PaymentAmount, 2);
                }
            }
            catch
            {
                allocationTable.Rows.Add(DBNull.Value, "", receipt.PaymentAmount, 2);
            }

            DataTable invoicePicker = null;
            try { invoicePicker = BuildInvoicePickerWithBlank(_invoiceCtrl.GetInvoicesForCustomerPicker(receipt.CusomerID)); } catch { }
            DataTable clearingPicker = BuildClearingTypeDataTable();

            using (var dlg = new Form())
            {
                dlg.Text = "Verify Receipt — Allocate to Invoices";
                dlg.Size = new Size(820, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(720, 400);
                dlg.BackColor = UITheme.Background;

                var top = new Panel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(12, 8, 12, 4) };
                var lblAmount = new Label
                {
                    Text = $"Receipt: {receipt.ReceiptVoucherCode}  |  Amount: {receipt.PaymentAmount:N2}",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = UITheme.TextDark,
                    Location = new Point(0, 4)
                };
                var lblCustomer = new Label
                {
                    AutoSize = true,
                    ForeColor = UITheme.TextGray,
                    Location = new Point(0, 24)
                };
                try
                {
                    var cust = _customerCtrl.GetById(receipt.CusomerID);
                    lblCustomer.Text = cust != null
                        ? $"Customer: {cust.CustomerCode} — {cust.CustomerName}"
                        : $"Customer ID: {receipt.CusomerID}";
                }
                catch { lblCustomer.Text = $"Customer ID: {receipt.CusomerID}"; }
                var lblRemain = new Label
                {
                    AutoSize = true,
                    ForeColor = UITheme.TextGray,
                    Location = new Point(0, 44)
                };
                top.Controls.Add(lblAmount);
                top.Controls.Add(lblCustomer);
                top.Controls.Add(lblRemain);

                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };

                var colInvoice = new DataGridViewComboBoxColumn
                {
                    Name = "colInvoice",
                    HeaderText = "Invoice *",
                    DataPropertyName = "Invoice ID",
                    DisplayMember = "DisplayText",
                    ValueMember = "Invoice ID",
                    Width = 280,
                    FlatStyle = FlatStyle.Flat
                };
                if (invoicePicker != null)
                    colInvoice.DataSource = invoicePicker;

                grid.Columns.Add(colInvoice);
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "colAmount",
                    HeaderText = "Allocated Amount *",
                    DataPropertyName = "Allocated Amount",
                    Width = 140
                });
                var colType = new DataGridViewComboBoxColumn
                {
                    Name = "colType",
                    HeaderText = "Clearing Type *",
                    DataPropertyName = "Clearing Type",
                    Width = 180,
                    FlatStyle = FlatStyle.Flat,
                    DataSource = clearingPicker,
                    DisplayMember = "Value",
                    ValueMember = "Code"
                };
                grid.Columns.Add(colType);
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "colCode",
                    HeaderText = "Invoice Code",
                    DataPropertyName = "Invoice Code",
                    Visible = false
                });

                grid.DataSource = allocationTable;
                foreach (DataGridViewRow row in grid.Rows)
                    if (!row.IsNewRow) ApplyExchangeLossRowState(row);
                grid.DataError += (s, e) => { e.ThrowException = false; };
                grid.CellValueChanged += (s, e) =>
                {
                    if (e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "colType" && e.RowIndex >= 0)
                        ApplyExchangeLossRowState(grid.Rows[e.RowIndex]);
                    UpdateAllocationRemainLabel(grid, receipt.PaymentAmount, lblRemain);
                };
                grid.CurrentCellDirtyStateChanged += (s, e) =>
                {
                    if (grid.IsCurrentCellDirty)
                        grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };
                grid.EditingControlShowing += (s, e) =>
                {
                    if (grid.CurrentCell?.OwningColumn?.Name == "colInvoice" && e.Control is ComboBox cmb)
                    {
                        cmb.SelectedIndexChanged -= AllocationInvoiceChanged;
                        cmb.SelectedIndexChanged += AllocationInvoiceChanged;
                    }
                };

                void AllocationInvoiceChanged(object sender, EventArgs e)
                {
                    if (grid.CurrentRow == null) return;
                    if (sender is ComboBox cmb && cmb.SelectedValue != null && cmb.SelectedValue != DBNull.Value)
                    {
                        if (!long.TryParse(cmb.SelectedValue.ToString(), out long invId) || invId <= 0)
                            return;
                        grid.CurrentRow.Cells["colInvoice"].Value = invId;
                        if (invoicePicker != null)
                        {
                            foreach (DataRow r in invoicePicker.Rows)
                            {
                                if (r["Invoice ID"] == DBNull.Value) continue;
                                if (!long.TryParse(r["Invoice ID"]?.ToString(), out long rowInvId) || rowInvId != invId)
                                    continue;
                                grid.CurrentRow.Cells["colCode"].Value = r["Invoice Code"]?.ToString();
                                break;
                            }
                        }
                    }
                }

                UpdateAllocationRemainLabel(grid, receipt.PaymentAmount, lblRemain);

                var btnAdd = UITheme.CreateSecondaryButton("Add Line");
                btnAdd.Width = 100;
                var btnRemove = UITheme.CreateSecondaryButton("Remove Line");
                btnRemove.Width = 110;
                var btnAddExchangeLoss = UITheme.CreateSecondaryButton("Add Exchange Loss");
                btnAddExchangeLoss.Width = 150;
                btnAdd.Click += (s, e) =>
                {
                    allocationTable.Rows.Add(DBNull.Value, "", 0m, ReceiptVoucherConstants.ClearingPartial);
                    UpdateAllocationRemainLabel(grid, receipt.PaymentAmount, lblRemain);
                };
                btnRemove.Click += (s, e) =>
                {
                    if (grid.CurrentRow == null || allocationTable.Rows.Count <= 1)
                    {
                        UITheme.ShowWarning("At least one allocation line is required.");
                        return;
                    }
                    allocationTable.Rows.RemoveAt(grid.CurrentRow.Index);
                    UpdateAllocationRemainLabel(grid, receipt.PaymentAmount, lblRemain);
                };
                btnAddExchangeLoss.Click += (s, e) =>
                {
                    decimal sum = SumAllocationGridAmounts(grid);
                    decimal remain = receipt.PaymentAmount - sum;
                    if (remain <= 0.01m)
                    {
                        UITheme.ShowWarning("Receipt is already fully allocated. Adjust line amounts before adding exchange loss.");
                        return;
                    }
                    allocationTable.Rows.Add(
                        DBNull.Value,
                        "(Exchange Loss)",
                        remain,
                        ReceiptVoucherConstants.ClearingExchangeLoss);
                    if (grid.Rows.Count > 0)
                        ApplyExchangeLossRowState(grid.Rows[grid.Rows.Count - 1]);
                    UpdateAllocationRemainLabel(grid, receipt.PaymentAmount, lblRemain);
                };

                var lineToolbar = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    FlowDirection = FlowDirection.LeftToRight,
                    Padding = new Padding(12, 4, 12, 4)
                };
                lineToolbar.Controls.Add(btnAdd);
                lineToolbar.Controls.Add(btnRemove);
                lineToolbar.Controls.Add(btnAddExchangeLoss);

                var btnSave = UITheme.CreatePrimaryButton("Verify Receipt");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!TryBuildReceiptAllocations(grid, out var allocations, out string error))
                    {
                        UITheme.ShowWarning(error);
                        return;
                    }

                    decimal sum = 0;
                    foreach (var a in allocations) sum += a.ReceivedAmount;
                    if (Math.Abs(sum - receipt.PaymentAmount) > 0.01m)
                    {
                        UITheme.ShowWarning(
                            $"Allocated total ({sum:N2}) does not equal receipt amount ({receipt.PaymentAmount:N2}). " +
                            "Adjust lines or use Add Exchange Loss for the difference.");
                        return;
                    }

                    var result = _financeWorkflow.ConfirmReceiptWithAllocations(rvId, allocations);
                    if (result.Success)
                    {
                        UITheme.ShowSuccess(result.Message);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadAll();
                    }
                    else UITheme.ShowWarning(result.Message);
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

                dlg.Controls.Add(btnPanel);
                dlg.Controls.Add(top);
                dlg.Controls.Add(lineToolbar);
                dlg.Controls.Add(grid);
                dlg.ShowDialog(this);
            }
        }

        private static DataTable CreateAllocationEditorTable()
        {
            var dt = new DataTable();
            var colInv = dt.Columns.Add("Invoice ID", typeof(long));
            colInv.AllowDBNull = true;
            dt.Columns.Add("Invoice Code", typeof(string));
            dt.Columns.Add("Allocated Amount", typeof(decimal));
            dt.Columns.Add("Clearing Type", typeof(int));
            return dt;
        }

        private static decimal SumAllocationGridAmounts(DataGridView grid)
        {
            decimal sum = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (decimal.TryParse(row.Cells["colAmount"]?.Value?.ToString(), out decimal amt))
                    sum += amt;
            }
            return sum;
        }

        private static void ApplyExchangeLossRowState(DataGridViewRow row)
        {
            if (row == null) return;
            int typeCode = GetClearingTypeFromCell(row.Cells["colType"]?.Value, ReceiptVoucherConstants.ClearingPartial);
            bool isLoss = ReceiptVoucherConstants.IsExchangeLoss(typeCode);
            if (isLoss)
            {
                row.Cells["colInvoice"].Value = DBNull.Value;
                row.Cells["colCode"].Value = "(Exchange Loss)";
                row.Cells["colInvoice"].ReadOnly = true;
            }
            else
            {
                row.Cells["colInvoice"].ReadOnly = false;
            }
        }

        private static void UpdateAllocationRemainLabel(DataGridView grid, decimal receiptAmount, Label lblRemain)
        {
            decimal sum = SumAllocationGridAmounts(grid);
            decimal remain = receiptAmount - sum;
            lblRemain.Text = $"Allocated: {sum:N2}  |  Remaining: {remain:N2}" +
                             (Math.Abs(remain) < 0.01m ? " (balanced)" : "");
            lblRemain.ForeColor = Math.Abs(remain) < 0.01m ? Color.DarkGreen : Color.DarkOrange;
        }

        private static bool TryBuildReceiptAllocations(DataGridView grid, out List<ReceiptAllocation> allocations, out string error)
        {
            allocations = new List<ReceiptAllocation>();
            error = null;
            var seenInvoices = new HashSet<long>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                int typeCode = GetClearingTypeFromCell(row.Cells["colType"]?.Value, ReceiptVoucherConstants.ClearingPartial);
                if (typeCode <= 0)
                {
                    error = "Each line must have a clearing type.";
                    return false;
                }

                if (!decimal.TryParse(row.Cells["colAmount"]?.Value?.ToString(), out decimal amount) || amount <= 0)
                {
                    error = "Each line must have a positive allocated amount.";
                    return false;
                }

                long? invoiceId = null;
                if (ReceiptVoucherConstants.IsExchangeLoss(typeCode))
                {
                    object invObj = row.Cells["colInvoice"]?.Value;
                    if (invObj != null && invObj != DBNull.Value &&
                        long.TryParse(invObj.ToString(), out long bogusInv) && bogusInv > 0)
                    {
                        error = "Exchange loss lines must not be linked to an invoice.";
                        return false;
                    }
                }
                else
                {
                    object invObj = row.Cells["colInvoice"]?.Value;
                    if (invObj == null || invObj == DBNull.Value ||
                        !long.TryParse(invObj.ToString(), out long invId) || invId <= 0)
                    {
                        error = "Each non-exchange-loss line must have an invoice selected.";
                        return false;
                    }
                    if (!seenInvoices.Add(invId))
                    {
                        error = "Duplicate invoice on allocation lines. Combine amounts into one line per invoice.";
                        return false;
                    }
                    invoiceId = invId;
                }

                allocations.Add(new ReceiptAllocation
                {
                    InvoiceId = invoiceId,
                    ReceivedAmount = amount,
                    Type = typeCode
                });
            }
            if (allocations.Count == 0)
            {
                error = "Add at least one allocation line.";
                return false;
            }
            return true;
        }

        private void ShowSelectedPaymentVoucherDetail()
        {
            if (_pvGrid?.CurrentRow?.Cells["ID"]?.Value == null)
            {
                UITheme.ShowWarning("Please select a payment voucher first.");
                return;
            }
            ShowPaymentVoucherDetail(Convert.ToInt64(_pvGrid.CurrentRow.Cells["ID"].Value));
        }

        private void ShowPaymentVoucherDetail(long paymentVoucherId)
        {
            try
            {
                var pv = _pvCtrl.GetById(paymentVoucherId);
                var header = _pvCtrl.GetHeaderDetail(paymentVoucherId);
                var lines = _pvCtrl.GetPurchaseOrderSettlementsDetailed(paymentVoucherId);
                string code = header?.Rows.Count > 0 ? header.Rows[0]["Voucher Code"]?.ToString() : paymentVoucherId.ToString();
                ShowVoucherDetailDialog(
                    $"Payment Voucher — {code}",
                    DetailViewHelper.SingleRowToFieldValueTable(header),
                    lines,
                    "PO Settlements",
                    pv?.SupplierID ?? 0,
                    isPaymentVoucher: true);
            }
            catch (Exception ex) { UITheme.ShowError(ex.Message); }
        }

        private void ShowSelectedReceiptVoucherDetail()
        {
            if (_rvGrid?.CurrentRow?.Cells["ID"]?.Value == null)
            {
                UITheme.ShowWarning("Please select a receipt voucher first.");
                return;
            }
            ShowReceiptVoucherDetail(Convert.ToInt64(_rvGrid.CurrentRow.Cells["ID"].Value));
        }

        private void ShowReceiptVoucherDetail(long receiptVoucherId)
        {
            try
            {
                var rv = _rvCtrl.GetById(receiptVoucherId);
                var header = _rvCtrl.GetHeaderDetail(receiptVoucherId);
                var lines = _rvCtrl.GetInvoiceAllocationsDetailed(receiptVoucherId);
                if (lines != null && lines.Columns.Contains("Invoice ID"))
                    lines.Columns.Remove("Invoice ID");
                string code = header?.Rows.Count > 0 ? header.Rows[0]["Voucher Code"]?.ToString() : receiptVoucherId.ToString();
                ShowVoucherDetailDialog(
                    $"Receipt Voucher — {code}",
                    DetailViewHelper.SingleRowToFieldValueTable(header),
                    lines,
                    "Invoice Allocations",
                    rv?.CusomerID ?? 0,
                    isPaymentVoucher: false);
            }
            catch (Exception ex) { UITheme.ShowError(ex.Message); }
        }

        private void ShowVoucherDetailDialog(string title, DataTable voucherFields, DataTable lines, string linesTabTitle, long partyId, bool isPaymentVoucher)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };

                var tabVoucher = new TabPage("Voucher");
                var voucherGrid = GridHelper.CreateStyledGrid();
                voucherGrid.DataSource = voucherFields;
                GridHelper.StyleGrid(voucherGrid);
                voucherGrid.Dock = DockStyle.Fill;
                tabVoucher.Controls.Add(voucherGrid);
                tabs.TabPages.Add(tabVoucher);

                if (partyId > 0)
                {
                    if (isPaymentVoucher && AppSession.CanView(PermissionModule.Supplier))
                        tabs.TabPages.Add(BuildSupplierProfileTab(partyId));
                    else if (!isPaymentVoucher && AppSession.CanView(PermissionModule.Customer))
                        tabs.TabPages.Add(BuildCustomerProfileTab(partyId));
                }

                if (lines != null)
                {
                    var tabLines = new TabPage(linesTabTitle);
                    var lineGrid = GridHelper.CreateStyledGrid();
                    lineGrid.DataSource = lines;
                    GridHelper.StyleGrid(lineGrid);
                    lineGrid.Dock = DockStyle.Fill;
                    tabLines.Controls.Add(lineGrid);
                    tabs.TabPages.Add(tabLines);
                }

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(tabs);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private TabPage BuildSupplierProfileTab(long supplierId)
        {
            var tab = new TabPage("Supplier");
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };

            var profileGrid = GridHelper.CreateStyledGrid();
            profileGrid.DataSource = BuildSupplierProfileFields(supplierId);
            GridHelper.StyleGrid(profileGrid);
            profileGrid.Dock = DockStyle.Fill;

            var quoteGrid = GridHelper.CreateStyledGrid();
            try
            {
                quoteGrid.DataSource = _supplierCtrl.GetRawMaterialQuotesBySupplier(supplierId);
                GridHelper.StyleGrid(quoteGrid);
            }
            catch { }
            quoteGrid.Dock = DockStyle.Fill;

            split.Panel1.Controls.Add(profileGrid);
            split.Panel2.Controls.Add(quoteGrid);
            tab.Controls.Add(split);
            return tab;
        }

        private TabPage BuildCustomerProfileTab(long customerId)
        {
            var tab = new TabPage("Customer");
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 180 };

            var profileGrid = GridHelper.CreateStyledGrid();
            profileGrid.DataSource = BuildCustomerProfileFields(customerId);
            GridHelper.StyleGrid(profileGrid);
            profileGrid.Dock = DockStyle.Fill;

            var bottomTabs = new TabControl { Dock = DockStyle.Fill };
            var contactGrid = GridHelper.CreateStyledGrid();
            contactGrid.DataSource = BuildCustomerContactsTable(customerId);
            GridHelper.StyleGrid(contactGrid);
            contactGrid.Dock = DockStyle.Fill;
            var contactTab = new TabPage("Contact Persons");
            contactTab.Controls.Add(contactGrid);

            var addressGrid = GridHelper.CreateStyledGrid();
            addressGrid.DataSource = BuildCustomerDeliveryAddressesTable(customerId);
            GridHelper.StyleGrid(addressGrid);
            addressGrid.Dock = DockStyle.Fill;
            var addressTab = new TabPage("Delivery Addresses");
            addressTab.Controls.Add(addressGrid);

            bottomTabs.TabPages.Add(contactTab);
            bottomTabs.TabPages.Add(addressTab);

            split.Panel1.Controls.Add(profileGrid);
            split.Panel2.Controls.Add(bottomTabs);
            tab.Controls.Add(split);
            return tab;
        }

        private DataTable BuildSupplierProfileFields(long supplierId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            var supplier = _supplierCtrl.GetById(supplierId);
            if (supplier == null) return dt;

            AddFieldRow(dt, "Supplier Name", supplier.SupplierName);
            AddFieldRow(dt, "Contact Person", supplier.ContactPerson);
            AddFieldRow(dt, "Phone", supplier.Phone);
            AddFieldRow(dt, "Email", supplier.Email);
            AddFieldRow(dt, "Billing Address", supplier.BillingAddress);
            AddFieldRow(dt, "Payment Term", supplier.PaymentTerm);
            AddFieldRow(dt, "Bank Account", supplier.BankAccount);
            AddFieldRow(dt, "Status", supplier.Status.ToString());
            return dt;
        }

        private DataTable BuildCustomerProfileFields(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            var customer = _customerCtrl.GetById(customerId);
            if (customer == null) return dt;

            AddFieldRow(dt, "Customer Code", customer.CustomerCode);
            AddFieldRow(dt, "Customer Ref Number", customer.CustomerRefNumber);
            AddFieldRow(dt, "Customer Name", customer.CustomerName);
            AddFieldRow(dt, "Billing Address", customer.BillingAddress);
            AddFieldRow(dt, "Payment Term", customer.PaymentTerm);
            return dt;
        }

        private DataTable BuildCustomerContactsTable(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Title");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var contact in _customerCtrl.GetContactPersons(customerId))
            {
                dt.Rows.Add(contact.Name, contact.Title, contact.Phone, contact.Email);
            }
            return dt;
        }

        private DataTable BuildCustomerDeliveryAddressesTable(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Delivery Address");
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var addr in _customerCtrl.GetDeliveryAddresses(customerId))
            {
                dt.Rows.Add(addr.DeliveryAddress, addr.ContactPerson, addr.Phone, addr.Email);
            }
            return dt;
        }

        private static void AddFieldRow(DataTable dt, string field, string value)
        {
            dt.Rows.Add(field, value ?? "");
        }

        private void ShowUpdatePaymentVoucherStatusDialog()
        {
            if (_pvGrid?.CurrentRow?.Cells["ID"]?.Value == null)
            {
                UITheme.ShowWarning("Please select a payment voucher first.");
                return;
            }
            if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, PermissionAction.Edit, this)) return;

            long id = Convert.ToInt64(_pvGrid.CurrentRow.Cells["ID"].Value);
            var pv = _pvCtrl.GetById(id);
            if (pv == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Update Payment Voucher Status";
                dlg.Size = new Size(420, 220);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var lblCurrent = new Label
                {
                    Text = pv.Status >= 0 && pv.Status < PVStatusNames.Length ? PVStatusNames[pv.Status] : pv.Status.ToString(),
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
                cmbStatus.Items.AddRange(PVStatusNames);
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(pv.Status, PVStatusNames.Length - 1));

                UITheme.AddFormRow(layout, 0, "Current", lblCurrent);
                UITheme.AddFormRow(layout, 1, "New Status", cmbStatus);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, PermissionAction.Edit, dlg)) return;
                    if (!_pvCtrl.UpdateStatus(id, cmbStatus.SelectedIndex))
                    {
                        UITheme.ShowWarning("Failed to update status.");
                        return;
                    }
                    UITheme.ShowSuccess("Status updated.");
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
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAll();
            }
        }

        private void ShowUpdateReceiptVoucherStatusDialog()
        {
            if (_rvGrid?.CurrentRow?.Cells["ID"]?.Value == null)
            {
                UITheme.ShowWarning("Please select a receipt voucher first.");
                return;
            }
            if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, PermissionAction.Edit, this)) return;

            long id = Convert.ToInt64(_rvGrid.CurrentRow.Cells["ID"].Value);
            var rv = _rvCtrl.GetById(id);
            if (rv == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Update Receipt Voucher Status";
                dlg.Size = new Size(440, 240);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var lblCurrent = new Label
                {
                    Text = rv.Status >= 0 && rv.Status < RVStatusNames.Length ? RVStatusNames[rv.Status] : rv.Status.ToString(),
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
                cmbStatus.Items.AddRange(RVStatusNames);
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(rv.Status, RVStatusNames.Length - 1));
                var lblHint = new Label
                {
                    Text = "Confirmed vouchers cannot revert to Draft.",
                    AutoSize = false,
                    MaximumSize = new Size(280, 0),
                    ForeColor = UITheme.TextGray,
                    Font = new Font("Segoe UI", 8.5f)
                };

                UITheme.AddFormRow(layout, 0, "Current", lblCurrent);
                UITheme.AddFormRow(layout, 1, "New Status", cmbStatus);
                UITheme.AddFormRow(layout, 2, "", lblHint);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, PermissionAction.Edit, dlg)) return;
                    if (!_rvCtrl.TryUpdateStatus(id, cmbStatus.SelectedIndex, out string error))
                    {
                        UITheme.ShowWarning(string.IsNullOrWhiteSpace(error) ? "Failed to update status." : error);
                        return;
                    }
                    UITheme.ShowSuccess("Status updated.");
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
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAll();
            }
        }

        private ComboBox BuildSupplierCombo(long selectedSupplierId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 280 };
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

        private ComboBox BuildCustomerCombo(long selectedCustomerId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
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

        private long ResolveSupplierId(ComboBox cmbSupplier)
        {
            long id = GetComboLongId(cmbSupplier);
            if (id > 0) return id;
            string name = (cmbSupplier.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return 0;
            return _supplierCtrl.FindSupplierIdByName(name);
        }

        private DataTable BuildPoPickerForSupplier(long supplierId, IEnumerable<long> ensurePoIds = null)
        {
            DataTable dt;
            if (supplierId <= 0)
            {
                dt = new DataTable();
                dt.Columns.Add("Purchase Order ID", typeof(long));
                dt.Columns["Purchase Order ID"].AllowDBNull = true;
                dt.Columns.Add("Purchase Order Code", typeof(string));
                dt.Columns.Add("Request Delivery Date", typeof(object));
                dt.Columns.Add("Supplier ID", typeof(long));
                dt.Columns.Add("DisplayText", typeof(string));
            }
            else
            {
                dt = _poCtrl.GetPurchaseOrdersForSupplierPicker(supplierId);
                if (dt == null)
                {
                    dt = new DataTable();
                    dt.Columns.Add("Purchase Order ID", typeof(long));
                    dt.Columns["Purchase Order ID"].AllowDBNull = true;
                    dt.Columns.Add("DisplayText", typeof(string));
                }
            }

            if (dt != null && !dt.Columns.Contains("DisplayText"))
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    string code = dt.Columns.Contains("Purchase Order Code") ? row["Purchase Order Code"]?.ToString() : "";
                    string reqDate = !dt.Columns.Contains("Request Delivery Date") || row["Request Delivery Date"] == DBNull.Value
                        ? ""
                        : Convert.ToDateTime(row["Request Delivery Date"]).ToString("yyyy-MM-dd");
                    row["DisplayText"] = string.IsNullOrEmpty(reqDate) ? code : $"{code} (Req: {reqDate})";
                }
            }

            if (ensurePoIds != null && dt != null)
            {
                foreach (long poId in ensurePoIds)
                {
                    if (poId <= 0) continue;
                    bool found = false;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Purchase Order ID"] != DBNull.Value &&
                            Convert.ToInt64(row["Purchase Order ID"]) == poId)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) continue;

                    var po = _poCtrl.GetById(poId);
                    if (po == null) continue;
                    string reqStr = po.RequestDeliveryDate == default ? "" : po.RequestDeliveryDate.ToString("yyyy-MM-dd");
                    string display = string.IsNullOrEmpty(reqStr) ? po.PurchaseOrderCode : $"{po.PurchaseOrderCode} (Req: {reqStr})";
                    if (!dt.Columns.Contains("Purchase Order Code"))
                        dt.Columns.Add("Purchase Order Code", typeof(string));
                    if (!dt.Columns.Contains("Request Delivery Date"))
                        dt.Columns.Add("Request Delivery Date", typeof(object));
                    if (!dt.Columns.Contains("Supplier ID"))
                        dt.Columns.Add("Supplier ID", typeof(long));
                    dt.Rows.Add(poId, po.PurchaseOrderCode,
                        po.RequestDeliveryDate == default ? (object)DBNull.Value : po.RequestDeliveryDate,
                        po.SupplierID, display);
                }
            }

            if (dt != null)
                InsertBlankPickerRow(dt, "Purchase Order ID", "DisplayText", "(Select PO)");
            return dt;
        }

        private static DataTable BuildClearingTypeDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Code", typeof(int));
            dt.Columns.Add("Value", typeof(string));
            foreach (var item in DictionaryService.GetItems(DictionaryService.Categories.PoPaymentType))
                dt.Rows.Add(item.Key, item.Value);
            return dt;
        }

        private static DataTable BuildInvoicePickerWithBlank(DataTable source)
        {
            if (source == null)
            {
                source = new DataTable();
                source.Columns.Add("Invoice ID", typeof(long));
                source.Columns["Invoice ID"].AllowDBNull = true;
                source.Columns.Add("DisplayText", typeof(string));
            }
            if (!source.Columns.Contains("DisplayText"))
                source.Columns.Add("DisplayText", typeof(string));
            if (!source.Columns["Invoice ID"].AllowDBNull)
                source.Columns["Invoice ID"].AllowDBNull = true;
            InsertBlankPickerRow(source, "Invoice ID", "DisplayText", "(Select Invoice)");
            return source;
        }

        private static void InsertBlankPickerRow(DataTable dt, string idColumn, string displayColumn, string blankLabel)
        {
            if (dt == null || !dt.Columns.Contains(idColumn)) return;
            if (!dt.Columns[idColumn].AllowDBNull)
                dt.Columns[idColumn].AllowDBNull = true;
            if (!dt.Columns.Contains(displayColumn))
                dt.Columns.Add(displayColumn, typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                if (row[idColumn] == DBNull.Value) return;
            }

            var blank = dt.NewRow();
            blank[idColumn] = DBNull.Value;
            blank[displayColumn] = blankLabel;
            dt.Rows.InsertAt(blank, 0);
        }

        private static IEnumerable<long> CollectPoIdsFromGrid(DataGridView grid)
        {
            var ids = new List<long>();
            if (grid == null) return ids;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                object val = row.Cells["colPo"]?.Value;
                if (val == null || val == DBNull.Value) continue;
                if (long.TryParse(val.ToString(), out long poId) && poId > 0)
                    ids.Add(poId);
            }
            return ids;
        }

        private static int GetClearingTypeFromCell(object cellValue, int defaultCode)
        {
            if (cellValue is int intVal) return intVal > 0 ? intVal : defaultCode;
            if (cellValue is DictionaryUIHelper.ComboBoxItem typeItem) return typeItem.Code;
            if (cellValue != null && int.TryParse(cellValue.ToString(), out int parsed) && parsed > 0)
                return parsed;
            return defaultCode;
        }

        private static DataTable CreatePoSettlementEditorTable()
        {
            var dt = new DataTable();
            var colPo = dt.Columns.Add("Purchase Order ID", typeof(long));
            colPo.AllowDBNull = true;
            dt.Columns.Add("Purchase Order Code", typeof(string));
            dt.Columns.Add("Request Delivery Date", typeof(string));
            dt.Columns.Add("Clearing Type", typeof(int));
            dt.Columns.Add("Pay Amount", typeof(decimal));
            return dt;
        }

        private static void SyncPoRowFromPicker(DataGridViewRow row, DataTable poPicker)
        {
            if (row == null || poPicker == null) return;
            object val = row.Cells["colPo"]?.Value;
            if (val == null || !long.TryParse(val.ToString(), out long poId) || poId <= 0) return;
            foreach (DataRow pr in poPicker.Rows)
            {
                if (pr["Purchase Order ID"] == DBNull.Value) continue;
                if (Convert.ToInt64(pr["Purchase Order ID"]) != poId) continue;
                row.Cells["colReqDate"].Value = pr["Request Delivery Date"] == DBNull.Value
                    ? ""
                    : Convert.ToDateTime(pr["Request Delivery Date"]).ToString("yyyy-MM-dd");
                if (row.DataBoundItem is DataRowView drv)
                {
                    drv.Row["Purchase Order Code"] = pr["Purchase Order Code"]?.ToString();
                    drv.Row["Request Delivery Date"] = row.Cells["colReqDate"].Value;
                }
                break;
            }
        }

        private static bool TryBuildPoSettlements(DataGridView grid, decimal voucherAmount,
            out List<VoucherPurchaseOrderLine> lines, out string error)
        {
            lines = new List<VoucherPurchaseOrderLine>();
            error = null;
            var seenPo = new HashSet<long>();
            decimal sum = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                object poObj = row.Cells["colPo"]?.Value;
                if (poObj == null || !long.TryParse(poObj.ToString(), out long poId) || poId <= 0)
                    continue;

                if (!seenPo.Add(poId))
                {
                    error = "Duplicate purchase order in settlement lines.";
                    return false;
                }

                if (!decimal.TryParse(row.Cells["colPay"]?.Value?.ToString(), out decimal payAmount) || payAmount <= 0)
                {
                    error = "Each PO line must have a positive pay amount.";
                    return false;
                }

                int typeCode = GetClearingTypeFromCell(row.Cells["colClearing"]?.Value, 1);

                sum += payAmount;
                lines.Add(new VoucherPurchaseOrderLine
                {
                    PurchaseOrderID = poId,
                    ClearingType = typeCode > 0 ? typeCode : 1,
                    PayAmount = payAmount
                });
            }

            if (lines.Count > 0 && Math.Abs(sum - voucherAmount) > 0.01m)
            {
                error = $"PO settlement total ({sum:N2}) must equal voucher amount ({voucherAmount:N2}).";
                return false;
            }
            return true;
        }

        private static bool TryValidateVoucherCode(string code, bool isNew, long recordId,
            Func<string, long, bool> existsByCode, out string error)
        {
            error = null;
            code = (code ?? "").Trim();
            if (string.IsNullOrEmpty(code))
            {
                error = "Voucher Code is required.";
                return false;
            }
            if (code.Length > 30)
            {
                error = "Voucher Code must be 30 characters or less.";
                return false;
            }
            long excludeId = isNew ? 0 : recordId;
            if (existsByCode(code, excludeId))
            {
                error = "Voucher Code already exists. Please use a unique code.";
                return false;
            }
            return true;
        }

        private static long GetComboLongId(ComboBox cmb)
        {
            if (cmb?.SelectedValue == null) return 0;
            return long.TryParse(cmb.SelectedValue.ToString(), out long id) ? id : 0;
        }

        private static void SetComboLongValue(ComboBox cmb, long value)
        {
            if (cmb?.Items == null || value <= 0) return;
            cmb.SelectedValue = value;
            if (cmb.SelectedValue == null || Convert.ToInt64(cmb.SelectedValue) != value)
            {
                for (int i = 0; i < cmb.Items.Count; i++)
                {
                    if (cmb.Items[i] is DataRowView drv &&
                        long.TryParse(drv[cmb.ValueMember]?.ToString(), out long id) && id == value)
                    {
                        cmb.SelectedIndex = i;
                        return;
                    }
                }
            }
        }
    }
}