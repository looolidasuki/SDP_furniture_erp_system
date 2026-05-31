using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FurnitureERP.Helpers
{
    public class DocumentExportData
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public DataTable Fields { get; set; }
        public DataTable Lines { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public static class PdfExportHelper
    {
        private const double Margin = 40;
        private const double LineHeight = 16;
        private const double TableHeaderHeight = 20;

        public static bool ExportToPdf(DocumentExportData data, IWin32Window owner = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string defaultName = SanitizeFileName(
                string.IsNullOrWhiteSpace(data.SuggestedFileName) ? data.Title : data.SuggestedFileName);
            if (!defaultName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                defaultName += ".pdf";

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF files (*.pdf)|*.pdf";
                sfd.DefaultExt = "pdf";
                sfd.FileName = defaultName;
                sfd.Title = "Save document as PDF";
                if (sfd.ShowDialog(owner) != DialogResult.OK)
                    return false;

                WritePdf(sfd.FileName, data);
                return true;
            }
        }

        public static void WritePdf(string filePath, DocumentExportData data)
        {
            var doc = new PdfDocument();
            doc.Info.Title = data.Title ?? "Document";
            doc.Info.Creator = "Furniture ERP";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);
            double y = Margin;
            double contentWidth = page.Width - Margin * 2;

            var titleFont = new XFont("Segoe UI", 16, XFontStyleEx.Bold);
            var subFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var labelFont = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            var valueFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var tableHeaderFont = new XFont("Segoe UI", 8, XFontStyleEx.Bold);
            var tableCellFont = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
            var sectionFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);

            y = DrawWrapped(gfx, data.Title ?? "Document", titleFont, XBrushes.DarkSlateGray,
                Margin, y, contentWidth) + 6;

            string printedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string subtitle = string.IsNullOrWhiteSpace(data.Subtitle)
                ? "Generated: " + printedAt
                : data.Subtitle + "  |  Generated: " + printedAt;
            y = DrawWrapped(gfx, subtitle, subFont, XBrushes.Gray, Margin, y, contentWidth) + 14;

            if (data.Fields != null && data.Fields.Rows.Count > 0)
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, LineHeight * 2);
                gfx.DrawString("Details", sectionFont, XBrushes.DarkSlateGray, Margin, y);
                y += 20;

                bool twoCol = data.Fields.Columns.Count >= 2
                    && data.Fields.Columns[0].ColumnName.Equals("Field", StringComparison.OrdinalIgnoreCase);

                foreach (DataRow row in data.Fields.Rows)
                {
                    y = EnsureSpace(doc, ref page, ref gfx, y, LineHeight + 4);
                    if (twoCol)
                    {
                        string label = row[0]?.ToString() ?? "";
                        string value = row[1]?.ToString() ?? "";
                        gfx.DrawString(label + ":", labelFont, XBrushes.DarkSlateGray, Margin, y);
                        y = DrawWrapped(gfx, value, valueFont, XBrushes.Black, Margin + 140, y, contentWidth - 140) + 4;
                    }
                    else
                    {
                        var parts = row.ItemArray.Select(v => v?.ToString() ?? "").ToArray();
                        y = DrawWrapped(gfx, string.Join(" | ", parts), valueFont, XBrushes.Black, Margin, y, contentWidth) + 4;
                    }
                }
                y += 10;
            }

            if (data.Lines != null && data.Lines.Columns.Count > 0)
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, TableHeaderHeight + 10);
                gfx.DrawString("Line Items", sectionFont, XBrushes.DarkSlateGray, Margin, y);
                y += 22;

                int colCount = data.Lines.Columns.Count;
                double[] colWidths = CalculateColumnWidths(data.Lines, contentWidth, colCount);

                y = EnsureSpace(doc, ref page, ref gfx, y, TableHeaderHeight);
                double x = Margin;
                for (int c = 0; c < colCount; c++)
                {
                    var rect = new XRect(x, y, colWidths[c], TableHeaderHeight);
                    gfx.DrawRectangle(XBrushes.LightSteelBlue, rect);
                    gfx.DrawString(data.Lines.Columns[c].ColumnName, tableHeaderFont, XBrushes.White,
                        new XRect(x + 3, y + 3, colWidths[c] - 6, TableHeaderHeight - 4), XStringFormats.TopLeft);
                    x += colWidths[c];
                }
                y += TableHeaderHeight;

                foreach (DataRow row in data.Lines.Rows)
                {
                    double rowHeight = TableHeaderHeight;
                    var cellTexts = new string[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        cellTexts[c] = row[c]?.ToString() ?? "";
                        double h = MeasureWrappedHeight(gfx, cellTexts[c], tableCellFont, colWidths[c] - 6);
                        rowHeight = Math.Max(rowHeight, h + 6);
                    }

                    y = EnsureSpace(doc, ref page, ref gfx, y, rowHeight);
                    x = Margin;
                    for (int c = 0; c < colCount; c++)
                    {
                        var rect = new XRect(x, y, colWidths[c], rowHeight);
                        gfx.DrawRectangle(new XPen(XColors.LightGray, 0.5), rect);
                        DrawWrapped(gfx, cellTexts[c], tableCellFont, XBrushes.Black,
                            x + 3, y + 3, colWidths[c] - 6);
                        x += colWidths[c];
                    }
                    y += rowHeight;
                }
            }

            gfx.Dispose();
            doc.Save(filePath);
        }

        private static double[] CalculateColumnWidths(DataTable table, double totalWidth, int colCount)
        {
            var widths = new double[colCount];
            for (int c = 0; c < colCount; c++)
            {
                int maxLen = table.Columns[c].ColumnName.Length;
                foreach (DataRow row in table.Rows)
                {
                    string s = row[c]?.ToString() ?? "";
                    if (s.Length > maxLen) maxLen = Math.Min(s.Length, 40);
                }
                widths[c] = Math.Max(50, maxLen * 5.5);
            }
            double sum = widths.Sum();
            if (sum <= 0) return widths.Select(_ => totalWidth / colCount).ToArray();
            for (int c = 0; c < colCount; c++)
                widths[c] = widths[c] / sum * totalWidth;
            return widths;
        }

        private static double EnsureSpace(PdfDocument doc, ref PdfPage page, ref XGraphics gfx, double y, double needed)
        {
            double bottom = page.Height - Margin;
            if (y + needed <= bottom)
                return y;

            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            return Margin;
        }

        private static double DrawWrapped(XGraphics gfx, string text, XFont font, XBrush brush,
            double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            var lines = WrapText(text, font, gfx, maxWidth);
            foreach (var line in lines)
            {
                gfx.DrawString(line, font, brush, x, y);
                y += LineHeight;
            }
            return y;
        }

        private static double MeasureWrappedHeight(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return LineHeight;
            return WrapText(text, font, gfx, maxWidth).Count * LineHeight;
        }

        private static System.Collections.Generic.List<string> WrapText(string text, XFont font, XGraphics gfx, double maxWidth)
        {
            var result = new System.Collections.Generic.List<string>();
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string remaining = paragraph;
                while (!string.IsNullOrEmpty(remaining))
                {
                    int fit = remaining.Length;
                    while (fit > 0 && gfx.MeasureString(remaining.Substring(0, fit), font).Width > maxWidth)
                        fit--;
                    if (fit == 0) fit = 1;
                    result.Add(remaining.Substring(0, fit).TrimEnd());
                    remaining = remaining.Substring(fit).TrimStart();
                }
            }
            if (result.Count == 0) result.Add("");
            return result;
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "document";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString();
        }
    }
}
