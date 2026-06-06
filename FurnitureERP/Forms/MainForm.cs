using System;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public partial class MainForm : Form
    {
        public Staff CurrentUser { get; set; }

        private Panel _sidebar;
        private Panel _contentPanel;
        private Panel _headerPanel;
        private Panel _navContainer;
        private Button _activeNavButton;

        public MainForm()
        {
            InitializeComponent();
            BuildLayout();
        }

        private void BuildLayout()
        {
            SuspendLayout();

            _sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = UITheme.NavDark };
            BuildSidebar();

            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            BuildHeader();

            _contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Background, Padding = new Padding(0) };

            Controls.Add(_contentPanel);
            Controls.Add(_sidebar);
            Controls.Add(_headerPanel);

            ResumeLayout();
        }

        private void BuildSidebar()
        {
            _sidebar.Controls.Clear();

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UITheme.NavDark,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            Panel logoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.NavDarkest,
                Padding = new Padding(12, 12, 12, 10)
            };
            var logoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            logoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            logoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            Label logoLabel = new Label
            {
                Text = "PREMIUM LIVING",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter,
                Margin = new Padding(0)
            };
            Label subLabel = new Label
            {
                Text = "ERP System",
                ForeColor = Color.FromArgb(160, 190, 230),
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter,
                Margin = new Padding(0, 2, 0, 0)
            };
            logoLayout.Controls.Add(logoLabel, 0, 0);
            logoLayout.Controls.Add(subLabel, 0, 1);
            logoPanel.Controls.Add(logoLayout);

            _navContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UITheme.NavDark,
                Padding = new Padding(0, 4, 0, 4)
            };

            Button logoutBtn = new Button
            {
                Text = "Logout",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(255, 120, 120),
                BackColor = Color.FromArgb(60, 20, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            logoutBtn.FlatAppearance.BorderSize = 0;
            logoutBtn.Click += (s, e) => {
                AppSession.Clear();
                if (MessageBox.Show("Logout from the system?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                { new FurnitureERP.LoginForm().Show(); Close(); }
            };

            sidebarLayout.Controls.Add(logoPanel, 0, 0);
            sidebarLayout.Controls.Add(_navContainer, 0, 1);
            sidebarLayout.Controls.Add(logoutBtn, 0, 2);
            _sidebar.Controls.Add(sidebarLayout);

            PopulateNavButtons();
        }

        private void PopulateNavButtons()
        {
            if (_navContainer == null) return;
            _navContainer.Controls.Clear();
            _navContainer.SuspendLayout();
            int yPos = 4;
            int buttonWidth = Math.Max(180, _navContainer.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            foreach (var name in GetVisibleNavItems())
            {
                Button btn = new Button
                {
                    Text = "  " + GetNavDisplayText(name),
                    Height = 40,
                    Width = buttonWidth,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.FromArgb(180, 210, 255),
                    BackColor = UITheme.NavDark,
                    Font = new Font("Segoe UI", 9),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    Tag = name,
                    Location = new Point(0, yPos),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = UITheme.NavHover;
                btn.Click += NavButton_Click;
                _navContainer.Controls.Add(btn);
                yPos += 44;
            }
            _navContainer.AutoScrollMinSize = new Size(0, yPos + 4);
            _navContainer.ResumeLayout(true);
            ResetNavScrollTop();
        }

        private void ResetNavScrollTop()
        {
            if (_navContainer == null) return;
            try
            {
                _navContainer.AutoScrollPosition = new Point(0, 0);
                if (_navContainer.VerticalScroll.Visible)
                    _navContainer.VerticalScroll.Value = _navContainer.VerticalScroll.Minimum;
            }
            catch { }
        }

        private static string[] GetVisibleNavItems()
        {
            var items = new System.Collections.Generic.List<string>();
            if (AppSession.IsLoggedIn) items.Add("Overview");
            if (AppSession.CanView(PermissionModule.Customer)
                || AppSession.CanView(PermissionModule.Quotation)
                || AppSession.CanView(PermissionModule.SalesOrder)
                || AppSession.CanView(PermissionModule.ReplySlip))
                items.Add("Customers");
            if (AppSession.CanView(PermissionModule.Quotation)) items.Add("Quotations");
            if (AppSession.CanView(PermissionModule.SalesOrder)) items.Add("Sales Orders");
            if (AppSession.CanView(PermissionModule.ProductionOrder)
                || AppSession.CanView(PermissionModule.RawMaterialRequestNote)
                || AppSession.CanView(PermissionModule.Product))
                items.Add("Production");
            if (AppSession.CanView(PermissionModule.RawMaterial)) items.Add("Raw Materials");
            if (AppSession.CanView(PermissionModule.PurchaseOrder)) items.Add("Purchase Orders");
            if (AppSession.CanView(PermissionModule.GoodsReceivedNote)) items.Add("Goods Received");
            if (AppSession.CanView(PermissionModule.Warehouse)) items.Add("Warehouse");
            if (AppSession.CanView(PermissionModule.InternalTransferForm)) items.Add("Internal Transfer");
            if (AppSession.CanView(PermissionModule.DeliveryNote)) items.Add("Delivery Notes");
            if (AppSession.CanView(PermissionModule.Invoice)) items.Add("Invoices");
            if (AppSession.CanView(PermissionModule.Refund)) items.Add("Refunds");
            if (AppSession.CanView(PermissionModule.PaymentVoucher) || AppSession.CanView(PermissionModule.ReceiptVoucher))
                items.Add("Finance Dept");
            if (AppSession.CanView(PermissionModule.Supplier)) items.Add("Suppliers");
            if (AppSession.IsSuperUser)
            {
                items.Add("Staff");
                items.Add("System Admin");
            }
            return items.ToArray();
        }

        private void BuildHeader()
        {
            Label moduleTitle = new Label { Name = "lblModuleTitle", Text = "Overview", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = UITheme.Primary, AutoSize = true, Location = new Point(20, 17) };
            Label userLabel = new Label { Name = "lblUser", AutoSize = true, ForeColor = UITheme.TextDark, Font = new Font("Segoe UI", 9) };
            _headerPanel.Controls.Add(moduleTitle);
            _headerPanel.Controls.Add(userLabel);
            _headerPanel.Resize += (s, e) => {
                if (_headerPanel.Controls["lblUser"] is Label ul)
                    ul.Location = new Point(_headerPanel.Width - ul.PreferredWidth - 20, (_headerPanel.Height - ul.PreferredHeight) / 2);
            };
        }

        private void NavButton_Click(object sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (_activeNavButton != null) { _activeNavButton.BackColor = UITheme.NavDark; _activeNavButton.ForeColor = Color.FromArgb(180, 210, 255); _activeNavButton.Font = new Font("Segoe UI", 9); }
            btn.BackColor = UITheme.NavActive; btn.ForeColor = Color.White; btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            _activeNavButton = btn;
            LoadModule(btn.Tag?.ToString() ?? "Overview");
        }

        public void LoadModule(string module)
        {
            if (!AppSession.IsLoggedIn)
            {
                UITheme.ShowWarning("Session expired. Please log in again.");
                return;
            }
            AppSession.TouchActivity();

            if (!CanAccessModule(module))
            {
                UITheme.ShowWarning("You do not have permission to access this module.");
                return;
            }

            if (_headerPanel.Controls["lblModuleTitle"] is Label t) t.Text = module;
            _contentPanel.Controls.Clear();
            _contentPanel.SuspendLayout();
            try
            {
                Control panel = null;
                switch (module)
                {
                    case "Overview": panel = new DashboardPanel(); break;
                    case "Customers":
                    case "Quotations":
                    case "Sales Orders": panel = new SalesPanel(module); break;
                    case "Production":
                    case "Raw Materials": panel = new ProductionPanel(module); break;
                    case "Purchase Orders":
                    case "Goods Received":
                    case "Suppliers": panel = new ProcurementPanel(module); break;
                    case "Warehouse":
                    case "Delivery Notes": panel = new WarehousePanel(module); break;
                    case "Internal Transfer": panel = new InternalTransferPanel(); break;
                    case "Invoices":
                    case "Refunds": panel = new FinancePanel(module); break;
                    case "Finance Dept": panel = new FinanceDeptPanel(); break;
                    case "Staff": panel = new StaffPanel(); break;
                    case "System Admin": panel = new SystemAdminPanel(module); break;
                }
                if (panel != null)
                {
                    panel.Dock = DockStyle.Fill;
                    _contentPanel.Controls.Add(panel);
                }
            }
            catch (Exception ex)
            {
                _contentPanel.Controls.Add(new Label { Text = "Error loading module: " + ex.Message, ForeColor = Color.Red, AutoSize = true, Location = new Point(20, 20) });
            }
            _contentPanel.ResumeLayout();
        }

        private static bool CanAccessModule(string module)
        {
            if (!AppSession.IsLoggedIn) return false;
            switch (module)
            {
                case "Overview": return true;
                case "Customers":
                    return AppSession.CanView(PermissionModule.Customer)
                        || AppSession.CanView(PermissionModule.Quotation)
                        || AppSession.CanView(PermissionModule.SalesOrder)
                        || AppSession.CanView(PermissionModule.ReplySlip);
                case "Quotations": return AppSession.CanView(PermissionModule.Quotation);
                case "Sales Orders": return AppSession.CanView(PermissionModule.SalesOrder);
                case "Production":
                    return AppSession.CanView(PermissionModule.ProductionOrder)
                        || AppSession.CanView(PermissionModule.RawMaterialRequestNote)
                        || AppSession.CanView(PermissionModule.Product);
                case "Raw Materials": return AppSession.CanView(PermissionModule.RawMaterial);
                case "Purchase Orders": return AppSession.CanView(PermissionModule.PurchaseOrder);
                case "Goods Received": return AppSession.CanView(PermissionModule.GoodsReceivedNote);
                case "Warehouse": return AppSession.CanView(PermissionModule.Warehouse);
                case "Internal Transfer": return AppSession.CanView(PermissionModule.InternalTransferForm);
                case "Delivery Notes": return AppSession.CanView(PermissionModule.DeliveryNote);
                case "Invoices": return AppSession.CanView(PermissionModule.Invoice);
                case "Refunds": return AppSession.CanView(PermissionModule.Refund);
                case "Finance Dept":
                    return AppSession.CanView(PermissionModule.PaymentVoucher)
                        || AppSession.CanView(PermissionModule.ReceiptVoucher);
                case "Suppliers": return AppSession.CanView(PermissionModule.Supplier);
                case "Staff":
                case "System Admin": return AppSession.IsSuperUser;
                default: return false;
            }
        }

        public void SetCurrentUser(Staff user)
        {
            CurrentUser = user;
            AppSession.StartSession(user);
            if (_headerPanel.Controls["lblUser"] is Label ul && user != null)
            {
                string roleLabel = AppSession.IsSuperUser ? "Super User" : user.Department;
                ul.Text = user.FullName + " | " + roleLabel;
                ul.Location = new Point(_headerPanel.Width - ul.PreferredWidth - 20, (_headerPanel.Height - ul.PreferredHeight) / 2);
            }
            PopulateNavButtons();
            ActivateNavButton("Overview");
            LoadModule("Overview");
        }

        private void ActivateNavButton(string module)
        {
            if (_navContainer == null) return;
            foreach (Control control in _navContainer.Controls)
            {
                if (!(control is Button btn)) continue;
                if (!string.Equals(btn.Tag?.ToString(), module, StringComparison.OrdinalIgnoreCase)) continue;
                if (_activeNavButton != null)
                {
                    _activeNavButton.BackColor = UITheme.NavDark;
                    _activeNavButton.ForeColor = Color.FromArgb(180, 210, 255);
                    _activeNavButton.Font = new Font("Segoe UI", 9);
                }
                btn.BackColor = UITheme.NavActive;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                _activeNavButton = btn;
                return;
            }
        }

        private static string GetNavDisplayText(string module)
        {
            if (string.Equals(module, "Overview", StringComparison.OrdinalIgnoreCase))
                return "📊  Overview";
            return module;
        }
    }
}
