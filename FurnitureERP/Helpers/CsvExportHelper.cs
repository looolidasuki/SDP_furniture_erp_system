using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class CsvExportHelper
    {
        public static bool ExportDataGridView(DataGridView grid, string defaultFileName, IWin32Window owner = null)
        {
            if (grid == null) return false;
            var table = GridHelper.CopyGridToDataTable(grid);
            return ExportDataTable(table, defaultFileName, owner);
        }

        public static bool ExportDataTable(DataTable table, string defaultFileName, IWin32Window owner = null)
        {
            if (table == null || table.Columns.Count == 0)
            {
                UITheme.ShowWarning("Nothing to export.");
                return false;
            }

            string fileName = PdfExportHelper.SanitizeFileName(defaultFileName ?? "export");
            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                fileName += ".csv";

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV files (*.csv)|*.csv";
                sfd.DefaultExt = "csv";
                sfd.FileName = fileName;
                sfd.Title = "Export to Excel (CSV)";
                if (sfd.ShowDialog(owner) != DialogResult.OK)
                    return false;

                WriteCsv(sfd.FileName, table);
                UITheme.ShowSuccess("Exported to " + sfd.FileName);
                return true;
            }
        }

        public static void WriteCsv(string filePath, DataTable table)
        {
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                var headers = new string[table.Columns.Count];
                for (int c = 0; c < table.Columns.Count; c++)
                    headers[c] = EscapeCsv(table.Columns[c].ColumnName);
                writer.WriteLine(string.Join(",", headers));

                foreach (DataRow row in table.Rows)
                {
                    if (row.RowState == DataRowState.Deleted) continue;
                    var cells = new string[table.Columns.Count];
                    for (int c = 0; c < table.Columns.Count; c++)
                        cells[c] = EscapeCsv(FormatCell(row[c]));
                    writer.WriteLine(string.Join(",", cells));
                }
            }
        }

        private static string FormatCell(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm");
            return value.ToString();
        }

        private static string EscapeCsv(string text)
        {
            text = text ?? "";
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            return text;
        }
    }
}
