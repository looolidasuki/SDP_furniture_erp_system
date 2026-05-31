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
    public class SalesPanel : UserControl
    {
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly QuotationController _quotationCtrl = new QuotationController();

        private TabControl _tabs;

        public SalesPanel(string module = "Customers")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            if (module == "Quotations") _tabs.SelectedIndex = 1;
            else if (module == "Sales Orders") _tabs.SelectedIndex = 2;
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            _tabs.TabPages.Add(BuildCustomerTab());
            _tabs.TabPages.Add(BuildQuotationTab());
            _tabs.TabPages.Add(BuildSalesOrderTab());

            Controls.Add(_tabs);
        }

        private TabPage BuildCustomerTab()
        {
            var page = new TabPage("Customers");
            page.Controls.Add(BuildCrudPanel("Customer",
                () => _customerCtrl.GetAllCustomers(),
                ShowCreateCustomerDialog,
                row => ShowCustomerDetailDialog(Convert.ToInt64(row.Cells[0].Value))));
            return page;
        }

        private TabPage BuildQuotationTab()
        {
            var page = new TabPage("Quotations");
            page.Controls.Add(BuildCrudPanel("Quotation",
                () => _quotationCtrl.GetAllQuotations(),
                ShowCreateQuotationDialog,
                row => ShowGenericDetailDialog("Quotation Details", row)));
            return page;
        }

        private TabPage BuildSalesOrderTab()
        {
            var page = new TabPage("Sales Orders");
            page.Controls.Add(BuildCrudPanel("Sales Order",
                () => _salesOrderCtrl.GetAllSalesOrders(),
                ShowCreateSalesOrderDialog,
                row => ShowSalesOrderDetailDialog(Convert.ToInt64(row.Cells[0].Value))));
            return page;
        }

        private Panel BuildCrudPanel(string entity, Func<DataTable> loadData, Action onCreate, Action<DataGridViewRow> onRowOpen)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };

            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
            Button btnNew = UITheme.CreatePrimaryButton($"+ New {entity}");
            btnNew.Location = new Point(8, 8);
            btnNew.Click += (s, e) => onCreate();
            DataGridView grid = GridHelper.CreateStyledGrid();
            Button btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Width + 18, 8);
            Button btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 8);
            btnDetail.Click += (s, e) =>
            {
                if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a record first."); return; }
                ShowGenericDetailDialog($"{entity} Details", grid.CurrentRow);
            };
            TextBox txtSearch = new TextBox { Width = 180, Height = 28, Location = new Point(btnDetail.Right + 10, 10) };
            ComboBox cmbStatus = new ComboBox { Width = 120, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(txtSearch.Right + 10, 10) };
            cmbStatus.Items.AddRange(new object[] { "All Status", "0", "1", "2", "3", "4" });
            cmbStatus.SelectedIndex = 0;

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                onRowOpen?.Invoke(grid.Rows[e.RowIndex]);
            };
            btnRefresh.Click += (s, e) => {
                try { grid.DataSource = loadData(); GridHelper.StyleGrid(grid); } catch { }
            };

            Action applyFilter = () =>
            {
                if (!(grid.DataSource is DataTable dt)) return;
                string keyword = txtSearch.Text.Trim().Replace("'", "''");
                var conditions = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var textColumns = dt.Columns.Cast<DataColumn>()
                        .Where(c => c.DataType == typeof(string))
                        .Select(c => $"[{c.ColumnName}] LIKE '%{keyword}%'");
                    string textFilter = string.Join(" OR ", textColumns);
                    if (!string.IsNullOrWhiteSpace(textFilter)) conditions.Add("(" + textFilter + ")");
                }
                if (cmbStatus.SelectedIndex > 0 && dt.Columns.Contains("Status"))
                {
                    conditions.Add("[Status] = " + (cmbStatus.SelectedIndex - 1));
                }
                dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
            };
            txtSearch.TextChanged += (s, e) => applyFilter();
            cmbStatus.SelectedIndexChanged += (s, e) => applyFilter();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetail);
            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(cmbStatus);

            try { grid.DataSource = loadData(); GridHelper.StyleGrid(grid); } catch { }

            panel.Controls.Add(grid);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private void ShowCreateCustomerDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Customer";
                dlg.Size = new Size(480, 280);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox(); var txtAddr = new TextBox(); var txtTerm = new TextBox();
                UITheme.AddFormRow(layout, 0, "Customer Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Billing Address", txtAddr);
                UITheme.AddFormRow(layout, 2, "Payment Term", txtTerm);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Customer Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        _customerCtrl.Insert(new Customer { CustomerName = txtName.Text.Trim(), BillingAddress = txtAddr.Text.Trim(), PaymentTerm = txtTerm.Text.Trim() });
                        MessageBox.Show("Customer created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowGenericDetailDialog(string title, DataGridViewRow row)
        {
            DetailViewHelper.ShowKeyValueDetail(this, title, row);
        }

        private void ShowCustomerDetailDialog(long id)
        {
            var customer = _customerCtrl.GetById(id);
            if (customer == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Customer Details / Edit";
                dlg.Size = new Size(480, 300);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox { Text = customer.CustomerName ?? "" };
                var txtAddr = new TextBox { Text = customer.BillingAddress ?? "" };
                var txtTerm = new TextBox { Text = customer.PaymentTerm ?? "" };
                UITheme.AddFormRow(layout, 0, "Customer Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Billing Address", txtAddr);
                UITheme.AddFormRow(layout, 2, "Payment Term", txtTerm);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        UITheme.ShowWarning("Customer Name is required.");
                        return;
                    }
                    customer.CustomerName = txtName.Text.Trim();
                    customer.BillingAddress = txtAddr.Text.Trim();
                    customer.PaymentTerm = txtTerm.Text.Trim();
                    if (_customerCtrl.Update(customer))
                    {
                        UITheme.ShowSuccess("Customer updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) BuildUIRefresh();
            }
        }

        private void ShowSalesOrderDetailDialog(long id)
        {
            var so = _salesOrderCtrl.GetById(id);
            if (so == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Sales Order Details / Edit";
                dlg.Size = new Size(520, 360);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtAddress = new TextBox { Text = so.DeliveryAddress ?? "" };
                var txtDiscount = new TextBox { Text = so.Discount.ToString() };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new object[] { "0", "1", "2", "3", "4" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(so.Status, 4));
                var txtRemark = new TextBox { Text = so.Remark ?? "", Multiline = true, Height = 70 };

                UITheme.AddFormRow(layout, 0, "Order Code", new Label { Text = so.SalesOrderCode, AutoSize = true, ForeColor = UITheme.TextDark });
                UITheme.AddFormRow(layout, 1, "Delivery Address", txtAddress);
                UITheme.AddFormRow(layout, 2, "Discount", txtDiscount);
                UITheme.AddFormRow(layout, 3, "Status", cmbStatus);
                UITheme.AddFormRow(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!decimal.TryParse(txtDiscount.Text.Trim(), out decimal discount))
                    {
                        UITheme.ShowWarning("Discount must be a valid number.");
                        return;
                    }
                    so.DeliveryAddress = txtAddress.Text.Trim();
                    so.Discount = discount;
                    so.Status = cmbStatus.SelectedIndex;
                    so.Remark = txtRemark.Text.Trim();
                    if (_salesOrderCtrl.Update(so))
                    {
                        UITheme.ShowSuccess("Sales order updated.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) BuildUIRefresh();
            }
        }

        private void BuildUIRefresh()
        {
            Controls.Clear();
            BuildUI();
        }

        private void ShowCreateQuotationDialog()
        {
            using (var dlg = UITheme.BuildInputDialog("New Quotation",
                new[] { "Customer ID *", "Staff ID *", "Currency ID", "Status (0-3)", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = UITheme.GetDialogValues(dlg);
                        var q = new Quotation
                        {
                            QuotationCode = "QT-TEMP",
                            CustomerID = long.Parse(vals[0]),
                            StaffID = long.Parse(vals[1]),
                            CurrencyID = string.IsNullOrEmpty(vals[2]) ? 1 : long.Parse(vals[2]),
                            Status = string.IsNullOrEmpty(vals[3]) ? 0 : int.Parse(vals[3]),
                            Remark = vals[4],
                            SequenceNumber = 1
                        };
                        long id = _quotationCtrl.Insert(q);
                        _quotationCtrl.UpdateCodeAfterInsert(id);
                        MessageBox.Show($"Quotation QT-{id} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        private void ShowCreateSalesOrderDialog()
        {
            using (var dlg = UITheme.BuildInputDialog("New Sales Order",
                new[] { "Customer ID *", "Staff ID *", "Currency ID", "Delivery Address *", "Discount", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = UITheme.GetDialogValues(dlg);
                        var so = new SalesOrder
                        {
                            SalesOrderCode = "SO-TEMP",
                            CustomerID = long.Parse(vals[0]),
                            StaffID = long.Parse(vals[1]),
                            CurrencyCurrencyID = string.IsNullOrEmpty(vals[2]) ? 1 : long.Parse(vals[2]),
                            DeliveryAddress = vals[3],
                            Discount = string.IsNullOrEmpty(vals[4]) ? 0 : decimal.Parse(vals[4]),
                            Status = string.IsNullOrEmpty(vals[5]) ? 0 : int.Parse(vals[5]),
                            Remark = vals[6]
                        };
                        long id = _salesOrderCtrl.Insert(so);
                        _salesOrderCtrl.UpdateCodeAfterInsert(id);
                        MessageBox.Show($"Sales Order SO-{id} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }
    }
}
