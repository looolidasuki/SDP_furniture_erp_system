using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FurnitureERP.Helpers;
using Sales_user.Controllers;
using Sales_user.Models;

namespace FurnitureERP
{
    public partial class MainForm : Form
    {
        // Controllers
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly QuotationController _quotationCtrl = new QuotationController();
        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly ProductController _productCtrl = new ProductController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly PurchaseOrderController _purchaseOrderCtrl = new PurchaseOrderController();
        private readonly GoodsReceivedNoteController _grnCtrl = new GoodsReceivedNoteController();
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly SupplierController _supplierCtrl = new SupplierController();
        private readonly StaffController _staffCtrl = new StaffController();
        private readonly RefundRequestController _refundCtrl = new RefundRequestController();
        private readonly SystemDictionaryController _dictCtrl = new SystemDictionaryController();

        public Staff CurrentUser { get; set; }

        // Layout controls
        private Panel _sidebar;
        private Panel _contentPanel;
        private Panel _headerPanel;
        private string _currentModule = "Dashboard";
        private Button _activeNavButton;

        public MainForm()
        {
            InitializeComponent();
            BuildLayout();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1200, 750);
            Text = "Premium Living Furniture — ERP System";
            WindowState = FormWindowState.Maximized;
            BackColor = Theme.Background;
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }

        private void BuildLayout()
        {
            SuspendLayout();

            // Sidebar
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Theme.NavDark,
                Padding = new Padding(0)
            };
            BuildSidebar();

