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
        private readonly FinanceWorkflowService _financeWorkflow = new FinanceWorkflowService();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();

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
            fields.Rows.Add("Report Scope", "Dashboard summary (charts displayed in app)");

            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Finance Dashboard Report",
                    fields,
                    null,
                    "Finance_Dashboard_Report");
                PdfExportHelper.ExportToPdf(data, this);
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to export PDF: " + ex.Message);
            }
        }

        private void BuildPVTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Payment Voucher");
            btnNew.Location = new Point(8, 8);
            btnNew.Click += (s, e) => {
                if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, PermissionAction.Create, this)) return;
                using (var dlg = BuildPVForm("Create Payment Voucher", null))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.PaymentVoucher);

            var lblSearch = new Label { Text = "Search Code:", Location = new Point(240, 14), AutoSize = true };
            _pvSearch = new TextBox { Location = new Point(325, 11), Width = 150 };
            _pvSearch.TextChanged += (s, e) => FilterPV();

            var lblFilter = new Label { Text = "Status:", Location = new Point(490, 14), AutoSize = true };
            _pvStatusFilter = new ComboBox { Location = new Point(540, 11), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _pvStatusFilter.Items.Add("All");
            _pvStatusFilter.Items.AddRange(PVStatusNames);
            _pvStatusFilter.SelectedIndex = 0;
            _pvStatusFilter.SelectedIndexChanged += (s, e) => FilterPV();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(lblSearch);
            toolbar.Controls.Add(_pvSearch);
            toolbar.Controls.Add(lblFilter);
            toolbar.Controls.Add(_pvStatusFilter);
            layout.Controls.Add(toolbar, 0, 0);

            _pvGrid = InitializeCustomGridView();
            _pvGrid.CellDoubleClick += _pvGrid_CellDoubleClick;
            layout.Controls.Add(_pvGrid, 0, 1);

            page.Controls.Add(layout);
        }

        private void _pvGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long id = Convert.ToInt64(_pvGrid.Rows[e.RowIndex].Cells["ID"].Value);
            var pv = _pvCtrl.GetById(id);
            if (pv == null) return;
            if (!AppSession.CanEdit(PermissionModule.PaymentVoucher)) return;

            using (var dlg = BuildPVForm("Payment Voucher Details", pv))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
            }
        }

        private void BuildRVTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var btnNew = UITheme.CreatePrimaryButton("+ New Receipt Voucher");
            btnNew.Location = new Point(8, 8);
            btnNew.Click += (s, e) => {
                if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, PermissionAction.Create, this)) return;
                using (var dlg = BuildRVForm("Create Receipt Voucher", null))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
                }
            };
            PermissionGuard.ApplyCreateButton(btnNew, PermissionModule.ReceiptVoucher);

            var btnVerify = UITheme.CreateSecondaryButton("Verify & Allocate");
            btnVerify.Location = new Point(190, 8);
            btnVerify.Click += (s, e) => VerifySelectedReceipt();
            PermissionGuard.ApplyEditButton(btnVerify, PermissionModule.ReceiptVoucher);

            var lblSearch = new Label { Text = "Search Code:", Location = new Point(330, 14), AutoSize = true };
            _rvSearch = new TextBox { Location = new Point(415, 11), Width = 150 };
            _rvSearch.TextChanged += (s, e) => FilterRV();

            var lblFilter = new Label { Text = "Status:", Location = new Point(580, 14), AutoSize = true };
            _rvStatusFilter = new ComboBox { Location = new Point(630, 11), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _rvStatusFilter.Items.Add("All");
            _rvStatusFilter.Items.AddRange(RVStatusNames);
            _rvStatusFilter.SelectedIndex = 0;
            _rvStatusFilter.SelectedIndexChanged += (s, e) => FilterRV();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnVerify);
            toolbar.Controls.Add(lblSearch);
            toolbar.Controls.Add(_rvSearch);
            toolbar.Controls.Add(lblFilter);
            toolbar.Controls.Add(_rvStatusFilter);
            layout.Controls.Add(toolbar, 0, 0);

            _rvGrid = InitializeCustomGridView();
            _rvGrid.CellDoubleClick += _rvGrid_CellDoubleClick;
            layout.Controls.Add(_rvGrid, 0, 1);

            page.Controls.Add(layout);
        }

        private void _rvGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            long id = Convert.ToInt64(_rvGrid.Rows[e.RowIndex].Cells["ID"].Value);
            var rv = _rvCtrl.GetById(id);
            if (rv == null) return;
            if (!AppSession.CanEdit(PermissionModule.ReceiptVoucher)) return;

            using (var dlg = BuildRVForm("Receipt Voucher Details", rv))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
            }
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
            var dlg = new Form { Text = title, Size = new Size(450, 440), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = UITheme.Background };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 8 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var txtSupplierId = new TextBox { Text = pv?.SupplierID.ToString() ?? string.Empty, Width = 240 };
            var txtPoId = new TextBox { Text = pv?.PurchaseOrderID?.ToString() ?? string.Empty, Width = 240 };
            var txtStaffId = new TextBox { Text = pv?.StaffID.ToString() ?? string.Empty, Width = 240 };
            var txtAmount = new TextBox { Text = pv?.Amount.ToString() ?? string.Empty, Width = 240 };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            cmbMethod.Items.AddRange(MethodNames);
            if (pv != null && !string.IsNullOrEmpty(pv.PaymentMethod))
                cmbMethod.SelectedIndex = Math.Max(0, Array.IndexOf(MethodNames, pv.PaymentMethod));
            else
                cmbMethod.SelectedIndex = 0;

            var txtRef = new TextBox { Text = pv?.PaymentRef ?? string.Empty, Width = 240 };

            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            cmbStatus.Items.AddRange(PVStatusNames);
            cmbStatus.SelectedIndex = pv != null ? Math.Max(0, Math.Min(pv.Status, 3)) : 0;

            var txtRemark = new TextBox { Text = pv?.Remark ?? string.Empty, Multiline = true, Height = 40, Width = 240 };

            UITheme.AddFormField(layout, 0, "Supplier ID *", txtSupplierId);
            UITheme.AddFormField(layout, 1, "Purchase Order ID", txtPoId);
            UITheme.AddFormField(layout, 2, "Staff ID *", txtStaffId);
            UITheme.AddFormField(layout, 3, "Amount *", txtAmount);
            UITheme.AddFormField(layout, 4, "Payment Method", cmbMethod);
            UITheme.AddFormField(layout, 5, "Reference", txtRef);
            UITheme.AddFormField(layout, 6, "Status", cmbStatus);
            UITheme.AddFormField(layout, 7, "Remark", txtRemark);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.PaymentVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.PaymentVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) => {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, action, dlg)) return;
                if (!long.TryParse(txtSupplierId.Text.Trim(), out long supplierId) ||
                    !long.TryParse(txtStaffId.Text.Trim(), out long staffId) ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount))
                { UITheme.ShowWarning("Valid Supplier ID, Staff ID and Amount are required."); return; }

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

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
            dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
            return dlg;
        }

        // 💡 核心 Debug 修改點：完美解決現有錯誤
        private Form BuildRVForm(string title, ReceiptVoucher existingRv)
        {
            bool isNew = existingRv == null;
            var dlg = new Form { Text = title, Size = new Size(450, 390), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = UITheme.Background };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 6 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 💡 1. 修正變數名稱 txtCustomerId (原本全小寫，改為與事件內名稱完全一致)
            var txtCustomerId = new TextBox { Text = existingRv?.CusomerID.ToString() ?? string.Empty, Width = 240 };
            var txtStaffId = new TextBox { Text = existingRv?.StaffID.ToString() ?? string.Empty, Width = 240 };
            var txtAmount = new TextBox { Text = existingRv?.PaymentAmount.ToString() ?? string.Empty, Width = 240 };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            cmbMethod.Items.AddRange(MethodNames);
            cmbMethod.SelectedIndex = existingRv != null ? Math.Max(0, Math.Min(existingRv.PaymentMethod, 3)) : 0;

            var txtRef = new TextBox { Text = existingRv?.PaymentMethodRef ?? string.Empty, Width = 240 };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            cmbStatus.Items.AddRange(RVStatusNames);
            cmbStatus.SelectedIndex = existingRv != null ? Math.Max(0, Math.Min(existingRv.Status, 2)) : 0;

            var txtRemark = new TextBox { Text = existingRv?.Remark ?? string.Empty, Multiline = true, Height = 40, Width = 240 };

            // 💡 2. 徹底拿掉不符合實體 SQL 結構的 Invoice ID 欄位
            UITheme.AddFormField(layout, 0, "Customer ID *", txtCustomerId);
            UITheme.AddFormField(layout, 1, "Staff ID *", txtStaffId);
            UITheme.AddFormField(layout, 2, "Amount *", txtAmount);
            UITheme.AddFormField(layout, 3, "Payment Method", cmbMethod);
            UITheme.AddFormField(layout, 4, "Reference", txtRef);
            UITheme.AddFormField(layout, 5, "Status", cmbStatus);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.ReceiptVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.ReceiptVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();

            btnSave.Click += (s, e) => {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, action, dlg)) return;
                if (!long.TryParse(txtCustomerId.Text.Trim(), out long custId) ||
                    !long.TryParse(txtStaffId.Text.Trim(), out long staffId) ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount))
                {
                    UITheme.ShowWarning("Valid Customer ID, Staff ID and Amount are required.");
                    return;
                }

                try
                {
                    // 💡 3. 將所有 rv 全部改為對齊參數的 existingRv，並將欄位對齊實體 SQL 名稱
                    var entity = new ReceiptVoucher
                    {
                        ReceiptVoucherID = existingRv?.ReceiptVoucherID ?? 0,
                        ReceiptVoucherCode = existingRv?.ReceiptVoucherCode ?? "RV-TEMP",
                        CusomerID = custId,
                        StaffID = staffId,
                        PaymentAmount = amount,
                        PaymentMethod = cmbMethod.SelectedIndex,
                        PaymentMethodRef = txtRef.Text.Trim(),
                        Status = cmbStatus.SelectedIndex,
                        Remark = txtRemark.Text.Trim(),
                        CurrencyID = existingRv?.CurrencyID ?? 1
                    };

                    if (isNew)
                    {
                        _rvCtrl.Insert(entity);
                    }
                    else
                    {
                        _rvCtrl.Update(entity);
                    }

                    UITheme.ShowSuccess(isNew ? "Receipt Voucher created." : "Receipt Voucher updated.");
                    dlg.DialogResult = DialogResult.OK; dlg.Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(layout);
            dlg.Controls.Add(btnPanel);
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

            if (_pvGrid.Columns.Contains("Status")) _pvGrid.Columns["Status"].Visible = false;
            if (_rvGrid.Columns.Contains("Status")) _rvGrid.Columns["Status"].Visible = false;

            UpdateSummary();
            LoadCharts();
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

            using (var dlg = new Form())
            {
                dlg.Text = "Verify Receipt — Allocate to Invoices";
                dlg.Size = new Size(520, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var lblAmount = new Label { Text = "Receipt Amount: " + receipt.PaymentAmount.ToString("N2"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                var txtInvoiceId = new TextBox();
                var txtAllocated = new TextBox { Text = receipt.PaymentAmount.ToString("N2") };
                var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbType.Items.AddRange(new[] { "1 - Deposit", "2 - Partial Payment", "3 - Final Payment", "4 - Exchange Loss" });
                cmbType.SelectedIndex = 1;

                UITheme.AddFormRow(layout, 0, "Summary", lblAmount);
                UITheme.AddFormRow(layout, 1, "Invoice ID *", txtInvoiceId);
                UITheme.AddFormRow(layout, 2, "Allocated Amount *", txtAllocated);
                UITheme.AddFormRow(layout, 3, "Clearing Type", cmbType);

                var btnSave = UITheme.CreatePrimaryButton("Verify Receipt");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!long.TryParse(txtInvoiceId.Text.Trim(), out long invoiceId) ||
                        !decimal.TryParse(txtAllocated.Text.Trim(), out decimal allocated))
                    {
                        UITheme.ShowWarning("Valid invoice ID and allocated amount are required.");
                        return;
                    }

                    var allocations = new System.Collections.Generic.List<ReceiptAllocation>
                    {
                        new ReceiptAllocation
                        {
                            InvoiceId = invoiceId,
                            ReceivedAmount = allocated,
                            Type = cmbType.SelectedIndex + 1
                        }
                    };

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

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }
    }
}