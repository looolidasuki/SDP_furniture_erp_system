using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class DetailViewHelper
    {
        /// <summary>
        /// Returns a copy of <paramref name="source"/> where mapped int columns are string display labels
        /// (avoids assigning text into Int32 columns on the original DataTable).
        /// </summary>
        public static DataTable MapIntColumnsToString(DataTable source, IReadOnlyDictionary<string, Func<int, string>> columnLabelMappers)
        {
            if (source == null) return null;
            if (columnLabelMappers == null || columnLabelMappers.Count == 0) return source.Copy();

            var result = new DataTable();
            foreach (DataColumn col in source.Columns)
            {
                Type columnType = columnLabelMappers.ContainsKey(col.ColumnName) ? typeof(string) : col.DataType;
                result.Columns.Add(col.ColumnName, columnType);
            }

            foreach (DataRow row in source.Rows)
            {
                var newRow = result.NewRow();
                foreach (DataColumn col in source.Columns)
                {
                    object val = row[col];
                    if (columnLabelMappers.TryGetValue(col.ColumnName, out var labelFor)
                        && val != null && val != DBNull.Value
                        && IsIntegerColumn(col))
                    {
                        newRow[col.ColumnName] = labelFor(Convert.ToInt32(val));
                    }
                    else
                    {
                        newRow[col.ColumnName] = val ?? DBNull.Value;
                    }
                }
                result.Rows.Add(newRow);
            }

            return result;
        }

        private static bool IsIntegerColumn(DataColumn col)
        {
            var t = col.DataType;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte);
        }

        public static DataTable SingleRowToFieldValueTable(DataTable singleRowTable)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            if (singleRowTable == null || singleRowTable.Rows.Count == 0) return dt;

            var row = singleRowTable.Rows[0];
            foreach (DataColumn col in singleRowTable.Columns)
            {
                var value = row[col] == DBNull.Value ? "" : row[col]?.ToString();
                dt.Rows.Add(col.ColumnName, value ?? "");
            }
            return dt;
        }

        public static DataTable RowToFieldValueTable(DataGridViewRow row)
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            if (row == null) return dt;

            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.OwningColumn == null) continue;
                dt.Rows.Add(cell.OwningColumn.HeaderText, cell.Value?.ToString() ?? "");
            }
            return dt;
        }

        public static void AttachPrintToolbar(Form form, Func<DocumentExportData> getExportData)
        {
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
            btnPrint.Location = new System.Drawing.Point(toolbar.Width - btnPrint.Width - 16, 8);
            toolbar.Resize += (s, e) => btnPrint.Left = Math.Max(8, toolbar.Width - btnPrint.Width - 16);

            btnPrint.Click += (s, e) =>
            {
                try
                {
                    var data = getExportData?.Invoke();
                    if (data == null)
                    {
                        UITheme.ShowWarning("No data available to print.");
                        return;
                    }

                    if (PdfExportHelper.ExportToPdf(data, form))
                        UITheme.ShowSuccess("PDF saved successfully.");
                }
                catch (Exception ex)
                {
                    UITheme.ShowError("Failed to export PDF: " + ex.Message);
                }
            };

            toolbar.Controls.Add(btnPrint);
            form.Controls.Add(toolbar);
            toolbar.BringToFront();
        }

        public static void ShowKeyValueDetail(Control owner, string title, DataGridViewRow row, DataTable lines = null)
        {
            var fields = RowToFieldValueTable(row);
            ShowDetail(owner, title, fields, lines, PdfExportHelper.SanitizeFileName(title));
        }

        public static void ShowDetail(Control owner, string title, DataTable fields, DataTable lines, string fileNameHint, DataTable paymentLines = null, DataTable grnLines = null, string linesTabTitle = "Order Lines", string paymentTabTitle = "Payment Vouchers", DataTable relatedDocuments = null, string auditDocumentType = null, long auditDocumentId = 0)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                bool hasLines = lines != null;
                bool hasPayments = paymentLines != null;
                bool hasGrns = grnLines != null;
                bool hasBottomPanel = hasLines || hasPayments || hasGrns;
                dlg.Size = hasBottomPanel ? new System.Drawing.Size(920, 620) : new System.Drawing.Size(640, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                Control content;
                if (hasBottomPanel)
                {
                    var split = new SplitContainer
                    {
                        Dock = DockStyle.Fill,
                        Orientation = Orientation.Horizontal,
                        SplitterDistance = 260
                    };

                    var headGrid = GridHelper.CreateStyledGrid();
                    headGrid.DataSource = fields;
                    GridHelper.StyleGrid(headGrid);
                    split.Panel1.Controls.Add(headGrid);

                    int tabCount = (hasLines ? 1 : 0) + (hasPayments ? 1 : 0) + (hasGrns ? 1 : 0);
                    if (tabCount > 1)
                    {
                        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Segoe UI", 9f) };
                        if (hasLines)
                        {
                            var tabLines = new TabPage(string.IsNullOrWhiteSpace(linesTabTitle) ? "Order Lines" : linesTabTitle);
                            var lineGrid = GridHelper.CreateStyledGrid();
                            lineGrid.DataSource = lines;
                            GridHelper.StyleGrid(lineGrid);
                            tabLines.Controls.Add(lineGrid);
                            lineGrid.Dock = DockStyle.Fill;
                            tabs.TabPages.Add(tabLines);
                        }
                        if (hasPayments)
                        {
                            var tabPay = new TabPage(string.IsNullOrWhiteSpace(paymentTabTitle) ? "Payment Vouchers" : paymentTabTitle);
                            var payGrid = GridHelper.CreateStyledGrid();
                            payGrid.DataSource = paymentLines;
                            GridHelper.StyleGrid(payGrid);
                            tabPay.Controls.Add(payGrid);
                            payGrid.Dock = DockStyle.Fill;
                            tabs.TabPages.Add(tabPay);
                        }
                        if (hasGrns)
                        {
                            var tabGrn = new TabPage("Goods Received (GRN)");
                            var grnGrid = GridHelper.CreateStyledGrid();
                            grnGrid.DataSource = grnLines;
                            GridHelper.StyleGrid(grnGrid);
                            tabGrn.Controls.Add(grnGrid);
                            grnGrid.Dock = DockStyle.Fill;
                            tabs.TabPages.Add(tabGrn);
                        }
                        split.Panel2.Controls.Add(tabs);
                    }
                    else if (hasLines)
                    {
                        var lineGrid = GridHelper.CreateStyledGrid();
                        lineGrid.DataSource = lines;
                        GridHelper.StyleGrid(lineGrid);
                        split.Panel2.Controls.Add(lineGrid);
                        lineGrid.Dock = DockStyle.Fill;
                    }
                    else if (hasPayments)
                    {
                        var payGrid = GridHelper.CreateStyledGrid();
                        payGrid.DataSource = paymentLines;
                        GridHelper.StyleGrid(payGrid);
                        split.Panel2.Controls.Add(payGrid);
                        payGrid.Dock = DockStyle.Fill;
                    }
                    else
                    {
                        var grnGrid = GridHelper.CreateStyledGrid();
                        grnGrid.DataSource = grnLines;
                        GridHelper.StyleGrid(grnGrid);
                        split.Panel2.Controls.Add(grnGrid);
                        grnGrid.Dock = DockStyle.Fill;
                    }
                    content = split;
                }
                else
                {
                    var grid = GridHelper.CreateStyledGrid();
                    grid.DataSource = fields;
                    GridHelper.StyleGrid(grid);
                    content = grid;
                }

                content.Dock = DockStyle.Fill;
                Control host = content;
                if (relatedDocuments != null && relatedDocuments.Rows.Count > 0)
                {
                    var outerTabs = new TabControl { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Segoe UI", 9f) };
                    var mainTab = new TabPage("Detail");
                    mainTab.Controls.Add(content);
                    outerTabs.TabPages.Add(mainTab);
                    outerTabs.TabPages.Add(RelatedDocumentsHelper.BuildRelatedDocumentsTab(relatedDocuments, dlg));
                    if (auditDocumentId > 0 && !string.IsNullOrWhiteSpace(auditDocumentType))
                        outerTabs.TabPages.Add(DocumentAuditService.BuildActivityTab(auditDocumentType, auditDocumentId));
                    host = outerTabs;
                }
                else if (auditDocumentId > 0 && !string.IsNullOrWhiteSpace(auditDocumentType))
                {
                    var outerTabs = new TabControl { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Segoe UI", 9f) };
                    var mainTab = new TabPage("Detail");
                    mainTab.Controls.Add(content);
                    outerTabs.TabPages.Add(mainTab);
                    outerTabs.TabPages.Add(DocumentAuditService.BuildActivityTab(auditDocumentType, auditDocumentId));
                    host = outerTabs;
                }
                dlg.Controls.Add(host);

                AttachPrintToolbar(dlg, () => new DocumentExportData
                {
                    Title = title,
                    Fields = fields?.Copy(),
                    Lines = lines?.Copy(),
                    SuggestedFileName = fileNameHint
                });

                dlg.ShowDialog(owner);
            }
        }

        public static DocumentExportData FromFieldValueTable(string title, DataTable fields, DataTable lines = null, string fileName = null)
        {
            return new DocumentExportData
            {
                Title = title,
                Fields = fields?.Copy(),
                Lines = lines?.Copy(),
                SuggestedFileName = fileName ?? PdfExportHelper.SanitizeFileName(title)
            };
        }
    }
}
