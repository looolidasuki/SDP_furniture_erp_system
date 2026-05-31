using System;
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
            btnNewInvoice.Click += (s, e) => ShowCreateInvoiceDialog();
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
            Button btnPrintInvoice = UITheme.CreateSecondaryButton("Print PDF");
            btnPrintInvoice.Location = new Point(btnDetailInvoice.Right + 10, 8);
            btnPrintInvoice.Click += (s, e) => PrintSelectedInvoice();
            _invoiceSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnPrintInvoice.Right + 10, 10) };
            _invoiceSearchBox.TextChanged += (s, e) => LoadInvoices();
            _invoiceStatusFilter = new ComboBox { Width = 120, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_invoiceSearchBox.Right + 10, 10) };
            _invoiceStatusFilter.Items.AddRange(new object[] { "All Status", "0", "1", "2", "3", "4" });
            _invoiceStatusFilter.SelectedIndex = 0;
            _invoiceStatusFilter.SelectedIndexChanged += (s, e) => LoadInvoices();
            invoiceToolbar.Controls.Add(btnNewInvoice);
            invoiceToolbar.Controls.Add(btnRefreshInvoice);
            invoiceToolbar.Controls.Add(btnDetailInvoice);
            invoiceToolbar.Controls.Add(btnPrintInvoice);
            invoiceToolbar.Controls.Add(_invoiceSearchBox);
            invoiceToolbar.Controls.Add(_invoiceStatusFilter);

            _invoiceGrid = GridHelper.CreateStyledGrid();
            _invoiceGrid.CellDoubleClick += InvoiceGrid_CellDoubleClick;

            Panel invoicePanel = new Panel { Dock = DockStyle.Fill };
            invoicePanel.Controls.Add(_invoiceGrid);
            invoicePanel.Controls.Add(invoiceToolbar);
            invoiceTab.Controls.Add(invoicePanel);

            // Refunds Tab
            TabPage refundTab = new TabPage("💰 Refund Requests");
            refundTab.BackColor = UITheme.Background;

            Panel refundToolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            Button btnNewRefund = UITheme.CreatePrimaryButton("+ New Refund");
            btnNewRefund.Location = new Point(0, 8);
            btnNewRefund.Click += (s, e) => ShowCreateRefundDialog();
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
            Button btnPrintRefund = UITheme.CreateSecondaryButton("Print PDF");
            btnPrintRefund.Location = new Point(btnDetailRefund.Right + 10, 8);
            btnPrintRefund.Click += (s, e) =>
            {
                if (_refundGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a refund request first."); return; }
                PrintRefundRow(_refundGrid.CurrentRow);
            };
            _refundSearchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnPrintRefund.Right + 10, 10) };
            _refundSearchBox.TextChanged += (s, e) => LoadRefunds();
            _refundStatusFilter = new ComboBox { Width = 120, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(_refundSearchBox.Right + 10, 10) };
            _refundStatusFilter.Items.AddRange(new object[] { "All Status", "0", "1", "2", "3", "4" });
            _refundStatusFilter.SelectedIndex = 0;
            _refundStatusFilter.SelectedIndexChanged += (s, e) => LoadRefunds();
            refundToolbar.Controls.Add(btnNewRefund);
            refundToolbar.Controls.Add(btnRefreshRefund);
            refundToolbar.Controls.Add(btnDetailRefund);
            refundToolbar.Controls.Add(btnPrintRefund);
            refundToolbar.Controls.Add(_refundSearchBox);
            refundToolbar.Controls.Add(_refundStatusFilter);

            _refundGrid = GridHelper.CreateStyledGrid();
            _refundGrid.CellDoubleClick += RefundGrid_CellDoubleClick;

            Panel refundPanel = new Panel { Dock = DockStyle.Fill };
            refundPanel.Controls.Add(_refundGrid);
            refundPanel.Controls.Add(refundToolbar);
            refundTab.Controls.Add(refundPanel);

            _tabControl.TabPages.Add(invoiceTab);
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
                _invoiceGrid.DataSource = _invoiceCtrl.GetAllInvoices();
                ApplyTableFilter(_invoiceGrid, _invoiceSearchBox?.Text, _invoiceStatusFilter?.SelectedIndex ?? 0);
                GridHelper.StyleGrid(_invoiceGrid);
            }
            catch { }
        }

        private void LoadRefunds()
        {
            try
            {
                _refundGrid.DataSource = _refundCtrl.GetAllRefundRequests();
                ApplyTableFilter(_refundGrid, _refundSearchBox?.Text, _refundStatusFilter?.SelectedIndex ?? 0);
                GridHelper.StyleGrid(_refundGrid);
            }
            catch { }
        }

        private void RefundGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _refundGrid.Rows[e.RowIndex];
            string requestCode = row.Cells["Request Code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(requestCode)) return;
            var refund = _refundCtrl.GetByCode(requestCode);
            if (refund == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Refund Request Details / Edit";
                dlg.Size = new Size(620, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtInvoice = new TextBox { Text = refund.InvoiceID?.ToString() ?? "" };
                var txtStaff = new TextBox { Text = refund.StaffID.ToString() };
                var txtAmount = new TextBox { Text = refund.RefundAmount.ToString() };
                var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbMethod.Items.AddRange(new[] { "Bank Transfer", "Credit Card", "Cash", "Store Credit" });
                cmbMethod.SelectedIndex = Math.Max(0, Math.Min(refund.RefundMethod, 3));
                var txtReason = new TextBox { Text = refund.RefundReason ?? "" };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0", "1", "2", "3", "4" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(refund.Status, 4));
                var txtRemark = new TextBox { Text = refund.Remark ?? "", Multiline = true, Height = 70 };

                UITheme.AddFormField(layout, 0, "Request Code", new Label { Text = refund.RefundRequestCode, AutoSize = true });
                UITheme.AddFormField(layout, 1, "Invoice ID", txtInvoice);
                UITheme.AddFormField(layout, 2, "Staff ID *", txtStaff);
                UITheme.AddFormField(layout, 3, "Refund Amount *", txtAmount);
                UITheme.AddFormField(layout, 4, "Refund Method", cmbMethod);
                UITheme.AddFormField(layout, 5, "Reason *", txtReason);
                UITheme.AddFormField(layout, 6, "Status", cmbStatus);
                UITheme.AddFormField(layout, 7, "Remark", txtRemark);

                var btnUpdate = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, args) => dlg.Close();
                btnUpdate.Click += (s, args) =>
                {
                    if (!long.TryParse(txtStaff.Text.Trim(), out long staffId) ||
                        !decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) ||
                        string.IsNullOrWhiteSpace(txtReason.Text))
                    {
                        UITheme.ShowWarning("Valid Staff ID, Amount and Reason are required.");
                        return;
                    }

                    refund.StaffID = staffId;
                    refund.InvoiceID = long.TryParse(txtInvoice.Text.Trim(), out long invoiceId) ? (long?)invoiceId : null;
                    refund.RefundAmount = amount;
                    refund.RefundMethod = cmbMethod.SelectedIndex;
                    refund.RefundReason = txtReason.Text.Trim();
                    refund.Status = cmbStatus.SelectedIndex;
                    refund.Remark = txtRemark.Text.Trim();

                    if (_refundCtrl.Update(refund))
                    {
                        UITheme.ShowSuccess("Refund request updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadRefunds();
                    }
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnUpdate);
                btnPanel.Controls.Add(btnClose);

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
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
            ShowInvoiceDetails(invoiceId);
        }

        private void ShowInvoiceDetails(long invoiceId)
        {
            DataTable lines = null;
            try { lines = _invoiceCtrl.GetInvoiceLines(invoiceId); } catch { }
            var fields = DetailViewHelper.RowToFieldValueTable(null);
            fields.Rows.Add("Invoice ID", invoiceId);
            DetailViewHelper.ShowDetail(this, $"Invoice Details — ID: {invoiceId}", fields, lines, $"Invoice_{invoiceId}");
        }

        private void ShowInvoiceTableDetailFromRow(DataGridViewRow row)
        {
            if (row?.Cells[0].Value == null) return;
            long invoiceId = Convert.ToInt64(row.Cells[0].Value);
            DataTable lines = null;
            try { lines = _invoiceCtrl.GetInvoiceLines(invoiceId); } catch { }
            DetailViewHelper.ShowDetail(this, $"Invoice Detail — ID: {invoiceId}",
                DetailViewHelper.RowToFieldValueTable(row), lines, $"Invoice_{invoiceId}");
        }

        private void ShowRefundTableDetailFromRow(DataGridViewRow row)
        {
            DetailViewHelper.ShowKeyValueDetail(this, "Refund Request Detail", row, null);
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
            try { lines = _invoiceCtrl.GetInvoiceLines(invoiceId); } catch { }
            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    $"Invoice — ID: {invoiceId}",
                    DetailViewHelper.RowToFieldValueTable(row),
                    lines,
                    $"Invoice_{invoiceId}");
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void PrintRefundRow(DataGridViewRow row)
        {
            try
            {
                var data = DetailViewHelper.FromFieldValueTable(
                    "Refund Request",
                    DetailViewHelper.RowToFieldValueTable(row),
                    null,
                    "RefundRequest");
                if (PdfExportHelper.ExportToPdf(data, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex) { UITheme.ShowError("Failed to export PDF: " + ex.Message); }
        }

        private void ShowCreateInvoiceDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Invoice";
                dlg.Size = new Size(480, 400);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCustomerId = new TextBox();
                var txtSalesOrderId = new TextBox();
                var txtStaffId = new TextBox();
                var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbType.Items.AddRange(new[] { "1 - Standard", "2 - Proforma", "3 - Credit Note" });
                cmbType.SelectedIndex = 0;
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0 - Draft", "1 - Sent", "2 - Paid", "3 - Overdue", "4 - Cancelled" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox();

                UITheme.AddFormField(layout, 0, "Customer ID *", txtCustomerId);
                UITheme.AddFormField(layout, 1, "Sales Order ID *", txtSalesOrderId);
                UITheme.AddFormField(layout, 2, "Staff ID *", txtStaffId);
                UITheme.AddFormField(layout, 3, "Invoice Type", cmbType);
                UITheme.AddFormField(layout, 4, "Status", cmbStatus);
                UITheme.AddFormField(layout, 5, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCustomerId.Text) ||
                        string.IsNullOrWhiteSpace(txtSalesOrderId.Text) ||
                        string.IsNullOrWhiteSpace(txtStaffId.Text))
                    {
                        MessageBox.Show("Customer ID, Sales Order ID and Staff ID are required.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    try
                    {
                        var inv = new Invoice
                        {
                            InvoiceCode = "INV-TEMP",
                            CustomerID = long.Parse(txtCustomerId.Text.Trim()),
                            SalesOrderID = long.Parse(txtSalesOrderId.Text.Trim()),
                            StaffID = long.Parse(txtStaffId.Text.Trim()),
                            InvoiceType = cmbType.SelectedIndex + 1,
                            Status = cmbStatus.SelectedIndex,
                            Remark = txtRemark.Text.Trim()
                        };
                        long id = _invoiceCtrl.Insert(inv);
                        _invoiceCtrl.UpdateCodeAfterInsert(id);
                        MessageBox.Show($"Invoice INV-{id} created successfully.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadInvoices();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom, Height = 50,
                    FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowCreateRefundDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Refund Request";
                dlg.Size = new Size(480, 420);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtInvoiceId = new TextBox();
                var txtStaffId = new TextBox();
                var txtAmount = new TextBox();
                var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbMethod.Items.AddRange(new[] { "Bank Transfer", "Credit Card", "Cash", "Store Credit" });
                cmbMethod.SelectedIndex = 0;
                var txtReason = new TextBox();
                var txtRemark = new TextBox();

                UITheme.AddFormField(layout, 0, "Invoice ID *", txtInvoiceId);
                UITheme.AddFormField(layout, 1, "Staff ID *", txtStaffId);
                UITheme.AddFormField(layout, 2, "Refund Amount *", txtAmount);
                UITheme.AddFormField(layout, 3, "Refund Method *", cmbMethod);
                UITheme.AddFormField(layout, 4, "Reason *", txtReason);
                UITheme.AddFormField(layout, 5, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtInvoiceId.Text) ||
                        string.IsNullOrWhiteSpace(txtStaffId.Text) ||
                        string.IsNullOrWhiteSpace(txtAmount.Text) ||
                        string.IsNullOrWhiteSpace(txtReason.Text))
                    {
                        MessageBox.Show("Invoice ID, Staff ID, Amount and Reason are required.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    try
                    {
                        var rr = new RefundRequest
                        {
                            RefundRequestCode = "RF-TEMP",
                            InvoiceID = long.Parse(txtInvoiceId.Text.Trim()),
                            StaffID = long.Parse(txtStaffId.Text.Trim()),
                            RefundAmount = decimal.Parse(txtAmount.Text.Trim()),
                            RefundMethod = cmbMethod.SelectedIndex,
                            RefundReason = txtReason.Text.Trim(),
                            Remark = txtRemark.Text.Trim(),
                            Status = 0
                        };
                        _refundCtrl.CreateRefundRequest(rr);
                        MessageBox.Show("Refund Request created successfully.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadRefunds();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom, Height = 50,
                    FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }
    }
}
