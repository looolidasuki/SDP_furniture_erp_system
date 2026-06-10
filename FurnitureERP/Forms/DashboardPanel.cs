using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    /// <summary>
    /// DashboardPanel displays high-level system metrics and overview.
    /// Inheritance: UserControl provides better stability for UI components.
    /// </summary>
    public class DashboardPanel : UserControl
    {
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly ProductController _productCtrl = new ProductController();

        private TextBox _txtModuleFilter;
        private Label _lblPermissionTitle;
        private Label _lblPermissionHint;
        private DataGridView _permissionGrid;
        private DataTable _matrixTable;

        public DashboardPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            Build();
        }

        private void Build()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;

            var cardsRow = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.Transparent };
            var cardTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            for (int i = 0; i < 4; i++)
                cardTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            string[] titles = { "Customers", "Sales Orders", "Invoices", "Products" };
            string[] values = {
                _customerCtrl.GetCount().ToString(),
                _salesOrderCtrl.GetCount().ToString(),
                _invoiceCtrl.GetCount().ToString(),
                _productCtrl.GetCount().ToString()
            };
            string[] icons = { "👥", "📝", "📄", "📦" };
            Color[] colors = { Color.DodgerBlue, Color.Orange, Color.Green, Color.Purple };
            for (int i = 0; i < 4; i++)
            {
                var kpiCard = CreateKpiCard(titles[i], values[i], icons[i], colors[i]);
                kpiCard.Dock = DockStyle.Fill;
                kpiCard.Margin = new Padding(8);
                cardTable.Controls.Add(kpiCard, i, 0);
            }
            cardsRow.Controls.Add(cardTable);

            var financeRow = BuildFinanceKpiRow();
            var tabs = BuildMainTabs();

            Controls.Add(tabs);
            if (financeRow != null) Controls.Add(financeRow);
            Controls.Add(cardsRow);

            PerformLayout();
            ResumeLayout(true);
        }

        private Panel BuildFinanceKpiRow()
        {
            bool showFinance = AppSession.CanView(PermissionModule.Invoice)
                || AppSession.CanView(PermissionModule.ReceiptVoucher)
                || AppSession.CanView(PermissionModule.PaymentVoucher);

            var row = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 4) };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            if (showFinance)
            {
                try
                {
                    table.Controls.Add(CreateKpiCard("Month Income (HKD)", DashboardOverviewService.GetMonthReceiptIncomeHkd().ToString("N0"), "💰", Color.FromArgb(34, 139, 34)), 0, 0);
                    table.Controls.Add(CreateKpiCard("Month Expense (HKD)", DashboardOverviewService.GetMonthPaymentExpenseHkd().ToString("N0"), "💸", Color.FromArgb(220, 53, 69)), 1, 0);
                    table.Controls.Add(CreateKpiCard("AR Outstanding (HKD)", DashboardOverviewService.GetTotalAccountsReceivableHkd().ToString("N0"), "📥", Color.FromArgb(0, 123, 255)), 2, 0);
                    table.Controls.Add(CreateKpiCard("AP Outstanding (HKD)", DashboardOverviewService.GetTotalAccountsPayableHkd().ToString("N0"), "📤", Color.FromArgb(255, 140, 0)), 3, 0);
                }
                catch
                {
                    table.Controls.Add(CreateKpiCard("Month Income (HKD)", "—", "💰", Color.Gray), 0, 0);
                    table.Controls.Add(CreateKpiCard("Month Expense (HKD)", "—", "💸", Color.Gray), 1, 0);
                    table.Controls.Add(CreateKpiCard("AR Outstanding (HKD)", "—", "📥", Color.Gray), 2, 0);
                    table.Controls.Add(CreateKpiCard("AP Outstanding (HKD)", "—", "📤", Color.Gray), 3, 0);
                }
            }
            else
            {
                table.Controls.Add(CreateKpiCard("Open Sales Orders", DashboardOverviewService.GetOpenSalesOrderCount().ToString(), "📝", Color.Orange), 0, 0);
                table.Controls.Add(CreateKpiCard("Open Purchase Orders", DashboardOverviewService.GetOpenPurchaseOrderCount().ToString(), "🛒", Color.FromArgb(255, 140, 0)), 1, 0);
                table.Controls.Add(CreateKpiCard("Active Production", DashboardOverviewService.GetActiveProductionOrderCount().ToString(), "🏭", Color.FromArgb(102, 16, 242)), 2, 0);
                table.Controls.Add(CreateKpiCard("Invoices", _invoiceCtrl.GetCount().ToString(), "📄", Color.Green), 3, 0);
            }

            foreach (Control c in table.Controls)
            {
                if (c is Panel p)
                {
                    p.Dock = DockStyle.Fill;
                    p.Margin = new Padding(8, 0, 8, 0);
                }
            }

            row.Controls.Add(table);
            return row;
        }

        private TabControl BuildMainTabs()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), Padding = new Point(8, 4) };

            var tabTasks = new TabPage("My Tasks") { BackColor = UITheme.Background };
            tabTasks.Controls.Add(BuildMyTasksSection());

            var tabAction = new TabPage("Action Items") { BackColor = UITheme.Background };
            tabAction.Controls.Add(BuildActionItemsSection());

            var tabPermissions = new TabPage("Permissions") { BackColor = UITheme.Background };
            tabPermissions.Controls.Add(BuildPermissionOverviewSection());

            tabs.TabPages.Add(tabTasks);
            tabs.TabPages.Add(tabAction);
            tabs.TabPages.Add(tabPermissions);
            return tabs;
        }

        private Control BuildMyTasksSection()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var lbl = new Label
            {
                Text = "Items needing your action (based on your permissions)",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(4, 6, 0, 0)
            };
            var grid = GridHelper.CreateStyledGrid();
            grid.Dock = DockStyle.Fill;

            try
            {
                grid.DataSource = DashboardOverviewService.GetMyPendingTasks(50);
                GridHelper.StyleGrid(grid);
                if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
                if (grid.Columns.Contains("Module")) grid.Columns["Module"].Visible = false;
                if (grid.Columns.Contains("Date") && grid.Columns["Date"] is DataGridViewColumn dateCol)
                    dateCol.DefaultCellStyle.Format = "yyyy-MM-dd";
            }
            catch { }

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = grid.Rows[e.RowIndex];
                if (row.Cells["Id"].Value == null || row.Cells["Id"].Value == DBNull.Value) return;
                long id = Convert.ToInt64(row.Cells["Id"].Value);
                string docType = row.Cells["Document Type"]?.Value?.ToString();
                string module = row.Cells["Module"]?.Value?.ToString();
                string code = row.Cells["Code"]?.Value?.ToString();
                if (id <= 0 || string.IsNullOrWhiteSpace(module)) return;
                var main = FindForm() as MainForm;
                main?.NavigateToDocument(new DocumentSearchResult
                {
                    DocumentType = docType,
                    Id = id,
                    Code = code,
                    Module = module
                });
            };

            panel.Controls.Add(grid);
            panel.Controls.Add(lbl);
            return panel;
        }

        private Control BuildActionItemsSection()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 220
            };

            var lblSo = new Label
            {
                Text = "Sales Orders — Uninvoiced balance",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(4, 6, 0, 0)
            };
            var soGrid = GridHelper.CreateStyledGrid();
            soGrid.Dock = DockStyle.Fill;
            var soPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            soPanel.Controls.Add(soGrid);
            soPanel.Controls.Add(lblSo);

            var lblPo = new Label
            {
                Text = "Purchase Orders — Unpaid balance",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(4, 6, 0, 0)
            };
            var poGrid = GridHelper.CreateStyledGrid();
            poGrid.Dock = DockStyle.Fill;
            var poPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            poPanel.Controls.Add(poGrid);
            poPanel.Controls.Add(lblPo);

            split.Panel1.Controls.Add(soPanel);
            split.Panel2.Controls.Add(poPanel);

            try
            {
                var soData = DictionaryUIHelper.LoadWithStatusLabels(
                    () => DashboardOverviewService.GetUnsettledSalesOrders(30),
                    "Status", DictionaryService.Categories.SalesOrder);
                GridHelper.BindStatusData(soGrid, soData, DictionaryService.Categories.SalesOrder);
                if (soGrid.Columns.Contains("Order ID")) soGrid.Columns["Order ID"].Visible = false;
            }
            catch { }

            try
            {
                var poData = DictionaryUIHelper.LoadWithStatusLabels(
                    () => DashboardOverviewService.GetUnsettledPurchaseOrders(30),
                    "Status", DictionaryService.Categories.PurchaseOrder);
                GridHelper.BindStatusData(poGrid, poData, DictionaryService.Categories.PurchaseOrder);
                if (poGrid.Columns.Contains("Purchase Order ID")) poGrid.Columns["Purchase Order ID"].Visible = false;
            }
            catch { }

            soGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long id = GridHelper.TryGetRowLongId(soGrid, soGrid.Rows[e.RowIndex], "Order ID");
                if (id <= 0) return;
                var main = FindForm() as MainForm;
                main?.NavigateToDocument(new DocumentSearchResult
                {
                    DocumentType = "Sales Order",
                    Id = id,
                    Module = "Sales Orders"
                });
            };

            poGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                long id = GridHelper.TryGetRowLongId(poGrid, poGrid.Rows[e.RowIndex], "Purchase Order ID");
                if (id <= 0) return;
                var main = FindForm() as MainForm;
                main?.NavigateToDocument(new DocumentSearchResult
                {
                    DocumentType = "Purchase Order",
                    Id = id,
                    Module = "Purchase Orders"
                });
            };

            return split;
        }

        private Control BuildPermissionOverviewSection()
        {
            var section = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            section.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(225, 230, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, section.Width - 1, section.Height - 1);
            };

            bool isSuperUser = AppSession.IsSuperUser;
            string userDeptKey = PermissionService.ResolveOverviewDepartmentKey(AppSession.CurrentUser);
            string userDeptLabel = PermissionService.GetDepartmentDisplayName(userDeptKey);
            if (string.IsNullOrWhiteSpace(AppSession.CurrentUser?.Department))
                userDeptLabel = "Unassigned";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 0, 0, 8) };

            _lblPermissionTitle = new Label
            {
                Text = isSuperUser ? "Cross-Department Permission Matrix" : "My Department Permissions",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblPermissionHint = new Label
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = UITheme.TextGray,
                AutoSize = true,
                Dock = DockStyle.Top,
                MaximumSize = new Size(980, 0),
                Text = isSuperUser
                    ? "Each row is a module; each column is a department. Cells show allowed actions (View, Create, Edit) or No access."
                    : "Your department: " + userDeptLabel + " — modules and allowed actions for your role."
            };

            header.Controls.Add(_lblPermissionHint);
            header.Controls.Add(_lblPermissionTitle);

            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            if (isSuperUser)
            {
                var lblFilter = new Label
                {
                    Text = "Filter module:",
                    AutoSize = true,
                    Location = new Point(0, 12),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = UITheme.TextDark
                };
                _txtModuleFilter = new TextBox
                {
                    Width = 240,
                    Location = new Point(88, 8)
                };
                _txtModuleFilter.TextChanged += (s, e) => ApplyModuleFilter();
                toolbar.Controls.Add(lblFilter);
                toolbar.Controls.Add(_txtModuleFilter);
            }
            else
            {
                var lblDept = new Label
                {
                    Text = "Department: " + userDeptLabel,
                    AutoSize = true,
                    Location = new Point(0, 12),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = UITheme.Primary
                };
                toolbar.Controls.Add(lblDept);
            }

            var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 4, 0, 0) };

            _permissionGrid = GridHelper.CreateStyledGrid();
            _permissionGrid.Dock = DockStyle.Fill;
            _permissionGrid.AutoGenerateColumns = true;
            _permissionGrid.RowHeadersVisible = false;
            _permissionGrid.ColumnHeadersVisible = true;
            _permissionGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _permissionGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _permissionGrid.CellFormatting += PermissionGrid_CellFormatting;

            gridHost.Controls.Add(_permissionGrid);

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(toolbar, 0, 1);
            layout.Controls.Add(gridHost, 0, 2);
            section.Controls.Add(layout);

            if (isSuperUser)
                LoadPermissionMatrix();
            else
                LoadDepartmentPermissionGrid(userDeptKey);
            return section;
        }

        private void LoadPermissionMatrix()
        {
            if (_permissionGrid == null) return;
            try
            {
                _matrixTable = PermissionService.BuildPermissionMatrixTable();
                ApplyModuleFilter();
            }
            catch (Exception ex)
            {
                UITheme.ShowError(ex.Message);
            }
        }

        private void ApplyModuleFilter()
        {
            if (_permissionGrid == null || _matrixTable == null) return;
            string kw = _txtModuleFilter?.Text?.Trim().Replace("'", "''") ?? "";
            var view = _matrixTable.Copy();
            if (!string.IsNullOrEmpty(kw))
                view.DefaultView.RowFilter = $"[Module] LIKE '%{kw}%'";

            _permissionGrid.DataSource = view;
            StylePermissionGrid(isMatrix: true);

            if (_lblPermissionHint != null)
            {
                int shown = view.DefaultView.Count;
                int total = _matrixTable.Rows.Count;
                _lblPermissionHint.Text = shown == total
                    ? "Each row is a module; each column is a department. Cells show allowed actions (View, Create, Edit) or No access. Showing all " + total + " modules."
                    : "Showing " + shown + " of " + total + " modules. Clear filter to see all.";
            }
        }

        private void LoadDepartmentPermissionGrid(string departmentKey)
        {
            if (_permissionGrid == null) return;
            try
            {
                var table = PermissionService.BuildPermissionOverviewTable(departmentKey);
                _permissionGrid.DataSource = table;
                StylePermissionGrid(isMatrix: false);
            }
            catch (Exception ex)
            {
                UITheme.ShowError(ex.Message);
            }
        }

        private void StylePermissionGrid(bool isMatrix)
        {
            GridHelper.StyleGrid(_permissionGrid);
            _permissionGrid.ColumnHeadersVisible = true;
            _permissionGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _permissionGrid.ColumnHeadersHeight = isMatrix ? 46 : 36;
            _permissionGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _permissionGrid.ScrollBars = ScrollBars.Both;
            if (_permissionGrid.Rows.Count > 0)
                _permissionGrid.FirstDisplayedScrollingRowIndex = 0;

            if (_permissionGrid.Columns.Contains("Module"))
            {
                _permissionGrid.Columns["Module"].FillWeight = isMatrix ? 120 : 160;
                _permissionGrid.Columns["Module"].MinimumWidth = 120;
                _permissionGrid.Columns["Module"].Frozen = isMatrix;
            }
            if (!isMatrix && _permissionGrid.Columns.Contains("Permissions"))
                _permissionGrid.Columns["Permissions"].FillWeight = 200;

            if (isMatrix)
            {
                foreach (DataGridViewColumn col in _permissionGrid.Columns)
                {
                    if (!col.Visible || col.Name == "Module") continue;
                    col.MinimumWidth = 110;
                    col.FillWeight = 100;
                }
            }
        }

        private static void PermissionGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null || e.ColumnIndex < 0) return;
            if (!(sender is DataGridView grid)) return;
            if (grid.Columns[e.ColumnIndex].Name == "Module") return;

            string text = e.Value.ToString();
            if (string.Equals(text, "No access", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = UITheme.TextGray;
            else
                e.CellStyle.ForeColor = Color.FromArgb(34, 100, 34);
        }


        private Panel CreateCard(string title)
        {
            Panel card = new Panel { BackColor = Color.White, Padding = new Padding(15) };

            // 繪製卡片邊框
            card.Paint += (s, e) => {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(225, 230, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // 建立標題 Label
            Label titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 35
            };

            card.Controls.Add(titleLbl);
            return card;
        }

        private Panel CreateKpiCard(string title, string value, string icon, Color accentColor)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.White };

            // 1. 繪製邊框與側邊強調色 (Paint 事件保持不變)
            card.Paint += (s, e) => {
                using (var brush = new SolidBrush(accentColor))
                    e.Graphics.FillRectangle(brush, 0, 0, 6, card.Height);
                using (var pen = new Pen(Color.FromArgb(220, 225, 230)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // 2. 使用一個容器來承載內部文字，避免使用 Location
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 10, 10, 10)
            };
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            // 3. 建立 Label
            Label iconLbl = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 16), AutoSize = true, Anchor = AnchorStyles.Left };
            Label valueLbl = new Label { Text = value, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = UITheme.TextDark, AutoSize = true, Anchor = AnchorStyles.Left };
            Label titleLbl = new Label { Text = title, Font = new Font("Segoe UI", 9), ForeColor = UITheme.TextGray, AutoSize = true, Anchor = AnchorStyles.Left };

            // 將控件放入表格，不再依賴硬編碼的 Point
            layout.Controls.Add(iconLbl, 0, 0);
            layout.Controls.Add(valueLbl, 0, 1);
            layout.Controls.Add(titleLbl, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateDetailCard(string title)
        {
            Panel card = new Panel { BackColor = Color.White, Padding = new Padding(15) };
            card.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(225, 230, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };
            card.Controls.Add(lbl);
            return card;
        }
    }
}