            // Header
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };
            _headerPanel.Paint += HeaderPanel_Paint;
            BuildHeader();

            // Content
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(20)
            };

            Controls.Add(_contentPanel);
            Controls.Add(_headerPanel);
            Controls.Add(_sidebar);

            ResumeLayout();
            LoadModule("Dashboard");
        }

        private void BuildSidebar()
        {
            // Logo area
            Panel logoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.NavDarkest
            };
            Label logoLabel = new Label
            {
                Text = "PREMIUM LIVING",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label subLabel = new Label
            {
                Text = "ERP System",
                ForeColor = Color.FromArgb(160, 190, 230),
                Font = new Font("Segoe UI", 8),
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoPanel.Controls.Add(logoLabel);
            logoPanel.Controls.Add(subLabel);
            _sidebar.Controls.Add(logoPanel);

            // Nav items
            var navItems = new[]
            {
                ("🏠", "Dashboard"),
                ("👥", "Customers"),
                ("📋", "Quotations"),
                ("🛒", "Sales Orders"),
                ("🏭", "Production"),
                ("📦", "Raw Materials"),
                ("🛍️", "Purchase Orders"),
                ("📬", "Goods Received"),
                ("🏪", "Warehouse"),
                ("🚚", "Delivery Notes"),
                ("🧾", "Invoices"),
                ("💰", "Refunds"),
                ("🏢", "Suppliers"),
                ("👤", "Staff"),
                ("⚙️", "System Admin")
            };

            Panel navContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.NavDark
            };

            int yPos = 8;
            foreach (var (icon, name) in navItems)
            {
                Button btn = CreateNavButton(icon, name);
                btn.Location = new Point(0, yPos);
                btn.Width = 220;
                navContainer.Controls.Add(btn);
                yPos += 44;
            }
            _sidebar.Controls.Add(navContainer);

            // Logout button
            Button logoutBtn = new Button
            {
                Text = "⏻  Logout",
                Dock = DockStyle.Bottom,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(255, 120, 120),
                BackColor = Color.FromArgb(60, 20, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            logoutBtn.FlatAppearance.BorderSize = 0;
            logoutBtn.Click += (s, e) =>
            {
                if (MessageBox.Show("Logout from the system?", "Confirm Logout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var login = new LoginForm();
                    login.Show();
                    Close();
                }
            };
            _sidebar.Controls.Add(logoutBtn);
        }

        private Button CreateNavButton(string icon, string name)
        {
            Button btn = new Button
            {
                Text = $"  {icon}  {name}",
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 210, 255),
                BackColor = Theme.NavDark,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = name
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.NavHover;
            btn.Click += NavButton_Click;
            return btn;
        }

        private void BuildHeader()
        {
            // User info label (right side)
            Label userLabel = new Label
            {
                AutoSize = true,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 9),
                Location = new Point(0, 0),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            userLabel.Name = "lblUser";
            _headerPanel.Controls.Add(userLabel);

            // Module title
            Label moduleTitle = new Label
            {
                Name = "lblModuleTitle",
                Text = "Dashboard",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Theme.Primary,
                AutoSize = true,
                Location = new Point(20, 17)
            };
            _headerPanel.Controls.Add(moduleTitle);

            _headerPanel.Resize += (s, e) =>
            {
                userLabel.Location = new Point(_headerPanel.Width - userLabel.Width - 20,
                    (_headerPanel.Height - userLabel.Height) / 2);
            };
        }

        private void HeaderPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 225, 235), 1),
                0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        }

        private void NavButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            string module = btn.Tag?.ToString() ?? btn.Text.Trim();

            // Update active state
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Theme.NavDark;
                _activeNavButton.ForeColor = Color.FromArgb(180, 210, 255);
                _activeNavButton.Font = new Font("Segoe UI", 9);
            }
            btn.BackColor = Theme.NavActive;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            _activeNavButton = btn;

            LoadModule(module);
        }

        private void LoadModule(string module)
        {
            _currentModule = module;

            // Update header title
            var titleLabel = _headerPanel.Controls["lblModuleTitle"] as Label;
            if (titleLabel != null) titleLabel.Text = module;

            _contentPanel.Controls.Clear();
            _contentPanel.SuspendLayout();

            try
            {
                switch (module)
                {
                    case "Dashboard": BuildDashboard(); break;
                    case "Customers": BuildCustomerModule(); break;
                    case "Quotations": BuildQuotationModule(); break;
                    case "Sales Orders": BuildSalesOrderModule(); break;
                    case "Production": BuildProductionModule(); break;
                    case "Raw Materials": BuildRawMaterialModule(); break;
                    case "Purchase Orders": BuildPurchaseOrderModule(); break;
                    case "Goods Received": BuildGoodsReceivedModule(); break;
                    case "Warehouse": BuildWarehouseModule(); break;
                    case "Delivery Notes": BuildDeliveryModule(); break;
                    case "Invoices": BuildInvoiceModule(); break;
                    case "Refunds": BuildRefundModule(); break;
                    case "Suppliers": BuildSupplierModule(); break;
                    case "Staff": BuildStaffModule(); break;
                    case "System Admin": BuildSystemAdminModule(); break;
                }
            }
            catch (Exception ex)
            {
                Label errLabel = new Label
                {
                    Text = $"Error loading module: {ex.Message}",
                    ForeColor = Color.Red,
                    AutoSize = true,
                    Location = new Point(20, 20)
                };
                _contentPanel.Controls.Add(errLabel);
            }

            _contentPanel.ResumeLayout();
        }

        // ─────────────── DASHBOARD ───────────────
        private void BuildDashboard()
        {
            Panel dashPanel = new Panel { Dock = DockStyle.Fill };

            // KPI Cards row
            Panel cardsRow = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(0, 0, 0, 16) };
            TableLayoutPanel cardTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
                cardTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            try
            {
                var customers = _customerCtrl.GetAllCustomers();
                var salesOrders = _salesOrderCtrl.GetAllSalesOrders();
                var invoices = _invoiceCtrl.GetAllInvoices();
                var production = _productCtrl.GetAllProducts();

                cardTable.Controls.Add(CreateKpiCard("Total Customers", customers?.Rows.Count.ToString() ?? "—", "👥", Theme.Primary), 0, 0);
                cardTable.Controls.Add(CreateKpiCard("Sales Orders", salesOrders?.Rows.Count.ToString() ?? "—", "🛒", Color.FromArgb(0, 168, 120)), 1, 0);
                cardTable.Controls.Add(CreateKpiCard("Invoices", invoices?.Rows.Count.ToString() ?? "—", "🧾", Color.FromArgb(230, 120, 20)), 2, 0);
                cardTable.Controls.Add(CreateKpiCard("Products", production?.Rows.Count.ToString() ?? "—", "📦", Color.FromArgb(160, 40, 180)), 3, 0);
            }
            catch
            {
                cardTable.Controls.Add(CreateKpiCard("Customers", "—", "👥", Theme.Primary), 0, 0);
                cardTable.Controls.Add(CreateKpiCard("Sales Orders", "—", "🛒", Color.FromArgb(0, 168, 120)), 1, 0);
                cardTable.Controls.Add(CreateKpiCard("Invoices", "—", "🧾", Color.FromArgb(230, 120, 20)), 2, 0);
                cardTable.Controls.Add(CreateKpiCard("Products", "—", "📦", Color.FromArgb(160, 40, 180)), 3, 0);
            }
            cardsRow.Controls.Add(cardTable);

            // Charts row
            TableLayoutPanel chartsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 0)
            };
            chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            chartsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            Panel chartPanel = CreateCard("Monthly Sales Overview");
            ChartControl barChart = new ChartControl { Dock = DockStyle.Fill };
            barChart.SetBarData(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                                new[] { 420000m, 380000m, 510000m, 490000m, 620000m, 580000m },
                                "HKD");
            chartPanel.Controls.Add(barChart);
            chartsRow.Controls.Add(chartPanel, 0, 0);

            Panel piePanel = CreateCard("Order Status Distribution");
            PieChartControl pieChart = new PieChartControl { Dock = DockStyle.Fill };
            pieChart.SetData(new[] { "Pending", "In Progress", "Completed", "Cancelled" },
                             new[] { 25f, 40f, 30f, 5f });
            piePanel.Controls.Add(pieChart);
            chartsRow.Controls.Add(piePanel, 1, 0);

            dashPanel.Controls.Add(chartsRow);
            dashPanel.Controls.Add(cardsRow);
            _contentPanel.Controls.Add(dashPanel);
        }

        private Panel CreateKpiCard(string title, string value, string icon, Color accentColor)
        {
            Panel card = new Panel
            {
                Margin = new Padding(6),
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(accentColor))
                    e.Graphics.FillRectangle(brush, 0, 0, 5, card.Height);
                using (var pen = new Pen(Color.FromArgb(230, 235, 245)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label iconLbl = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 22), AutoSize = true, Location = new Point(16, 16) };
            Label valueLbl = new Label { Text = value, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Theme.TextDark, AutoSize = true, Location = new Point(60, 14) };
            Label titleLbl = new Label { Text = title, Font = new Font("Segoe UI", 8), ForeColor = Theme.TextGray, AutoSize = true, Location = new Point(60, 52) };

            card.Controls.Add(iconLbl);
            card.Controls.Add(valueLbl);
            card.Controls.Add(titleLbl);
            return card;
        }

        private Panel CreateCard(string title)
        {
            Panel card = new Panel
            {
                Margin = new Padding(6),
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(225, 230, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            Label titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 30
            };
            card.Controls.Add(titleLbl);
            return card;
        }

        // ─────────────── GENERIC CRUD MODULE BUILDER ───────────────
        private void BuildGenericModule(string title, Func<DataTable> loadData, Action onCreate)
        {
            Panel wrapper = new Panel { Dock = DockStyle.Fill };

            // Toolbar
            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 8) };
            Button btnCreate = CreatePrimaryButton($"+ New {title}");
            btnCreate.Location = new Point(0, 8);
            btnCreate.Click += (s, e) => onCreate();
            Button btnRefresh = CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnCreate.Width + 10, 8);

            DataGridView grid = CreateStyledGrid();
            btnRefresh.Click += (s, e) =>
            {
                try { grid.DataSource = loadData(); StyleGrid(grid); }
                catch (Exception ex) { ShowError(ex.Message); }
            };

            toolbar.Controls.Add(btnCreate);
            toolbar.Controls.Add(btnRefresh);

            try { grid.DataSource = loadData(); StyleGrid(grid); }
            catch { /* no db connection */ }

            wrapper.Controls.Add(grid);
            wrapper.Controls.Add(toolbar);
            _contentPanel.Controls.Add(wrapper);
        }

        // ─────────────── CUSTOMER MODULE ───────────────
        private void BuildCustomerModule()
        {
            BuildGenericModule("Customer", () => _customerCtrl.GetAllCustomers(), ShowCreateCustomerDialog);
        }

        private void ShowCreateCustomerDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Customer";
                dlg.Size = new Size(480, 380);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = Theme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtName = new TextBox(); var txtAddr = new TextBox(); var txtTerm = new TextBox();

                AddFormField(layout, 0, "Customer Name *", txtName);
                AddFormField(layout, 1, "Billing Address", txtAddr);
                AddFormField(layout, 2, "Payment Term", txtTerm);

                var btnSave = CreatePrimaryButton("Save");
                var btnCancel = CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { ShowWarning("Customer Name is required."); return; }
                    try
                    {
                        _customerCtrl.Insert(new Customer { CustomerName = txtName.Text.Trim(), BillingAddress = txtAddr.Text.Trim(), PaymentTerm = txtTerm.Text.Trim() });
                        ShowSuccess("Customer created successfully."); dlg.DialogResult = DialogResult.OK; dlg.Close();
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
                LoadModule("Customers");
            }
        }

        // ─────────────── QUOTATION MODULE ───────────────
        private void BuildQuotationModule() => BuildGenericModule("Quotation", () => _quotationCtrl.GetAllQuotations(), ShowCreateQuotationDialog);
        private void ShowCreateQuotationDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Quotation",
                new[] { "Customer ID *", "Staff ID *", "Currency ID", "Status (0-3)", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
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
                        ShowSuccess($"Quotation QT-{id} created."); LoadModule("Quotations");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── SALES ORDER MODULE ───────────────
        private void BuildSalesOrderModule() => BuildGenericModule("Sales Order", () => _salesOrderCtrl.GetAllSalesOrders(), ShowCreateSalesOrderDialog);
        private void ShowCreateSalesOrderDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Sales Order",
                new[] { "Customer ID *", "Staff ID *", "Currency ID", "Delivery Address *", "Discount", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
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
                        ShowSuccess($"Sales Order SO-{id} created."); LoadModule("Sales Orders");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── PRODUCTION MODULE ───────────────
        private void BuildProductionModule() => BuildGenericModule("Production Order", () => _productionCtrl.GetAllProductionOrders(), ShowCreateProductionDialog);
        private void ShowCreateProductionDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Production Order",
                new[] { "Sales Order ID *", "Staff ID *", "Est. Finish Date (yyyy-MM-dd) *", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var po = new ProductionOrder
                        {
                            ProductionOrderCode = "PO-TEMP",
                            SalesOrderID = long.Parse(vals[0]),
                            StaffID = long.Parse(vals[1]),
                            EstFinishDate = DateTime.Parse(vals[2]),
                            Status = string.IsNullOrEmpty(vals[3]) ? 0 : int.Parse(vals[3]),
                            Remark = vals[4]
                        };
                        long id = _productionCtrl.Insert(po);
                        _productionCtrl.UpdateCodeAfterInsert(id);
                        ShowSuccess($"Production Order PO-{id} created."); LoadModule("Production");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── RAW MATERIALS MODULE ───────────────
        private void BuildRawMaterialModule() => BuildGenericModule("Raw Material", () => _rawMaterialCtrl.GetAllRawMaterials(), ShowCreateRawMaterialDialog);
        private void ShowCreateRawMaterialDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Raw Material",
                new[] { "Raw Material Code *", "Category", "Size", "Color", "Min Stock Level", "Status" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var rm = new RawMaterial
                        {
                            RawMaterialCode = vals[0],
                            Category = vals[1],
                            Size = vals[2],
                            Color = vals[3],
                            MinimumStockLevel = string.IsNullOrEmpty(vals[4]) ? 0 : int.Parse(vals[4]),
                            Status = string.IsNullOrEmpty(vals[5]) ? 1 : int.Parse(vals[5])
                        };
                        _rawMaterialCtrl.Insert(rm);
                        ShowSuccess("Raw Material created."); LoadModule("Raw Materials");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── PURCHASE ORDERS MODULE ───────────────
        private void BuildPurchaseOrderModule() => BuildGenericModule("Purchase Order", () => _purchaseOrderCtrl.GetAllPurchaseOrders(), ShowCreatePurchaseOrderDialog);
        private void ShowCreatePurchaseOrderDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Purchase Order",
                new[] { "Supplier ID *", "Staff ID *", "Request Delivery Date (yyyy-MM-dd) *", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var po = new PurchaseOrder
                        {
                            PurchaseOrderCode = "PO-TEMP",
                            SupplierID = long.Parse(vals[0]),
                            StaffID = long.Parse(vals[1]),
                            RequestDeliveryDate = DateTime.Parse(vals[2]),
                            Status = string.IsNullOrEmpty(vals[3]) ? 0 : int.Parse(vals[3]),
                            Remark = vals[4]
                        };
                        long id = _purchaseOrderCtrl.Insert(po);
                        _purchaseOrderCtrl.UpdateCodeAfterInsert(id);
                        ShowSuccess($"Purchase Order created (ID: {id})."); LoadModule("Purchase Orders");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── GOODS RECEIVED MODULE ───────────────
        private void BuildGoodsReceivedModule() => BuildGenericModule("GRN", () => _grnCtrl.GetAllGoodsReceivedNotes(), ShowCreateGrnDialog);
        private void ShowCreateGrnDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Goods Received Note",
                new[] { "Supplier ID *", "Purchase Order ID *", "Staff ID *", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var grn = new GoodsReceivedNote
                        {
                            GoodsReceivedNoteCode = "GRN-TEMP",
                            SupplierID = long.Parse(vals[0]),
                            PurchaseOrderID = long.Parse(vals[1]),
                            StaffID = long.Parse(vals[2]),
                            Status = string.IsNullOrEmpty(vals[3]) ? 0 : int.Parse(vals[3]),
                            Remark = vals[4]
                        };
                        long id = _grnCtrl.Insert(grn);
                        if (id <= 0) { ShowError("GRN creation failed: invalid ID."); return; }
                        _grnCtrl.UpdateCodeAfterInsert(id);
                        ShowSuccess(DocumentCodeHelper.Build("GRN", id) + " created."); LoadModule("Goods Received");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── WAREHOUSE MODULE ───────────────
        private void BuildWarehouseModule() => BuildGenericModule("Warehouse", () => _warehouseCtrl.GetAllWarehouses(), ShowCreateWarehouseDialog);
        private void ShowCreateWarehouseDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Warehouse", new[] { "Warehouse Name *", "Address" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        _warehouseCtrl.Insert(new Warehouse { WarehouseName = vals[0], WarehouseAddress = vals[1] });
                        ShowSuccess("Warehouse created."); LoadModule("Warehouse");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── DELIVERY MODULE ───────────────
        private void BuildDeliveryModule() => BuildGenericModule("Delivery Note", () => _deliveryCtrl.GetAllDeliveryNotes(), ShowCreateDeliveryDialog);
        private void ShowCreateDeliveryDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Delivery Note",
                new[] { "Customer ID *", "Sales Order ID *", "Staff ID *", "Warehouse ID *", "Ship Method *", "Tracking Number *", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var dn = new DeliveryNote
                        {
                            DeliveryNoteCode = "DN-TEMP",
                            CustomerID = long.Parse(vals[0]),
                            SalesOrderID = long.Parse(vals[1]),
                            StaffID = long.Parse(vals[2]),
                            WarehouseID = long.Parse(vals[3]),
                            ShipMethod = vals[4],
                            TrackingNumber = vals[5],
                            Status = string.IsNullOrEmpty(vals[6]) ? 0 : int.Parse(vals[6]),
                            Remark = vals[7]
                        };
                        long id = _deliveryCtrl.Insert(dn);
                        if (id <= 0) { ShowError("Delivery note creation failed: invalid ID."); return; }
                        _deliveryCtrl.UpdateCodeAfterInsert(id);
                        ShowSuccess(DeliveryNoteController.FormatDeliveryNoteCode(id) + " created."); LoadModule("Delivery Notes");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── INVOICE MODULE ───────────────
        private void BuildInvoiceModule() => BuildGenericModule("Invoice", () => _invoiceCtrl.GetAllInvoices(), ShowCreateInvoiceDialog);
        private void ShowCreateInvoiceDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Invoice",
                new[] { "Customer ID *", "Sales Order ID *", "Staff ID *", "Invoice Type", "Status", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var inv = new Invoice
                        {
                            InvoiceCode = "INV-TEMP",
                            CustomerID = long.Parse(vals[0]),
                            SalesOrderID = long.Parse(vals[1]),
                            StaffID = long.Parse(vals[2]),
                            InvoiceType = string.IsNullOrEmpty(vals[3]) ? 1 : int.Parse(vals[3]),
                            Status = string.IsNullOrEmpty(vals[4]) ? 0 : int.Parse(vals[4]),
                            Remark = vals[5]
                        };
                        long id = _invoiceCtrl.Insert(inv);
                        _invoiceCtrl.UpdateCodeAfterInsert(id);
                        ShowSuccess($"Invoice {DocumentCodeHelper.FormatInvoiceCode(id)} created."); LoadModule("Invoices");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── REFUND MODULE ───────────────
        private void BuildRefundModule() => BuildGenericModule("Refund Request", () => _refundCtrl.GetAllRefundRequests(), ShowCreateRefundDialog);
        private void ShowCreateRefundDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Refund Request",
                new[] { "Invoice ID *", "Staff ID *", "Refund Amount *", "Refund Method *", "Reason *", "Remark" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        var rr = new RefundRequest
                        {
                            RefundRequestCode = "RF-TEMP",
                            InvoiceID = long.Parse(vals[0]),
                            StaffID = long.Parse(vals[1]),
                            RefundAmount = decimal.Parse(vals[2]),
                            RefundMethod = vals[3],
                            RefundReason = vals[4],
                            Remark = vals[5],
                            Status = 0
                        };
                        _refundCtrl.CreateRefundRequest(rr);
                        ShowSuccess("Refund Request created."); LoadModule("Refunds");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── SUPPLIER MODULE ───────────────
        private void BuildSupplierModule() => BuildGenericModule("Supplier", () => _supplierCtrl.GetAllSuppliers(), ShowCreateSupplierDialog);
        private void ShowCreateSupplierDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Supplier",
                new[] { "Supplier Name *", "Contact Person", "Phone", "Email", "Billing Address", "Payment Term" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        _supplierCtrl.Insert(new Supplier
                        {
                            SupplierName = vals[0], ContactPerson = vals[1], Phone = vals[2],
                            Email = vals[3], BillingAddress = vals[4], PaymentTerm = vals[5], Status = 1
                        });
                        ShowSuccess("Supplier created."); LoadModule("Suppliers");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── STAFF MODULE ───────────────
        private void BuildStaffModule() => BuildGenericModule("Staff", () => _staffCtrl.GetAllStaff(), ShowCreateStaffDialog);
        private void ShowCreateStaffDialog()
        {
            using (var dlg = BuildSimpleInputDialog("New Staff Member",
                new[] { "Username *", "Password *", "First Name *", "Last Name *", "Title", "Department", "Email *", "Phone", "Employ Date (yyyy-MM-dd)" }))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var vals = GetDialogValues(dlg);
                        _staffCtrl.Insert(new Staff
                        {
                            Username = vals[0], Password = vals[1], FirstName = vals[2], LastName = vals[3],
                            Title = vals[4], Department = vals[5], Email = vals[6], Phone = vals[7],
                            EmployDate = string.IsNullOrEmpty(vals[8]) ? DateTime.Today : DateTime.Parse(vals[8]),
                            Status = 1
                        });
                        ShowSuccess("Staff member created."); LoadModule("Staff");
                    }
                    catch (Exception ex) { ShowError(ex.Message); }
                }
            }
        }

        // ─────────────── SYSTEM ADMIN MODULE ───────────────
        private void BuildSystemAdminModule()
        {
            Panel wrapper = new Panel { Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            Panel dictCard = CreateCard("System Dictionary");
            DataGridView dictGrid = CreateStyledGrid();
            dictGrid.Dock = DockStyle.Fill;
            try { dictGrid.DataSource = _dictCtrl.GetAllDictionaries(); StyleGrid(dictGrid); } catch { }
            dictCard.Controls.Add(dictGrid);
            layout.Controls.Add(dictCard, 0, 0);

            Panel productCard = CreateCard("Product Catalog");
            DataGridView productGrid = CreateStyledGrid();
            productGrid.Dock = DockStyle.Fill;
            try { productGrid.DataSource = _productCtrl.GetAllProducts(); StyleGrid(productGrid); } catch { }
            productCard.Controls.Add(productGrid);
            layout.Controls.Add(productCard, 1, 0);

            Panel supplierQuoteCard = CreateCard("Supplier Raw Material Quotes");
            DataGridView sqGrid = CreateStyledGrid();
            sqGrid.Dock = DockStyle.Fill;
            try { sqGrid.DataSource = _rawMaterialCtrl.GetAllSupplierQuotes(); StyleGrid(sqGrid); } catch { }
            supplierQuoteCard.Controls.Add(sqGrid);
            layout.Controls.Add(supplierQuoteCard, 0, 1);

            Panel infoCard = CreateCard("System Information");
            Label infoLabel = new Label
            {
                Text = "Premium Living Furniture ERP\n\nVersion: 2.0\nFramework: .NET 4.8\nDatabase: MySQL\n\nModules: Sales, Production, Warehouse,\nLogistics, Finance, System Admin",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextDark,
                Padding = new Padding(10)
            };
            infoCard.Controls.Add(infoLabel);
            layout.Controls.Add(infoCard, 1, 1);

            wrapper.Controls.Add(layout);
            _contentPanel.Controls.Add(wrapper);
        }

        // ─────────────── HELPER METHODS ───────────────
        private DataGridView CreateStyledGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 235, 245),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate = { Height = 32 }
            };
        }

        private void StyleGrid(DataGridView grid)
        {
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Padding = new Padding(4, 0, 0, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextDark,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(4, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(210, 225, 255),
                SelectionForeColor = Theme.TextDark
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 255)
            };
            grid.EnableHeadersVisualStyles = false;
        }

        private Button CreatePrimaryButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 150, Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private Button CreateSecondaryButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 110, Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Theme.Primary }
            };
        }

        private void AddFormField(TableLayoutPanel layout, int row, string label, Control control)
        {
            layout.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextDark
            }, 0, row);
            control.Dock = DockStyle.Fill;
            layout.Controls.Add(control, 1, row);
        }

        private Form BuildSimpleInputDialog(string title, string[] fieldLabels)
        {
            Form dlg = new Form
            {
                Text = title,
                Size = new Size(460, 80 + fieldLabels.Length * 50),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = Theme.Background
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = fieldLabels.Length,
                Padding = new Padding(16, 16, 16, 8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < fieldLabels.Length; i++)
            {
                var txt = new TextBox { Tag = fieldLabels[i] };
                AddFormField(layout, i, fieldLabels[i], txt);
            }

            var btnSave = CreatePrimaryButton("Save");
            var btnCancel = CreateSecondaryButton("Cancel");
            btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            btnSave.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(layout);
            dlg.Controls.Add(btnPanel);
            return dlg;
        }

        private string[] GetDialogValues(Form dlg)
        {
            var layout = dlg.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (layout == null) return new string[0];
            var vals = new System.Collections.Generic.List<string>();
            foreach (Control ctrl in layout.Controls)
            {
                if (ctrl is TextBox txt) vals.Add(txt.Text.Trim());
            }
            return vals.ToArray();
        }

        private void ShowError(string msg) => MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        private void ShowWarning(string msg) => MessageBox.Show(msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private void ShowSuccess(string msg) => MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void SetCurrentUser(Staff user)
        {
            CurrentUser = user;
            var userLabel = _headerPanel.Controls["lblUser"] as Label;
            if (userLabel != null && user != null)
            {
                userLabel.Text = $"👤  {user.FullName}  |  {user.Department}";
                userLabel.Location = new Point(_headerPanel.Width - userLabel.PreferredWidth - 20,
                    (_headerPanel.Height - userLabel.PreferredHeight) / 2);
            }
        }
    }
}
