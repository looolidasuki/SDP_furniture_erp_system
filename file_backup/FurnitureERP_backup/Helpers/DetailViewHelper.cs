using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class DetailViewHelper
    {
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
            foreach (Control c in form.Controls)
            {
                if (c.Dock == DockStyle.Fill || c is DataGridView || c is SplitContainer)
                    c.Dock = DockStyle.Fill;
            }

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
            btnPrint.Location = new System.Drawing.Point(toolbar.Width - 150, 8);
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

        public static void ShowDetail(Control owner, string title, DataTable fields, DataTable lines, string fileNameHint)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = lines != null ? new System.Drawing.Size(760, 520) : new System.Drawing.Size(640, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                Control content;
                if (lines != null)
                {
                    var split = new SplitContainer
                    {
                        Dock = DockStyle.Fill,
                        Orientation = Orientation.Horizontal,
                        SplitterDistance = 220
                    };
                    var headGrid = GridHelper.CreateStyledGrid();
                    headGrid.DataSource = fields;
                    GridHelper.StyleGrid(headGrid);
                    var lineGrid = GridHelper.CreateStyledGrid();
                    lineGrid.DataSource = lines;
                    GridHelper.StyleGrid(lineGrid);
                    split.Panel1.Controls.Add(headGrid);
                    split.Panel2.Controls.Add(lineGrid);
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
                dlg.Controls.Add(content);

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
