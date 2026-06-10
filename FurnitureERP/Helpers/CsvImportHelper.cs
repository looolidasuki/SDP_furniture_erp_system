using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class CsvImportHelper
    {
        public static string SampleImportFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleData", "Import");

        public static bool TryPickCsvFile(IWin32Window owner, out string filePath)
        {
            filePath = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                ofd.Title = "Select CSV file to import";
                if (ofd.ShowDialog(owner) != DialogResult.OK)
                    return false;
                filePath = ofd.FileName;
                return !string.IsNullOrWhiteSpace(filePath);
            }
        }

        public static DataTable ReadCsvFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found.", filePath);

            var lines = ReadAllLinesWithEncoding(filePath);
            if (lines.Count == 0)
                return new DataTable();

            var headers = ParseCsvLine(lines[0]);
            if (headers.Count == 0)
                throw new InvalidDataException("CSV header row is empty.");

            var table = new DataTable();
            foreach (var header in headers)
            {
                string name = NormalizeHeader(header);
                if (string.IsNullOrWhiteSpace(name))
                    name = "Column" + (table.Columns.Count + 1);
                if (table.Columns.Contains(name))
                    name = name + "_" + (table.Columns.Count + 1);
                table.Columns.Add(name, typeof(string));
            }

            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var cells = ParseCsvLine(lines[i]);
                var row = table.NewRow();
                for (int c = 0; c < table.Columns.Count; c++)
                    row[c] = c < cells.Count ? (cells[c] ?? "") : "";
                if (IsEmptyDataRow(row))
                    continue;
                table.Rows.Add(row);
            }

            return table;
        }

        public static void RevealSampleFolder(IWin32Window owner)
        {
            string folder = SampleImportFolder;
            if (!Directory.Exists(folder))
            {
                UITheme.ShowWarning("Sample folder not found:\n" + folder);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Cannot open folder: " + ex.Message);
            }
        }

        private static List<string> ReadAllLinesWithEncoding(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Encoding encoding = DetectEncoding(bytes);
            string text = encoding.GetString(bytes);
            var lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            return lines;
        }

        private static Encoding DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(true);
            return new UTF8Encoding(false);
        }

        private static string NormalizeHeader(string header) =>
            (header ?? "").Trim().Replace(" ", "");

        private static bool IsEmptyDataRow(DataRow row)
        {
            foreach (DataColumn col in row.Table.Columns)
            {
                if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
                    return false;
            }
            return true;
        }

        public static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
            {
                result.Add("");
                return result;
            }

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }

            result.Add(sb.ToString());
            return result;
        }

        public static string GetCell(DataRow row, params string[] columnNames)
        {
            if (row == null) return "";
            foreach (var name in columnNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (DataColumn col in row.Table.Columns)
                {
                    if (string.Equals(col.ColumnName, name, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(NormalizeHeader(col.ColumnName), NormalizeHeader(name), StringComparison.OrdinalIgnoreCase))
                    {
                        return row[col]?.ToString()?.Trim() ?? "";
                    }
                }
            }
            return "";
        }
    }
}
