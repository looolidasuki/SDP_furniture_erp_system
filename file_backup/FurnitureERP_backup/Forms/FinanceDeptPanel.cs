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
    public class FinanceDeptPanel : UserControl
    {
        private readonly PaymentVoucherController _pvCtrl = new PaymentVoucherController();
        private readonly ReceiptVoucherController _rvCtrl = new ReceiptVoucherController();

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
            _tabs.TabPages.Add(BuildOverviewTab());
            _tabs.TabPages.Add(BuildPaymentVoucherTab());
            _tabs.TabPages.Add(BuildReceiptVoucherTab());
            Controls.Add(_tabs);
        }

        private TabPage BuildOverviewTab()
        {
            var tab = new TabPage("Overview & Analytics") { BackColor = UITheme.Background };
            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var summaryPanel = new Panel { Dock = DockStyle.Fill };
            _lblTotalIncome = MakeSummaryCard("Total Income", "RM 0", Color.FromArgb(0, 168, 120), 0, summaryPanel);
            _lblTotalExpense = MakeSummaryCard("Total Expense", "RM 0", Color.FromArgb(220, 60, 60), 220, summaryPanel);
            _lblNetFlow = MakeSummaryCard("Net Cash Flow", "RM 0", Color.FromArgb(63, 118, 210), 440, summaryPanel);
            var btnExportReport = UITheme.CreatePrimaryButton("Print Report PDF");
            btnExportReport.Width = 150;
            btnExportReport.Location = new Point(660, 20);
            btnExportReport.Click += (s, e) => ExportOverviewReportPdf();
            summaryPanel.Controls.Add(btnExportReport);
            outer.Controls.Add(summaryPanel, 0, 0);

            var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var incomeCard = UITheme.CreateCard("Monthly Income (Receipt Vouchers)");
            _incomeChart = new ChartControl { Dock = DockStyle.Fill, BackColor = Color.White };
            incomeCard.Controls.Add(_incomeChart);
            row1.Controls.Add(incomeCard, 0, 0);
            var expenseCard = UITheme.CreateCard("Monthly Expense (Payment Vouchers)");
            _expenseChart = new ChartControl { Dock = DockStyle.Fill, BackColor = Color.White };
            expenseCard.Controls.Add(_expenseChart);
            row1.Controls.Add(expenseCard, 1, 0);
            outer.Controls.Add(row1, 0, 1);

            var row2 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var incomePieCard = UITheme.CreateCard("Income by Payment Method");
            _incomePie = new PieChartControl { Dock = DockStyle.Fill, BackColor = Color.White };
            incomePieCard.Controls.Add(_incomePie);
            row2.Controls.Add(incomePieCard, 0, 0);
            var expensePieCard = UITheme.CreateCard("Expense by Payment Method");
            _expensePie = new PieChartControl { Dock = DockStyle.Fill, BackColor = Color.White };
            expensePieCard.Controls.Add(_expensePie);
            row2.Controls.Add(expensePieCard, 1, 0);
            outer.Controls.Add(row2, 0, 2);

            tab.Controls.Add(outer);
            return tab;
        }

        private Label MakeSummaryCard(string title, string value, Color color, int x, Panel parent)
        {
            var card = new Panel { Width = 200, Height = 64, Location = new Point(x, 8), BackColor = Color.White };
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(color), 0, 0, 6, card.Height);
                using (var pen = new System.Drawing.Pen(Color.FromArgb(220, 228, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            var lblTitle = new Label { Text = title, AutoSize = false, Width = 190, Height = 20, Location = new Point(14, 8), Font = new Font("Segoe UI", 8f), ForeColor = Color.Gray };
            var lblValue = new Label { Text = value, AutoSize = false, Width = 190, Height = 28, Location = new Point(14, 28), Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = color };
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            parent.Controls.Add(card);
            return lblValue;
        }

        private TabPage BuildPaymentVoucherTab()
        {
            var tab = new TabPage("Payment Vouchers") { BackColor = UITheme.Background };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Payment Voucher");
            btnNew.Location = new Point(0, 8);
            btnNew.Click += (s, e) => ShowCreatePVDialog();
            var btnRefresh = UITheme.CreateSecondaryButton("Refresh");
            btnRefresh.Location = new Point(btnNew.Width + 10, 8);
            btnRefresh.Click += (s, e) => LoadPaymentVouchers();
            _pvSearch = new TextBox { Width = 180, Height = 28, Location = new Point(btnRefresh.Right + 10, 10) };
            _pvSearch.TextChanged += (s, e) => LoadPaymentVouchers();
            _pvStatusFilter = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_pvSearch.Right + 10, 10) };
            _pvStatusFilter.Items.AddRange(new object[] { "All Status", "0 - Draft", "1 - Approved", "2 - Paid", "3 - Cancelled" });
            _pvStatusFilter.SelectedIndex = 0;
            _pvStatusFilter.SelectedIndexChanged += (s, e) => LoadPaymentVouchers();
            toolbar.Controls.AddRange(new Control[] { btnNew, btnRefresh, _pvSearch, _pvStatusFilter });
            _pvGrid = GridHelper.CreateStyledGrid();
            _pvGrid.CellDoubleClick += PVGrid_CellDoubleClick;
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_pvGrid);
            panel.Controls.Add(toolbar);
            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage BuildReceiptVoucherTab()
        {
            var tab = new TabPage("Receipt Vouchers") { BackColor = UITheme.Background };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Receipt Voucher");
            btnNew.Location = new Point(0, 8);
            btnNew.Click += (s, e) => ShowCreateRVDialog();
            var btnRefresh = UITheme.CreateSecondaryButton("Refresh");
            btnRefresh.Location = new Point(btnNew.Width + 10, 8);
            btnRefresh.Click += (s, e) => LoadReceiptVouchers();
            _rvSearch = new TextBox { Width = 180, Height = 28, Location = new Point(btnRefresh.Right + 10, 10) };
            _rvSearch.TextChanged += (s, e) => LoadReceiptVouchers();
            _rvStatusFilter = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_rvSearch.Right + 10, 10) };
            _rvStatusFilter.Items.AddRange(new object[] { "All Status", "0 - Draft", "1 - Confirmed", "2 - Cancelled" });
            _rvStatusFilter.SelectedIndex = 0;
            _rvStatusFilter.SelectedIndexChanged += (s, e) => LoadReceiptVouchers();
            toolbar.Controls.AddRange(new Control[] { btnNew, btnRefresh, _rvSearch, _rvStatusFilter });
            _rvGrid = GridHelper.CreateStyledGrid();
            _rvGrid.CellDoubleClick += RVGrid_CellDoubleClick;
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_rvGrid);
            panel.Controls.Add(toolbar);
            tab.Controls.Add(panel);
            return tab;
        }

        private void LoadAll() { LoadPaymentVouchers(); LoadReceiptVouchers(); LoadCharts(); }

        private void LoadPaymentVouchers()
        {
            try
            {
                var dt = _pvCtrl.GetAllPaymentVouchers();
                if (dt != null) AddStatusLabels(dt, "Status", PVStatusNames);
                _pvGrid.DataSource = dt;
                ApplyFilter(_pvGrid, _pvSearch?.Text, _pvStatusFilter?.SelectedIndex ?? 0);
                GridHelper.StyleGrid(_pvGrid);
            }
            catch { }
        }

        private void LoadReceiptVouchers()
        {
            try
            {
                var dt = _rvCtrl.GetAllReceiptVouchers();
                if (dt != null) AddStatusLabels(dt, "Status", RVStatusNames);
                _rvGrid.DataSource = dt;
                ApplyFilter(_rvGrid, _rvSearch?.Text, _rvStatusFilter?.SelectedIndex ?? 0);
                GridHelper.StyleGrid(_rvGrid);
            }
            catch { }
        }

        private void LoadCharts()
        {
            try
            {
                decimal totalIncome = 0, totalExpense = 0;

                var rvMonthly = _rvCtrl.GetMonthlyTotals();
                if (rvMonthly != null && rvMonthly.Rows.Count > 0)
                {
                    var rows = rvMonthly.Rows.Cast<DataRow>().Reverse().ToArray();
                    var labels = rows.Select(r => r["Month"].ToString().Substring(5)).ToArray();
                    var values = rows.Select(r => Convert.ToDecimal(r["Total"])).ToArray();
                    _incomeChart.SetBarData(labels, values, "RM");
                    totalIncome = values.Sum();
                    _lblTotalIncome.Text = "RM " + totalIncome.ToString("N0");
                }

                var pvMonthly = _pvCtrl.GetMonthlyTotals();
                if (pvMonthly != null && pvMonthly.Rows.Count > 0)
                {
                    var rows = pvMonthly.Rows.Cast<DataRow>().Reverse().ToArray();
                    var labels = rows.Select(r => r["Month"].ToString().Substring(5)).ToArray();
                    var values = rows.Select(r => Convert.ToDecimal(r["Total"])).ToArray();
                    _expenseChart.SetBarData(labels, values, "RM");
                    totalExpense = values.Sum();
                    _lblTotalExpense.Text = "RM " + totalExpense.ToString("N0");
                }

                decimal net = totalIncome - totalExpense;
                _lblNetFlow.Text = "RM " + net.ToString("N0");
                _lblNetFlow.ForeColor = net >= 0 ? Color.FromArgb(0, 168, 120) : Color.FromArgb(220, 60, 60);

                var rvMethod = _rvCtrl.GetMethodBreakdown();
                if (rvMethod != null && rvMethod.Rows.Count > 0)
                {
                    // 針對 ReceiptVoucher 如果保留 int 則維持原樣
                    var labels = rvMethod.Rows.Cast<DataRow>().Select(r => MethodLabel(Convert.ToInt32(r["Method"]))).ToArray();
                    var values = rvMethod.Rows.Cast<DataRow>().Select(r => (float)Convert.ToDecimal(r["Total"])).ToArray();
                    _incomePie.SetData(labels, values);
                }

                var pvMethod = _pvCtrl.GetMethodBreakdown();
                if (pvMethod != null && pvMethod.Rows.Count > 0)
                {
                    // ✨ 針對修正後的 PaymentVoucher (Method為字串型態)，直接讀取字串顯示標籤
                    var labels = pvMethod.Rows.Cast<DataRow>().Select(r => r["Method"]?.ToString() ?? "Unknown").ToArray();
                    var values = pvMethod.Rows.Cast<DataRow>().Select(r => (float)Convert.ToDecimal(r["Total"])).ToArray();
                    _expensePie.SetData(labels, values);
                }
            }
            catch { }
        }

        private string MethodLabel(int m) => m >= 0 && m < MethodNames.Length ? MethodNames[m] : m.ToString();

        private void AddStatusLabels(DataTable dt, string colName, string[] names)
        {
            if (!dt.Columns.Contains(colName)) return;
            dt.Columns.Add("Status Label", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                if (row[colName] == DBNull.Value) continue;
                int s = Convert.ToInt32(row[colName]);
                row["Status Label"] = s >= 0 && s < names.Length ? names[s] : s.ToString();
            }
        }

        private void ApplyFilter(DataGridView grid, string keyword, int statusIndex)
        {
            if (!(grid.DataSource is DataTable dt)) return;
            keyword = (keyword ?? "").Trim().Replace("'", "''");
            var conditions = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var cols = dt.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(string))
                    .Select(c => "[" + c.ColumnName + "] LIKE '%" + keyword + "%'");
                string f = string.Join(" OR ", cols);
                if (!string.IsNullOrWhiteSpace(f)) conditions.Add("(" + f + ")");
            }
            if (statusIndex > 0 && dt.Columns.Contains("Status"))
                conditions.Add("[Status] = " + (statusIndex - 1));
            dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
        }

        private void PVGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idObj = _pvGrid.Rows[e.RowIndex].Cells[0].Value;
            if (idObj == null) return;
            var pv = _pvCtrl.GetById(Convert.ToInt64(idObj));
            if (pv == null) return;
            using (var dlg = BuildPVForm("Payment Voucher - " + pv.PaymentVoucherCode, pv))
                if (dlg.ShowDialog(this) == DialogResult.OK) { LoadPaymentVouchers(); LoadCharts(); }
        }

        private void RVGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idObj = _rvGrid.Rows[e.RowIndex].Cells[0].Value;
            if (idObj == null) return;
            var rv = _rvCtrl.GetById(Convert.ToInt64(idObj));
            if (rv == null) return;
            using (var dlg = BuildRVForm("Receipt Voucher - " + rv.ReceiptVoucherCode, rv))
                if (dlg.ShowDialog(this) == DialogResult.OK) { LoadReceiptVouchers(); LoadCharts(); }
        }

        private void ShowCreatePVDialog()
        {
            using (var dlg = BuildPVForm("New Payment Voucher", null))
                if (dlg.ShowDialog(this) == DialogResult.OK) { LoadPaymentVouchers(); LoadCharts(); }
        }

        private void ShowCreateRVDialog()
        {
            using (var dlg = BuildRVForm("New Receipt Voucher", null))
                if (dlg.ShowDialog(this) == DialogResult.OK) { LoadReceiptVouchers(); LoadCharts(); }
        }

        // 🛠️ 徹底修正後的 BuildPVForm 方法
        private Form BuildPVForm(string title, PaymentVoucher pv)
        {
            bool isNew = pv == null;
            // 稍微加高表單高度 (從 400 改到 520) 以容納核銷資訊區塊
            var dlg = new Form { Text = title, Size = new Size(480, 520), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = UITheme.Background };

            // 主佈局改為 FlowLayoutPanel，方便動態由上往下排版
            var mainLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16) };

            // A. 基礎憑證資訊表單 (原本的 TableLayoutPanel)
            var layout = new TableLayoutPanel { Width = 430, Height = 250, ColumnCount = 2, RowCount = 8 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var txtSupplierId = new TextBox { Text = pv?.SupplierID.ToString() ?? string.Empty, Width = 260 };
            var txtPoId = new TextBox { Text = pv?.PurchaseOrderID?.ToString() ?? string.Empty, Width = 260 };
            var txtStaffId = new TextBox { Text = pv?.StaffID.ToString() ?? string.Empty, Width = 260 };
            var txtAmount = new TextBox { Text = pv?.Amount.ToString() ?? string.Empty, Width = 260 };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
            cmbMethod.Items.AddRange(MethodNames);
            if (pv != null && !string.IsNullOrEmpty(pv.PaymentMethod))
                cmbMethod.SelectedIndex = Math.Max(0, Array.IndexOf(MethodNames, pv.PaymentMethod));
            else
                cmbMethod.SelectedIndex = 0;

            var txtRef = new TextBox { Text = pv?.PaymentRef ?? string.Empty, Width = 260 };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
            cmbStatus.Items.AddRange(PVStatusNames);
            cmbStatus.SelectedIndex = pv != null ? Math.Max(0, Math.Min(pv.Status, 3)) : 0;
            var txtRemark = new TextBox { Text = pv?.Remark ?? string.Empty, Multiline = true, Height = 40, Width = 260 };

            UITheme.AddFormField(layout, 0, "Supplier ID *", txtSupplierId);
            UITheme.AddFormField(layout, 1, "Purchase Order ID", txtPoId);
            UITheme.AddFormField(layout, 2, "Staff ID *", txtStaffId);
            UITheme.AddFormField(layout, 3, "Amount *", txtAmount);
            UITheme.AddFormField(layout, 4, "Payment Method", cmbMethod);
            UITheme.AddFormField(layout, 5, "Reference", txtRef);
            UITheme.AddFormField(layout, 6, "Status", cmbStatus);
            UITheme.AddFormField(layout, 7, "Remark", txtRemark);

            mainLayout.Controls.Add(layout);

            // ✨ B. 新增：採購單核銷明細展示區塊 (GroupBox)
            if (!isNew && pv.PurchaseOrderID.HasValue)
            {
                var grpPO = new GroupBox { Text = "Linked Purchase Order Detail (核銷採購單資訊)", Width = 430, Height = 110, ForeColor = Color.DarkSlateGray, Margin = new Padding(0, 15, 0, 0) };
                var poLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10, 5, 10, 5) };
                poLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
                poLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                // 建立展示標籤
                var lblPoCode = new Label { Text = $"PO Code: {pv.PurchaseOrderCode}", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true };
                var lblPoTotal = new Label { Text = $"PO Total Amount: RM {pv.PurchaseOrderTotalAmount:N2}", AutoSize = true };
                var lblPayAmount = new Label { Text = $"This Voucher Settled: RM {pv.VoucherPayAmount:N2}", ForeColor = Color.Green, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true };

                poLayout.Controls.Add(lblPoCode, 0, 0);
                poLayout.Controls.Add(lblPoTotal, 0, 1);
                poLayout.Controls.Add(lblPayAmount, 0, 2);
                grpPO.Controls.Add(poLayout);

                mainLayout.Controls.Add(grpPO);
            }

            // C. 功能按鈕區
            var btnSave = UITheme.CreatePrimaryButton("Save");
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                if (!long.TryParse(txtSupplierId.Text.Trim(), out long supplierId) ||
                    !long.TryParse(txtStaffId.Text.Trim(), out long staffId) ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount))
                {
                    UITheme.ShowWarning("Valid Supplier ID, Staff ID and Amount are required.");
                    return;
                }

                try
                {
                    var entity = new PaymentVoucher
                    {
                        PaymentVoucherID = pv?.PaymentVoucherID ?? 0,
                        PaymentVoucherCode = pv?.PaymentVoucherCode ?? "PV-TEMP",
                        SupplierID = supplierId,
                        PurchaseOrderID = long.TryParse(txtPoId.Text.Trim(), out long poId) ? (long?)poId : null,
                        StaffID = staffId,
                        Amount = amount,
                        PaymentMethod = cmbMethod.SelectedItem?.ToString() ?? "Cash",
                        PaymentRef = txtRef.Text.Trim(),
                        Status = cmbStatus.SelectedIndex,
                        Remark = txtRemark.Text.Trim()
                    };

                    if (isNew) _pvCtrl.Insert(entity); else _pvCtrl.Update(entity);
                    UITheme.ShowSuccess(isNew ? "Payment Voucher created." : "Payment Voucher updated.");
                    dlg.DialogResult = DialogResult.OK; dlg.Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            var btnPanel = new FlowLayoutPanel { Width = 430, Height = 40, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 10, 0, 0) };
            btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
            if (!isNew)
            {
                var btnPrint = UITheme.CreateSecondaryButton("Print PDF");
                btnPrint.Width = 110;
                btnPrint.Click += (s, e) => PrintPaymentVoucherPdf(pv);
                btnPanel.Controls.Add(btnPrint);
            }

            mainLayout.Controls.Add(btnPanel);
            dlg.Controls.Add(mainLayout);
            return dlg;
        }

        private Form BuildRVForm(string title, ReceiptVoucher rv)
        {
            bool isNew = rv == null;
            var dlg = new Form { Text = title, Size = new Size(480, 380), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = UITheme.Background };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var txtInvoiceId = new TextBox { Text = rv?.InvoiceID?.ToString() ?? string.Empty };
            var txtCustomerId = new TextBox { Text = rv?.CustomerID?.ToString() ?? string.Empty };
            var txtStaffId = new TextBox { Text = rv?.StaffID.ToString() ?? string.Empty };
            var txtAmount = new TextBox { Text = rv?.Amount.ToString() ?? string.Empty };
            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(MethodNames);
            cmbMethod.SelectedIndex = rv != null ? Math.Max(0, Math.Min(rv.PaymentMethod, 3)) : 0;
            var txtRef = new TextBox { Text = rv?.PaymentRef ?? string.Empty };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(RVStatusNames);
            cmbStatus.SelectedIndex = rv != null ? Math.Max(0, Math.Min(rv.Status, 2)) : 0;
            var txtRemark = new TextBox { Text = rv?.Remark ?? string.Empty, Multiline = true, Height = 50 };
            UITheme.AddFormField(layout, 0, "Invoice ID", txtInvoiceId);
            UITheme.AddFormField(layout, 1, "Customer ID", txtCustomerId);
            UITheme.AddFormField(layout, 2, "Staff ID *", txtStaffId);
            UITheme.AddFormField(layout, 3, "Amount *", txtAmount);
            UITheme.AddFormField(layout, 4, "Payment Method", cmbMethod);
            UITheme.AddFormField(layout, 5, "Reference", txtRef);
            UITheme.AddFormField(layout, 6, "Status", cmbStatus);
            var btnSave = UITheme.CreatePrimaryButton("Save");
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                if (!long.TryParse(txtStaffId.Text.Trim(), out long staffId) || !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount))
                { UITheme.ShowWarning("Valid Staff ID and Amount are required."); return; }
                try
                {
                    var entity = new ReceiptVoucher { ReceiptVoucherID = rv?.ReceiptVoucherID ?? 0, ReceiptVoucherCode = rv?.ReceiptVoucherCode ?? "RV-TEMP", InvoiceID = long.TryParse(txtInvoiceId.Text.Trim(), out long invId) ? (long?)invId : null, CustomerID = long.TryParse(txtCustomerId.Text.Trim(), out long custId) ? (long?)custId : null, StaffID = staffId, Amount = amount, PaymentMethod = cmbMethod.SelectedIndex, PaymentRef = txtRef.Text.Trim(), Status = cmbStatus.SelectedIndex, Remark = txtRemark.Text.Trim() };
                    if (isNew) _rvCtrl.Insert(entity); else _rvCtrl.Update(entity);
                    UITheme.ShowSuccess(isNew ? "Receipt Voucher created." : "Receipt Voucher updated.");
                    dlg.DialogResult = DialogResult.OK; dlg.Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
            if (!isNew)
            {
                var btnPrint = UITheme.CreateSecondaryButton("Print PDF");
                btnPrint.Width = 110;
                btnPrint.Click += (s, e) => PrintReceiptVoucherPdf(rv);
                btnPanel.Controls.Add(btnPrint);
            }
            dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
            return dlg;
        }

        private void ExportOverviewReportPdf()
        {
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            fields.Rows.Add("Total Income", _lblTotalIncome?.Text ?? "");
            fields.Rows.Add("Total Expense", _lblTotalExpense?.Text ?? "");
            fields.Rows.Add("Net Cash Flow", _lblNetFlow?.Text ?? "");
            fields.Rows.Add("Report Period", "Monthly aggregates (see charts in application)");

            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Finance Overview Report", fields, null, "Finance_Overview_Report");
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void PrintPaymentVoucherPdf(PaymentVoucher pv)
        {
            if (pv == null) return;
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            fields.Rows.Add("Voucher Code", pv.PaymentVoucherCode);
            fields.Rows.Add("Supplier ID", pv.SupplierID);
            fields.Rows.Add("Purchase Order ID", pv.PurchaseOrderID?.ToString() ?? "");
            fields.Rows.Add("Staff ID", pv.StaffID);
            fields.Rows.Add("Amount", pv.Amount.ToString("N2"));
            fields.Rows.Add("Payment Method", pv.PaymentMethod);
            fields.Rows.Add("Reference", pv.PaymentRef ?? "");
            fields.Rows.Add("Status", pv.Status >= 0 && pv.Status < PVStatusNames.Length ? PVStatusNames[pv.Status] : pv.Status.ToString());
            fields.Rows.Add("Remark", pv.Remark ?? "");
            if (pv.PurchaseOrderID.HasValue)
            {
                fields.Rows.Add("PO Code", pv.PurchaseOrderCode ?? "");
                fields.Rows.Add("PO Total", pv.PurchaseOrderTotalAmount.ToString("N2"));
                fields.Rows.Add("Settled Amount", pv.VoucherPayAmount.ToString("N2"));
            }
            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Payment Voucher — " + pv.PaymentVoucherCode, fields, null, pv.PaymentVoucherCode);
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void PrintReceiptVoucherPdf(ReceiptVoucher rv)
        {
            if (rv == null) return;
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            fields.Rows.Add("Voucher Code", rv.ReceiptVoucherCode);
            fields.Rows.Add("Invoice ID", rv.InvoiceID?.ToString() ?? "");
            fields.Rows.Add("Customer ID", rv.CustomerID?.ToString() ?? "");
            fields.Rows.Add("Staff ID", rv.StaffID);
            fields.Rows.Add("Amount", rv.Amount.ToString("N2"));
            fields.Rows.Add("Payment Method", rv.PaymentMethod >= 0 && rv.PaymentMethod < MethodNames.Length ? MethodNames[rv.PaymentMethod] : rv.PaymentMethod.ToString());
            fields.Rows.Add("Reference", rv.PaymentRef ?? "");
            fields.Rows.Add("Status", rv.Status >= 0 && rv.Status < RVStatusNames.Length ? RVStatusNames[rv.Status] : rv.Status.ToString());
            fields.Rows.Add("Remark", rv.Remark ?? "");
            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Receipt Voucher — " + rv.ReceiptVoucherCode, fields, null, rv.ReceiptVoucherCode);
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }
    }
}