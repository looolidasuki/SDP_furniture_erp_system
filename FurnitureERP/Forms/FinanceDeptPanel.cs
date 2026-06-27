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
        private readonly SalesOrderController _soCtrl = new SalesOrderController();
        private readonly CurrencyController _currencyCtrl = new CurrencyController();

        private TabControl _tabs;
        private DataGridView _pvGrid;
        private DataGridView _rvGrid;
        private ChartControl _incomeChart;
        private ChartControl _expenseChart;
        private PieChartControl _incomePie;
        private PieChartControl _expensePie;
        private Label _lblTotalIncome;
        private Label _lblTotalExpense;
        private Label _lblNetFlow;
        private DataGridView _rvCurrencyGrid;
        private DataGridView _pvCurrencyGrid;
        private DataTable _rvCurrencyBreakdown;
        private DataTable _pvCurrencyBreakdown;

        private const string HkdLabel = "HKD";

        private static readonly string[] MethodNames = { "Cash", "Bank Transfer", "Credit Card", "Cheque" };
        private static readonly string[] PVStatusNames = { "Draft", "Approved", "Paid", "Cancelled" };
        private static readonly string[] RVStatusNames = { "Draft", "Confirmed", "Cancelled" };

        private sealed class AllocationGridSection
        {
            public Panel Section { get; set; }
            public DataGridView Grid { get; set; }
            public DataTable Table { get; set; }
            public Label RemainLabel { get; set; }
            public Action RefreshPicker { get; set; }
        }

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

            var tabReconcile = new TabPage("📋 Outstanding") { BackColor = UITheme.Background };
            BuildReconciliationTab(tabReconcile);

            if (AppSession.CanView(PermissionModule.PaymentVoucher) || AppSession.CanView(PermissionModule.ReceiptVoucher))
                _tabs.TabPages.Add(tabDash);
            if (AppSession.CanView(PermissionModule.PaymentVoucher))
                _tabs.TabPages.Add(tabPV);
            if (AppSession.CanView(PermissionModule.ReceiptVoucher))
                _tabs.TabPages.Add(tabRV);
            if (AppSession.CanView(PermissionModule.PaymentVoucher) || AppSession.CanView(PermissionModule.ReceiptVoucher))
                _tabs.TabPages.Add(tabReconcile);
            Controls.Add(_tabs);
        }

        private void BuildDashboardTab(TabPage page)
        {
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            var cardPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            _lblTotalIncome = new Label { Text = "HKD 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.DarkGreen, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _lblTotalExpense = new Label { Text = "HKD 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.DarkRed, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _lblNetFlow = new Label { Text = "HKD 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.Navy, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

            var pnlIn = UITheme.CreateCard("Total Income (HKD)"); pnlIn.Controls.Add(_lblTotalIncome);
            var pnlOut = UITheme.CreateCard("Total Expenses (HKD)"); pnlOut.Controls.Add(_lblTotalExpense);
            var pnlNet = UITheme.CreateCard("Net Cash Flow (HKD)"); pnlNet.Controls.Add(_lblNetFlow);

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

            mainLayout.Controls.Add(chartLayout, 0, 1);

            var breakdownLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
            breakdownLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            breakdownLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _rvCurrencyGrid = GridHelper.CreateStyledGrid();
            _rvCurrencyGrid.Dock = DockStyle.Fill;
            _pvCurrencyGrid = GridHelper.CreateStyledGrid();
            _pvCurrencyGrid.Dock = DockStyle.Fill;

            var rvBreakdownCard = WrapDashboardSection("Receipt Vouchers by Currency", _rvCurrencyGrid);
            var pvBreakdownCard = WrapDashboardSection("Payment Vouchers by Currency", _pvCurrencyGrid);
            breakdownLayout.Controls.Add(rvBreakdownCard, 0, 0);
            breakdownLayout.Controls.Add(pvBreakdownCard, 1, 0);
            mainLayout.Controls.Add(breakdownLayout, 0, 2);

            page.Controls.Add(mainLayout);
        }

        private static Panel WrapDashboardSection(string title, Control content)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            var lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UITheme.TextDark
            };
            content.Dock = DockStyle.Fill;
            card.Controls.Add(content);
            card.Controls.Add(lbl);
            return card;
        }

        private void ExportDashboardReportPdf()
        {
            var fields = new DataTable();
            fields.Columns.Add("Field");
            fields.Columns.Add("Value");
            fields.Rows.Add("Total Income (HKD)", _lblTotalIncome?.Text ?? "");
            fields.Rows.Add("Total Expenses (HKD)", _lblTotalExpense?.Text ?? "");
            fields.Rows.Add("Net Cash Flow (HKD)", _lblNetFlow?.Text ?? "");
            fields.Rows.Add("Reporting Basis", "HKD amounts use document-locked exchange rates at save time.");
            fields.Rows.Add("Report Scope", "Finance dashboard summary with charts and currency breakdown");

            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Finance Dashboard Report",
                    fields,
                    BuildCurrencyBreakdownForPdf(),
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
                new PdfChartImage { Title = "Income Trend (HKD)", Image = _incomeChart?.ToBitmap(500, 220) },
                new PdfChartImage
                {
                    Title = "Income by Payment Method (HKD)",
                    Image = _incomePie?.ToBitmap(400, 300),
                    PreferSquareFrame = true
                },
                new PdfChartImage { Title = "Expense Trend (HKD)", Image = _expenseChart?.ToBitmap(500, 220) },
                new PdfChartImage
                {
                    Title = "Expense by Payment Method (HKD)",
                    Image = _expensePie?.ToBitmap(400, 300),
                    PreferSquareFrame = true
                }
            };
        }

        private DataTable BuildCurrencyBreakdownForPdf()
        {
            var combined = new DataTable();
            combined.Columns.Add("Category");
            combined.Columns.Add("Currency");
            combined.Columns.Add("Foreign Total", typeof(decimal));
            combined.Columns.Add("Weighted Rate", typeof(decimal));
            combined.Columns.Add("HKD Total", typeof(decimal));
            combined.Columns.Add("Count", typeof(int));

            AppendBreakdownRows(combined, "Receipt", _rvCurrencyBreakdown);
            AppendBreakdownRows(combined, "Payment", _pvCurrencyBreakdown);
            return combined;
        }

        private static void AppendBreakdownRows(DataTable target, string category, DataTable source)
        {
            if (source == null) return;
            foreach (DataRow row in source.Rows)
            {
                target.Rows.Add(
                    category,
                    row["Currency"],
                    row["Foreign Total"],
                    row["Weighted Rate"],
                    row["HKD Total"],
                    row["Count"]);
            }
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

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnView);
            toolbar.Controls.Add(btnUpdateStatus);
            layout.Controls.Add(toolbar, 0, 0);

            _pvGrid = InitializeCustomGridView();
            _pvGrid.CellDoubleClick += _pvGrid_CellDoubleClick;
            layout.Controls.Add(FilterBlockHelper.CreateFilterBlock(_pvGrid, "Payment Voucher Filters", DictionaryService.Categories.PaymentVoucher), 0, 1);
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

            var btnVerifyRv = UITheme.CreateSecondaryButton("Verify Receipt");
            btnVerifyRv.Location = new Point(btnUpdateRvStatus.Right + 10, 10);
            btnVerifyRv.Click += (s, e) => VerifySelectedReceipt();
            PermissionGuard.ApplyEditButton(btnVerifyRv, PermissionModule.ReceiptVoucher);

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnDetail);
            toolbar.Controls.Add(btnUpdateRvStatus);
            toolbar.Controls.Add(btnVerifyRv);
            layout.Controls.Add(toolbar, 0, 0);

            _rvGrid = InitializeCustomGridView();
            _rvGrid.CellDoubleClick += _rvGrid_CellDoubleClick;
            layout.Controls.Add(FilterBlockHelper.CreateFilterBlock(_rvGrid, "Receipt Voucher Filters", DictionaryService.Categories.ReceiptVoucher), 0, 1);
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

        private void BuildReconciliationTab(TabPage page)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280
            };
            var apGrid = GridHelper.CreateStyledGrid();
            var arGrid = GridHelper.CreateStyledGrid();

            Action loadOutstanding = () =>
            {
                try
                {
                    var ap = FinanceReconciliationService.GetAccountsPayableOutstanding();
                    ap = GridHelper.DecorateStatusTable(ap, "Status", DictionaryService.Categories.PurchaseOrder);
                    GridHelper.BindStatusData(apGrid, ap, DictionaryService.Categories.PurchaseOrder);

                    var ar = FinanceReconciliationService.GetAccountsReceivableOutstanding();
                    ar = GridHelper.DecorateStatusTable(ar, "Status", DictionaryService.Categories.Invoice);
                    GridHelper.BindStatusData(arGrid, ar, DictionaryService.Categories.Invoice);
                }
                catch (Exception ex)
                {
                    UITheme.ShowError("Failed to load outstanding balances: " + ex.Message);
                }
            };

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UITheme.Background };
            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(8, 8);
            btnRefresh.Click += (s, e) => loadOutstanding();
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(new Label
            {
                Text = "Unsettled purchase orders (AP) and invoices (AR).",
                Location = new Point(btnRefresh.Right + 12, 12),
                AutoSize = true,
                ForeColor = UITheme.TextGray
            });

            var apPanel = new Panel { Dock = DockStyle.Fill };
            apPanel.Controls.Add(apGrid);
            apPanel.Controls.Add(new Label
            {
                Text = "Accounts Payable — Outstanding POs",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 0)
            });

            var arPanel = new Panel { Dock = DockStyle.Fill };
            arPanel.Controls.Add(arGrid);
            arPanel.Controls.Add(new Label
            {
                Text = "Accounts Receivable — Outstanding Invoices",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 0)
            });

            split.Panel1.Controls.Add(apPanel);
            split.Panel2.Controls.Add(arPanel);
            page.Controls.Add(split);
            page.Controls.Add(toolbar);
            loadOutstanding();
        }

        private DataGridView InitializeCustomGridView()
        {
            var gv = GridHelper.CreateStyledGrid();
            gv.RowHeadersVisible = false;
            gv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            gv.ColumnHeadersHeight = 35;
            return gv;
        }

        private Form BuildPVForm(string title, PaymentVoucher pv)
        {
            bool isNew = pv == null;
            bool allocationsReadOnly = pv?.Status >= 2;
            var dlg = new Form
            {
                Text = title,
                Size = new Size(900, 760),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(820, 680),
                BackColor = UITheme.Background
            };

            long defaultStaffId = pv?.StaffID ?? AppSession.CurrentUser?.StaffID ?? 0;
            string staffLabel = AppSession.CurrentUser?.FullName
                ?? (defaultStaffId > 0 ? defaultStaffId.ToString() : "—");

            var txtCode = new TextBox
            {
                Text = isNew ? "(auto-generated)" : (pv?.PaymentVoucherCode ?? ""),
                Width = 300,
                MaxLength = 30,
                ReadOnly = isNew,
                ForeColor = isNew ? UITheme.TextGray : UITheme.TextDark
            };
            var cmbSupplier = new ComboBox { Width = 300 };
            var supplierBinder = SupplierComboHelper.Attach(cmbSupplier, _supplierCtrl, pv?.SupplierID ?? 0);
            var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };
            var txtAmount = new TextBox { Text = pv?.Amount.ToString("0.##") ?? string.Empty, Width = 300 };
            var cmbCurrency = BuildCurrencyCombo(pv?.CurrencyID ?? 1);
            var lblAmountHkd = new Label { AutoSize = true, ForeColor = UITheme.TextGray };

            var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            cmbMethod.Items.AddRange(MethodNames);
            if (pv != null && !string.IsNullOrEmpty(pv.PaymentMethod))
            {
                int idx = Array.IndexOf(MethodNames, pv.PaymentMethod);
                cmbMethod.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else cmbMethod.SelectedIndex = 0;

            var txtRef = new TextBox { Text = pv?.PaymentRef ?? string.Empty, Width = 300 };
            var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            cmbStatus.Items.AddRange(PVStatusNames);
            cmbStatus.SelectedIndex = pv != null ? Math.Max(0, Math.Min(pv.Status, 3)) : 0;
            var txtRemark = new TextBox { Text = pv?.Remark ?? string.Empty, Multiline = true, Height = 44, Width = 300 };

            int fieldRows = 10;
            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 8),
                ColumnCount = 2,
                RowCount = fieldRows
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < fieldRows; i++)
                headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 9 ? 52 : 32));
            UITheme.AddFormField(headerLayout, 0, isNew ? "Voucher Code" : "Voucher Code *", txtCode);
            UITheme.AddFormField(headerLayout, 1, "Supplier *", cmbSupplier);
            UITheme.AddFormField(headerLayout, 2, "Staff", lblStaff);
            UITheme.AddFormField(headerLayout, 3, "Currency *", cmbCurrency);
            UITheme.AddFormField(headerLayout, 4, "Amount *", txtAmount);
            UITheme.AddFormField(headerLayout, 5, "HKD Equivalent", lblAmountHkd);
            UITheme.AddFormField(headerLayout, 6, "Payment Method", cmbMethod);
            UITheme.AddFormField(headerLayout, 7, "Method Ref", txtRef);
            UITheme.AddFormField(headerLayout, 8, "Status", cmbStatus);
            UITheme.AddFormField(headerLayout, 9, "Remark", txtRemark);

            decimal GetPaymentAmount()
            {
                return decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) ? amt : 0m;
            }

            long GetPvSupplierId()
            {
                long id = supplierBinder.GetSelectedId();
                if (id > 0) return id;
                return ResolveSupplierId(cmbSupplier);
            }

            var poSeedTable = CreatePoAllocationEditorTable();
            if (pv?.PaymentVoucherID > 0)
            {
                try
                {
                    var existingLines = _pvCtrl.GetPurchaseOrderSettlements(pv.PaymentVoucherID);
                    foreach (var line in existingLines)
                    {
                        poSeedTable.Rows.Add(
                            line.PurchaseOrderID,
                            line.PurchaseOrderCode ?? "",
                            line.PayAmount > 0 ? line.PayAmount.ToString("0.##") : "",
                            line.ClearingType > 0 ? line.ClearingType : 1);
                    }
                }
                catch { }
            }
            if (poSeedTable.Rows.Count == 0)
                poSeedTable.Rows.Add(DBNull.Value, "", "", 1);

            var poAlloc = BuildPoAllocationSection(
                GetPaymentAmount,
                GetPvSupplierId,
                poSeedTable,
                allocationsReadOnly);

            Action refreshPvHkd = () =>
            {
                long cid = GetComboLongId(cmbCurrency);
                decimal rate = pv?.ExchangeRate > 0 && cid == (pv?.CurrencyID ?? 0)
                    ? pv.ExchangeRate
                    : _currencyCtrl.GetRateToBase(cid > 0 ? cid : 1);
                if (decimal.TryParse(txtAmount.Text.Trim(), out decimal amt))
                    lblAmountHkd.Text = $"HKD {CurrencyConversionService.ToBaseAmount(amt, rate):N2} (rate {rate:N4})";
                else lblAmountHkd.Text = $"Rate {rate:N4} — enter amount";
                UpdatePoAllocationRemainLabel(poAlloc.Grid, GetPaymentAmount(), poAlloc.RemainLabel);
            };
            cmbCurrency.SelectedIndexChanged += (s, e) => refreshPvHkd();
            txtAmount.TextChanged += (s, e) => refreshPvHkd();
            refreshPvHkd();

            supplierBinder.SelectionCommitted += (s, e) => poAlloc.RefreshPicker();
            cmbSupplier.Leave += (s, e) =>
            {
                if (supplierBinder.GetSelectedId() > 0) return;
                if (GetPvSupplierId() > 0)
                    poAlloc.RefreshPicker();
            };

            headerLayout.Dock = DockStyle.Top;
            headerLayout.AutoSize = true;
            headerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var headerScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            headerScroll.Controls.Add(headerLayout);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            split.Panel1.Controls.Add(headerScroll);
            split.Panel2.Controls.Add(poAlloc.Section);
            dlg.Load += (s, e) => FitVoucherFormHeaderSplitter(split, headerLayout);
            dlg.Shown += (s, e) => FitVoucherFormHeaderSplitter(split, headerLayout);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.PaymentVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.PaymentVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.PaymentVoucher, action, dlg)) return;

                long supplierId = GetPvSupplierId();
                if (supplierId <= 0 || defaultStaffId <= 0 ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    UITheme.ShowWarning("Valid Supplier and Amount are required.");
                    return;
                }

                string voucherCode = isNew ? "PV-TEMP" : txtCode.Text.Trim();
                if (!isNew && !TryValidateVoucherCode(voucherCode, false, pv?.PaymentVoucherID ?? 0, _pvCtrl.ExistsByCode, out string codeError))
                {
                    UITheme.ShowWarning(codeError);
                    return;
                }

                List<VoucherPurchaseOrderLine> lines = null;
                if (!allocationsReadOnly)
                {
                    if (!TryBuildPaymentAllocations(poAlloc.Grid, out lines, out string allocError))
                    {
                        UITheme.ShowWarning(allocError);
                        return;
                    }
                    decimal sum = 0;
                    foreach (var line in lines) sum += line.PayAmount;
                    if (Math.Abs(sum - amount) > 0.01m)
                    {
                        UITheme.ShowWarning(
                            $"Allocated total ({sum:N2}) does not equal payment amount ({amount:N2}). Adjust allocation lines.");
                        return;
                    }
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
                        CurrencyID = GetComboLongId(cmbCurrency) > 0 ? GetComboLongId(cmbCurrency) : 1,
                        PaymentMethod = cmbMethod.SelectedItem?.ToString() ?? "Cash",
                        PaymentRef = txtRef.Text.Trim(),
                        Status = cmbStatus.SelectedIndex,
                        Remark = txtRemark.Text.Trim(),
                        PurchaseOrderLines = allocationsReadOnly
                            ? (pv?.PaymentVoucherID > 0 ? _pvCtrl.GetPurchaseOrderSettlements(pv.PaymentVoucherID) : null)
                            : lines
                    };
                    if (isNew) _pvCtrl.Insert(entity);
                    else _pvCtrl.Update(entity);
                    UITheme.ShowSuccess(isNew ? "Payment Voucher created." : "Payment Voucher updated.");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex) { UITheme.ShowError(ex.Message); }
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
            dlg.Controls.Add(split);
            return dlg;
        }

        private void ApplyPvPurchaseOrderLink(long poId, FilteredComboBinder supplierBinder, TextBox txtAmount,
            Label lblBalance, Action<bool> setSuppress)
        {
            if (poId <= 0)
            {
                lblBalance.Text = "Optional — select a PO to auto-fill amount, or enter amount manually.";
                return;
            }

            var po = _poCtrl.GetById(poId);
            setSuppress(true);
            try
            {
                if (po != null && po.SupplierID > 0 && supplierBinder.GetSelectedId() != po.SupplierID)
                {
                    supplierBinder.SuppressEvents = true;
                    try { supplierBinder.SelectById(po.SupplierID); }
                    finally { supplierBinder.SuppressEvents = false; }
                }
            }
            finally { setSuppress(false); }

            UpdatePoBalanceLabel(poId, lblBalance);
            decimal outstanding = _pvCtrl.GetOutstandingByPurchaseOrder(poId);
            if (outstanding > 0)
                txtAmount.Text = outstanding.ToString("0.##");
        }

        private Form BuildRVForm(string title, ReceiptVoucher existingRv)
        {
            bool isNew = existingRv == null;
            bool allocationsReadOnly = existingRv?.Status == 1;
            var dlg = new Form
            {
                Text = title,
                Size = new Size(920, 760),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(820, 680),
                BackColor = UITheme.Background
            };

            long defaultStaffId = existingRv?.StaffID ?? AppSession.CurrentUser?.StaffID ?? 0;
            string staffLabel = AppSession.CurrentUser?.FullName
                ?? (defaultStaffId > 0 ? defaultStaffId.ToString() : "—");

            var txtCode = new TextBox
            {
                Text = isNew ? "(auto-generated)" : (existingRv?.ReceiptVoucherCode ?? ""),
                Width = 300,
                MaxLength = 30,
                ReadOnly = isNew,
                ForeColor = isNew ? UITheme.TextGray : UITheme.TextDark
            };
            var cmbCustomer = new ComboBox { Width = 300 };
            var customerBinder = CustomerComboHelper.Attach(cmbCustomer, _customerCtrl, existingRv?.CusomerID ?? 0);
            var lblStaff = new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark };
            var txtAmount = new TextBox { Text = existingRv?.PaymentAmount.ToString("0.##") ?? string.Empty, Width = 300 };
            var cmbCurrency = BuildCurrencyCombo(existingRv?.CurrencyID ?? 1);
            var lblAmountHkd = new Label { AutoSize = true, ForeColor = UITheme.TextGray };

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
            if (existingRv?.Status == 1)
            {
                cmbStatus.Items.Add(RVStatusNames[1]);
                cmbStatus.SelectedIndex = 0;
                cmbStatus.Enabled = false;
            }
            else
            {
                cmbStatus.Items.Add(RVStatusNames[0]);
                cmbStatus.Items.Add(RVStatusNames[2]);
                cmbStatus.SelectedIndex = existingRv?.Status == 2 ? 1 : 0;
            }
            var txtRemark = new TextBox { Text = existingRv?.Remark ?? string.Empty, Multiline = true, Height = 44, Width = 300 };

            int rvFieldRows = 11;
            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 8),
                ColumnCount = 2,
                RowCount = rvFieldRows
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rvFieldRows; i++)
                headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 10 ? 52 : 32));
            UITheme.AddFormField(headerLayout, 0, isNew ? "Voucher Code" : "Voucher Code *", txtCode);
            UITheme.AddFormField(headerLayout, 1, "Customer *", cmbCustomer);
            UITheme.AddFormField(headerLayout, 2, "Staff", lblStaff);
            UITheme.AddFormField(headerLayout, 3, "Currency *", cmbCurrency);
            UITheme.AddFormField(headerLayout, 4, "Amount *", txtAmount);
            UITheme.AddFormField(headerLayout, 5, "HKD Equivalent", lblAmountHkd);
            UITheme.AddFormField(headerLayout, 6, "Payment Method", cmbMethod);
            UITheme.AddFormField(headerLayout, 7, "Method Ref", txtRef);
            UITheme.AddFormField(headerLayout, 8, "Received Date *", dtpReceived);
            UITheme.AddFormField(headerLayout, 9, "Status", cmbStatus);
            UITheme.AddFormField(headerLayout, 10, "Remark", txtRemark);

            long GetRvCustomerId() => CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);

            decimal GetReceiptAmount()
            {
                return decimal.TryParse(txtAmount.Text.Trim(), out decimal amt) ? amt : 0m;
            }

            var allocationSeed = CreateAllocationEditorTable();
            if (existingRv?.ReceiptVoucherID > 0)
            {
                try
                {
                    var existing = _rvCtrl.GetInvoiceAllocationsForEditor(existingRv.ReceiptVoucherID);
                    if (existing != null && existing.Rows.Count > 0)
                    {
                        foreach (DataRow row in existing.Rows)
                        {
                            object invId = row["Invoice ID"];
                            string code = row["Invoice Code"]?.ToString() ?? "";
                            if (invId == DBNull.Value || invId == null)
                                code = "(Exchange Loss)";
                            allocationSeed.Rows.Add(
                                invId == DBNull.Value ? DBNull.Value : invId,
                                code,
                                row["Allocated Amount"],
                                row["Clearing Type"]);
                        }
                    }
                }
                catch { }
            }
            if (allocationSeed.Rows.Count == 0)
                allocationSeed.Rows.Add(DBNull.Value, "", 0m, ReceiptVoucherConstants.ClearingPartial);

            var receiptAlloc = BuildReceiptAllocationSection(
                GetReceiptAmount,
                GetRvCustomerId,
                allocationSeed,
                allocationsReadOnly);

            Action refreshRvHkd = () =>
            {
                long cid = GetComboLongId(cmbCurrency);
                decimal rate = existingRv?.ExchangeRate > 0 && cid == (existingRv?.CurrencyID ?? 0)
                    ? existingRv.ExchangeRate
                    : _currencyCtrl.GetRateToBase(cid > 0 ? cid : 1);
                if (decimal.TryParse(txtAmount.Text.Trim(), out decimal amt))
                    lblAmountHkd.Text = $"HKD {CurrencyConversionService.ToBaseAmount(amt, rate):N2} (rate {rate:N4})";
                else lblAmountHkd.Text = $"Rate {rate:N4} — enter amount";
                UpdateAllocationRemainLabel(receiptAlloc.Grid, GetReceiptAmount(), receiptAlloc.RemainLabel);
            };
            cmbCurrency.SelectedIndexChanged += (s, e) => refreshRvHkd();
            txtAmount.TextChanged += (s, e) => refreshRvHkd();
            refreshRvHkd();

            customerBinder.SelectionCommitted += (s, e) => receiptAlloc.RefreshPicker();
            cmbCustomer.Leave += (s, e) =>
            {
                if (customerBinder.GetSelectedId() > 0) return;
                if (ResolveCustomerId(cmbCustomer) > 0)
                    receiptAlloc.RefreshPicker();
            };

            headerLayout.Dock = DockStyle.Top;
            headerLayout.AutoSize = true;
            headerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var headerScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            headerScroll.Controls.Add(headerLayout);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            split.Panel1.Controls.Add(headerScroll);
            split.Panel2.Controls.Add(receiptAlloc.Section);
            dlg.Load += (s, e) => FitVoucherFormHeaderSplitter(split, headerLayout);
            dlg.Shown += (s, e) => FitVoucherFormHeaderSplitter(split, headerLayout);

            var btnSave = UITheme.CreatePrimaryButton("Save");
            if (isNew) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.ReceiptVoucher);
            else PermissionGuard.ApplyEditButton(btnSave, PermissionModule.ReceiptVoucher);
            var btnCancel = UITheme.CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => dlg.Close();
            btnSave.Click += (s, e) =>
            {
                var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                if (!PermissionGuard.Ensure(PermissionModule.ReceiptVoucher, action, dlg)) return;

                long custId = ResolveCustomerId(cmbCustomer);
                if (custId <= 0 || defaultStaffId <= 0 ||
                    !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    UITheme.ShowWarning("Valid Customer and Amount are required.");
                    return;
                }

                string voucherCode = isNew ? "RV-TEMP" : txtCode.Text.Trim();
                if (!isNew && !TryValidateVoucherCode(voucherCode, false, existingRv?.ReceiptVoucherID ?? 0, _rvCtrl.ExistsByCode, out string codeError))
                {
                    UITheme.ShowWarning(codeError);
                    return;
                }

                int statusValue = existingRv?.Status == 1 ? 1 : (cmbStatus.SelectedIndex == 1 ? 2 : 0);

                List<ReceiptAllocation> allocations = null;
                if (!allocationsReadOnly)
                {
                    if (!TryBuildReceiptAllocations(receiptAlloc.Grid, out allocations, out string allocError))
                    {
                        UITheme.ShowWarning(allocError);
                        return;
                    }
                    decimal sum = 0;
                    foreach (var a in allocations) sum += a.ReceivedAmount;
                    if (Math.Abs(sum - amount) > 0.01m)
                    {
                        UITheme.ShowWarning(
                            $"Allocated total ({sum:N2}) does not equal receipt amount ({amount:N2}). Adjust allocation lines.");
                        return;
                    }
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
                        Status = statusValue,
                        Remark = txtRemark.Text.Trim(),
                        CurrencyID = GetComboLongId(cmbCurrency) > 0 ? GetComboLongId(cmbCurrency) : 1
                    };
                    long rvId;
                    if (isNew)
                        rvId = _rvCtrl.Insert(entity);
                    else
                    {
                        _rvCtrl.Update(entity);
                        rvId = entity.ReceiptVoucherID;
                    }

                    if (!allocationsReadOnly && allocations != null)
                    {
                        var allocResult = _financeWorkflow.SaveReceiptDraftAllocations(rvId, allocations);
                        if (!allocResult.Success)
                        {
                            UITheme.ShowWarning(allocResult.Message);
                            return;
                        }
                    }

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
            dlg.Controls.Add(split);
            return dlg;
        }

        private void LoadAll()
        {
            GridHelper.BindStatusData(
                _pvGrid,
                _pvCtrl.GetAllPaymentVouchers(),
                "Status",
                DictionaryService.Categories.PaymentVoucher);
            GridHelper.BindStatusData(
                _rvGrid,
                _rvCtrl.GetAllReceiptVouchers(),
                "Status",
                DictionaryService.Categories.ReceiptVoucher);

            if (_pvGrid.Columns.Contains("ID")) _pvGrid.Columns["ID"].Visible = false;
            if (_rvGrid.Columns.Contains("ID")) _rvGrid.Columns["ID"].Visible = false;

            UpdateSummary();
            LoadCharts();
            LoadCurrencyBreakdown();
        }

        private void UpdateSummary()
        {
            decimal totalIn = SumGridHkd(_rvGrid, excludedStatus: 2);
            decimal totalOut = SumGridHkd(_pvGrid, excludedStatus: 3);
            _lblTotalIncome.Text = $"{HkdLabel} {totalIn:N2}";
            _lblTotalExpense.Text = $"{HkdLabel} {totalOut:N2}";
            _lblNetFlow.Text = $"{HkdLabel} {(totalIn - totalOut):N2}";
            _lblNetFlow.ForeColor = (totalIn - totalOut) >= 0 ? Color.DarkGreen : Color.DarkRed;
        }

        private static decimal SumGridHkd(DataGridView grid, int excludedStatus)
        {
            var dt = grid?.DataSource as DataTable;
            if (dt == null) return 0m;
            string hkdColumn = dt.Columns.Contains("Amount (HKD)") ? "Amount (HKD)" : "Amount";
            decimal total = 0m;
            foreach (DataRowView row in dt.DefaultView)
            {
                if (Convert.ToInt32(row["Status"]) == excludedStatus) continue;
                if (row[hkdColumn] == DBNull.Value) continue;
                total += Convert.ToDecimal(row[hkdColumn]);
            }
            return total;
        }

        private void LoadCurrencyBreakdown()
        {
            try
            {
                _rvCurrencyBreakdown = _rvCtrl.GetIncomeByCurrency();
                _pvCurrencyBreakdown = _pvCtrl.GetExpenseByCurrency();
                _rvCurrencyGrid.DataSource = _rvCurrencyBreakdown;
                _pvCurrencyGrid.DataSource = _pvCurrencyBreakdown;
                GridHelper.StyleGrid(_rvCurrencyGrid);
                GridHelper.StyleGrid(_pvCurrencyGrid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Currency breakdown load error: " + ex.Message);
            }
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
                    _incomeChart.SetBarData(labels, values, HkdLabel);
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
                    _expenseChart.SetBarData(labels, values, HkdLabel);
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
                grid.CurrentCellDirtyStateChanged += (s, e) => OnAllocationGridCurrentCellDirtyStateChanged(grid);
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

        private static void FitVoucherFormHeaderSplitter(SplitContainer split, Control headerLayout, int minAllocationHeight = 200)
        {
            if (split == null || headerLayout == null || split.Height <= split.SplitterWidth) return;

            int splitterW = split.SplitterWidth;
            int panel1Min = split.Panel1MinSize;
            int panel2Min = minAllocationHeight;
            int maxForPanel2 = split.Height - splitterW - panel1Min;
            if (maxForPanel2 < 25) return;

            if (panel2Min > maxForPanel2)
                panel2Min = maxForPanel2;

            try
            {
                if (split.Panel2MinSize != panel2Min)
                    split.Panel2MinSize = panel2Min;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            headerLayout.PerformLayout();
            int preferred = headerLayout.PreferredSize.Height;
            if (preferred <= 0)
                preferred = headerLayout.GetPreferredSize(new Size(Math.Max(1, split.Panel1.ClientSize.Width), 0)).Height;
            if (preferred <= 0)
                preferred = headerLayout.Height;

            int minDist = panel1Min;
            int maxDist = split.Height - splitterW - split.Panel2MinSize;
            if (maxDist < minDist) return;

            int target = preferred + 6;
            try
            {
                split.SplitterDistance = Math.Max(minDist, Math.Min(target, maxDist));
            }
            catch (InvalidOperationException)
            {
                // Container not ready yet; ignore and retry on next layout pass.
            }
        }

        private static void OnAllocationGridCurrentCellDirtyStateChanged(DataGridView grid)
        {
            if (!grid.IsCurrentCellDirty) return;
            if (grid.CurrentCell is DataGridViewComboBoxCell)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private static void UpdateAllocationRemainLabel(DataGridView grid, decimal receiptAmount, Label lblRemain)
        {
            decimal sum = SumAllocationGridAmounts(grid);
            decimal remain = receiptAmount - sum;
            lblRemain.Text = $"Total: {receiptAmount:N2}  |  Allocated: {sum:N2}  |  Remaining: {remain:N2}" +
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

        private AllocationGridSection BuildReceiptAllocationSection(
            Func<decimal> getReceiptAmount,
            Func<long> getCustomerId,
            DataTable seedRows,
            bool readOnly)
        {
            var allocationTable = seedRows ?? CreateAllocationEditorTable();
            DataTable invoicePicker = null;
            try
            {
                long customerId = getCustomerId();
                if (customerId > 0)
                    invoicePicker = BuildInvoicePickerWithBlank(_invoiceCtrl.GetInvoicesForCustomerPicker(customerId));
            }
            catch { }
            DataTable clearingPicker = BuildClearingTypeDataTable();

            var section = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            var lblRemain = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = UITheme.TextGray,
                Text = "Invoice allocations — amounts must sum to receipt total."
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = readOnly
            };

            var colInvoice = new DataGridViewComboBoxColumn
            {
                Name = "colInvoice",
                HeaderText = "Invoice *",
                DataPropertyName = "Invoice ID",
                DisplayMember = "DisplayText",
                ValueMember = "Invoice ID",
                Width = 280,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = readOnly
            };
            if (invoicePicker != null)
                colInvoice.DataSource = invoicePicker;

            grid.Columns.Add(colInvoice);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAmount",
                HeaderText = "Allocated Amount *",
                DataPropertyName = "Allocated Amount",
                Width = 140,
                ReadOnly = readOnly
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
                ValueMember = "Code",
                ReadOnly = readOnly
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
                if (readOnly) return;
                if (e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "colType" && e.RowIndex >= 0)
                    ApplyExchangeLossRowState(grid.Rows[e.RowIndex]);
                UpdateAllocationRemainLabel(grid, getReceiptAmount(), lblRemain);
            };
            grid.CurrentCellDirtyStateChanged += (s, e) => OnAllocationGridCurrentCellDirtyStateChanged(grid);
            grid.EditingControlShowing += (s, e) =>
            {
                if (readOnly) return;
                if (grid.CurrentCell?.OwningColumn?.Name == "colInvoice" && e.Control is ComboBox cmb)
                {
                    cmb.SelectedIndexChanged -= ReceiptAllocationInvoiceChanged;
                    cmb.SelectedIndexChanged += ReceiptAllocationInvoiceChanged;
                }
            };

            void ReceiptAllocationInvoiceChanged(object sender, EventArgs e)
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

            Action refreshInvoicePicker = () =>
            {
                DataTable picker = null;
                try
                {
                    long customerId = getCustomerId();
                    if (customerId > 0)
                        picker = BuildInvoicePickerWithBlank(_invoiceCtrl.GetInvoicesForCustomerPicker(customerId));
                }
                catch { }
                invoicePicker = picker;
                colInvoice.DataSource = picker;
            };

            UpdateAllocationRemainLabel(grid, getReceiptAmount(), lblRemain);

            var lineToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 4),
                Visible = !readOnly
            };
            var btnAdd = UITheme.CreateSecondaryButton("Add Line");
            btnAdd.Width = 100;
            var btnRemove = UITheme.CreateSecondaryButton("Remove Line");
            btnRemove.Width = 110;
            var btnAddExchangeLoss = UITheme.CreateSecondaryButton("Add Exchange Loss");
            btnAddExchangeLoss.Width = 150;
            btnAdd.Click += (s, e) =>
            {
                allocationTable.Rows.Add(DBNull.Value, "", 0m, ReceiptVoucherConstants.ClearingPartial);
                UpdateAllocationRemainLabel(grid, getReceiptAmount(), lblRemain);
            };
            btnRemove.Click += (s, e) =>
            {
                if (grid.CurrentRow == null || allocationTable.Rows.Count <= 1)
                {
                    UITheme.ShowWarning("At least one allocation line is required.");
                    return;
                }
                allocationTable.Rows.RemoveAt(grid.CurrentRow.Index);
                UpdateAllocationRemainLabel(grid, getReceiptAmount(), lblRemain);
            };
            btnAddExchangeLoss.Click += (s, e) =>
            {
                decimal sum = SumAllocationGridAmounts(grid);
                decimal remain = getReceiptAmount() - sum;
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
                UpdateAllocationRemainLabel(grid, getReceiptAmount(), lblRemain);
            };
            lineToolbar.Controls.Add(btnAdd);
            lineToolbar.Controls.Add(btnRemove);
            lineToolbar.Controls.Add(btnAddExchangeLoss);

            var gridHost = new Panel { Dock = DockStyle.Fill };
            gridHost.Controls.Add(grid);
            gridHost.Controls.Add(new Label
            {
                Text = "Invoice Allocations",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark
            });

            section.Controls.Add(gridHost);
            section.Controls.Add(lineToolbar);
            section.Controls.Add(lblRemain);
            return new AllocationGridSection
            {
                Section = section,
                Grid = grid,
                Table = allocationTable,
                RemainLabel = lblRemain,
                RefreshPicker = refreshInvoicePicker
            };
        }

        private static DataTable CreatePoAllocationEditorTable()
        {
            var dt = new DataTable();
            var colPo = dt.Columns.Add("Purchase Order ID", typeof(long));
            colPo.AllowDBNull = true;
            dt.Columns.Add("PO Code", typeof(string));
            dt.Columns.Add("Pay Amount", typeof(string));
            dt.Columns.Add("Clearing Type", typeof(int));
            return dt;
        }

        private static decimal SumPoAllocationGridAmounts(DataGridView grid)
        {
            decimal sum = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (decimal.TryParse(row.Cells["colPoAmount"]?.Value?.ToString(), out decimal amt))
                    sum += amt;
            }
            return sum;
        }

        private static void UpdatePoAllocationRemainLabel(DataGridView grid, decimal paymentAmount, Label lblRemain)
        {
            decimal sum = SumPoAllocationGridAmounts(grid);
            decimal remain = paymentAmount - sum;
            lblRemain.Text = $"Total: {paymentAmount:N2}  |  Allocated: {sum:N2}  |  Remaining: {remain:N2}" +
                             (Math.Abs(remain) < 0.01m ? " (balanced)" : "");
            lblRemain.ForeColor = Math.Abs(remain) < 0.01m ? Color.DarkGreen : Color.DarkOrange;
        }

        private static bool TryBuildPaymentAllocations(DataGridView grid, out List<VoucherPurchaseOrderLine> lines, out string error)
        {
            lines = new List<VoucherPurchaseOrderLine>();
            error = null;
            var seenPo = new HashSet<long>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                object poObj = row.Cells["colPo"]?.Value;
                if (poObj == null || poObj == DBNull.Value ||
                    !long.TryParse(poObj.ToString(), out long poId) || poId <= 0)
                {
                    error = "Each line must have a purchase order selected.";
                    return false;
                }
                if (!seenPo.Add(poId))
                {
                    error = "Duplicate purchase order on allocation lines. Combine amounts into one line per PO.";
                    return false;
                }

                if (!decimal.TryParse(row.Cells["colPoAmount"]?.Value?.ToString(), out decimal amount) || amount <= 0)
                {
                    error = "Each line must have a positive pay amount.";
                    return false;
                }

                int typeCode = GetClearingTypeFromCell(row.Cells["colPoType"]?.Value, 1);
                if (typeCode <= 0)
                {
                    error = "Each line must have a payment type.";
                    return false;
                }

                lines.Add(new VoucherPurchaseOrderLine
                {
                    PurchaseOrderID = poId,
                    ClearingType = typeCode,
                    PayAmount = amount
                });
            }
            if (lines.Count == 0)
            {
                error = "Add at least one purchase order allocation line.";
                return false;
            }
            return true;
        }

        private AllocationGridSection BuildPoAllocationSection(
            Func<decimal> getPaymentAmount,
            Func<long> getSupplierId,
            DataTable seedRows,
            bool readOnly)
        {
            var allocationTable = seedRows ?? CreatePoAllocationEditorTable();
            DataTable poPicker = BuildPoPickerForSupplier(getSupplierId());
            DataTable clearingPicker = BuildClearingTypeDataTable();

            var section = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            var lblRemain = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = UITheme.TextGray,
                Text = "PO allocations — amounts must sum to payment total."
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = readOnly
            };

            var colPo = new DataGridViewComboBoxColumn
            {
                Name = "colPo",
                HeaderText = "Purchase Order *",
                DataPropertyName = "Purchase Order ID",
                DisplayMember = "DisplayText",
                ValueMember = "Purchase Order ID",
                Width = 300,
                FlatStyle = FlatStyle.Flat,
                DataSource = poPicker,
                ReadOnly = readOnly
            };
            grid.Columns.Add(colPo);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPoAmount",
                HeaderText = "Pay Amount *",
                DataPropertyName = "Pay Amount",
                Width = 180,
                MinimumWidth = 120,
                ReadOnly = readOnly
            });
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "colPoType",
                HeaderText = "Payment Type *",
                DataPropertyName = "Clearing Type",
                Width = 180,
                FlatStyle = FlatStyle.Flat,
                DataSource = clearingPicker,
                DisplayMember = "Value",
                ValueMember = "Code",
                ReadOnly = readOnly
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPoCode",
                HeaderText = "PO Code",
                DataPropertyName = "PO Code",
                Visible = false
            });

            grid.DataSource = allocationTable;
            grid.DataError += (s, e) => { e.ThrowException = false; };
            grid.CellValueChanged += (s, e) =>
            {
                if (readOnly) return;
                UpdatePoAllocationRemainLabel(grid, getPaymentAmount(), lblRemain);
            };
            grid.CellEndEdit += (s, e) =>
            {
                if (readOnly) return;
                if (e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "colPoAmount")
                    UpdatePoAllocationRemainLabel(grid, getPaymentAmount(), lblRemain);
            };
            grid.CurrentCellDirtyStateChanged += (s, e) => OnAllocationGridCurrentCellDirtyStateChanged(grid);
            grid.EditingControlShowing += (s, e) =>
            {
                if (readOnly) return;
                if (grid.CurrentCell?.OwningColumn?.Name == "colPo" && e.Control is ComboBox cmb)
                {
                    cmb.SelectedIndexChanged -= PoAllocationPoChanged;
                    cmb.SelectedIndexChanged += PoAllocationPoChanged;
                }
            };

            void PoAllocationPoChanged(object sender, EventArgs e)
            {
                if (grid.CurrentRow == null) return;
                if (sender is ComboBox cmb && cmb.SelectedValue != null && cmb.SelectedValue != DBNull.Value)
                {
                    if (!long.TryParse(cmb.SelectedValue.ToString(), out long poId) || poId <= 0)
                        return;
                    grid.CurrentRow.Cells["colPo"].Value = poId;
                    if (poPicker != null)
                    {
                        foreach (DataRow r in poPicker.Rows)
                        {
                            if (r["Purchase Order ID"] == DBNull.Value) continue;
                            if (!long.TryParse(r["Purchase Order ID"]?.ToString(), out long rowPoId) || rowPoId != poId)
                                continue;
                            grid.CurrentRow.Cells["colPoCode"].Value = r["Purchase Order Code"]?.ToString();
                            break;
                        }
                    }
                }
            }

            Action refreshPoPicker = () =>
            {
                IEnumerable<long> ensureIds = allocationTable.Rows.Cast<DataRow>()
                    .Where(r => r["Purchase Order ID"] != DBNull.Value)
                    .Select(r => Convert.ToInt64(r["Purchase Order ID"]))
                    .Where(id => id > 0);
                poPicker = BuildPoPickerForSupplier(getSupplierId(), ensureIds);
                colPo.DataSource = poPicker;
            };

            UpdatePoAllocationRemainLabel(grid, getPaymentAmount(), lblRemain);

            var lineToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 4),
                Visible = !readOnly
            };
            var btnAdd = UITheme.CreateSecondaryButton("Add Line");
            btnAdd.Width = 100;
            var btnRemove = UITheme.CreateSecondaryButton("Remove Line");
            btnRemove.Width = 110;
            btnAdd.Click += (s, e) =>
            {
                allocationTable.Rows.Add(DBNull.Value, "", "", 1);
                UpdatePoAllocationRemainLabel(grid, getPaymentAmount(), lblRemain);
            };
            btnRemove.Click += (s, e) =>
            {
                if (grid.CurrentRow == null || allocationTable.Rows.Count <= 1)
                {
                    UITheme.ShowWarning("At least one allocation line is required.");
                    return;
                }
                allocationTable.Rows.RemoveAt(grid.CurrentRow.Index);
                UpdatePoAllocationRemainLabel(grid, getPaymentAmount(), lblRemain);
            };
            lineToolbar.Controls.Add(btnAdd);
            lineToolbar.Controls.Add(btnRemove);

            var gridHost = new Panel { Dock = DockStyle.Fill };
            gridHost.Controls.Add(grid);
            gridHost.Controls.Add(new Label
            {
                Text = "Purchase Order Allocations",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark
            });

            section.Controls.Add(gridHost);
            section.Controls.Add(lineToolbar);
            section.Controls.Add(lblRemain);
            return new AllocationGridSection
            {
                Section = section,
                Grid = grid,
                Table = allocationTable,
                RemainLabel = lblRemain,
                RefreshPicker = refreshPoPicker
            };
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
                    isPaymentVoucher: true,
                    paymentVoucherId);
            }
            catch (Exception ex) { UITheme.ShowError(ex.Message); }
        }

        public void OpenPaymentVoucherDetail(long paymentVoucherId) => ShowPaymentVoucherDetail(paymentVoucherId);

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
                    isPaymentVoucher: false,
                    receiptVoucherId);
            }
            catch (Exception ex) { UITheme.ShowError(ex.Message); }
        }

        public void OpenReceiptVoucherDetail(long receiptVoucherId) => ShowReceiptVoucherDetail(receiptVoucherId);

        private void ShowVoucherDetailDialog(string title, DataTable voucherFields, DataTable lines, string linesTabTitle, long partyId, bool isPaymentVoucher, long voucherId)
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

                if (voucherId > 0)
                {
                    DataTable related = isPaymentVoucher
                        ? RelatedDocumentsHelper.GetPaymentVoucherRelated(voucherId)
                        : RelatedDocumentsHelper.GetReceiptVoucherRelated(voucherId);
                    if (related != null && related.Rows.Count > 0)
                        tabs.TabPages.Add(RelatedDocumentsHelper.BuildRelatedDocumentsTab(related, dlg));

                    tabs.TabPages.Add(DocumentAuditService.BuildActivityTab(
                        isPaymentVoucher ? DocumentAuditService.Types.PaymentVoucher : DocumentAuditService.Types.ReceiptVoucher,
                        voucherId));
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
            var cmb = new ComboBox { Width = 280 };
            SupplierComboHelper.Attach(cmb, _supplierCtrl, selectedSupplierId);
            return cmb;
        }

        private ComboBox BuildCustomerCombo(long selectedCustomerId = 0)
        {
            var cmb = new ComboBox { Width = 300 };
            CustomerComboHelper.Attach(cmb, _customerCtrl, selectedCustomerId);
            return cmb;
        }

        private long ResolveSupplierId(ComboBox cmbSupplier) =>
            SupplierComboHelper.ResolveSupplierId(cmbSupplier, _supplierCtrl);

        private long ResolveCustomerId(ComboBox cmbCustomer) =>
            CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);

        private static string ExtractLeadingCode(string text)
        {
            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return text;
            int separator = text.IndexOf('—');
            if (separator < 0)
                separator = text.IndexOf(" - ", StringComparison.Ordinal);
            return separator > 0 ? text.Substring(0, separator).Trim() : text;
        }

        private long ResolveSalesOrderId(ComboBox cmbSalesOrder, long customerId)
        {
            long id = GetComboLongId(cmbSalesOrder);
            if (id > 0) return id;
            string text = ExtractLeadingCode(cmbSalesOrder.Text);
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var so = _soCtrl.GetByCode(text);
            if (so == null) return 0;
            if (customerId > 0 && so.CustomerID != customerId) return 0;
            return so.SalesOrderID;
        }

        private long ResolveInvoiceId(ComboBox cmbInvoice, long customerId, long salesOrderId)
        {
            long id = GetComboLongId(cmbInvoice);
            if (id > 0) return id;
            string text = ExtractLeadingCode(cmbInvoice.Text);
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var invoice = _invoiceCtrl.GetByCode(text);
            if (invoice == null) return 0;
            if (customerId > 0 && invoice.CustomerID != customerId) return 0;
            if (salesOrderId > 0 && invoice.SalesOrderID != salesOrderId) return 0;
            return invoice.InvoiceID;
        }

        private long ResolvePurchaseOrderId(ComboBox cmbPo, long supplierId)
        {
            long id = GetComboLongId(cmbPo);
            if (id > 0) return id;
            string text = ExtractLeadingCode(cmbPo.Text);
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var po = _poCtrl.GetByCode(text);
            if (po == null) return 0;
            if (supplierId > 0 && po.SupplierID != supplierId) return 0;
            return po.PurchaseOrderID;
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

        private static int GetClearingTypeFromCell(object cellValue, int defaultCode)
        {
            if (cellValue is int intVal) return intVal > 0 ? intVal : defaultCode;
            if (cellValue is DictionaryUIHelper.ComboBoxItem typeItem) return typeItem.Code;
            if (cellValue != null && int.TryParse(cellValue.ToString(), out int parsed) && parsed > 0)
                return parsed;
            return defaultCode;
        }

        private void BindPoLinkCombo(ComboBox combo, DataTable poPicker, long selectedPoId)
        {
            combo.DataSource = null;
            if (poPicker == null)
            {
                combo.Items.Clear();
                return;
            }
            combo.DataSource = poPicker;
            combo.DisplayMember = "DisplayText";
            combo.ValueMember = "Purchase Order ID";
            if (selectedPoId > 0) SetComboLongValue(combo, selectedPoId);
        }

        private void UpdatePoBalanceLabel(long poId, Label lblBalance)
        {
            if (poId <= 0)
            {
                lblBalance.Text = "Optional — select a PO to auto-fill amount, or enter amount manually.";
                return;
            }
            decimal total = _poCtrl.GetTotalAmount(poId);
            decimal settled = _pvCtrl.GetSettledTotalByPurchaseOrder(poId);
            decimal outstanding = _pvCtrl.GetOutstandingByPurchaseOrder(poId);
            lblBalance.Text = $"PO Total: {total:N2}  |  Settled: {settled:N2}  |  Outstanding: {outstanding:N2}";
        }

        private DataTable BuildSalesOrderPicker(long customerId)
        {
            DataTable dt;
            if (customerId <= 0)
            {
                dt = new DataTable();
                dt.Columns.Add("Order ID", typeof(long));
                dt.Columns["Order ID"].AllowDBNull = true;
                dt.Columns.Add("Order Code", typeof(string));
                dt.Columns.Add("DisplayText", typeof(string));
            }
            else
            {
                dt = _soCtrl.GetSalesOrdersPickerByCustomer(customerId);
                if (dt != null && !dt.Columns.Contains("DisplayText"))
                {
                    dt.Columns.Add("DisplayText", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        string code = row["Order Code"]?.ToString();
                        string customerRef = row.Table.Columns.Contains("Customer Ref") ? row["Customer Ref"]?.ToString() : "";
                        row["DisplayText"] = string.IsNullOrWhiteSpace(customerRef) ? code : $"{code} — {customerRef}";
                    }
                }
            }
            InsertBlankPickerRow(dt, "Order ID", "DisplayText", "(Select Sales Order)");
            return dt;
        }

        private DataTable BuildInvoicePickerForRv(long customerId, long salesOrderId)
        {
            DataTable dt;
            if (customerId <= 0)
            {
                dt = new DataTable();
                dt.Columns.Add("Invoice ID", typeof(long));
                dt.Columns["Invoice ID"].AllowDBNull = true;
                dt.Columns.Add("DisplayText", typeof(string));
            }
            else
            {
                dt = _invoiceCtrl.GetInvoicesForSalesOrderPicker(customerId, salesOrderId);
            }
            return BuildInvoicePickerWithBlank(dt);
        }

        private static void BindSalesOrderCombo(ComboBox combo, DataTable soPicker, long selectedSoId)
        {
            combo.DataSource = null;
            if (soPicker == null)
            {
                combo.Items.Clear();
                return;
            }
            combo.DataSource = soPicker;
            combo.DisplayMember = "DisplayText";
            combo.ValueMember = "Order ID";
            if (selectedSoId > 0) SetComboLongValue(combo, selectedSoId);
        }

        private static void BindInvoiceCombo(ComboBox combo, DataTable invoicePicker, long selectedInvoiceId)
        {
            combo.DataSource = null;
            if (invoicePicker == null)
            {
                combo.Items.Clear();
                return;
            }
            combo.DataSource = invoicePicker;
            combo.DisplayMember = "DisplayText";
            combo.ValueMember = "Invoice ID";
            if (selectedInvoiceId > 0) SetComboLongValue(combo, selectedInvoiceId);
        }

        private void ApplyInvoiceLink(long invoiceId, ComboBox cmbSalesOrder, TextBox txtAmount, Label lblBalance,
            Action<bool> setSuppress)
        {
            if (invoiceId <= 0)
            {
                lblBalance.Text = "Select an invoice to auto-fill amount.";
                return;
            }

            var invoice = _invoiceCtrl.GetById(invoiceId);
            if (invoice != null && invoice.SalesOrderID > 0 && GetComboLongId(cmbSalesOrder) != invoice.SalesOrderID)
            {
                setSuppress(true);
                try { SetComboLongValue(cmbSalesOrder, invoice.SalesOrderID); }
                finally { setSuppress(false); }
            }

            decimal total = _invoiceCtrl.GetInvoiceTotal(invoiceId);
            decimal outstanding = _invoiceCtrl.GetOutstandingByInvoice(invoiceId);
            decimal received = Math.Max(0, total - outstanding);
            lblBalance.Text = $"Invoice Total: {total:N2}  |  Received: {received:N2}  |  Outstanding: {outstanding:N2}";
            if (outstanding > 0)
                txtAmount.Text = outstanding.ToString("0.##");
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

        private ComboBox BuildCurrencyCombo(long selectedCurrencyId = 1)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            var dt = _currencyCtrl.GetAllForCombo();
            cmb.DataSource = dt;
            cmb.DisplayMember = "Code";
            cmb.ValueMember = "Currency ID";
            if (selectedCurrencyId > 0)
                SetComboLongValue(cmb, selectedCurrencyId);
            return cmb;
        }

        private static long GetComboLongId(ComboBox cmb)
        {
            if (cmb == null) return 0;
            try
            {
                if (cmb.SelectedValue != null && cmb.SelectedValue != DBNull.Value
                    && long.TryParse(cmb.SelectedValue.ToString(), out long id) && id > 0)
                    return id;
            }
            catch { }

            if (cmb.SelectedIndex >= 0 && cmb.SelectedIndex < cmb.Items.Count
                && cmb.Items[cmb.SelectedIndex] is DataRowView drv
                && drv.Row.Table.Columns.Contains(cmb.ValueMember)
                && drv[cmb.ValueMember] != DBNull.Value
                && long.TryParse(drv[cmb.ValueMember].ToString(), out long rowId))
                return rowId;

            return 0;
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