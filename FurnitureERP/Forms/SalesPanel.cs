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
    public class SalesPanel : UserControl
    {
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly QuotationController _quotationCtrl = new QuotationController();
        private readonly SalesWorkflowService _salesWorkflow = new SalesWorkflowService();

        private TabControl _tabs;

        public SalesPanel(string module = "Customers")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            Tag = module;
            BuildUI();
            SelectModuleTab(module);
        }

        private void BuildUI()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            if (AppSession.CanView(PermissionModule.Customer))
                _tabs.TabPages.Add(BuildCustomerTab());
            if (AppSession.CanView(PermissionModule.Quotation))
                _tabs.TabPages.Add(BuildQuotationTab());
            if (AppSession.CanView(PermissionModule.SalesOrder))
                _tabs.TabPages.Add(BuildSalesOrderTab());

            Controls.Add(_tabs);

            if (AppSession.CanView(PermissionModule.Product))
            {
                var productBar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    BackColor = UITheme.Background,
                    Padding = new Padding(8, 6, 8, 4)
                };
                Button btnViewProducts = UITheme.CreateSecondaryButton("📦 View Products");
                btnViewProducts.Location = new Point(8, 6);
                btnViewProducts.Click += (s, e) => ProductionPanel.ShowProductsViewerDialog(this);
                productBar.Controls.Add(btnViewProducts);
                Controls.Add(productBar);
            }

            SelectModuleTab();
        }

        private void SelectModuleTab(string module = null)
        {
            if (_tabs == null || _tabs.TabPages.Count == 0) return;
            module = module ?? Tag?.ToString();
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                var text = _tabs.TabPages[i].Text;
                if (text.StartsWith("Quotation", StringComparison.OrdinalIgnoreCase) && module == "Quotations") { _tabs.SelectedIndex = i; return; }
                if (text.StartsWith("Sales Order", StringComparison.OrdinalIgnoreCase) && module == "Sales Orders") { _tabs.SelectedIndex = i; return; }
            }
        }

        private TabPage BuildCustomerTab()
        {
            var page = new TabPage("Customers");
            page.Controls.Add(BuildCrudPanel("Customer", PermissionModule.Customer,
                () => _customerCtrl.GetAllCustomers(),
                ShowCreateCustomerDialog,
                row => OpenCustomerRow(row)));
            return page;
        }

        private TabPage BuildQuotationTab()
        {
            var page = new TabPage("Quotations");
            page.Controls.Add(BuildCrudPanel("Quotation", PermissionModule.Quotation,
                () => DictionaryUIHelper.LoadWithStatusLabels(() => _quotationCtrl.GetAllQuotations(), "Status", DictionaryService.Categories.Quotation),
                ShowCreateQuotationDialog,
                row => OpenQuotationRow(row),
                DictionaryService.Categories.Quotation,
                grid =>
                {
                    var btnConvert = UITheme.CreateSecondaryButton("Convert to SO");
                    btnConvert.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a quotation first."); return; }
                        ConvertQuotationToSalesOrder(Convert.ToInt64(grid.CurrentRow.Cells[0].Value));
                    };
                    return btnConvert;
                }));
            return page;
        }

        private TabPage BuildSalesOrderTab()
        {
            var page = new TabPage("Sales Orders");
            page.Controls.Add(BuildCrudPanel("Sales Order", PermissionModule.SalesOrder,
                () => DictionaryUIHelper.LoadWithStatusLabels(() => _salesOrderCtrl.GetAllSalesOrders(), "Status", DictionaryService.Categories.SalesOrder),
                ShowCreateSalesOrderDialog,
                row => OpenSalesOrderRow(row),
                DictionaryService.Categories.SalesOrder,
                grid =>
                {
                    var btnProduction = UITheme.CreateSecondaryButton("Create Production");
                    btnProduction.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a sales order first."); return; }
                        CreateProductionFromSalesOrder(Convert.ToInt64(grid.CurrentRow.Cells[0].Value));
                    };
                    return btnProduction;
                }));
            return page;
        }

        private void OpenQuotationRow(DataGridViewRow row)
        {
            long id = Convert.ToInt64(row.Cells[0].Value);
            var quotation = _quotationCtrl.GetById(id);
            if (quotation == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Quotation Details";
                dlg.Size = new Size(640, 480);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var info = new Label
                {
                    Text = quotation.QuotationCode + "  |  Status: " + DictionaryService.GetDisplayName(DictionaryService.Categories.Quotation, quotation.Status),
                    Dock = DockStyle.Top,
                    Height = 32,
                    Padding = new Padding(12, 8, 0, 0),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.Dock = DockStyle.Fill;
                try
                {
                    lineGrid.DataSource = _quotationCtrl.GetProductLines(id);
                    GridHelper.StyleGrid(lineGrid);
                }
                catch { }

                var btnConvert = UITheme.CreatePrimaryButton("Convert to Sales Order");
                btnConvert.Click += (s, e) =>
                {
                    ConvertQuotationToSalesOrder(id);
                    dlg.Close();
                };
                PermissionGuard.ApplyEditButton(btnConvert, PermissionModule.Quotation);

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnConvert);
                btnPanel.Controls.Add(btnClose);

                dlg.Controls.Add(lineGrid);
                dlg.Controls.Add(btnPanel);
                dlg.Controls.Add(info);
                dlg.ShowDialog(this);
                BuildUIRefresh();
            }
        }

        private void ConvertQuotationToSalesOrder(long quotationId)
        {
            if (!PermissionGuard.Ensure(PermissionModule.Quotation, PermissionAction.Edit, this)) return;
            long staffId = AppSession.CurrentUser?.StaffID ?? 1;
            var result = _salesWorkflow.ConvertQuotationToSalesOrder(quotationId, staffId);
            if (result.Success) UITheme.ShowSuccess(result.Message);
            else UITheme.ShowWarning(result.Message);
            BuildUIRefresh();
        }

        private void CreateProductionFromSalesOrder(long salesOrderId)
        {
            if (!PermissionGuard.Ensure(PermissionModule.ProductionOrder, PermissionAction.Create, this)) return;
            long staffId = AppSession.CurrentUser?.StaffID ?? 1;
            var result = _salesWorkflow.CreateProductionFromSalesOrder(salesOrderId, staffId, DateTime.Today.AddDays(14));
            if (result.Success) UITheme.ShowSuccess(result.Message);
            else UITheme.ShowWarning(result.Message);
        }

        private void OpenCustomerRow(DataGridViewRow row)
        {
            if (AppSession.CanEdit(PermissionModule.Customer))
                ShowCustomerDetailDialog(Convert.ToInt64(row.Cells[0].Value));
            else
                ShowGenericDetailDialog("Customer Details", row);
        }

        private void OpenSalesOrderRow(DataGridViewRow row)
        {
            if (AppSession.CanEdit(PermissionModule.SalesOrder))
                ShowSalesOrderDetailDialog(Convert.ToInt64(row.Cells[0].Value));
            else
                ShowGenericDetailDialog("Sales Order Details", row);
        }

        private Panel BuildCrudPanel(string entity, string permissionModule, Func<DataTable> loadData, Action onCreate, Action<DataGridViewRow> onRowOpen, string statusCategory = null, Func<DataGridView, Button> extraButtonFactory = null)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };

            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
            Button btnNew = UITheme.CreatePrimaryButton($"+ New {entity}");
            btnNew.Location = new Point(8, 8);
            btnNew.Click += (s, e) => { if (PermissionGuard.Ensure(permissionModule, PermissionAction.Create, this)) onCreate(); };
            PermissionGuard.ApplyCreateButton(btnNew, permissionModule);
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
            ComboBox cmbStatus = new ComboBox { Width = 140, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(txtSearch.Right + 10, 10) };
            if (!string.IsNullOrEmpty(statusCategory))
                DictionaryUIHelper.BindStatusFilter(cmbStatus, statusCategory);
            else
            {
                cmbStatus.Items.Add("All Status");
                cmbStatus.SelectedIndex = 0;
            }

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
                int? statusCode = DictionaryUIHelper.GetFilterStatusCode(cmbStatus);
                if (statusCode.HasValue && dt.Columns.Contains("Status"))
                    conditions.Add("[Status] = " + statusCode.Value);
                dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
            };
            txtSearch.TextChanged += (s, e) => applyFilter();
            cmbStatus.SelectedIndexChanged += (s, e) => applyFilter();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetail);
            if (extraButtonFactory != null)
            {
                var extraBtn = extraButtonFactory(grid);
                extraBtn.Location = new Point(btnDetail.Right + 10, 8);
                toolbar.Controls.Add(extraBtn);
                txtSearch.Location = new Point(extraBtn.Right + 10, 10);
                cmbStatus.Location = new Point(txtSearch.Right + 10, 10);
            }
            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(cmbStatus);

            try { grid.DataSource = loadData(); GridHelper.StyleGrid(grid); } catch { }

            panel.Controls.Add(grid);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private void ShowCreateCustomerDialog()
        {
            if (!PermissionGuard.Ensure(PermissionModule.Customer, PermissionAction.Create, this)) return;
            ShowCustomerFormDialog(null);
        }

        private void ShowGenericDetailDialog(string title, DataGridViewRow row)
        {
            DetailViewHelper.ShowKeyValueDetail(this, title, row);
        }

        private void ShowCustomerDetailDialog(long id)
        {
            var customer = _customerCtrl.GetById(id);
            if (customer == null) return;
            ShowCustomerFormDialog(customer);
        }

        private void ShowCustomerFormDialog(Customer existing)
        {
            bool isEdit = existing != null;
            var originalContactIds = isEdit
                ? _customerCtrl.GetContactPersons(existing.CustomerID).Select(c => c.ContactPersonID).ToList()
                : new List<long>();
            var originalAddressIds = isEdit
                ? _customerCtrl.GetDeliveryAddresses(existing.CustomerID).Select(a => a.AddressID).ToList()
                : new List<long>();

            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Customer Details / Edit" : "New Customer";
                dlg.Size = new Size(780, 560);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var tabs = new TabControl { Dock = DockStyle.Fill };

                var generalPage = new TabPage("General");
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

                var txtName = new TextBox { Text = existing?.CustomerName ?? "" };
                var txtAddr = new TextBox { Text = existing?.BillingAddress ?? "" };
                var txtTerm = new TextBox { Text = existing?.PaymentTerm ?? "" };
                UITheme.AddFormRow(layout, 0, "Customer Name *", txtName);
                UITheme.AddFormRow(layout, 1, "Billing Address", txtAddr);
                UITheme.AddFormRow(layout, 2, "Payment Term", txtTerm);
                generalPage.Controls.Add(layout);

                var contactGrid = CreateCustomerChildGrid(
                    new[] { ("ID", "ID"), ("Contact Person", "ContactPerson"), ("Title", "Title"), ("Phone", "Phone"), ("Email", "Email") },
                    "ID");
                if (isEdit)
                    LoadContactPersonGrid(contactGrid, _customerCtrl.GetContactPersons(existing.CustomerID));
                var contactPage = new TabPage("Contact Persons");
                contactPage.Controls.Add(WrapEditableGrid(contactGrid, "Add or edit contact persons for this customer."));

                var deliveryGrid = CreateCustomerChildGrid(
                    new[] { ("ID", "ID"), ("Delivery Address", "DeliveryAddress"), ("Contact Person", "ContactPerson"), ("Phone", "Phone"), ("Email", "Email") },
                    "ID");
                if (isEdit)
                    LoadDeliveryAddressGrid(deliveryGrid, _customerCtrl.GetDeliveryAddresses(existing.CustomerID));
                var deliveryPage = new TabPage("Delivery Addresses");
                deliveryPage.Controls.Add(WrapEditableGrid(deliveryGrid, "Add or edit delivery addresses for this customer."));

                tabs.TabPages.Add(generalPage);
                tabs.TabPages.Add(contactPage);
                tabs.TabPages.Add(deliveryPage);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Save");
                PermissionGuard.ApplyEditButton(btnSave, PermissionModule.Customer);
                if (!isEdit) PermissionGuard.ApplyCreateButton(btnSave, PermissionModule.Customer);
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isEdit ? PermissionAction.Edit : PermissionAction.Create;
                    if (!PermissionGuard.Ensure(PermissionModule.Customer, action, dlg)) return;
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        UITheme.ShowWarning("Customer Name is required.");
                        return;
                    }
                    try
                    {
                        long customerId;
                        if (isEdit)
                        {
                            existing.CustomerName = txtName.Text.Trim();
                            existing.BillingAddress = txtAddr.Text.Trim();
                            existing.PaymentTerm = txtTerm.Text.Trim();
                            if (!_customerCtrl.Update(existing))
                            {
                                UITheme.ShowWarning("Failed to update customer.");
                                return;
                            }
                            customerId = existing.CustomerID;
                        }
                        else
                        {
                            customerId = _customerCtrl.Insert(new Customer
                            {
                                CustomerName = txtName.Text.Trim(),
                                BillingAddress = txtAddr.Text.Trim(),
                                PaymentTerm = txtTerm.Text.Trim()
                            });
                        }

                        _customerCtrl.SyncContactPersons(customerId, ReadContactPersonsFromGrid(contactGrid), originalContactIds);
                        _customerCtrl.SyncDeliveryAddresses(customerId, ReadDeliveryAddressesFromGrid(deliveryGrid), originalAddressIds);

                        UITheme.ShowSuccess(isEdit ? "Customer updated." : "Customer created successfully.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(tabs);
                dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    BuildUIRefresh();
            }
        }

        private static Panel WrapEditableGrid(DataGridView grid, string hint)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var lbl = new Label
            {
                Text = hint,
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f)
            };
            panel.Controls.Add(grid);
            panel.Controls.Add(lbl);
            return panel;
        }

        private static DataGridView CreateCustomerChildGrid(IEnumerable<(string Header, string Name)> columns, string hiddenColumnName)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            foreach (var col in columns)
            {
                grid.Columns.Add(col.Name, col.Header);
            }
            grid.Columns[hiddenColumnName].Visible = false;
            GridHelper.ApplyStyle(grid);
            return grid;
        }

        private static void LoadContactPersonGrid(DataGridView grid, IEnumerable<ContactPerson> contacts)
        {
            grid.Rows.Clear();
            foreach (var cp in contacts)
            {
                grid.Rows.Add(cp.ContactPersonID, cp.Name ?? "", cp.Title ?? "", cp.Phone ?? "", cp.Email ?? "");
            }
        }

        private static void LoadDeliveryAddressGrid(DataGridView grid, IEnumerable<CustomerDeliveryAddress> addresses)
        {
            grid.Rows.Clear();
            foreach (var addr in addresses)
            {
                grid.Rows.Add(addr.AddressID, addr.DeliveryAddress ?? "", addr.ContactPerson ?? "", addr.Phone ?? "", addr.Email ?? "");
            }
        }

        private static List<ContactPerson> ReadContactPersonsFromGrid(DataGridView grid)
        {
            var list = new List<ContactPerson>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string name = row.Cells["ContactPerson"].Value?.ToString();
                string title = row.Cells["Title"].Value?.ToString();
                string phone = row.Cells["Phone"].Value?.ToString();
                string email = row.Cells["Email"].Value?.ToString();
                long id = 0;
                long.TryParse(row.Cells["ID"].Value?.ToString(), out id);
                if (id == 0 && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                    continue;
                list.Add(new ContactPerson
                {
                    ContactPersonID = id,
                    Name = name?.Trim(),
                    Title = title?.Trim(),
                    Phone = phone?.Trim(),
                    Email = email?.Trim()
                });
            }
            return list;
        }

        private static List<CustomerDeliveryAddress> ReadDeliveryAddressesFromGrid(DataGridView grid)
        {
            var list = new List<CustomerDeliveryAddress>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string address = row.Cells["DeliveryAddress"].Value?.ToString();
                string contact = row.Cells["ContactPerson"].Value?.ToString();
                string phone = row.Cells["Phone"].Value?.ToString();
                string email = row.Cells["Email"].Value?.ToString();
                long id = 0;
                long.TryParse(row.Cells["ID"].Value?.ToString(), out id);
                if (id == 0 && string.IsNullOrWhiteSpace(address) && string.IsNullOrWhiteSpace(contact))
                    continue;
                list.Add(new CustomerDeliveryAddress
                {
                    AddressID = id,
                    DeliveryAddress = address?.Trim(),
                    ContactPerson = contact?.Trim(),
                    Phone = phone?.Trim(),
                    Email = email?.Trim()
                });
            }
            return list;
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
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.SalesOrder, so.Status);
                var txtRemark = new TextBox { Text = so.Remark ?? "", Multiline = true, Height = 70 };

                UITheme.AddFormRow(layout, 0, "Order Code", new Label { Text = so.SalesOrderCode, AutoSize = true, ForeColor = UITheme.TextDark });
                UITheme.AddFormRow(layout, 1, "Delivery Address", txtAddress);
                UITheme.AddFormRow(layout, 2, "Discount", txtDiscount);
                UITheme.AddFormRow(layout, 3, "Status", cmbStatus);
                UITheme.AddFormRow(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Update");
                PermissionGuard.ApplyEditButton(btnSave, PermissionModule.SalesOrder);
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!PermissionGuard.Ensure(PermissionModule.SalesOrder, PermissionAction.Edit, dlg)) return;
                    if (!decimal.TryParse(txtDiscount.Text.Trim(), out decimal discount))
                    {
                        UITheme.ShowWarning("Discount must be a valid number.");
                        return;
                    }
                    so.DeliveryAddress = txtAddress.Text.Trim();
                    so.Discount = discount;
                    int newStatus = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                    var validateResult = _salesWorkflow.ValidateSalesOrderStatus(so.SalesOrderID, newStatus);
                    if (!validateResult.Success)
                    {
                        UITheme.ShowWarning(validateResult.Message);
                        return;
                    }
                    so.Status = newStatus;
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
            if (!PermissionGuard.Ensure(PermissionModule.Quotation, PermissionAction.Create, this)) return;
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
            if (!PermissionGuard.Ensure(PermissionModule.SalesOrder, PermissionAction.Create, this)) return;
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
