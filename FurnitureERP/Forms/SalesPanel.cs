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
        private readonly ReplySlipController _replySlipCtrl = new ReplySlipController();
        private readonly ProductController _productCtrl = new ProductController();
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly ReceiptVoucherController _receiptCtrl = new ReceiptVoucherController();
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
            // Reply slip is printed from Delivery Note (paired RS-* code); standalone Sales tab retired.

            Controls.Add(_tabs);
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
                EditCustomerRow,
                row => OpenCustomerRow(row),
                null,
                null,
                row => ShowCustomerViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Customer ID")),
                "Customer ID"));
            return page;
        }

        private TabPage BuildQuotationTab()
        {
            var page = new TabPage("Quotations");
            page.Controls.Add(BuildCrudPanel("Quotation", PermissionModule.Quotation,
                () => DictionaryUIHelper.LoadWithStatusLabels(() => _quotationCtrl.GetAllQuotations(), "Status", DictionaryService.Categories.Quotation),
                ShowCreateQuotationDialog,
                row => ShowQuotationDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Quotation ID")),
                row => OpenQuotationRow(row),
                DictionaryService.Categories.Quotation,
                grid =>
                {
                    var extras = new List<Control>();
                    var btnConvert = UITheme.CreateSecondaryButton("Convert to SO");
                    btnConvert.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a quotation first."); return; }
                        ConvertQuotationToSalesOrder(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Quotation ID"));
                    };
                    extras.Add(btnConvert);
                    extras.AddRange(CreateViewProductsToolbarExtras());
                    return extras.ToArray();
                },
                row => ShowQuotationViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Quotation ID")),
                "Quotation ID"));
            return page;
        }

        private TabPage BuildSalesOrderTab()
        {
            var page = new TabPage("Sales Orders");
            page.Controls.Add(BuildCrudPanel("Sales Order", PermissionModule.SalesOrder,
                () => DictionaryUIHelper.LoadWithStatusLabels(() => _salesOrderCtrl.GetAllSalesOrders(), "Status", DictionaryService.Categories.SalesOrder),
                ShowCreateSalesOrderDialog,
                row => ShowSalesOrderDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Order ID")),
                row => OpenSalesOrderRow(row),
                DictionaryService.Categories.SalesOrder,
                grid =>
                {
                    var btnConfirm = UITheme.CreateSecondaryButton("Confirm Order");
                    btnConfirm.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a sales order first."); return; }
                        ConfirmSalesOrder(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Order ID"));
                    };
                    var btnProduction = UITheme.CreateSecondaryButton("Create Production");
                    btnProduction.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a sales order first."); return; }
                        CreateProductionFromSalesOrder(GridHelper.TryGetRowLongId(grid, grid.CurrentRow, "Order ID"));
                    };
                    var extras = new List<Control> { btnConfirm, btnProduction };
                    extras.AddRange(CreateViewProductsToolbarExtras());
                    return extras.ToArray();
                },
                row => ShowSalesOrderViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Order ID")),
                "Order ID"));
            return page;
        }

        private TabPage BuildReplySlipTab()
        {
            var page = new TabPage("Reply Slips");
            page.Controls.Add(BuildCrudPanel("Reply Slip", PermissionModule.ReplySlip,
                () => DictionaryUIHelper.LoadWithStatusLabels(() => _replySlipCtrl.GetAllReplySlips(), "Status", DictionaryService.Categories.ReplySlip),
                ShowCreateReplySlipDialog,
                row => ShowReplySlipDetailDialog(Convert.ToInt64(row.Cells[0].Value)),
                row => OpenReplySlipRow(row),
                DictionaryService.Categories.ReplySlip,
                grid =>
                {
                    var btnPrint = UITheme.CreateSecondaryButton("Print PDF");
                    btnPrint.Click += (s, e) =>
                    {
                        if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a reply slip first."); return; }
                        PrintReplySlipPdf(Convert.ToInt64(grid.CurrentRow.Cells[0].Value));
                    };
                    return new Control[] { btnPrint };
                },
                row => ShowReplySlipViewDetailDialog(Convert.ToInt64(row.Cells[0].Value))));
            return page;
        }

        private void OpenReplySlipRow(DataGridViewRow row)
        {
            ShowReplySlipViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Reply Slip ID"));
        }

        private void ShowReplySlipViewDetailDialog(long replySlipId)
        {
            var export = BuildReplySlipExportData(replySlipId);
            if (export == null)
            {
                UITheme.ShowWarning("Reply slip not found.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"Reply Slip Detail — {export.SlipCode}";
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 260
                };

                var headerGrid = GridHelper.CreateStyledGrid();
                headerGrid.DataSource = export.Fields;
                GridHelper.StyleGrid(headerGrid);

                var lineGrid = GridHelper.CreateStyledGrid();
                lineGrid.DataSource = export.Lines;
                GridHelper.StyleGrid(lineGrid);

                split.Panel1.Controls.Add(headerGrid);
                split.Panel2.Controls.Add(lineGrid);
                dlg.Controls.Add(split);

                var toolbar = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    Padding = new Padding(8, 8, 16, 8),
                    BackColor = UITheme.Background
                };
                var btnPrint = UITheme.CreatePrimaryButton("Print PDF");
                btnPrint.Width = 130;
                btnPrint.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnPrint.Location = new Point(toolbar.Width - btnPrint.Width - 16, 8);
                toolbar.Resize += (s, e) => btnPrint.Left = Math.Max(8, toolbar.Width - btnPrint.Width - 16);
                btnPrint.Click += (s, e) =>
                {
                    try
                    {
                        var pdfData = BuildReplySlipPdfData(replySlipId);
                        if (pdfData == null)
                        {
                            UITheme.ShowWarning("No data available to print.");
                            return;
                        }
                        if (ReplySlipPdfHelper.ExportToPdf(pdfData, dlg))
                            UITheme.ShowSuccess("PDF saved successfully.");
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError("Failed to export PDF: " + ex.Message);
                    }
                };
                toolbar.Controls.Add(btnPrint);
                dlg.Controls.Add(toolbar);
                toolbar.BringToFront();

                dlg.ShowDialog(this);
            }
        }

        private void PrintReplySlipPdf(long replySlipId)
        {
            try
            {
                var pdfData = BuildReplySlipPdfData(replySlipId);
                if (pdfData == null)
                {
                    UITheme.ShowWarning("Reply slip not found.");
                    return;
                }

                if (ReplySlipPdfHelper.ExportToPdf(pdfData, this))
                    UITheme.ShowSuccess("PDF saved successfully.");
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to export PDF: " + ex.Message);
            }
        }

        private ReplySlipPdfData BuildReplySlipPdfData(long replySlipId)
        {
            DataTable header = null;
            DataTable lines = null;
            if (!TryLoadReplySlipDetail(replySlipId, out header, out lines, out decimal total))
                return null;

            return ReplySlipPdfHelper.FromHeaderAndLines(header, lines, total, $"ReplySlip_{replySlipId}");
        }

        private bool TryLoadReplySlipDetail(long replySlipId, out DataTable header, out DataTable lines, out decimal total)
        {
            header = null;
            lines = null;
            total = 0;

            try { header = _replySlipCtrl.GetHeaderDetail(replySlipId); } catch { }
            if (header == null || header.Rows.Count == 0)
                return false;

            try { lines = _replySlipCtrl.GetProductLinesDetailed(replySlipId); } catch { }
            try { total = _replySlipCtrl.GetTotalAmount(replySlipId); } catch { }
            return true;
        }

        private ReplySlipExportData BuildReplySlipExportData(long replySlipId)
        {
            if (!TryLoadReplySlipDetail(replySlipId, out DataTable header, out DataTable lines, out decimal total))
                return null;

            AppendReplySlipTotalRow(lines, total);

            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            DecorateReplySlipStatusField(fields, header.Rows[0]);
            try { fields.Rows.Add("Total Amount", total.ToString("0.00")); } catch { }

            string slipCode = header.Columns.Contains("Reply Slip Code")
                ? header.Rows[0]["Reply Slip Code"]?.ToString()
                : ("RS-" + replySlipId);

            return new ReplySlipExportData
            {
                SlipCode = slipCode ?? ("RS-" + replySlipId),
                Fields = fields,
                Lines = lines
            };
        }

        private static void DecorateReplySlipStatusField(DataTable fields, DataRow headerRow)
        {
            if (fields == null || headerRow == null || !headerRow.Table.Columns.Contains("Status")) return;
            if (headerRow["Status"] == DBNull.Value) return;

            int statusCode = Convert.ToInt32(headerRow["Status"]);
            string label = DictionaryService.GetDisplayName(DictionaryService.Categories.ReplySlip, statusCode);
            foreach (DataRow row in fields.Rows)
            {
                if (string.Equals(row["Field"]?.ToString(), "Status", StringComparison.OrdinalIgnoreCase))
                {
                    row["Value"] = label;
                    break;
                }
            }
        }

        private static void AppendReplySlipTotalRow(DataTable lines, decimal total)
        {
            if (lines == null || !lines.Columns.Contains("Amount")) return;

            var totalRow = lines.NewRow();
            foreach (DataColumn col in lines.Columns)
            {
                if (col.ColumnName == "Product Code") totalRow[col] = "Total Amount";
                else if (col.ColumnName == "Amount") totalRow[col] = total;
                else if (col.DataType == typeof(string)) totalRow[col] = "";
                else totalRow[col] = DBNull.Value;
            }
            lines.Rows.Add(totalRow);
        }

        private sealed class ReplySlipExportData
        {
            public string SlipCode { get; set; }
            public DataTable Fields { get; set; }
            public DataTable Lines { get; set; }
        }

        private void OpenQuotationRow(DataGridViewRow row)
        {
            ShowQuotationViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Quotation ID"));
        }

        private void ShowQuotationDetailDialog(long id)
        {
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

                var btnEdit = UITheme.CreateSecondaryButton("Edit");
                btnEdit.Click += (s, e) =>
                {
                    dlg.Hide();
                    ShowEditQuotationDialog(id);
                    dlg.Close();
                };
                PermissionGuard.ApplyEditButton(btnEdit, PermissionModule.Quotation);

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
                btnPanel.Controls.Add(btnEdit);
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

        private void ConfirmSalesOrder(long salesOrderId)
        {
            if (!PermissionGuard.Ensure(PermissionModule.SalesOrder, PermissionAction.Edit, this)) return;
            var order = _salesOrderCtrl.GetFullById(salesOrderId);
            if (order == null) { UITheme.ShowWarning("Sales order not found."); return; }
            if (order.Status != 0)
            {
                UITheme.ShowWarning("Only draft sales orders can be confirmed.");
                return;
            }
            if (MessageBox.Show(
                    "Confirm this sales order and reserve available finished-goods stock?",
                    "Confirm Order",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var result = _salesWorkflow.ConfirmSalesOrder(salesOrderId);
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
            BuildUIRefresh();
        }

        private void OpenCustomerRow(DataGridViewRow row)
        {
            long customerId = GridHelper.TryGetRowLongId(row.DataGridView, row, "Customer ID");
            if (customerId <= 0) return;
            if (AppSession.CanEdit(PermissionModule.Customer))
            {
                var customer = _customerCtrl.GetById(customerId);
                if (customer != null) ShowCustomerFormDialog(customer);
            }
            else
            {
                ShowCustomerViewDetailDialog(customerId);
            }
        }

        private void EditCustomerRow(DataGridViewRow row)
        {
            long customerId = GridHelper.TryGetRowLongId(row.DataGridView, row, "Customer ID");
            if (customerId <= 0) return;
            var customer = _customerCtrl.GetById(customerId);
            if (customer == null) return;
            ShowCustomerFormDialog(customer);
        }

        private void OpenSalesOrderRow(DataGridViewRow row)
        {
            ShowSalesOrderViewDetailDialog(GridHelper.TryGetRowLongId(row.DataGridView, row, "Order ID"));
        }

        private Panel BuildCrudPanel(string entity, string permissionModule, Func<DataTable> loadData, Action onCreate, Action<DataGridViewRow> onEdit, Action<DataGridViewRow> onRowOpen, string statusCategory = null, Func<DataGridView, Control[]> extraControlsFactory = null, Action<DataGridViewRow> onViewDetail = null, string idColumnName = null)
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
            Button btnEdit = UITheme.CreateSecondaryButton("Edit");
            btnEdit.Location = new Point(btnDetail.Right + 10, 8);
            btnEdit.Click += (s, e) =>
            {
                if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a record first."); return; }
                if (!PermissionGuard.Ensure(permissionModule, PermissionAction.Edit, this)) return;
                onEdit?.Invoke(grid.CurrentRow);
            };
            PermissionGuard.ApplyEditButton(btnEdit, permissionModule);
            btnDetail.Click += (s, e) =>
            {
                if (grid.CurrentRow == null) { UITheme.ShowWarning("Please select a record first."); return; }
                if (onViewDetail != null) onViewDetail(grid.CurrentRow);
                else ShowGenericDetailDialog($"{entity} Details", grid.CurrentRow);
            };

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                onRowOpen?.Invoke(grid.Rows[e.RowIndex]);
            };
            btnRefresh.Click += (s, e) => {
                try { grid.DataSource = loadData(); GridHelper.StyleGrid(grid); } catch { }
            };

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnDetail);
            toolbar.Controls.Add(btnEdit);
            if (extraControlsFactory != null)
            {
                int x = btnEdit.Right + 10;
                foreach (var extra in extraControlsFactory(grid))
                {
                    extra.Location = new Point(x, 8);
                    toolbar.Controls.Add(extra);
                    x = extra.Right + 10;
                }
            }

            var filterBox = FilterBlockHelper.CreateFilterBlock(grid, $"{entity} Filters");

            try { grid.DataSource = loadData(); GridHelper.StyleGrid(grid); } catch { }

            panel.Controls.Add(grid);
            panel.Controls.Add(filterBox);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private void ShowSalesOrderViewDetailDialog(long salesOrderId)
        {
            DataTable header = null;
            DataTable lines = null;
            try { header = _salesOrderCtrl.GetHeaderDetail(salesOrderId); } catch { }
            try { lines = _salesOrderCtrl.GetProductLinesDetailed(salesOrderId); } catch { }

            decimal total = 0;
            try { total = _salesOrderCtrl.GetTotalAmount(salesOrderId); } catch { }

            string remark = "";
            try
            {
                if (header != null && header.Rows.Count > 0 && header.Columns.Contains("Remark"))
                    remark = header.Rows[0]["Remark"]?.ToString() ?? "";
            }
            catch { }

            AppendSalesOrderTotalRow(lines, total);

            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            if (fields != null)
            {
                var remarkRows = fields.Select("Field = 'Remark'");
                foreach (var r in remarkRows) r.Delete();
                fields.AcceptChanges();
            }

            string code = header?.Rows.Count > 0 && header.Columns.Contains("Order Code")
                ? header.Rows[0]["Order Code"]?.ToString()
                : salesOrderId.ToString();
            ShowDocumentTabbedViewDetail(
                $"Sales Order — {code}",
                fields,
                lines,
                $"SalesOrder_{salesOrderId}",
                "Order",
                "Product Lines",
                string.IsNullOrWhiteSpace(remark) ? "—" : remark);
        }

        private void ShowQuotationViewDetailDialog(long quotationId)
        {
            DataTable header = null;
            DataTable lines = null;
            try { header = _quotationCtrl.GetHeaderDetail(quotationId); } catch { }
            try { lines = _quotationCtrl.GetProductLinesDetailed(quotationId); } catch { }

            var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
            try
            {
                fields.Rows.Add("Total Amount", _quotationCtrl.GetTotalAmount(quotationId).ToString("0.00"));
            }
            catch { }

            string code = header?.Rows.Count > 0 && header.Columns.Contains("Quotation Code")
                ? header.Rows[0]["Quotation Code"]?.ToString()
                : quotationId.ToString();
            ShowDocumentTabbedViewDetail(
                $"Quotation — {code}",
                fields,
                lines,
                $"Quotation_{quotationId}",
                "Quotation",
                "Product Lines");
        }

        private void ShowDocumentTabbedViewDetail(string title, DataTable fields, DataTable lines, string fileNameHint, string headerTabTitle, string linesTabTitle, string linesRemark = null)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };

                var tabHeader = new TabPage(headerTabTitle);
                var headerGrid = GridHelper.CreateStyledGrid();
                headerGrid.DataSource = fields;
                GridHelper.StyleGrid(headerGrid);
                headerGrid.Dock = DockStyle.Fill;
                tabHeader.Controls.Add(headerGrid);
                tabs.TabPages.Add(tabHeader);

                if (lines != null)
                {
                    var tabLines = new TabPage(linesTabTitle);
                    Control linesContent;
                    if (linesRemark != null)
                    {
                        var lineLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                        lineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                        lineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

                        var lineGrid = GridHelper.CreateStyledGrid();
                        lineGrid.DataSource = lines;
                        GridHelper.StyleGrid(lineGrid);
                        lineGrid.Dock = DockStyle.Fill;

                        var lblRemark = new Label
                        {
                            Dock = DockStyle.Fill,
                            AutoSize = false,
                            TextAlign = ContentAlignment.TopLeft,
                            Padding = new Padding(12, 10, 12, 0),
                            Font = new Font("Segoe UI", 9),
                            ForeColor = UITheme.TextDark,
                            Text = "Remark: " + linesRemark
                        };

                        lineLayout.Controls.Add(lineGrid, 0, 0);
                        lineLayout.Controls.Add(lblRemark, 0, 1);
                        linesContent = lineLayout;
                    }
                    else
                    {
                        var lineGrid = GridHelper.CreateStyledGrid();
                        lineGrid.DataSource = lines;
                        GridHelper.StyleGrid(lineGrid);
                        lineGrid.Dock = DockStyle.Fill;
                        linesContent = lineGrid;
                    }

                    tabLines.Controls.Add(linesContent);
                    linesContent.Dock = DockStyle.Fill;
                    tabs.TabPages.Add(tabLines);
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

                DetailViewHelper.AttachPrintToolbar(dlg, () =>
                    DetailViewHelper.FromFieldValueTable(title, fields?.Copy(), lines?.Copy(), fileNameHint));

                dlg.ShowDialog(this);
            }
        }

        private static void AppendSalesOrderTotalRow(DataTable lines, decimal total)
        {
            if (lines == null || !lines.Columns.Contains("Amount")) return;

            var totalRow = lines.NewRow();
            foreach (DataColumn col in lines.Columns)
            {
                if (col.ColumnName == "Item") totalRow[col] = "Total Amount";
                else if (col.ColumnName == "Amount") totalRow[col] = total;
                else if (col.DataType == typeof(string)) totalRow[col] = "";
                else totalRow[col] = DBNull.Value;
            }
            lines.Rows.Add(totalRow);
        }

        private IEnumerable<Control> CreateViewProductsToolbarExtras()
        {
            if (!AppSession.CanView(PermissionModule.Product))
                yield break;
            yield return CreateViewProductsButton(this);
        }

        private static Button CreateViewProductsButton(Control owner)
        {
            var btn = UITheme.CreateSecondaryButton("View Products");
            btn.Click += (s, e) => ProductionPanel.ShowProductsViewerDialog(owner);
            return btn;
        }

        private Panel WrapEditableProductGrid(DataGridView grid, string hint, Control owner)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 36 };
            int x = 0;
            if (AppSession.CanView(PermissionModule.Product))
            {
                var btnViewProducts = CreateViewProductsButton(owner);
                btnViewProducts.Location = new Point(x, 4);
                topBar.Controls.Add(btnViewProducts);
                x = btnViewProducts.Right + 12;
            }

            var lbl = new Label
            {
                Text = hint,
                Location = new Point(x, 10),
                AutoSize = true,
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f)
            };
            topBar.Controls.Add(lbl);

            grid.Dock = DockStyle.Fill;
            panel.Controls.Add(grid);
            panel.Controls.Add(topBar);
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

        private void ShowCustomerViewDetailDialog(long customerId)
        {
            var customer = _customerCtrl.GetById(customerId);
            if (customer == null)
            {
                UITheme.ShowWarning("Customer not found.");
                return;
            }

            string title = string.IsNullOrWhiteSpace(customer.CustomerCode)
                ? $"Customer — {customer.CustomerName}"
                : $"Customer — {customer.CustomerCode}";

            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(960, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
                tabs.TabPages.Add(BuildCustomerProfileViewTab(customer));
                tabs.TabPages.Add(BuildCustomerContactsViewTab(customerId));
                tabs.TabPages.Add(BuildCustomerAddressesViewTab(customerId));

                if (AppSession.CanView(PermissionModule.DeliveryNote))
                    tabs.TabPages.Add(BuildCustomerDeliveryNotesViewTab(customerId));

                if (AppSession.CanView(PermissionModule.ReceiptVoucher))
                    tabs.TabPages.Add(BuildCustomerReceiptVouchersViewTab(customerId));

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

        private TabPage BuildCustomerProfileViewTab(Customer customer)
        {
            var tab = new TabPage("Customer");
            var grid = GridHelper.CreateStyledGrid();
            grid.DataSource = BuildCustomerProfileFields(customer);
            GridHelper.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            tab.Controls.Add(grid);
            return tab;
        }

        private static DataTable BuildCustomerProfileFields(Customer customer)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            AddCustomerFieldRow(dt, "Customer Code", customer.CustomerCode);
            AddCustomerFieldRow(dt, "Customer Ref Number", customer.CustomerRefNumber);
            AddCustomerFieldRow(dt, "Customer Name", customer.CustomerName);
            AddCustomerFieldRow(dt, "Billing Address", customer.BillingAddress);
            AddCustomerFieldRow(dt, "Payment Term", customer.PaymentTerm);
            return dt;
        }

        private static void AddCustomerFieldRow(DataTable dt, string field, string value)
        {
            dt.Rows.Add(field, value ?? "");
        }

        private TabPage BuildCustomerContactsViewTab(long customerId)
        {
            var tab = new TabPage("Contact Persons");
            var grid = CreateReadOnlyGrid();
            grid.DataSource = BuildContactsReadOnlyTable(_customerCtrl.GetContactPersons(customerId));
            GridHelper.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            tab.Controls.Add(grid);
            return tab;
        }

        private TabPage BuildCustomerAddressesViewTab(long customerId)
        {
            var tab = new TabPage("Delivery Addresses");
            var grid = CreateReadOnlyGrid();
            grid.DataSource = BuildDeliveryAddressesReadOnlyTable(_customerCtrl.GetDeliveryAddresses(customerId));
            GridHelper.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            tab.Controls.Add(grid);
            return tab;
        }

        private static DataTable BuildContactsReadOnlyTable(IEnumerable<ContactPerson> contacts)
        {
            var dt = new DataTable();
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Title");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var contact in contacts)
                dt.Rows.Add(contact.Name, contact.Title, contact.Phone, contact.Email);
            return dt;
        }

        private static DataTable BuildDeliveryAddressesReadOnlyTable(IEnumerable<CustomerDeliveryAddress> addresses)
        {
            var dt = new DataTable();
            dt.Columns.Add("Delivery Address");
            dt.Columns.Add("Contact Person");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Email");
            foreach (var addr in addresses)
                dt.Rows.Add(addr.DeliveryAddress, addr.ContactPerson, addr.Phone, addr.Email);
            return dt;
        }

        private TabPage BuildCustomerDeliveryNotesViewTab(long customerId)
        {
            var tab = new TabPage("Delivery Notes");
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 220
            };

            var listGrid = CreateReadOnlyGrid();
            DataTable dnList = null;
            try
            {
                dnList = _deliveryCtrl.GetByCustomer(customerId);
                dnList = DictionaryService.DecorateStatusColumn(dnList, "Status", DictionaryService.Categories.Delivery);
            }
            catch { }
            listGrid.DataSource = dnList;
            GridHelper.StyleGrid(listGrid);
            if (listGrid.Columns.Contains("Delivery Note ID"))
                listGrid.Columns["Delivery Note ID"].Visible = false;
            if (listGrid.Columns.Contains("Status"))
                listGrid.Columns["Status"].Visible = false;

            var detailSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200
            };
            var headerGrid = CreateReadOnlyGrid();
            var linesGrid = CreateReadOnlyGrid();
            detailSplit.Panel1.Controls.Add(headerGrid);
            detailSplit.Panel2.Controls.Add(linesGrid);
            headerGrid.Dock = DockStyle.Fill;
            linesGrid.Dock = DockStyle.Fill;

            listGrid.SelectionChanged += (s, e) =>
            {
                if (listGrid.CurrentRow?.Cells["Delivery Note ID"]?.Value == null)
                {
                    headerGrid.DataSource = null;
                    linesGrid.DataSource = null;
                    return;
                }
                long dnId = Convert.ToInt64(listGrid.CurrentRow.Cells["Delivery Note ID"].Value);
                LoadCustomerDeliveryNoteDetail(dnId, headerGrid, linesGrid);
            };

            split.Panel1.Controls.Add(listGrid);
            split.Panel2.Controls.Add(detailSplit);
            tab.Controls.Add(split);

            if (listGrid.Rows.Count > 0)
            {
                listGrid.Rows[0].Selected = true;
                long dnId = Convert.ToInt64(listGrid.Rows[0].Cells["Delivery Note ID"].Value);
                LoadCustomerDeliveryNoteDetail(dnId, headerGrid, linesGrid);
            }

            return tab;
        }

        private TabPage BuildCustomerReceiptVouchersViewTab(long customerId)
        {
            var tab = new TabPage("Receipt Vouchers");
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 220
            };

            var listGrid = CreateReadOnlyGrid();
            DataTable rvList = null;
            try { rvList = _receiptCtrl.GetByCustomer(customerId); } catch { }
            listGrid.DataSource = rvList;
            GridHelper.StyleGrid(listGrid);
            if (listGrid.Columns.Contains("ID"))
                listGrid.Columns["ID"].Visible = false;
            if (listGrid.Columns.Contains("Status"))
                listGrid.Columns["Status"].Visible = false;

            var detailSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200
            };
            var headerGrid = CreateReadOnlyGrid();
            var linesGrid = CreateReadOnlyGrid();
            detailSplit.Panel1.Controls.Add(headerGrid);
            detailSplit.Panel2.Controls.Add(linesGrid);
            headerGrid.Dock = DockStyle.Fill;
            linesGrid.Dock = DockStyle.Fill;

            listGrid.SelectionChanged += (s, e) =>
            {
                if (listGrid.CurrentRow?.Cells["ID"]?.Value == null)
                {
                    headerGrid.DataSource = null;
                    linesGrid.DataSource = null;
                    return;
                }
                long rvId = Convert.ToInt64(listGrid.CurrentRow.Cells["ID"].Value);
                LoadCustomerReceiptVoucherDetail(rvId, headerGrid, linesGrid);
            };

            split.Panel1.Controls.Add(listGrid);
            split.Panel2.Controls.Add(detailSplit);
            tab.Controls.Add(split);

            if (listGrid.Rows.Count > 0)
            {
                listGrid.Rows[0].Selected = true;
                long rvId = Convert.ToInt64(listGrid.Rows[0].Cells["ID"].Value);
                LoadCustomerReceiptVoucherDetail(rvId, headerGrid, linesGrid);
            }

            return tab;
        }

        private void LoadCustomerDeliveryNoteDetail(long deliveryNoteId, DataGridView headerGrid, DataGridView linesGrid)
        {
            try
            {
                DataTable header = _deliveryCtrl.GetHeaderDetail(deliveryNoteId);
                DataTable lines = _deliveryCtrl.GetExportProductLines(deliveryNoteId);
                decimal total = _deliveryCtrl.GetTotalAmount(deliveryNoteId);
                int totalShipQty = _deliveryCtrl.GetTotalShipQty(deliveryNoteId);

                AppendCustomerDeliveryNoteTotalRow(lines, total);
                var fields = DetailViewHelper.SingleRowToFieldValueTable(header);
                DecorateCustomerDeliveryNoteStatusField(fields, header?.Rows.Count > 0 ? header.Rows[0] : null);
                if (fields != null)
                {
                    fields.Rows.Add("Total Ship Qty", totalShipQty.ToString());
                    fields.Rows.Add("Total Amount", total.ToString("0.00"));
                }

                headerGrid.DataSource = fields;
                linesGrid.DataSource = lines;
                GridHelper.StyleGrid(headerGrid);
                GridHelper.StyleGrid(linesGrid);
            }
            catch
            {
                headerGrid.DataSource = null;
                linesGrid.DataSource = null;
            }
        }

        private void LoadCustomerReceiptVoucherDetail(long receiptVoucherId, DataGridView headerGrid, DataGridView linesGrid)
        {
            try
            {
                var header = _receiptCtrl.GetHeaderDetail(receiptVoucherId);
                var lines = _receiptCtrl.GetInvoiceAllocationsDetailed(receiptVoucherId);
                if (lines != null && lines.Columns.Contains("Invoice ID"))
                    lines.Columns.Remove("Invoice ID");

                headerGrid.DataSource = DetailViewHelper.SingleRowToFieldValueTable(header);
                linesGrid.DataSource = lines;
                GridHelper.StyleGrid(headerGrid);
                GridHelper.StyleGrid(linesGrid);
            }
            catch
            {
                headerGrid.DataSource = null;
                linesGrid.DataSource = null;
            }
        }

        private static void DecorateCustomerDeliveryNoteStatusField(DataTable fields, DataRow headerRow)
        {
            if (fields == null || headerRow == null || !headerRow.Table.Columns.Contains("Status")) return;
            if (headerRow["Status"] == DBNull.Value) return;

            int statusCode = Convert.ToInt32(headerRow["Status"]);
            string label = DictionaryService.GetDisplayName(DictionaryService.Categories.Delivery, statusCode);
            foreach (DataRow row in fields.Rows)
            {
                if (string.Equals(row["Field"]?.ToString(), "Status", StringComparison.OrdinalIgnoreCase))
                {
                    row["Value"] = label;
                    break;
                }
            }
        }

        private static void AppendCustomerDeliveryNoteTotalRow(DataTable lines, decimal total)
        {
            if (lines == null || !lines.Columns.Contains("Amount")) return;

            var totalRow = lines.NewRow();
            foreach (DataColumn col in lines.Columns)
            {
                if (col.ColumnName == "Product Code") totalRow[col] = "Total Amount";
                else if (col.ColumnName == "Amount") totalRow[col] = total;
                else if (col.DataType == typeof(string)) totalRow[col] = "";
                else totalRow[col] = DBNull.Value;
            }
            lines.Rows.Add(totalRow);
        }

        private static DataGridView CreateReadOnlyGrid()
        {
            var grid = GridHelper.CreateStyledGrid();
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            return grid;
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

                var generalPage = new TabPage("General") { AutoScroll = true };
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 2,
                    RowCount = 5,
                    Padding = new Padding(16),
                    Width = 700
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

                var txtName = new TextBox { Text = existing?.CustomerName ?? "" };
                var txtCode = new TextBox { Text = existing?.CustomerCode ?? "", ReadOnly = existing != null };
                var txtRef = new TextBox { Text = existing?.CustomerRefNumber ?? "" };
                var txtAddr = new TextBox
                {
                    Text = existing?.BillingAddress ?? "",
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                var cmbTerm = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                DictionaryUIHelper.BindPaymentTermCombo(cmbTerm, existing?.PaymentTerm);

                var toolTip = new ToolTip();
                UITheme.AddFormRow(layout, 0, "Customer Code", txtCode);
                UITheme.AddFormRow(layout, 1, "Customer Ref No.", txtRef);
                toolTip.SetToolTip(txtRef, "Format: PO-PL-#########");
                UITheme.AddFormRow(layout, 2, "Customer Name *", txtName);
                UITheme.AddFormRow(layout, 3, "Billing Address", txtAddr);
                UITheme.AddFormRow(layout, 4, "Payment Term", cmbTerm);
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
                            existing.CustomerCode = txtCode.Text.Trim();
                            existing.CustomerRefNumber = txtRef.Text.Trim();
                            existing.CustomerName = txtName.Text.Trim();
                            existing.BillingAddress = txtAddr.Text.Trim();
                            existing.PaymentTerm = DictionaryUIHelper.GetSelectedPaymentTerm(cmbTerm);
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
                                CustomerCode = txtCode.Text.Trim(),
                                CustomerRefNumber = txtRef.Text.Trim(),
                                CustomerName = txtName.Text.Trim(),
                                BillingAddress = txtAddr.Text.Trim(),
                                PaymentTerm = DictionaryUIHelper.GetSelectedPaymentTerm(cmbTerm)
                            });
                            if (string.IsNullOrWhiteSpace(txtCode.Text))
                                _customerCtrl.UpdateCodeAfterInsert(customerId);
                            if (string.IsNullOrWhiteSpace(txtRef.Text))
                                _customerCtrl.UpdateRefNumberAfterInsert(customerId);
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
            var so = _salesOrderCtrl.GetFullById(id);
            if (so == null) return;
            ShowSalesOrderFormDialog(so);
        }

        private void BuildUIRefresh()
        {
            Controls.Clear();
            BuildUI();
        }

        private void ShowCreateQuotationDialog()
        {
            if (!PermissionGuard.Ensure(PermissionModule.Quotation, PermissionAction.Create, this)) return;
            ShowQuotationFormDialog(null);
        }

        private void ShowEditQuotationDialog(long quotationId)
        {
            if (!PermissionGuard.Ensure(PermissionModule.Quotation, PermissionAction.Edit, this)) return;
            var quotation = _quotationCtrl.GetById(quotationId);
            if (quotation == null) return;
            ShowQuotationFormDialog(quotation);
        }

        private void ShowQuotationFormDialog(Quotation existing)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Quotation" : "New Quotation";
                dlg.Size = new Size(860, 560);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var cmbCustomer = BuildCustomerCombo();
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Quotation, existing?.Status ?? 0);
                var txtRemark = new TextBox { Multiline = true, Height = 70, Text = existing?.Remark ?? string.Empty };
                var lblStaff = new Label { Text = AppSession.CurrentUser?.Username ?? "Current User", AutoSize = true, ForeColor = UITheme.TextDark };
                var lblCurrency = new Label { Text = "Default Currency", AutoSize = true, ForeColor = UITheme.TextDark };
                if (isEdit) SetComboValue(cmbCustomer, existing.CustomerID);
                UITheme.AddFormRow(form, 0, "Customer *", cmbCustomer);
                UITheme.AddFormRow(form, 1, "Staff", lblStaff);
                UITheme.AddFormRow(form, 2, "Currency", lblCurrency);
                UITheme.AddFormRow(form, 3, "Status / Remark", BuildStatusRemarkRow(cmbStatus, txtRemark));

                var lineGrid = BuildEditableQuotationLineGrid();
                if (isEdit) LoadProductLinesToGrid(lineGrid, _quotationCtrl.GetProductLinesInternal(existing.QuotationID));

                root.Controls.Add(form, 0, 0);
                root.Controls.Add(WrapEditableProductGrid(lineGrid, "Pick multiple products and set quantity/price/discount.", dlg), 0, 1);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Create");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isEdit ? PermissionAction.Edit : PermissionAction.Create;
                    if (!PermissionGuard.Ensure(PermissionModule.Quotation, action, dlg)) return;
                    long customerId = GetComboId(cmbCustomer);
                    if (customerId <= 0)
                    {
                        UITheme.ShowWarning("Please select a customer.");
                        return;
                    }
                    var lines = ReadProductLinesFromGrid(lineGrid);
                    if (lines.Count == 0)
                    {
                        UITheme.ShowWarning("Please add at least one product line.");
                        return;
                    }
                    try
                    {
                        if (isEdit)
                        {
                            existing.CustomerID = customerId;
                            existing.StaffID = AppSession.CurrentUser?.StaffID ?? 1;
                            existing.CurrencyID = 1;
                            existing.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                            existing.Remark = txtRemark.Text.Trim();
                            _quotationCtrl.UpdateStatus(existing.QuotationID, existing.Status);
                            _quotationCtrl.ReplaceProductLines(existing.QuotationID, lines);
                        }
                        else
                        {
                            var q = new Quotation
                            {
                                QuotationCode = "QT-TEMP",
                                CustomerID = customerId,
                                StaffID = AppSession.CurrentUser?.StaffID ?? 1,
                                CurrencyID = 1,
                                Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus),
                                Remark = txtRemark.Text.Trim(),
                                SequenceNumber = 1
                            };
                            long id = _quotationCtrl.Insert(q);
                            _quotationCtrl.UpdateCodeAfterInsert(id);
                            _quotationCtrl.ReplaceProductLines(id, lines);
                        }
                        UITheme.ShowSuccess(isEdit ? "Quotation updated." : "Quotation created.");
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
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) BuildUIRefresh();
            }
        }

        private void ShowCreateSalesOrderDialog()
        {
            if (!PermissionGuard.Ensure(PermissionModule.SalesOrder, PermissionAction.Create, this)) return;
            ShowSalesOrderFormDialog(null);
        }

        private void ShowCreateReplySlipDialog()
        {
            if (!PermissionGuard.Ensure(PermissionModule.ReplySlip, PermissionAction.Create, this)) return;
            ShowReplySlipFormDialog(null);
        }

        private void ShowReplySlipDetailDialog(long id)
        {
            var slip = _replySlipCtrl.GetById(id);
            if (slip == null) return;
            ShowReplySlipFormDialog(slip);
        }

        private void ShowReplySlipFormDialog(ReplySlip existing)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Reply Slip" : "New Reply Slip";
                dlg.Size = new Size(900, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 8 };
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbSalesOrder = BuildSalesOrderCombo();
                var cmbCustomer = BuildCustomerCombo();
                var txtSignedBy = new TextBox { Text = existing?.SignedBy ?? "" };
                var dtpSignedDate = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = existing?.SignedDate.HasValue ?? false };
                if (existing?.SignedDate.HasValue == true) dtpSignedDate.Value = existing.SignedDate.Value;
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.ReplySlip, existing?.Status ?? 0);
                var txtRemark = new TextBox { Multiline = true, Height = 70, Text = existing?.Remark ?? string.Empty };
                var lblStaff = new Label { Text = AppSession.CurrentUser?.Username ?? "Current User", AutoSize = true, ForeColor = UITheme.TextDark };
                var lblCurrency = new Label { Text = "Default Currency", AutoSize = true, ForeColor = UITheme.TextDark };

                if (isEdit)
                {
                    SetComboValue(cmbSalesOrder, existing.SalesOrderID);
                    SetComboValue(cmbCustomer, existing.CustomerID);
                }

                cmbSalesOrder.SelectedIndexChanged += (s, e) =>
                {
                    if (!(cmbSalesOrder.SelectedItem is DataRowView rowView)) return;
                    if (!rowView.Row.Table.Columns.Contains("Customer ID")) return;
                    long cid = Convert.ToInt64(rowView["Customer ID"]);
                    SetComboValue(cmbCustomer, cid);
                };

                UITheme.AddFormRow(form, 0, "Sales Order *", cmbSalesOrder);
                UITheme.AddFormRow(form, 1, "Customer *", cmbCustomer);
                UITheme.AddFormRow(form, 2, "Staff", lblStaff);
                UITheme.AddFormRow(form, 3, "Currency", lblCurrency);
                UITheme.AddFormRow(form, 4, "Signed By / Date", BuildSignerRow(txtSignedBy, dtpSignedDate));
                UITheme.AddFormRow(form, 5, "Status", cmbStatus);
                UITheme.AddFormRow(form, 6, "Remark", txtRemark);

                var lineGrid = BuildEditableProductLineGrid();
                if (isEdit) LoadProductLinesToGrid(lineGrid, _replySlipCtrl.GetProductLinesInternal(existing.ReplySlipID));

                root.Controls.Add(form, 0, 0);
                root.Controls.Add(WrapEditableGrid(lineGrid, "Pick products and signed quantities for this reply slip."), 0, 1);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Create");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isEdit ? PermissionAction.Edit : PermissionAction.Create;
                    if (!PermissionGuard.Ensure(PermissionModule.ReplySlip, action, dlg)) return;
                    long soId = GetComboId(cmbSalesOrder);
                    long customerId = GetComboId(cmbCustomer);
                    if (soId <= 0 || customerId <= 0)
                    {
                        UITheme.ShowWarning("Please select sales order and customer.");
                        return;
                    }
                    var lines = ReadProductLinesFromGrid(lineGrid);
                    if (lines.Count == 0)
                    {
                        UITheme.ShowWarning("Please add at least one product line.");
                        return;
                    }
                    try
                    {
                        if (isEdit)
                        {
                            existing.SalesOrderID = soId;
                            existing.CustomerID = customerId;
                            existing.SignedBy = txtSignedBy.Text.Trim();
                            existing.SignedDate = dtpSignedDate.Checked ? (DateTime?)dtpSignedDate.Value.Date : null;
                            existing.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                            existing.Remark = txtRemark.Text.Trim();
                            if (!_replySlipCtrl.Update(existing))
                            {
                                UITheme.ShowWarning("Failed to update reply slip.");
                                return;
                            }
                            _replySlipCtrl.ReplaceProductLines(existing.ReplySlipID, lines);
                        }
                        else
                        {
                            var slip = new ReplySlip
                            {
                                ReplySlipCode = "RS-TEMP",
                                SalesOrderID = soId,
                                CustomerID = customerId,
                                StaffID = AppSession.CurrentUser?.StaffID ?? 1,
                                CurrencyID = 1,
                                SignedBy = txtSignedBy.Text.Trim(),
                                SignedDate = dtpSignedDate.Checked ? (DateTime?)dtpSignedDate.Value.Date : null,
                                Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus),
                                Remark = txtRemark.Text.Trim()
                            };
                            long id = _replySlipCtrl.Insert(slip);
                            _replySlipCtrl.UpdateCodeAfterInsert(id);
                            _replySlipCtrl.ReplaceProductLines(id, lines);
                        }

                        UITheme.ShowSuccess(isEdit ? "Reply slip updated." : "Reply slip created.");
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
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) BuildUIRefresh();
            }
        }

        private void ShowSalesOrderFormDialog(SalesOrder existing)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Sales Order" : "New Sales Order";
                dlg.Size = new Size(900, 600);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var form = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 7 };
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var cmbCustomer = BuildCustomerCombo();
                var cmbDeliveryAddress = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDown,
                    Width = 340
                };
                var dtpRequestedDelivery = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Value = existing?.RequestedDeliveryDate ?? DateTime.Today
                };
                var txtDiscount = new TextBox { Text = (existing?.Discount ?? 0).ToString() };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                if (isEdit)
                    DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.SalesOrder, existing?.Status ?? 0);
                else
                {
                    DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.SalesOrder, 0);
                    cmbStatus.Enabled = false;
                }
                var txtRemark = new TextBox { Multiline = true, Height = 70, Text = existing?.Remark ?? string.Empty };
                var lblStaff = new Label { Text = AppSession.CurrentUser?.Username ?? "Current User", AutoSize = true, ForeColor = UITheme.TextDark };
                var lblCurrency = new Label { Text = "Default Currency", AutoSize = true, ForeColor = UITheme.TextDark };
                var cmbCustomerRefNumber = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDown,
                    Width = 340
                };

                Action<long> loadDeliveryAddresses = customerId =>
                {
                    try
                    {
                        var addrs = _customerCtrl.GetDeliveryAddresses(customerId);
                        var commonFromSo = _salesOrderCtrl.GetCommonDeliveryAddressesByCustomer(customerId);
                        var dt = new DataTable();
                        dt.Columns.Add("DisplayText", typeof(string));
                        dt.Columns.Add("DeliveryAddress", typeof(string));
                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var a in addrs)
                        {
                            var addr = (a.DeliveryAddress ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(addr) || !seen.Add(addr)) continue;
                            var display = DeliveryAddressDisplayHelper.FormatDisplay(addr, a.ContactPerson, a.Phone);
                            dt.Rows.Add(display, addr);
                        }

                        if (commonFromSo != null && commonFromSo.Columns.Contains("Delivery Address"))
                        {
                            foreach (DataRow r in commonFromSo.Rows)
                            {
                                var raw = r["Delivery Address"]?.ToString()?.Trim();
                                if (string.IsNullOrWhiteSpace(raw)) continue;
                                var addr = DeliveryAddressDisplayHelper.TryParseCombined(raw, out string onlyAddr, out _, out _)
                                    ? onlyAddr
                                    : raw;
                                if (string.IsNullOrWhiteSpace(addr) || !seen.Add(addr)) continue;
                                dt.Rows.Add(addr, addr);
                            }
                        }

                        if (dt.Rows.Count == 0)
                        {
                            dt.Rows.Add(existing?.DeliveryAddress ?? "", existing?.DeliveryAddress ?? "");
                        }

                        cmbDeliveryAddress.DataSource = dt;
                        cmbDeliveryAddress.DisplayMember = "DisplayText";
                        cmbDeliveryAddress.ValueMember = "DeliveryAddress";
                        if (isEdit && !string.IsNullOrWhiteSpace(existing?.DeliveryAddress))
                        {
                            var addrOnly = existing.DeliveryAddress;
                            if (DeliveryAddressDisplayHelper.TryParseCombined(addrOnly, out string parsedAddr, out _, out _))
                                addrOnly = parsedAddr;
                            try { cmbDeliveryAddress.SelectedValue = addrOnly; }
                            catch { cmbDeliveryAddress.Text = addrOnly; }
                            if (cmbDeliveryAddress.SelectedIndex < 0)
                                cmbDeliveryAddress.Text = addrOnly;
                        }
                    }
                    catch { }
                };

                Action<long> loadCustomerRefNumbers = customerId =>
                {
                    try
                    {
                        var dt = new DataTable();
                        dt.Columns.Add("Value", typeof(string));
                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var refs = _salesOrderCtrl.GetCommonCustomerRefNumbersByCustomer(customerId);
                        if (refs != null && refs.Columns.Contains("Customer Ref Number"))
                        {
                            foreach (DataRow r in refs.Rows)
                            {
                                var v = r["Customer Ref Number"]?.ToString()?.Trim();
                                if (string.IsNullOrWhiteSpace(v) || !seen.Add(v)) continue;
                                dt.Rows.Add(v);
                            }
                        }

                        cmbCustomerRefNumber.DataSource = dt;
                        cmbCustomerRefNumber.DisplayMember = "Value";
                        cmbCustomerRefNumber.ValueMember = "Value";
                        if (isEdit && !string.IsNullOrWhiteSpace(existing?.CustomerRefNumber))
                            cmbCustomerRefNumber.Text = existing.CustomerRefNumber;
                    }
                    catch { }
                };

                if (isEdit) SetComboValue(cmbCustomer, existing.CustomerID);
                if (isEdit)
                {
                    loadDeliveryAddresses(existing.CustomerID);
                    loadCustomerRefNumbers(existing.CustomerID);
                }

                cmbCustomer.SelectedIndexChanged += (s, e) =>
                {
                    long cid = GetComboId(cmbCustomer);
                    if (cid > 0)
                    {
                        loadDeliveryAddresses(cid);
                        loadCustomerRefNumbers(cid);
                    }
                };

                // For newly created order, ensure delivery address list is populated
                // if ComboBox default-selected customer exists.
                if (!isEdit)
                {
                    long cid = GetComboId(cmbCustomer);
                    if (cid > 0)
                    {
                        loadDeliveryAddresses(cid);
                        loadCustomerRefNumbers(cid);
                    }
                }

                UITheme.AddFormRow(form, 0, "Customer *", cmbCustomer);
                UITheme.AddFormRow(form, 1, "Staff", lblStaff);
                UITheme.AddFormRow(form, 2, "Currency", lblCurrency);
                UITheme.AddFormRow(form, 3, "Delivery Address *", cmbDeliveryAddress);
                UITheme.AddFormRow(form, 4, "Requested Delivery Date", dtpRequestedDelivery);
                UITheme.AddFormRow(form, 5, "Discount", txtDiscount);
                UITheme.AddFormRow(form, 6, "Customer Ref Number (PO-PL-#########)", cmbCustomerRefNumber);
                UITheme.AddFormRow(form, 7, "Status / Remark", BuildStatusRemarkRow(cmbStatus, txtRemark));

                var lineGrid = BuildEditableSalesOrderLineGrid();
                if (isEdit) LoadProductLinesToGrid(lineGrid, _salesOrderCtrl.GetProductLinesInternal(existing.SalesOrderID));

                root.Controls.Add(form, 0, 0);
                root.Controls.Add(WrapEditableProductGrid(lineGrid, "Pick multiple products and set quantity/price/discount.", dlg), 0, 1);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Create");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    var action = isEdit ? PermissionAction.Edit : PermissionAction.Create;
                    if (!PermissionGuard.Ensure(PermissionModule.SalesOrder, action, dlg)) return;
                    long customerId = GetComboId(cmbCustomer);
                    if (customerId <= 0)
                    {
                        UITheme.ShowWarning("Please select a customer.");
                        return;
                    }
                    var selectedDeliveryAddress = DeliveryAddressDisplayHelper.ResolveAddressOnly(
                        cmbDeliveryAddress.SelectedValue?.ToString(),
                        cmbDeliveryAddress.Text);
                    if (string.IsNullOrWhiteSpace(selectedDeliveryAddress))
                    {
                        UITheme.ShowWarning("Delivery address is required.");
                        return;
                    }
                    if (!decimal.TryParse(txtDiscount.Text.Trim(), out decimal discount))
                    {
                        UITheme.ShowWarning("Discount must be numeric.");
                        return;
                    }
                    var lines = ReadProductLinesFromGrid(lineGrid);
                    if (lines.Count == 0)
                    {
                        UITheme.ShowWarning("Please add at least one product line.");
                        return;
                    }
                    try
                    {
                        if (isEdit)
                        {
                            existing.CustomerID = customerId;
                            existing.DeliveryAddress = selectedDeliveryAddress.Trim();
                            existing.RequestedDeliveryDate = dtpRequestedDelivery.Value.Date;
                            existing.CustomerRefNumber = (cmbCustomerRefNumber.Text ?? "").Trim();
                            existing.Discount = discount;
                            existing.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                            existing.Remark = txtRemark.Text.Trim();
                            var validateResult = _salesWorkflow.ValidateSalesOrderStatus(existing.SalesOrderID, existing.Status);
                            if (!validateResult.Success)
                            {
                                UITheme.ShowWarning(validateResult.Message);
                                return;
                            }
                            if (!_salesOrderCtrl.Update(existing))
                            {
                                UITheme.ShowWarning("Failed to update sales order.");
                                return;
                            }
                            _salesOrderCtrl.ReplaceProductLines(existing.SalesOrderID, lines);
                        }
                        else
                        {
                            var so = new SalesOrder
                            {
                                SalesOrderCode = "SO-TEMP",
                                CustomerID = customerId,
                                StaffID = AppSession.CurrentUser?.StaffID ?? 1,
                                CurrencyCurrencyID = 1,
                                DeliveryAddress = selectedDeliveryAddress.Trim(),
                                RequestedDeliveryDate = dtpRequestedDelivery.Value.Date,
                                CustomerRefNumber = (cmbCustomerRefNumber.Text ?? "").Trim(),
                                Discount = discount,
                                Status = 0,
                                Remark = txtRemark.Text.Trim()
                            };
                            long id = _salesOrderCtrl.Insert(so);
                            _salesOrderCtrl.UpdateCodeAfterInsert(id);
                            if (string.IsNullOrWhiteSpace((cmbCustomerRefNumber.Text ?? "").Trim()))
                                _salesOrderCtrl.UpdateCustomerRefNumberAfterInsert(id);
                            _salesOrderCtrl.ReplaceProductLines(id, lines);
                        }
                        UITheme.ShowSuccess(isEdit ? "Sales order updated." : "Sales order created.");
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
                dlg.Controls.Add(root);
                dlg.Controls.Add(btnPanel);
                if (dlg.ShowDialog(this) == DialogResult.OK) BuildUIRefresh();
            }
        }

        private ComboBox BuildCustomerCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dt = _customerCtrl.GetAllCustomers();
            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                string code = dt.Columns.Contains("Customer Code") ? row["Customer Code"]?.ToString() : "";
                string refNo = dt.Columns.Contains("Customer Ref Number") ? row["Customer Ref Number"]?.ToString() : "";
                string name = row["Customer Name"]?.ToString();
                string prefix = "";
                if (!string.IsNullOrWhiteSpace(code)) prefix += code;
                if (!string.IsNullOrWhiteSpace(refNo)) prefix += (prefix.Length > 0 ? " / " : "") + refNo;
                row["DisplayText"] = string.IsNullOrWhiteSpace(prefix) ? name : (prefix + " - " + name);
            }
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Customer ID";
            return cmb;
        }

        private ComboBox BuildSalesOrderCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            var dt = _salesOrderCtrl.GetAllSalesOrders();
            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in dt.Rows)
                row["DisplayText"] = row["Order Code"]?.ToString();
            cmb.DataSource = dt;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Order ID";
            return cmb;
        }

        private static FlowLayoutPanel BuildSignerRow(TextBox txtSignedBy, DateTimePicker dtpSignedDate)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            txtSignedBy.Width = 220;
            dtpSignedDate.Width = 160;
            row.Controls.Add(txtSignedBy);
            row.Controls.Add(dtpSignedDate);
            return row;
        }

        private static FlowLayoutPanel BuildStatusRemarkRow(ComboBox cmbStatus, TextBox txtRemark)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            cmbStatus.Width = 180;
            txtRemark.Width = 420;
            row.Controls.Add(cmbStatus);
            row.Controls.Add(txtRemark);
            return row;
        }

        private DataGridView BuildEditableProductLineGrid()
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

            var products = _productCtrl.GetProductsForPicker();
            if (!products.Columns.Contains("DisplayText"))
                products.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in products.Rows)
                row["DisplayText"] = row["Product Code"]?.ToString();

            var productCol = new DataGridViewComboBoxColumn
            {
                Name = "ProductID",
                HeaderText = "Product",
                DataSource = products,
                DisplayMember = "DisplayText",
                ValueMember = "Product ID",
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(productCol);
            grid.Columns.Add("AvailableStock", "Available Stock");
            grid.Columns["AvailableStock"].ReadOnly = true;
            grid.Columns.Add("Price", "Price");
            grid.Columns.Add("Quantity", "Quantity");
            grid.Columns.Add("Discount", "Discount");
            GridHelper.ApplyStyle(grid);
            grid.Columns["ProductID"].Visible = true;
            return grid;
        }

        private DataGridView BuildEditableQuotationLineGrid()
        {
            var grid = BuildEditableProductLineGrid();

            grid.EditingControlShowing += (s, e) =>
            {
                if (grid.CurrentCell == null || grid.CurrentCell.OwningColumn == null) return;
                if (!string.Equals(grid.CurrentCell.OwningColumn.Name, "ProductID", StringComparison.OrdinalIgnoreCase)) return;
                if (!(e.Control is ComboBox cb)) return;

                cb.DropDownStyle = ComboBoxStyle.DropDown;
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
                cb.SelectionChangeCommitted += (_, __) =>
                {
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    if (grid.CurrentCell != null)
                        ApplyQuotationLineDefaults(grid, grid.CurrentCell.RowIndex);
                };
            };

            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (!string.Equals(grid.Columns[e.ColumnIndex].Name, "ProductID", StringComparison.OrdinalIgnoreCase)) return;
                ApplyQuotationLineDefaults(grid, e.RowIndex);
            };
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.DataError += (s, e) => { e.ThrowException = false; };

            return grid;
        }

        private void ApplyQuotationLineDefaults(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex];
            if (row.IsNewRow) return;

            if (TryGetProductBasePrice(grid, row.Cells["ProductID"].Value, out decimal basePrice))
                row.Cells["Price"].Value = basePrice;

            ApplySalesOrderAvailableStock(grid, rowIndex);
        }

        private DataGridView BuildEditableSalesOrderLineGrid()
        {
            var grid = BuildEditableProductLineGrid();

            // Sales order requires product picker to support typing + dropdown.
            grid.EditingControlShowing += (s, e) =>
            {
                if (grid.CurrentCell == null || grid.CurrentCell.OwningColumn == null) return;
                if (!string.Equals(grid.CurrentCell.OwningColumn.Name, "ProductID", StringComparison.OrdinalIgnoreCase)) return;
                if (!(e.Control is ComboBox cb)) return;

                cb.DropDownStyle = ComboBoxStyle.DropDown;
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
            };

            // Existing product unit price should be fixed in SO lines.
            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (!string.Equals(grid.Columns[e.ColumnIndex].Name, "ProductID", StringComparison.OrdinalIgnoreCase)) return;
                ApplySalesOrderFixedUnitPrice(grid, e.RowIndex);
                ApplySalesOrderAvailableStock(grid, e.RowIndex);
            };
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.RowsAdded += (s, e) =>
            {
                for (int i = 0; i < e.RowCount; i++)
                    EnsureSalesOrderPriceCellState(grid, e.RowIndex + i);
            };
            grid.DataError += (s, e) =>
            {
                // Ignore temporary mismatch while user is typing in combo box.
                e.ThrowException = false;
            };

            for (int i = 0; i < grid.Rows.Count; i++)
                EnsureSalesOrderPriceCellState(grid, i);

            return grid;
        }

        private void ApplySalesOrderFixedUnitPrice(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex];
            if (row.IsNewRow) return;

            if (!TryGetProductBasePrice(grid, row.Cells["ProductID"].Value, out decimal basePrice))
            {
                EnsureSalesOrderPriceCellState(grid, rowIndex);
                return;
            }

            row.Cells["Price"].Value = basePrice;
            EnsureSalesOrderPriceCellState(grid, rowIndex);
        }

        private void EnsureSalesOrderPriceCellState(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex];
            if (row.IsNewRow) return;
            row.Cells["Price"].ReadOnly = TryGetProductBasePrice(grid, row.Cells["ProductID"].Value, out _);
        }

        private bool TryGetProductBasePrice(DataGridView grid, object productCellValue, out decimal basePrice)
        {
            basePrice = 0m;
            if (productCellValue == null || productCellValue == DBNull.Value) return false;
            if (!long.TryParse(productCellValue.ToString(), out long productId) || productId <= 0) return false;

            if (!(grid.Columns["ProductID"] is DataGridViewComboBoxColumn comboCol)) return false;
            if (!(comboCol.DataSource is DataTable dt)) return false;
            if (!dt.Columns.Contains("Product ID") || !dt.Columns.Contains("Base Price")) return false;

            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Product ID"] == DBNull.Value) continue;
                if (Convert.ToInt64(dr["Product ID"]) != productId) continue;
                basePrice = dr["Base Price"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Base Price"]);
                return true;
            }
            return false;
        }

        private void LoadProductLinesToGrid(DataGridView grid, DataTable lines)
        {
            if (lines == null) return;
            grid.Rows.Clear();
            foreach (DataRow row in lines.Rows)
            {
                decimal discount = row.Table.Columns.Contains("discountAmount") && row["discountAmount"] != DBNull.Value
                    ? Convert.ToDecimal(row["discountAmount"])
                    : 0;
                decimal qty = row.Table.Columns.Contains("quantity")
                    ? Convert.ToDecimal(row["quantity"])
                    : Convert.ToDecimal(row["orderQuantity"]);
                long productId = Convert.ToInt64(row["productID"]);
                string available = TryGetAvailableStockFromPicker(grid, productId);
                if (grid.Columns.Contains("AvailableStock"))
                    grid.Rows.Add(productId, available, Convert.ToDecimal(row["price"]), qty, discount);
                else
                    grid.Rows.Add(productId, Convert.ToDecimal(row["price"]), qty, discount);
            }
        }

        private static string TryGetAvailableStockFromPicker(DataGridView grid, long productId)
        {
            if (!grid.Columns.Contains("ProductID")) return "0";
            var productCol = grid.Columns["ProductID"] as DataGridViewComboBoxColumn;
            var dt = productCol?.DataSource as DataTable;
            if (dt == null || !dt.Columns.Contains("Available Stock")) return "0";
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Product ID"] == DBNull.Value) continue;
                if (Convert.ToInt64(dr["Product ID"]) != productId) continue;
                return dr["Available Stock"] == DBNull.Value ? "0" : Convert.ToDecimal(dr["Available Stock"]).ToString("N2");
            }
            return "0";
        }

        private void ApplySalesOrderAvailableStock(DataGridView grid, int rowIndex)
        {
            if (!grid.Columns.Contains("AvailableStock") || !grid.Columns.Contains("ProductID")) return;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var cell = grid.Rows[rowIndex].Cells["ProductID"];
            if (cell?.Value == null) return;
            if (!long.TryParse(cell.Value.ToString(), out long productId) || productId <= 0) return;
            grid.Rows[rowIndex].Cells["AvailableStock"].Value = TryGetAvailableStockFromPicker(grid, productId);
        }

        private static List<(long ProductID, decimal Price, decimal Quantity, decimal Discount)> ReadProductLinesFromGrid(DataGridView grid)
        {
            var list = new List<(long ProductID, decimal Price, decimal Quantity, decimal Discount)>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["ProductID"].Value == null) continue;
                if (!long.TryParse(row.Cells["ProductID"].Value.ToString(), out long productId) || productId <= 0) continue;
                decimal.TryParse(row.Cells["Price"].Value?.ToString(), out decimal price);
                decimal.TryParse(row.Cells["Quantity"].Value?.ToString(), out decimal quantity);
                decimal.TryParse(row.Cells["Discount"].Value?.ToString(), out decimal discount);
                if (quantity <= 0) continue;
                list.Add((productId, price, quantity, discount));
            }
            return list;
        }

        private static void SetComboValue(ComboBox cmb, long value)
        {
            try { cmb.SelectedValue = value; }
            catch { }
        }

        private static long GetComboId(ComboBox cmb)
        {
            if (cmb?.SelectedValue == null) return 0;
            long.TryParse(cmb.SelectedValue.ToString(), out long id);
            return id;
        }
    }
}
