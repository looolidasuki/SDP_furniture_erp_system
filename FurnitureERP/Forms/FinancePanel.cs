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
    public class FinancePanel : UserControl
    {
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly RefundRequestController _refundCtrl = new RefundRequestController();
        private readonly FinanceWorkflowService _financeWorkflow = new FinanceWorkflowService();
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly ProductController _productCtrl = new ProductController();
        private readonly ReceiptVoucherController _receiptCtrl = new ReceiptVoucherController();
        private long _depositProductId;

        private TabControl _tabControl;
        private DataGridView _invoiceGrid;
        private DataGridView _refundGrid;
        private TextBox _invoiceSearchBox;
        private ComboBox _invoiceStatusFilter;
        private TextBox _refundSearchBox;
        private ComboBox _refundStatusFilter;

        public FinancePanel(string module = "Invoices")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            LoadData();
            if (module == "Refunds") _tabControl.SelectedIndex = 1;
        }

        private void BuildUI()
        {
            _tabControl = new TabControl { Dock = DockStyle.Fill };

            // Invoices Tab
            TabPage invoiceTab = new TabPage("🧾 Invoices");
            invoiceTab.BackColor = UITheme.Background;

            Panel invoiceToolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            Button btnNewInvoice = UITheme.CreatePrimaryButton("+ New Invoice");
            btnNewInvoice.Location = new Point(0, 8);
            btnNewInvoice.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.Invoice, PermissionAction.Create, this)) ShowCreateInvoiceDialog(); };
            PermissionGuard.ApplyCreateButton(btnNewInvoice, PermissionModule.Invoice);
            Button btnRefreshInvoice = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefreshInvoice.Location = new Point(btnNewInvoice.Width + 10, 8);
            btnRefreshInvoice.Click += (s, e) => LoadInvoices();
            Button btnDetailInvoice = UITheme.CreateSecondaryButton("View Detail");
            btnDetailInvoice.Location = new Point(btnRefreshInvoice.Right + 10, 8);
            btnDetailInvoice.Click += (s, e) =>
            {
                if (_invoiceGrid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select an invoice first."); return; }
                ShowInvoiceTableDetailFromRow(_invoiceGrid.CurrentRow);
            };
            Button btnEditInvoice = UITheme.CreateSecondaryButton("Edit");
            btnEditInvoice.Location = new Point(btnDetailInvoice.Right + 10, 8);
            btnEditInvoice.Click += (s, e) => EditSelectedInvoice();
            PermissionGuard.ApplyEditButton(btnEditInvoice, PermissionModule.Invoice);
            Button btnPrintInvoice = UITheme.CreateSecondaryButton("Print PDF");
            btnPrintInvoice.Location = new Point(btnEditInvoice.Right + 10, 8);
            btnPrintInvoice.Click += (s, e) => PrintSelectedInvoice();
            Button btnInvoiceFromDelivery = UITheme.CreateSecondaryButton("Invoice from Delivery");
            btnInvoiceFromDelivery.Location = new Point(btnPrintInvoice.Right + 10, 8);
            btnInvoiceFromDelivery.Click += (s, e) => CreateInvoiceFromDelivery();
            PermissionGuard.ApplyCreateButton(btnInvoiceFromDelivery, PermissionModule.Invoice);
            _invoiceSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnInvoiceFromDelivery.Right + 10, 10) };
            _invoiceSearchBox.TextChanged += (s, e) => ApplyInvoiceTableFilter(_invoiceSearchBox?.Text);
            _invoiceStatusFilter = new ComboBox { Width = 160, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_invoiceSearchBox.Right + 10, 10) };
            PopulateInvoiceStatusFilter();
            _invoiceStatusFilter.SelectedIndexChanged += (s, e) =>
            {
                ApplyInvoiceTableFilter(_invoiceSearchBox?.Text);
            };
            invoiceToolbar.Controls.Add(btnNewInvoice);
            invoiceToolbar.Controls.Add(btnRefreshInvoice);
            invoiceToolbar.Controls.Add(btnDetailInvoice);
            invoiceToolbar.Controls.Add(btnEditInvoice);
            invoiceToolbar.Controls.Add(btnPrintInvoice);
            invoiceToolbar.Controls.Add(btnInvoiceFromDelivery);
            invoiceToolbar.Controls.Add(_invoiceSearchBox);
            invoiceToolbar.Controls.Add(_invoiceStatusFilter);

            _invoiceGrid = GridHelper.CreateStyledGrid();
            _invoiceGrid.CellDoubleClick += InvoiceGrid_CellDoubleClick;

            Panel invoicePanel = new Panel { Dock = DockStyle.Fill };
            invoicePanel.Controls.Add(_invoiceGrid);
            invoicePanel.Controls.Add(FilterBlockHelper.CreateFilterBlock(_invoiceGrid, "Invoice Filters"));
            invoicePanel.Controls.Add(invoiceToolbar);
            invoiceTab.Controls.Add(invoicePanel);

            // Refunds Tab
            TabPage refundTab = new TabPage("💰 Refund Requests");
            refundTab.BackColor = UITheme.Background;

            Panel refundToolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            Button btnNewRefund = UITheme.CreatePrimaryButton("+ New Refund");
            btnNewRefund.Location = new Point(0, 8);
            btnNewRefund.Click += (s, e) => { if (PermissionGuard.Ensure(PermissionModule.Refund, PermissionAction.Create, this)) ShowCreateRefundDialog(); };
            PermissionGuard.ApplyCreateButton(btnNewRefund, PermissionModule.Refund);
            Button btnRefreshRefund = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefreshRefund.Location = new Point(btnNewRefund.Width + 10, 8);
            btnRefreshRefund.Click += (s, e) => LoadRefunds();
            Button btnDetailRefund = UITheme.CreateSecondaryButton("View Detail");
            btnDetailRefund.Location = new Point(btnRefreshRefund.Right + 10, 8);
            btnDetailRefund.Click += (s, e) =>
            {
                if (_refundGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a refund request first."); return; }
                ShowRefundTableDetailFromRow(_refundGrid.CurrentRow);
            };
            Button btnEditRefund = UITheme.CreateSecondaryButton("Edit");
            btnEditRefund.Location = new Point(btnDetailRefund.Right + 10, 8);
            btnEditRefund.Click += (s, e) => EditSelectedRefund();
            PermissionGuard.ApplyEditButton(btnEditRefund, PermissionModule.Refund);
            Button btnPrintRefund = UITheme.CreateSecondaryButton("Print PDF");
            btnPrintRefund.Location = new Point(btnEditRefund.Right + 10, 8);
            btnPrintRefund.Click += (s, e) =>
            {
                if (_refundGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a refund request first."); return; }
                PrintRefundRow(_refundGrid.CurrentRow);
            };
            _refundSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnPrintRefund.Right + 10, 10) };
            _refundSearchBox.TextChanged += (s, e) => LoadRefunds();
            _refundStatusFilter = new ComboBox { Width = 140, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_refundSearchBox.Right + 10, 10) };
            DictionaryUIHelper.BindStatusFilter(_refundStatusFilter, DictionaryService.Categories.RefundStatus);
            _refundStatusFilter.SelectedIndexChanged += (s, e) => LoadRefunds();
            refundToolbar.Controls.Add(btnNewRefund);
            refundToolbar.Controls.Add(btnRefreshRefund);
            refundToolbar.Controls.Add(btnDetailRefund);
            refundToolbar.Controls.Add(btnEditRefund);
            refundToolbar.Controls.Add(btnPrintRefund);
            refundToolbar.Controls.Add(_refundSearchBox);
            refundToolbar.Controls.Add(_refundStatusFilter);

            _refundGrid = GridHelper.CreateStyledGrid();
            _refundGrid.CellDoubleClick += RefundGrid_CellDoubleClick;

            Panel refundPanel = new Panel { Dock = DockStyle.Fill };
            refundPanel.Controls.Add(_refundGrid);
            refundPanel.Controls.Add(FilterBlockHelper.CreateFilterBlock(_refundGrid, "Refund Filters"));
            refundPanel.Controls.Add(refundToolbar);
            refundTab.Controls.Add(refundPanel);

            if (AppSession.CanView(PermissionModule.Invoice))
                _tabControl.TabPages.Add(invoiceTab);
            if (AppSession.CanView(PermissionModule.Refund))
                _tabControl.TabPages.Add(refundTab);
            Controls.Add(_tabControl);
        }

        private void LoadData()
        {
            LoadInvoices();
            LoadRefunds();
        }

        private void LoadInvoices()
        {
            try
            {
                var dt = _invoiceCtrl.GetAllInvoices();
                dt = DictionaryService.DecorateStatusColumn(dt, "Status", DictionaryService.Categories.Invoice);
                if (dt != null && dt.Columns.Contains("Invoice Type"))
                {
                    if (!dt.Columns.Contains("Type Label"))
                        dt.Columns.Add("Type Label", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Invoice Type"] == DBNull.Value) continue;
                        int t = Convert.ToInt32(row["Invoice Type"]);
                        row["Type Label"] = DictionaryService.GetDisplayName(DictionaryService.Categories.InvoiceType, t);
                    }
                }
                _invoiceGrid.DataSource = dt;
                ApplyInvoiceTableFilter(_invoiceSearchBox?.Text);
                GridHelper.StyleGrid(_invoiceGrid);
                HideInvoiceGridCodeColumns();
            }
            catch { }
        }

        private void PopulateInvoiceStatusFilter()
        {
            if (_invoiceStatusFilter == null) return;
            _invoiceStatusFilter.Items.Clear();
            _invoiceStatusFilter.Items.Add("All Status");
            foreach (var item in DictionaryService.GetItems(DictionaryService.Categories.Invoice))
                _invoiceStatusFilter.Items.Add(new ComboBoxItem(item.Key, item.Value));
            _invoiceStatusFilter.SelectedIndex = 0;
        }

        private void HideInvoiceGridCodeColumns()
        {
            if (_invoiceGrid == null) return;
            foreach (string col in new[] { "Status", "Invoice Type" })
            {
                if (_invoiceGrid.Columns.Contains(col))
                    _invoiceGrid.Columns[col].Visible = false;
            }
        }

        private void ApplyInvoiceTableFilter(string keyword)
        {
            if (!(_invoiceGrid.DataSource is DataTable dt)) return;
            keyword = (keyword ?? "").Trim().Replace("'", "''");
            var conditions = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var textConditions = new System.Collections.Generic.List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(string))
                        textConditions.Add($"[{col.ColumnName}] LIKE '%{keyword}%'");
                }
                if (textConditions.Count > 0)
                    conditions.Add("(" + string.Join(" OR ", textConditions) + ")");
            }

            if (_invoiceStatusFilter != null && _invoiceStatusFilter.SelectedIndex > 0 &&
                _invoiceStatusFilter.SelectedItem is ComboBoxItem statusItem && dt.Columns.Contains("Status"))
            {
                conditions.Add("[Status] = " + statusItem.Code);
            }

            dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
        }

        private long DepositProductId
        {
            get
            {
                if (_depositProductId <= 0)
                    _depositProductId = _productCtrl.EnsureDepositProductId();
                return _depositProductId;
            }
        }

        private void LoadRefunds()
        {
            try
            {
                _refundGrid.DataSource = _refundCtrl.GetAllRefundRequests();
                ApplyRefundTableFilter();
                GridHelper.StyleGrid(_refundGrid);
                HideRefundGridInternalColumns();
            }
            catch { }
        }

        private void ApplyRefundTableFilter()
        {
            if (!(_refundGrid.DataSource is DataTable dt)) return;
            var filters = new List<string>();
            string keyword = (_refundSearchBox?.Text ?? "").Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var parts = new List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(string))
                        parts.Add($"[{col.ColumnName}] LIKE '%{keyword}%'");
                }
                if (parts.Count > 0)
                    filters.Add("(" + string.Join(" OR ", parts) + ")");
            }

            if (_refundStatusFilter != null && _refundStatusFilter.SelectedIndex > 0)
            {
                int? status = DictionaryUIHelper.GetFilterStatusCode(_refundStatusFilter);
                if (status.HasValue && dt.Columns.Contains("Status"))
                    filters.Add("[Status] = " + status.Value);
            }

            dt.DefaultView.RowFilter = filters.Count > 0 ? string.Join(" AND ", filters) : "";
        }

        private void HideRefundGridInternalColumns()
        {
            if (_refundGrid == null) return;
            foreach (string col in new[] { "Refund Method", "Refund Reason", "Status" })
            {
                if (_refundGrid.Columns.Contains(col))
                    _refundGrid.Columns[col].Visible = false;
            }
        }

        private void RefundGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _refundGrid.Rows[e.RowIndex];
            if (!AppSession.CanEdit(PermissionModule.Refund))
            {
                ShowRefundTableDetailFromRow(row);
                return;
            }
            string requestCode = row.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode)) return;
            var refund = _refundCtrl.GetByCode(requestCode);
            if (refund == null) return;
            ShowRefundEditorDialog(refund);
        }

        private void ApplyTableFilter(DataGridView grid, string keyword, int statusIndex)
        {
            if (!(grid.DataSource is DataTable dt)) return;
            keyword = (keyword ?? "").Trim().Replace("'", "''");
            var conditions = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var textConditions = new System.Collections.Generic.List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(string))
                    {
                        textConditions.Add($"[{col.ColumnName}] LIKE '%{keyword}%'");
                    }
                }
                if (textConditions.Count > 0)
                {
                    conditions.Add("(" + string.Join(" OR ", textConditions) + ")");
                }
            }

            if (statusIndex > 0 && dt.Columns.Contains("Status"))
            {
                conditions.Add("[Status] = " + (statusIndex - 1));
            }

            dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
        }

        private void InvoiceGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _invoiceGrid.Rows[e.RowIndex];
            long invoiceId = Convert.ToInt64(row.Cells[0].Value);
            if (AppSession.CanEdit(PermissionModule.Invoice)) ShowInvoiceEditDialog(invoiceId);
            else ShowInvoiceDetails(invoiceId);
        }

        private void EditSelectedInvoice()
        {
            if (_invoiceGrid?.CurrentRow?.Cells[0].Value == null) { UITheme.ShowWarning("Please select an invoice first."); return; }
            if (!PermissionGuard.Ensure(PermissionModule.Invoice, PermissionAction.Edit, this)) return;
            ShowInvoiceEditDialog(Convert.ToInt64(_invoiceGrid.CurrentRow.Cells[0].Value));
        }

        private void EditSelectedRefund()
        {
            if (_refundGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a refund request first."); return; }
            if (!PermissionGuard.Ensure(PermissionModule.Refund, PermissionAction.Edit, this)) return;
            int rowIdx = _refundGrid.CurrentRow.Index;
            RefundGrid_CellDoubleClick(_refundGrid, new DataGridViewCellEventArgs(0, rowIdx));
        }

        private void ShowInvoiceEditDialog(long invoiceId)
        {
            var invoice = _invoiceCtrl.GetById(invoiceId);
            if (invoice == null) return;

            bool isDeposit = invoice.InvoiceType == InvoiceController.InvoiceTypeDeposit;
            bool locked = _invoiceCtrl.HasVerifiedReceiptAllocations(invoiceId);

            using (var dlg = new Form())
            {
                dlg.Text = "Invoice Details / Edit";
                dlg.Size = new Size(920, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = isDeposit ? 9 : 8, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbCustomer = BuildCustomerCombo(invoice.CustomerID);
                var cmbSalesOrder = BuildSalesOrderCombo(invoice.CustomerID, invoice.SalesOrderID);
                cmbCustomer.Enabled = !locked;
                cmbSalesOrder.Enabled = !locked;

                string staffName = "—";
                try
                {
                    var hdr = _invoiceCtrl.GetHeaderDetail(invoiceId);
                    if (hdr != null && hdr.Rows.Count > 0 && hdr.Columns.Contains("Staff"))
                        staffName = (hdr.Rows[0]["Staff"]?.ToString() ?? "").Trim();
                }
                catch { }
                if (string.IsNullOrWhiteSpace(staffName)) staffName = "—";

                var lblStaff = new Label { Text = staffName, AutoSize = true, ForeColor = UITheme.TextDark };
                var lblType = new Label
                {
                    Text = DictionaryService.GetDisplayName(DictionaryService.Categories.InvoiceType, invoice.InvoiceType),
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };

                var cmbStatus = BuildInvoiceStatusCombo(invoice.Status);
                var txtRemark = new TextBox { Text = invoice.Remark ?? "", Multiline = true, Height = 48 };

                decimal invTotal = 0, received = 0;
                try
                {
                    invTotal = _invoiceCtrl.GetInvoiceTotal(invoiceId);
                    var hdr = _invoiceCtrl.GetHeaderDetail(invoiceId);
                    if (hdr != null && hdr.Rows.Count > 0 && hdr.Columns.Contains("Total Received"))
                        decimal.TryParse(hdr.Rows[0]["Total Received"]?.ToString(), out received);
                }
                catch { }

                var lblFinancial = new Label
                {
                    AutoSize = false,
                    Height = 40,
                    ForeColor = UITheme.TextGray,
                    Text = $"Invoice Total: {invTotal:0.00}   |   Received: {received:0.00}   |   Outstanding: {(invTotal - received):0.00}"
                };

                TextBox txtDepositAmount = null;
                int rowIdx = 0;
                UITheme.AddFormField(layout, rowIdx++, "Invoice Code", new Label { Text = invoice.InvoiceCode, AutoSize = true });
                UITheme.AddFormField(layout, rowIdx++, "Customer *", cmbCustomer);
                UITheme.AddFormField(layout, rowIdx++, "Sales Order *", cmbSalesOrder);
                UITheme.AddFormField(layout, rowIdx++, "Staff", lblStaff);
                UITheme.AddFormField(layout, rowIdx++, "Invoice Type", lblType);
                if (isDeposit)
                {
                    decimal depAmt = _invoiceCtrl.GetDepositLineAmount(invoiceId, DepositProductId);
                    txtDepositAmount = new TextBox { Text = depAmt.ToString("0.00"), Width = 200 };
                    if (locked) txtDepositAmount.ReadOnly = true;
                    UITheme.AddFormField(layout, rowIdx++, "Deposit Amount *", txtDepositAmount);
                }
                UITheme.AddFormField(layout, rowIdx++, "Status", cmbStatus);
                UITheme.AddFormField(layout, rowIdx++, "Financial Summary", lblFinancial);
                UITheme.AddFormField(layout, rowIdx++, "Remark", txtRemark);
                if (locked)
                {
                    var lblLock = new Label { AutoSize = true, ForeColor = UITheme.TextGray, Text = "Customer, sales order and deposit amount are locked after receipt verification." };
                    UITheme.AddFormField(layout, rowIdx++, "", lblLock);
                }

                cmbCustomer.SelectedIndexChanged += (s, e) =>
                {
                    long cid = GetComboLongId(cmbCustomer);
                    BindSalesOrderCombo(cmbSalesOrder, cid, 0);
                };

                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.ReadOnly = true;
                try { lineGrid.DataSource = _invoiceCtrl.GetInvoiceLinesForView(invoiceId); GridHelper.StyleGrid(lineGrid); } catch { }

                layout.AutoSize = true;
                layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                layout.Dock = DockStyle.Top;
                var headerHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                headerHost.Controls.Add(layout);

                var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
                split.Panel1.Controls.Add(headerHost);
                split.Panel2.Controls.Add(lineGrid);

                var btnUpdate = UITheme.CreatePrimaryButton("Update");
                PermissionGuard.ApplyEditButton(btnUpdate, PermissionModule.Invoice);
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                var txtDepositAmountCapture = txtDepositAmount;
                btnUpdate.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.Invoice, PermissionAction.Edit, dlg)) return;
                    long customerId = GetComboLongId(cmbCustomer);
                    long salesOrderId = GetComboLongId(cmbSalesOrder);
                    if (customerId <= 0 || salesOrderId <= 0)
                    {
                        UITheme.ShowWarning("Please select a valid customer and sales order.");
                        return;
                    }
                    if (!locked)
                    {
                        invoice.CustomerID = customerId;
                        invoice.SalesOrderID = salesOrderId;
                    }
                    invoice.Status = GetSelectedStatusCode(cmbStatus);
                    invoice.Remark = txtRemark.Text.Trim();

                    if (!_invoiceCtrl.Update(invoice))
                    {
                        UITheme.ShowWarning("Failed to update invoice.");
                        return;
                    }
                    if (isDeposit && txtDepositAmountCapture != null && !locked)
                    {
                        if (!decimal.TryParse(txtDepositAmountCapture.Text.Trim(), out decimal depAmt) || depAmt <= 0)
                        {
                            UITheme.ShowWarning("Deposit amount must be greater than zero.");
                            return;
                        }
                        if (!_invoiceCtrl.UpdateDepositLineAmount(invoiceId, DepositProductId, depAmt))
                        {
                            UITheme.ShowWarning("Failed to update deposit line amount.");
                            return;
                        }
                    }

                    UITheme.ShowSuccess("Invoice updated.");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadInvoices();
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnUpdate);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(split);
                dlg.Controls.Add(btnPanel);
                dlg.Shown += (s, e) =>
                {
                    if (split.Height > 200) split.SplitterDistance = Math.Min(320, split.Height - 180);
                };
                dlg.ShowDialog(this);
            }
        }

        private void ShowInvoiceDetails(long invoiceId)
        {
            ShowInvoiceViewDialog(invoiceId, null);
        }

        private void ShowInvoiceTableDetailFromRow(DataGridViewRow row)
        {
            if (row?.Cells[0].Value == null) return;
            long invoiceId = Convert.ToInt64(row.Cells[0].Value);
            ShowInvoiceViewDialog(invoiceId, row);
        }

        private void ShowInvoiceViewDialog(long invoiceId, DataGridViewRow listRow)
        {
            DataTable header = null;
            DataTable lines = null;
            DataTable receipts = null;
            try { header = _invoiceCtrl.GetHeaderDetail(invoiceId); } catch { }
            try { lines = _invoiceCtrl.GetInvoiceLinesForView(invoiceId); } catch { }
            try { receipts = _invoiceCtrl.GetReceiptSettlementsByInvoice(invoiceId); } catch { }

            var fields = BuildInvoiceViewFields(header, invoiceId);
            string linesTabTitle = GetInvoiceLinesTabTitle(header);
            if (receipts != null)
            {
                try
                {
                    foreach (DataRow r in receipts.Rows)
                    {
                        string entryType = receipts.Columns.Contains("Entry Type")
                            ? r["Entry Type"]?.ToString()
                            : "Receipt";
                        if (string.Equals(entryType, "Refund", StringComparison.OrdinalIgnoreCase))
                        {
                            if (receipts.Columns.Contains("Allocation Type"))
                                r["Allocation Type"] = "Refund (Paid)";
                            continue;
                        }
                        if (receipts.Columns.Contains("Allocation Type") && int.TryParse(r["Allocation Type"]?.ToString(), out int t) && t > 0)
                            r["Allocation Type"] = DictionaryService.GetDisplayName(DictionaryService.Categories.PoPaymentType, t);
                    }
                    receipts.AcceptChanges();
                }
                catch { }
            }

            string code = listRow?.Cells["Invoice Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(code) && header != null && header.Rows.Count > 0)
                code = header.Rows[0]["Invoice Code"]?.ToString();
            string title = string.IsNullOrWhiteSpace(code) ? $"Invoice Detail — ID: {invoiceId}" : $"Invoice Detail — {code}";

            DetailViewHelper.ShowDetail(this, title, fields, lines, $"Invoice_{invoiceId}", receipts, null,
                linesTabTitle, "Receipts & Refunds");
        }

        private static string GetInvoiceLinesTabTitle(DataTable header)
        {
            if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Invoice Type")
                && int.TryParse(header.Rows[0]["Invoice Type"]?.ToString(), out int invoiceType)
                && invoiceType == InvoiceController.InvoiceTypeDeposit)
                return "Deposit Line";
            return "Invoice Lines";
        }

        private DataTable BuildInvoiceViewFields(DataTable header, long invoiceId)
        {
            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            if (fields == null) return fields;
            try
            {
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Invoice Type"))
                {
                    var typeRows = fields.Select("Field = 'Invoice Type'");
                    foreach (DataRow r in typeRows)
                    {
                        if (int.TryParse(r["Value"]?.ToString(), out int code))
                            r["Value"] = DictionaryService.GetDisplayName(DictionaryService.Categories.InvoiceType, code);
                    }
                    fields.AcceptChanges();
                }
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Status"))
                {
                    var statusRows = fields.Select("Field = 'Status'");
                    foreach (DataRow r in statusRows)
                    {
                        if (int.TryParse(r["Value"]?.ToString(), out int code))
                            r["Value"] = DictionaryService.GetDisplayName(DictionaryService.Categories.Invoice, code);
                    }
                    fields.AcceptChanges();
                }

                decimal total = _invoiceCtrl.GetInvoiceTotal(invoiceId);
                decimal received = 0;
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Total Received"))
                    decimal.TryParse(header.Rows[0]["Total Received"]?.ToString(), out received);

                RemoveInvoiceFieldRow(fields, "Total Received");
                fields.Rows.Add("Invoice Total Amount", total.ToString("0.00"));
                fields.Rows.Add("Total Received", received.ToString("0.00"));
                fields.Rows.Add("Outstanding Balance", (total - received).ToString("0.00"));
            }
            catch { }
            return fields;
        }

        private static void RemoveInvoiceFieldRow(DataTable fields, string fieldName)
        {
            if (fields == null) return;
            var rows = fields.Select($"Field = '{fieldName.Replace("'", "''")}'");
            foreach (DataRow r in rows)
                fields.Rows.Remove(r);
            fields.AcceptChanges();
        }

        private void ShowRefundTableDetailFromRow(DataGridViewRow row)
        {
            string requestCode = row?.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode)) return;
            DataTable header = null;
            try { header = _refundCtrl.GetHeaderDetail(requestCode); } catch { }
            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            string title = string.IsNullOrWhiteSpace(requestCode)
                ? "Refund Request Detail"
                : $"Refund Request — {requestCode}";
            DetailViewHelper.ShowDetail(this, title, fields, null, $"Refund_{requestCode}");
        }

        private void PrintSelectedInvoice()
        {
            if (_invoiceGrid?.CurrentRow?.Cells[0].Value == null)
            {
                UITheme.ShowWarning("Please select an invoice first.");
                return;
            }
            PrintInvoiceRow(_invoiceGrid.CurrentRow);
        }

        private void PrintInvoiceRow(DataGridViewRow row)
        {
            long invoiceId = Convert.ToInt64(row.Cells[0].Value);
            DataTable lines = null;
            DataTable header = null;
            try { lines = _invoiceCtrl.GetInvoiceLinesForView(invoiceId); } catch { }
            try { header = _invoiceCtrl.GetHeaderDetail(invoiceId); } catch { }
            var fields = BuildInvoiceViewFields(header, invoiceId);

            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    $"Invoice — {row.Cells["Invoice Code"]?.Value ?? invoiceId.ToString()}",
                    fields,
                    lines,
                    $"Invoice_{invoiceId}");
                PdfExportHelper.ExportToPdf(data, this);
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void PrintRefundRow(DataGridViewRow row)
        {
            string requestCode = row?.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode)) return;
            try
            {
                DataTable header = _refundCtrl.GetHeaderDetail(requestCode);
                var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
                var data = DetailViewHelper.FromFieldValueTable(
                    $"Refund Request — {requestCode}",
                    fields,
                    null,
                    $"Refund_{requestCode}");
                PdfExportHelper.ExportToPdf(data, this);
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void ShowCreateInvoiceDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Invoice";
                dlg.Size = new Size(560, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 7, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbCustomer = BuildCustomerCombo();
                var cmbSalesOrder = BuildSalesOrderCombo(0, 0);
                var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
                cmbType.Items.Add("1 - Deposit (prepayment invoice)");
                cmbType.Items.Add("2 - Normal (use Invoice from Delivery for shipped goods)");
                cmbType.SelectedIndex = 0;

                var txtDepositAmount = new TextBox { Width = 200, Location = new Point(0, 4) };
                var lblNormalHint = new Label
                {
                    AutoSize = false,
                    Width = 360,
                    Height = 48,
                    ForeColor = UITheme.TextGray,
                    Text = "Normal invoices for delivered goods are created via Invoice from Delivery on the toolbar.",
                    Location = new Point(0, 4),
                    Visible = false
                };
                var pnlTypeDetail = new Panel { Height = 56, MinimumSize = new Size(200, 56) };
                pnlTypeDetail.Controls.Add(txtDepositAmount);
                pnlTypeDetail.Controls.Add(lblNormalHint);
                var staffDisplay = AppSession.CurrentUser;
                var lblStaff = new Label
                {
                    Text = staffDisplay != null && !string.IsNullOrWhiteSpace(staffDisplay.FullName)
                        ? staffDisplay.FullName
                        : staffDisplay?.Username ?? "Current User",
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbStatus = BuildInvoiceStatusCombo(0);
                var txtRemark = new TextBox { Multiline = true, Height = 48 };

                UITheme.AddFormField(layout, 0, "Customer *", cmbCustomer);
                UITheme.AddFormField(layout, 1, "Sales Order *", cmbSalesOrder);
                UITheme.AddFormField(layout, 2, "Invoice Type *", cmbType);
                UITheme.AddFormField(layout, 3, "Amount / Note", pnlTypeDetail);
                UITheme.AddFormField(layout, 4, "Staff", lblStaff);
                UITheme.AddFormField(layout, 5, "Status", cmbStatus);
                UITheme.AddFormField(layout, 6, "Remark", txtRemark);

                void RefreshTypeUi()
                {
                    bool deposit = cmbType.SelectedIndex == 0;
                    txtDepositAmount.Visible = deposit;
                    lblNormalHint.Visible = !deposit;
                }
                cmbType.SelectedIndexChanged += (s, e) => RefreshTypeUi();
                cmbCustomer.SelectedIndexChanged += (s, e) =>
                {
                    BindSalesOrderCombo(cmbSalesOrder, GetComboLongId(cmbCustomer), 0);
                };
                RefreshTypeUi();

                layout.Dock = DockStyle.Fill;

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (cmbType.SelectedIndex == 1)
                    {
                        UITheme.ShowWarning("To create a normal invoice, use Invoice from Delivery after the delivery note is confirmed.");
                        return;
                    }
                    long customerId = GetComboLongId(cmbCustomer);
                    long salesOrderId = GetComboLongId(cmbSalesOrder);
                    if (customerId <= 0 || salesOrderId <= 0)
                    {
                        UITheme.ShowWarning("Please select a customer and sales order.");
                        return;
                    }
                    var so = _salesOrderCtrl.GetFullById(salesOrderId);
                    if (so == null || so.CustomerID != customerId)
                    {
                        UITheme.ShowWarning("Selected sales order does not belong to the selected customer.");
                        return;
                    }
                    if (!decimal.TryParse(txtDepositAmount.Text.Trim(), out decimal depAmt) || depAmt <= 0)
                    {
                        UITheme.ShowWarning("Deposit amount must be greater than zero.");
                        return;
                    }
                    long staffId = AppSession.CurrentUser?.StaffID ?? 0;
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Current user staff record is required.");
                        return;
                    }
                    var result = _financeWorkflow.CreateDepositInvoice(
                        customerId, salesOrderId, staffId, depAmt, txtRemark.Text.Trim(), GetSelectedStatusCode(cmbStatus));
                    if (result.Success)
                    {
                        UITheme.ShowSuccess(result.Message);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadInvoices();
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

        private void ShowCreateRefundDialog() => ShowRefundEditorDialog(null);

        private void ShowRefundEditorDialog(RefundRequest existing)
        {
            bool isNew = existing == null;
            int reasonCode = 0;
            if (!isNew)
            {
                var resolved = DictionaryService.ResolveRefundReasonCode(existing.RefundReason);
                reasonCode = resolved ?? 1;
            }

            using (var dlg = new Form())
            {
                dlg.Text = isNew ? "New Refund Request" : $"Edit Refund — {existing.RefundRequestCode}";
                dlg.Size = new Size(560, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                int row = 0;
                if (!isNew)
                {
                    UITheme.AddFormField(layout, row++, "Request Code",
                        new Label { Text = existing.RefundRequestCode, AutoSize = true, ForeColor = UITheme.TextDark });
                }

                var cmbInvoice = BuildInvoicePickerCombo(isNew ? 0 : existing.InvoiceID ?? 0);
                var cmbReceipt = BuildReceiptPickerCombo(isNew ? 0 : existing.ReceiptVoucherID ?? 0);
                var txtAmount = new TextBox { Text = isNew ? "" : existing.RefundAmount.ToString("0.00") };
                var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
                DictionaryUIHelper.BindStatusCombo(cmbMethod, DictionaryService.Categories.RefundMethod,
                    isNew ? 1 : existing.RefundMethod);
                var cmbReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
                DictionaryUIHelper.BindStatusCombo(cmbReason, DictionaryService.Categories.RefundReason, reasonCode);
                var txtRefundRef = new TextBox { Text = isNew ? "" : existing.RefundRef ?? "" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
                if (isNew)
                    DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.RefundStatus, 0);
                else
                    DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.RefundStatus, existing.Status);
                var txtRemark = new TextBox
                {
                    Text = isNew ? "" : existing.Remark ?? "",
                    Multiline = true,
                    Height = 56,
                    ScrollBars = ScrollBars.Vertical
                };

                string staffLabel = AppSession.CurrentUser != null
                    ? (string.IsNullOrWhiteSpace(AppSession.CurrentUser.FullName)
                        ? AppSession.CurrentUser.Username
                        : AppSession.CurrentUser.FullName)
                    : (isNew ? "—" : existing.StaffID.ToString());

                UITheme.AddFormField(layout, row++, "Invoice *", cmbInvoice);
                UITheme.AddFormField(layout, row++, "Receipt Voucher", cmbReceipt);
                UITheme.AddFormField(layout, row++, "Staff", new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark });
                UITheme.AddFormField(layout, row++, "Refund Amount *", txtAmount);
                UITheme.AddFormField(layout, row++, "Refund Method *", cmbMethod);
                UITheme.AddFormField(layout, row++, "Refund Reason *", cmbReason);
                UITheme.AddFormField(layout, row++, "Refund Transaction Ref", txtRefundRef);
                UITheme.AddFormField(layout, row++, "Status", cmbStatus);
                UITheme.AddFormField(layout, row, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton(isNew ? "Create" : "Update");
                if (isNew)
                    PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.Refund);
                else
                    PermissionGuard.ApplyEditButton(btnSave, PermissionModule.Refund);
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isNew ? PermissionAction.Create : PermissionAction.Edit;
                    if (!PermissionGuard.Ensure(PermissionModule.Refund, action, dlg)) return;

                    long invoiceId = GetComboLongId(cmbInvoice);
                    if (invoiceId <= 0)
                    {
                        UITheme.ShowWarning("Please select an invoice.");
                        return;
                    }
                    if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                    {
                        UITheme.ShowWarning("Refund amount must be greater than zero.");
                        return;
                    }
                    int methodCode = DictionaryUIHelper.GetSelectedStatusCode(cmbMethod);
                    if (methodCode <= 0)
                    {
                        UITheme.ShowWarning("Please select a refund method.");
                        return;
                    }
                    int selectedReasonCode = DictionaryUIHelper.GetSelectedStatusCode(cmbReason);
                    if (selectedReasonCode <= 0)
                    {
                        UITheme.ShowWarning("Please select a refund reason.");
                        return;
                    }
                    long staffId = AppSession.CurrentUser?.StaffID ?? (isNew ? 0 : existing.StaffID);
                    if (staffId <= 0)
                    {
                        UITheme.ShowWarning("Current user staff record is required.");
                        return;
                    }

                    long receiptId = GetComboLongId(cmbReceipt);
                    var rr = isNew ? new RefundRequest() : existing;
                    rr.InvoiceID = invoiceId;
                    rr.ReceiptVoucherID = receiptId > 0 ? (long?)receiptId : null;
                    rr.StaffID = staffId;
                    rr.RefundAmount = amount;
                    rr.RefundMethod = methodCode;
                    rr.RefundReason = DictionaryService.GetRefundReasonStorageKey(selectedReasonCode);
                    rr.RefundRef = txtRefundRef.Text.Trim();
                    rr.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                    rr.Remark = txtRemark.Text.Trim();

                    try
                    {
                        if (isNew)
                        {
                            rr.RefundRequestCode = "RF-TEMP";
                            long newRefundId = _refundCtrl.CreateRefundRequest(rr);
                            if (newRefundId <= 0)
                            {
                                UITheme.ShowWarning("Failed to create refund request.");
                                return;
                            }
                            rr.RefundRequestID = newRefundId;
                            if (rr.Status == FinanceWorkflowService.RefundStatusPaid && rr.InvoiceID.HasValue)
                            {
                                var settle = _financeWorkflow.ApplyRefundPaidSettlement(rr);
                                if (!settle.Success)
                                {
                                    UITheme.ShowWarning(settle.Message);
                                    return;
                                }
                            }
                            UITheme.ShowSuccess("Refund request created.");
                        }
                        else if (!_refundCtrl.Update(rr))
                        {
                            UITheme.ShowWarning("Failed to update refund request.");
                            return;
                        }
                        else
                        {
                            if (rr.Status == FinanceWorkflowService.RefundStatusPaid && rr.InvoiceID.HasValue)
                            {
                                var settle = _financeWorkflow.ApplyRefundPaidSettlement(rr);
                                if (!settle.Success)
                                {
                                    UITheme.ShowWarning(settle.Message);
                                    return;
                                }
                            }
                            UITheme.ShowSuccess("Refund request updated.");
                        }

                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadRefunds();
                        LoadInvoices();
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

                var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                scroll.Controls.Add(layout);
                dlg.Controls.Add(scroll);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private ComboBox BuildInvoicePickerCombo(long selectedInvoiceId)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
            var dt = _invoiceCtrl.GetInvoicesForPicker();
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Invoice ID";
            if (selectedInvoiceId > 0) SetComboLongValue(cmb, selectedInvoiceId);
            else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            return cmb;
        }

        private ComboBox BuildReceiptPickerCombo(long selectedReceiptId)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
            var dt = _receiptCtrl.GetReceiptVouchersForPicker();
            if (dt != null)
            {
                if (!dt.Columns.Contains("DisplayText"))
                    dt.Columns.Add("DisplayText", typeof(string));
                var withNone = dt.Clone();
                var none = withNone.NewRow();
                none["Receipt Voucher ID"] = 0L;
                none["Voucher Code"] = "";
                none["DisplayText"] = "(None)";
                withNone.Rows.Add(none);
                foreach (DataRow row in dt.Rows)
                    withNone.ImportRow(row);
                cmb.DataSource = withNone;
            }
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Receipt Voucher ID";
            SetComboLongValue(cmb, selectedReceiptId);
            return cmb;
        }

        private ComboBox BuildDeliveryNotePickerCombo(long selectedDeliveryNoteId = 0)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 420
            };
            DataTable dt = null;
            try { dt = _deliveryCtrl.GetDeliveryNotesForInvoicingPicker(); } catch { }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Delivery Note ID";
            if (selectedDeliveryNoteId > 0)
                SetComboLongValue(cmb, selectedDeliveryNoteId);
            else if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
            return cmb;
        }

        private void CreateInvoiceFromDelivery()
        {
            if (!PermissionGuard.Ensure(PermissionModule.Invoice, PermissionAction.Create, this)) return;

            DataTable pickerSource = null;
            try { pickerSource = _deliveryCtrl.GetDeliveryNotesForInvoicingPicker(); } catch { }
            if (pickerSource == null || pickerSource.Rows.Count == 0)
            {
                UITheme.ShowWarning(
                    "No delivery notes are ready for invoicing. A note must be completed (status) and have remaining quantity to invoice.");
                return;
            }

            long deliveryId = 0;
            using (var dlg = new Form())
            {
                dlg.Text = "Create Invoice from Delivery";
                dlg.Size = new Size(520, 180);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(16),
                    ColumnCount = 2,
                    RowCount = 1
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.Controls.Add(new Label
                {
                    Text = "Delivery Note *",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    ForeColor = UITheme.TextDark
                }, 0, 0);

                var cmbDelivery = BuildDeliveryNotePickerCombo();
                cmbDelivery.Dock = DockStyle.Top;
                cmbDelivery.Width = 340;
                layout.Controls.Add(cmbDelivery, 1, 0);

                var btnOk = UITheme.CreatePrimaryButton("OK");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnOk.Click += (s, e) =>
                {
                    if (GetComboLongId(cmbDelivery) <= 0)
                    {
                        UITheme.ShowWarning("Please select a delivery note.");
                        return;
                    }
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

                dlg.Controls.Add(btnPanel);
                dlg.Controls.Add(layout);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                deliveryId = GetComboLongId(cmbDelivery);
            }

            if (deliveryId <= 0)
            {
                UITheme.ShowWarning("Please select a delivery note.");
                return;
            }

            OpenInvoiceFromDeliveryLinesDialog(deliveryId);
        }

        private void OpenInvoiceFromDeliveryLinesDialog(long deliveryId)
        {
            var delivery = _deliveryCtrl.GetById(deliveryId);
                if (delivery == null) { UITheme.ShowWarning("Delivery note not found."); return; }

                if (delivery.Status < 3)
                {
                    UITheme.ShowWarning("Delivery must be completed before invoicing.");
                    return;
                }

                DataTable dt = null;
                try { dt = _deliveryCtrl.GetLinesForInvoicing(deliveryId); } catch { }
                if (dt == null || dt.Rows.Count == 0) { UITheme.ShowWarning("Delivery note has no product lines."); return; }

                if (!dt.Columns.Contains("To Invoice Qty"))
                    dt.Columns.Add("To Invoice Qty", typeof(int));
                foreach (DataRow r in dt.Rows)
                {
                    int remaining = 0;
                    try { remaining = Convert.ToInt32(r["Remaining Qty"]); } catch { }
                    r["To Invoice Qty"] = Math.Max(0, remaining);
                }

                bool hasQtyToInvoice = false;
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["To Invoice Qty"]) > 0) { hasQtyToInvoice = true; break; }
                }
                if (!hasQtyToInvoice)
                {
                    UITheme.ShowWarning("This delivery note has no remaining quantity to invoice.");
                    return;
                }

                using (var pickDlg = new Form())
                {
                    pickDlg.Text = $"Invoice from Delivery — {delivery.DeliveryNoteCode}";
                    pickDlg.Size = new Size(860, 520);
                    pickDlg.StartPosition = FormStartPosition.CenterParent;
                    pickDlg.BackColor = UITheme.Background;
                    pickDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    pickDlg.MaximizeBox = false;

                    var top = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(12, 8, 12, 8) };
                    var lbl = new Label { Text = "Normal invoice from delivery", AutoSize = true, ForeColor = UITheme.TextDark, Left = 0, Top = 8 };
                    var chkOffset = new CheckBox
                    {
                        Text = "Apply deposit offset (add DEPOSIT line qty -1)",
                        AutoSize = true,
                        Left = 0,
                        Top = 32
                    };
                    top.Controls.Add(lbl);
                    top.Controls.Add(chkOffset);

                    var grid = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = false,
                        AutoGenerateColumns = true,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        MultiSelect = false,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        BackgroundColor = Color.White,
                        BorderStyle = BorderStyle.FixedSingle,
                        RowHeadersVisible = false
                    };
                    grid.DataSource = dt;
                    GridHelper.StyleGrid(grid);
                    if (grid.Columns.Contains("Product ID"))
                        grid.Columns["Product ID"].Visible = false;
                    if (grid.Columns.Contains("To Invoice Qty"))
                    {
                        grid.Columns["To Invoice Qty"].ReadOnly = false;
                        grid.Columns["To Invoice Qty"].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 230);
                    }

                    var btnCreate = UITheme.CreatePrimaryButton("Create Invoice");
                    var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                    btnCancel.Click += (s, e) => pickDlg.Close();
                    btnCreate.Click += (s, e) =>
                    {
                        var map = new Dictionary<long, int>();
                        foreach (DataRow r in dt.Rows)
                        {
                            long pid = 0;
                            int qty = 0;
                            try { pid = Convert.ToInt64(r["Product ID"]); } catch { }
                            try { qty = Convert.ToInt32(r["To Invoice Qty"]); } catch { qty = 0; }
                            if (pid > 0 && qty > 0) map[pid] = qty;
                        }

                        long staffId = AppSession.CurrentUser?.StaffID ?? 1;
                        var result = _financeWorkflow.CreateInvoiceFromDeliveryPartial(
                            deliveryId, staffId, map, InvoiceController.InvoiceTypeNormal, chkOffset.Checked);
                        if (result.Success)
                        {
                            UITheme.ShowSuccess(result.Message);
                            LoadInvoices();
                            pickDlg.Close();
                        }
                        else UITheme.ShowWarning(result.Message);
                    };
                    var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                    btnPanel.Controls.Add(btnCreate);
                    btnPanel.Controls.Add(btnCancel);

                    pickDlg.Controls.Add(grid);
                    pickDlg.Controls.Add(btnPanel);
                    pickDlg.Controls.Add(top);
                    pickDlg.ShowDialog(this);
                }
        }

        private ComboBox BuildCustomerCombo(long selectedCustomerId = 0)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
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

        private ComboBox BuildSalesOrderCombo(long customerId, long selectedSalesOrderId)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            BindSalesOrderCombo(cmb, customerId, selectedSalesOrderId);
            return cmb;
        }

        private void BindSalesOrderCombo(ComboBox cmb, long customerId, long selectedSalesOrderId)
        {
            DataTable dt;
            if (customerId > 0)
                dt = _salesOrderCtrl.GetSalesOrdersPickerByCustomer(customerId);
            else
            {
                dt = new DataTable();
                dt.Columns.Add("Order ID", typeof(long));
                dt.Columns.Add("DisplayText", typeof(string));
            }
            if (dt != null && !dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string code = row.Table.Columns.Contains("Order Code") ? row["Order Code"]?.ToString() : "";
                    string cref = row.Table.Columns.Contains("Customer Ref") ? row["Customer Ref"]?.ToString() : "";
                    row["DisplayText"] = string.IsNullOrWhiteSpace(cref) ? code : $"{code} ({cref})";
                }
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Order ID";
            if (selectedSalesOrderId > 0) SetComboLongValue(cmb, selectedSalesOrderId);
            else if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        private static ComboBox BuildInvoiceStatusCombo(int selectedStatus)
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            foreach (var item in DictionaryService.GetItems(DictionaryService.Categories.Invoice))
                cmb.Items.Add(new ComboBoxItem(item.Key, $"{item.Key} - {item.Value}"));
            SelectStatusCombo(cmb, selectedStatus);
            return cmb;
        }

        private static void SelectStatusCombo(ComboBox cmb, int statusCode)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is ComboBoxItem item && item.Code == statusCode)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = Math.Min(statusCode, cmb.Items.Count - 1);
        }

        private static int GetSelectedStatusCode(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboBoxItem item) return item.Code;
            return cmb.SelectedIndex >= 0 ? cmb.SelectedIndex : 0;
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

        private sealed class ComboBoxItem
        {
            public int Code { get; }
            public string Text { get; }
            public ComboBoxItem(int code, string text) { Code = code; Text = text; }
            public override string ToString() => Text;
        }
    }
}
