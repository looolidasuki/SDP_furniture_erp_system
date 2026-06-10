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
            invoiceToolbar.Controls.Add(btnNewInvoice);
            invoiceToolbar.Controls.Add(btnRefreshInvoice);
            invoiceToolbar.Controls.Add(btnDetailInvoice);
            invoiceToolbar.Controls.Add(btnEditInvoice);
            invoiceToolbar.Controls.Add(btnPrintInvoice);
            invoiceToolbar.Controls.Add(btnInvoiceFromDelivery);

            _invoiceGrid = GridHelper.CreateStyledGrid();
            _invoiceGrid.CellDoubleClick += InvoiceGrid_CellDoubleClick;

            Panel invoicePanel = new Panel { Dock = DockStyle.Fill };
            invoicePanel.Controls.Add(_invoiceGrid);
            invoicePanel.Controls.Add(FilterBlockHelper.CreateFilterBlock(_invoiceGrid, "Invoice Filters", DictionaryService.Categories.Invoice));
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
            Button btnUpdateRefundStatus = UITheme.CreateSecondaryButton("Update Status");
            btnUpdateRefundStatus.Location = new Point(btnDetailRefund.Right + 10, 8);
            btnUpdateRefundStatus.Click += (s, e) => ShowUpdateRefundStatusDialog();
            PermissionGuard.ApplyEditButton(btnUpdateRefundStatus, PermissionModule.Refund);
            Button btnEditRefund = UITheme.CreateSecondaryButton("Edit");
            btnEditRefund.Location = new Point(btnUpdateRefundStatus.Right + 10, 8);
            btnEditRefund.Click += (s, e) => EditSelectedRefund();
            PermissionGuard.ApplyEditButton(btnEditRefund, PermissionModule.Refund);
            Button btnPrintRefund = UITheme.CreateSecondaryButton("Print PDF");
            btnPrintRefund.Location = new Point(btnEditRefund.Right + 10, 8);
            btnPrintRefund.Click += (s, e) =>
            {
                if (_refundGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a refund request first."); return; }
                PrintRefundRow(_refundGrid.CurrentRow);
            };
            refundToolbar.Controls.Add(btnNewRefund);
            refundToolbar.Controls.Add(btnRefreshRefund);
            refundToolbar.Controls.Add(btnDetailRefund);
            refundToolbar.Controls.Add(btnUpdateRefundStatus);
            refundToolbar.Controls.Add(btnEditRefund);
            refundToolbar.Controls.Add(btnPrintRefund);

            _refundGrid = GridHelper.CreateStyledGrid();
            _refundGrid.CellDoubleClick += RefundGrid_CellDoubleClick;

            Panel refundPanel = new Panel { Dock = DockStyle.Fill };
            refundPanel.Controls.Add(_refundGrid);
            refundPanel.Controls.Add(FilterBlockHelper.CreateFilterBlock(_refundGrid, "Refund Filters", DictionaryService.Categories.RefundStatus));
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
                dt = GridHelper.DecorateStatusTable(dt, "Status", DictionaryService.Categories.Invoice);
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
                GridHelper.BindStatusData(_invoiceGrid, dt, DictionaryService.Categories.Invoice);
                HideInvoiceGridCodeColumns();
            }
            catch (Exception ex)
            {
                UITheme.ShowWarning("Failed to load invoices: " + ex.Message);
            }
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
                GridHelper.BindStatusData(
                    _refundGrid,
                    _refundCtrl.GetAllRefundRequests(),
                    "Status",
                    DictionaryService.Categories.RefundStatus);
                HideRefundGridInternalColumns();
            }
            catch { }
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

                CustomerComboHelper.WireCustomerChanged(cmbCustomer, _customerCtrl, customerId =>
                    BindSalesOrderCombo(cmbSalesOrder, customerId, 0));

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
                    long customerId = CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);
                    long salesOrderId = GetComboLongId(cmbSalesOrder);
                    if (customerId <= 0 || salesOrderId <= 0)
                    {
                        UITheme.ShowWarning("Please select or type a valid customer and sales order.");
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
            DecorateInvoiceReceiptSettlements(receipts);

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

        private static void DecorateInvoiceReceiptSettlements(DataTable receipts)
        {
            if (receipts == null) return;
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

        private void ShowRefundTableDetailFromRow(DataGridViewRow row)
        {
            string requestCode = row?.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode))
            {
                UITheme.ShowWarning("Please select a refund request first.");
                return;
            }
            ShowRefundDetail(requestCode);
        }

        private void ShowRefundDetail(string requestCode)
        {
            try
            {
                var header = _refundCtrl.GetHeaderDetail(requestCode);
                if (header == null || header.Rows.Count == 0)
                {
                    UITheme.ShowWarning("Refund request not found.");
                    return;
                }

                var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
                var refund = _refundCtrl.GetByCode(requestCode);
                long customerId = _refundCtrl.ResolveCustomerId(requestCode);
                string title = $"Refund Request — {requestCode}";

                using (var dlg = new Form())
                {
                    dlg.Text = title;
                    dlg.Size = new Size(920, 620);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.BackColor = UITheme.Background;

                    var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };

                    var tabRequest = new TabPage("Request");
                    var requestGrid = GridHelper.CreateStyledGrid();
                    requestGrid.DataSource = fields;
                    GridHelper.StyleGrid(requestGrid);
                    requestGrid.Dock = DockStyle.Fill;
                    tabRequest.Controls.Add(requestGrid);
                    tabs.TabPages.Add(tabRequest);

                    if (customerId > 0 && AppSession.CanView(PermissionModule.Customer))
                        tabs.TabPages.Add(BuildRefundCustomerTab(customerId));

                    if (refund?.InvoiceID is long invoiceId && invoiceId > 0 && AppSession.CanView(PermissionModule.Invoice))
                        tabs.TabPages.Add(BuildRefundInvoiceTab(invoiceId));

                    if (refund?.ReceiptVoucherID is long receiptVoucherId && receiptVoucherId > 0
                        && AppSession.CanView(PermissionModule.ReceiptVoucher))
                        tabs.TabPages.Add(BuildRefundReceiptVoucherTab(receiptVoucherId));

                    var btnClose = UITheme.CreateSecondaryButton("Close");
                    btnClose.Click += (s, e) => dlg.Close();
                    var btnPrint = UITheme.CreatePrimaryButton("Print PDF");
                    btnPrint.Click += (s, e) =>
                    {
                        try
                        {
                            var data = DetailViewHelper.FromFieldValueTable(title, fields, null, $"Refund_{requestCode}");
                            if (PdfExportHelper.ExportToPdf(data, dlg))
                                UITheme.ShowSuccess("PDF saved successfully.");
                        }
                        catch (Exception ex)
                        {
                            UITheme.ShowError("Failed to export PDF: " + ex.Message);
                        }
                    };

                    var btnPanel = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 50,
                        FlowDirection = FlowDirection.RightToLeft,
                        Padding = new Padding(8)
                    };
                    btnPanel.Controls.Add(btnPrint);
                    btnPanel.Controls.Add(btnClose);
                    dlg.Controls.Add(tabs);
                    dlg.Controls.Add(btnPanel);
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                UITheme.ShowError(ex.Message);
            }
        }

        private TabPage BuildRefundInvoiceTab(long invoiceId)
        {
            var tab = new TabPage("Invoice");
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 220 };

            DataTable invoiceHeader = null;
            DataTable lines = null;
            DataTable receipts = null;
            try { invoiceHeader = _invoiceCtrl.GetHeaderDetail(invoiceId); } catch { }
            try { lines = _invoiceCtrl.GetInvoiceLinesForView(invoiceId); } catch { }
            try { receipts = _invoiceCtrl.GetReceiptSettlementsByInvoice(invoiceId); } catch { }
            DecorateInvoiceReceiptSettlements(receipts);

            var headerGrid = GridHelper.CreateStyledGrid();
            headerGrid.DataSource = BuildInvoiceViewFields(invoiceHeader, invoiceId);
            GridHelper.StyleGrid(headerGrid);
            headerGrid.Dock = DockStyle.Fill;

            var bottomTabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            string linesTabTitle = GetInvoiceLinesTabTitle(invoiceHeader);

            var linesGrid = GridHelper.CreateStyledGrid();
            linesGrid.DataSource = lines;
            GridHelper.StyleGrid(linesGrid);
            linesGrid.Dock = DockStyle.Fill;
            var linesTab = new TabPage(linesTabTitle);
            linesTab.Controls.Add(linesGrid);

            var receiptsGrid = GridHelper.CreateStyledGrid();
            receiptsGrid.DataSource = receipts;
            GridHelper.StyleGrid(receiptsGrid);
            receiptsGrid.Dock = DockStyle.Fill;
            var receiptsTab = new TabPage("Receipts & Refunds");
            receiptsTab.Controls.Add(receiptsGrid);

            bottomTabs.TabPages.Add(linesTab);
            bottomTabs.TabPages.Add(receiptsTab);
            split.Panel1.Controls.Add(headerGrid);
            split.Panel2.Controls.Add(bottomTabs);
            tab.Controls.Add(split);
            return tab;
        }

        private TabPage BuildRefundReceiptVoucherTab(long receiptVoucherId)
        {
            var tab = new TabPage("Receipt Voucher");
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 220 };

            DataTable rvHeader = null;
            DataTable allocations = null;
            try { rvHeader = _receiptCtrl.GetHeaderDetail(receiptVoucherId); } catch { }
            try
            {
                allocations = _receiptCtrl.GetInvoiceAllocationsDetailed(receiptVoucherId);
                if (allocations != null && allocations.Columns.Contains("Invoice ID"))
                    allocations.Columns.Remove("Invoice ID");
            }
            catch { }

            var headerGrid = GridHelper.CreateStyledGrid();
            headerGrid.DataSource = DetailViewHelper.SingleRowToFieldValueTable(rvHeader);
            GridHelper.StyleGrid(headerGrid);
            headerGrid.Dock = DockStyle.Fill;

            var allocGrid = GridHelper.CreateStyledGrid();
            allocGrid.DataSource = allocations;
            GridHelper.StyleGrid(allocGrid);
            allocGrid.Dock = DockStyle.Fill;

            split.Panel1.Controls.Add(headerGrid);
            split.Panel2.Controls.Add(allocGrid);
            tab.Controls.Add(split);
            return tab;
        }

        private TabPage BuildRefundCustomerTab(long customerId)
        {
            var tab = new TabPage("Customer");
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 180 };

            var profileGrid = GridHelper.CreateStyledGrid();
            profileGrid.DataSource = BuildRefundCustomerProfileFields(customerId);
            GridHelper.StyleGrid(profileGrid);
            profileGrid.Dock = DockStyle.Fill;

            var bottomTabs = new TabControl { Dock = DockStyle.Fill };
            var contactGrid = GridHelper.CreateStyledGrid();
            contactGrid.DataSource = BuildRefundCustomerContactsTable(customerId);
            GridHelper.StyleGrid(contactGrid);
            contactGrid.Dock = DockStyle.Fill;
            var contactTab = new TabPage("Contact Persons");
            contactTab.Controls.Add(contactGrid);

            var addressGrid = GridHelper.CreateStyledGrid();
            addressGrid.DataSource = BuildRefundCustomerDeliveryAddressesTable(customerId);
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

        private DataTable BuildRefundCustomerProfileFields(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            var customer = _customerCtrl.GetById(customerId);
            if (customer == null) return dt;

            AddRefundFieldRow(dt, "Customer Code", customer.CustomerCode);
            AddRefundFieldRow(dt, "Customer Ref Number", customer.CustomerRefNumber);
            AddRefundFieldRow(dt, "Customer Name", customer.CustomerName);
            AddRefundFieldRow(dt, "Billing Address", customer.BillingAddress);
            AddRefundFieldRow(dt, "Payment Term", customer.PaymentTerm);
            return dt;
        }

        private DataTable BuildRefundCustomerContactsTable(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Title");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var contact in _customerCtrl.GetContactPersons(customerId))
                dt.Rows.Add(contact.Name, contact.Title, contact.Phone, contact.Email);
            return dt;
        }

        private DataTable BuildRefundCustomerDeliveryAddressesTable(long customerId)
        {
            var dt = new DataTable();
            dt.Columns.Add("Delivery Address");
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var addr in _customerCtrl.GetDeliveryAddresses(customerId))
                dt.Rows.Add(addr.DeliveryAddress, addr.ContactPerson, addr.Phone, addr.Email);
            return dt;
        }

        private static void AddRefundFieldRow(DataTable dt, string field, string value)
        {
            dt.Rows.Add(field, value ?? "");
        }

        private void ShowUpdateRefundStatusDialog()
        {
            if (_refundGrid?.CurrentRow == null)
            {
                UITheme.ShowWarning("Please select a refund request first.");
                return;
            }
            if (!PermissionGuard.Ensure(PermissionModule.Refund, PermissionAction.Edit, this)) return;

            string requestCode = _refundGrid.CurrentRow.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode)) return;

            var refund = _refundCtrl.GetByCode(requestCode);
            if (refund == null)
            {
                UITheme.ShowWarning("Refund request not found.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "Update Refund Request Status";
                dlg.Size = new Size(440, 220);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var lblCurrent = new Label
                {
                    Text = DictionaryService.GetDisplayName(DictionaryService.Categories.RefundStatus, refund.Status),
                    AutoSize = true,
                    ForeColor = UITheme.TextDark
                };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.RefundStatus, refund.Status);

                UITheme.AddFormRow(layout, 0, "Current", lblCurrent);
                UITheme.AddFormRow(layout, 1, "New Status", cmbStatus);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.Refund, PermissionAction.Edit, dlg)) return;

                    int newStatus = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                    if (!DictionaryService.CanTransition(DictionaryService.Categories.RefundStatus, refund.Status, newStatus))
                    {
                        UITheme.ShowWarning("This status transition is not allowed.");
                        return;
                    }

                    long staffId = AppSession.CurrentUser?.StaffID ?? refund.StaffID;
                    if (!_refundCtrl.UpdateStatus(requestCode, newStatus, staffId))
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
                    LoadRefunds();
            }
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
                dlg.Size = new Size(580, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 8, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbCustomer = BuildCustomerCombo();
                var cmbSalesOrder = BuildSalesOrderCombo(0, 0);
                var lblSoTotal = new Label
                {
                    AutoSize = true,
                    ForeColor = UITheme.TextDark,
                    Text = "—"
                };
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
                UITheme.AddFormField(layout, 2, "Total Amount", lblSoTotal);
                UITheme.AddFormField(layout, 3, "Invoice Type *", cmbType);
                UITheme.AddFormField(layout, 4, "Amount / Note", pnlTypeDetail);
                UITheme.AddFormField(layout, 5, "Staff", lblStaff);
                UITheme.AddFormField(layout, 6, "Status", cmbStatus);
                UITheme.AddFormField(layout, 7, "Remark", txtRemark);

                void RefreshSoTotal()
                {
                    try
                    {
                        long soId = SalesOrderComboHelper.ResolveSalesOrderId(cmbSalesOrder, _salesOrderCtrl);
                        if (soId <= 0)
                        {
                            lblSoTotal.Text = "—";
                            return;
                        }
                        var so = _salesOrderCtrl.GetFullById(soId);
                        if (so == null)
                        {
                            lblSoTotal.Text = "—";
                            return;
                        }
                        if (so.TotalAmount > 0)
                            lblSoTotal.Text = $"{so.TotalAmount:N2} (HKD {so.TotalAmountBase:N2})";
                        else
                            lblSoTotal.Text = "—";
                    }
                    catch
                    {
                        lblSoTotal.Text = "—";
                    }
                }

                void RefreshTypeUi()
                {
                    bool deposit = cmbType.SelectedIndex == 0;
                    txtDepositAmount.Visible = deposit;
                    lblNormalHint.Visible = !deposit;
                }
                cmbType.SelectedIndexChanged += (s, e) => RefreshTypeUi();
                CustomerComboHelper.WireCustomerChanged(cmbCustomer, _customerCtrl, customerId =>
                {
                    BindSalesOrderCombo(cmbSalesOrder, customerId, 0);
                    lblSoTotal.Text = "—";
                });
                var soBinder = SalesOrderComboHelper.GetBinder(cmbSalesOrder);
                if (soBinder != null)
                    soBinder.SelectionCommitted += (s, e) => RefreshSoTotal();
                cmbSalesOrder.Leave += (s, e) => RefreshSoTotal();
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
                    long customerId = CustomerComboHelper.ResolveCustomerId(cmbCustomer, _customerCtrl);
                    long salesOrderId = SalesOrderComboHelper.ResolveSalesOrderId(cmbSalesOrder, _salesOrderCtrl);
                    if (customerId <= 0 || salesOrderId <= 0)
                    {
                        UITheme.ShowWarning("Please select or type a valid customer and sales order.");
                        return;
                    }
                    SalesOrder so;
                    try
                    {
                        so = _salesOrderCtrl.GetFullById(salesOrderId);
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowWarning("Unable to load sales order: " + ex.Message);
                        return;
                    }
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
                dlg.Size = new Size(580, 560);
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

                long initialInvoiceId = isNew ? 0 : existing.InvoiceID ?? 0;
                long initialReceiptId = isNew ? 0 : existing.ReceiptVoucherID ?? 0;
                var cmbInvoice = BuildInvoicePickerCombo(initialInvoiceId);
                var cmbReceipt = BuildReceiptPickerCombo(initialReceiptId, initialInvoiceId);
                var lblInvoiceTotal = new Label
                {
                    AutoSize = true,
                    ForeColor = UITheme.TextDark,
                    Text = "—"
                };
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
                UITheme.AddFormField(layout, row++, "Invoice Total", lblInvoiceTotal);
                UITheme.AddFormField(layout, row++, "Receipt Voucher", cmbReceipt);
                UITheme.AddFormField(layout, row++, "Staff", new Label { Text = staffLabel, AutoSize = true, ForeColor = UITheme.TextDark });
                UITheme.AddFormField(layout, row++, "Refund Amount *", txtAmount);

                FilteredComboBinder receiptBinder = GetComboBinder(cmbReceipt);
                Action refreshInvoiceContext = () =>
                {
                    long invoiceId = GetComboLongId(cmbInvoice);
                    decimal total = invoiceId > 0 ? _invoiceCtrl.GetInvoiceTotal(invoiceId) : 0m;
                    lblInvoiceTotal.Text = invoiceId > 0 ? $"{total:N2}" : "—";
                    if (isNew && invoiceId > 0 && total > 0)
                        txtAmount.Text = total.ToString("0.00");

                    if (receiptBinder != null)
                    {
                        long keepReceiptId = receiptBinder.GetSelectedId();
                        if (keepReceiptId <= 0 && initialInvoiceId == invoiceId)
                            keepReceiptId = initialReceiptId;
                        receiptBinder.SetSource(BuildReceiptPickerTable(invoiceId), keepReceiptId);
                    }
                };
                var invoiceBinder = GetComboBinder(cmbInvoice);
                if (invoiceBinder != null)
                    invoiceBinder.SelectionCommitted += (s, e) => refreshInvoiceContext();
                cmbInvoice.Leave += (s, e) => refreshInvoiceContext();
                if (initialInvoiceId > 0)
                    refreshInvoiceContext();
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
            var cmb = new ComboBox { Width = 320 };
            var binder = new FilteredComboBinder(cmb, "Invoice ID", "DisplayText");
            binder.SetSource(_invoiceCtrl.GetInvoicesForPicker() ?? EmptyInvoicePickerTable(), selectedInvoiceId);
            cmb.Tag = binder;
            return cmb;
        }

        private ComboBox BuildReceiptPickerCombo(long selectedReceiptId, long invoiceId = 0)
        {
            var cmb = new ComboBox { Width = 320 };
            var binder = new FilteredComboBinder(cmb, "Receipt Voucher ID", "DisplayText");
            binder.SetSource(BuildReceiptPickerTable(invoiceId), selectedReceiptId);
            cmb.Tag = binder;
            return cmb;
        }

        private DataTable BuildReceiptPickerTable(long invoiceId = 0)
        {
            DataTable dt = invoiceId > 0
                ? _receiptCtrl.GetReceiptVouchersForInvoicePicker(invoiceId)
                : _receiptCtrl.GetReceiptVouchersForPicker();

            DataTable withNone = dt?.Clone();
            if (withNone == null)
            {
                withNone = new DataTable();
                withNone.Columns.Add("Receipt Voucher ID", typeof(long));
                withNone.Columns.Add("DisplayText", typeof(string));
            }

            var none = withNone.NewRow();
            none["Receipt Voucher ID"] = 0L;
            if (withNone.Columns.Contains("Voucher Code"))
                none["Voucher Code"] = "";
            none["DisplayText"] = "(None)";
            withNone.Rows.Add(none);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                    withNone.ImportRow(row);
            }
            return withNone;
        }

        private static DataTable EmptyInvoicePickerTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Invoice ID", typeof(long));
            dt.Columns.Add("DisplayText", typeof(string));
            return dt;
        }

        private static FilteredComboBinder GetComboBinder(ComboBox cmb) => cmb?.Tag as FilteredComboBinder;

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

        private void BindSalesOrderCombo(ComboBox cmb, long customerId, long selectedSalesOrderId)
        {
            SalesOrderComboHelper.Rebind(cmb, _salesOrderCtrl, customerId, selectedSalesOrderId);
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
            if (cmb?.Tag is FilteredComboBinder binder)
            {
                long binderId = binder.GetSelectedId();
                if (binderId > 0) return binderId;
            }
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
