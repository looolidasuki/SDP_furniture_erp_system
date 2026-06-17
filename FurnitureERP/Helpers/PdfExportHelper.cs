using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FurnitureERP.Helpers
{
    public class PdfChartImage
    {
        public string Title { get; set; }
        public Image Image { get; set; }
        /// <summary>When true, PDF frame uses a square plot area (better for pie charts).</summary>
        public bool PreferSquareFrame { get; set; }
    }

    public class DocumentExportData
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public DataTable Fields { get; set; }
        public DataTable Lines { get; set; }
        public List<PdfChartImage> Charts { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public static class PdfExportHelper
    {
        private const double Margin = 40;
        private const double TitleTopPadding = 6;
        private const double LineHeight = 16;
        private const double TableHeaderHeight = 22;
        private const double TableCellPadding = 5;
        private const double SectionTitleHeight = 22;
        private const double SectionGap = 20;

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
            double y = Margin + TitleTopPadding;
            double contentWidth = page.Width.Point - Margin * 2;

            var titleFont = new XFont("Segoe UI", 16, XFontStyleEx.Bold);
            var subFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var labelFont = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            var valueFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var tableHeaderFont = new XFont("Segoe UI", 8, XFontStyleEx.Bold);
            var tableCellFont = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
            var sectionFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);

            y = DrawWrapped(gfx, data.Title ?? "Document", titleFont, XBrushes.DarkSlateGray,
                Margin, y, contentWidth) + 8;

            string printedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string subtitle = string.IsNullOrWhiteSpace(data.Subtitle)
                ? "Generated: " + printedAt
                : data.Subtitle + "  |  Generated: " + printedAt;
            y = DrawWrapped(gfx, subtitle, subFont, XBrushes.Gray, Margin, y, contentWidth) + 14;

            if (data.Fields != null && data.Fields.Rows.Count > 0)
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, SectionTitleHeight + LineHeight);
                y = DrawSectionTitle(gfx, "Details", sectionFont, Margin, contentWidth, y);

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
                y += SectionGap;
                y = EnsureSpace(doc, ref page, ref gfx, y, SectionTitleHeight + TableHeaderHeight + 10);
                y = DrawSectionTitle(gfx, "Line Items", sectionFont, Margin, contentWidth, y);

                int colCount = data.Lines.Columns.Count;
                double[] colWidths = CalculateColumnWidths(data.Lines, contentWidth, colCount);

                y = EnsureSpace(doc, ref page, ref gfx, y, TableHeaderHeight);
                double x = Margin;
                for (int c = 0; c < colCount; c++)
                {
                    var rect = new XRect(x, y, colWidths[c], TableHeaderHeight);
                    gfx.DrawRectangle(XBrushes.LightSteelBlue, rect);
                    DrawTableCell(gfx, data.Lines.Columns[c].ColumnName, tableHeaderFont, XBrushes.White,
                        rect, IsNumericColumn(data.Lines.Columns[c]));
                    x += colWidths[c];
                }
                y += TableHeaderHeight;

                foreach (DataRow row in data.Lines.Rows)
                {
                    double rowHeight = TableHeaderHeight;
                    var cellTexts = new string[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        cellTexts[c] = FormatTableCellValue(row[c], data.Lines.Columns[c]);
                        double innerW = colWidths[c] - TableCellPadding * 2;
                        double h = MeasureWrappedHeight(gfx, cellTexts[c], tableCellFont, innerW);
                        rowHeight = Math.Max(rowHeight, h + TableCellPadding * 2);
                    }

                    y = EnsureSpace(doc, ref page, ref gfx, y, rowHeight);
                    x = Margin;
                    for (int c = 0; c < colCount; c++)
                    {
                        var rect = new XRect(x, y, colWidths[c], rowHeight);
                        gfx.DrawRectangle(new XPen(XColors.LightGray, 0.5), rect);
                        DrawTableCell(gfx, cellTexts[c], tableCellFont, XBrushes.Black,
                            rect, IsNumericColumn(data.Lines.Columns[c]));
                        x += colWidths[c];
                    }
                    y += rowHeight;
                }
                y += SectionGap;
            }

            if (data.Charts != null && data.Charts.Count > 0)
            {
                y = DrawChartSection(doc, ref page, ref gfx, y, contentWidth, data.Charts, sectionFont);
            }

            gfx.Dispose();
            doc.Save(filePath);

            if (data.Charts != null)
            {
                foreach (var chart in data.Charts)
                    chart.Image?.Dispose();
            }
        }

        private static double DrawChartSection(PdfDocument doc, ref PdfPage page, ref XGraphics gfx, double y,
            double contentWidth, List<PdfChartImage> charts, XFont sectionFont)
        {
            var chartTitleFont = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
            const double gap = 14;
            const double titleHeight = 20;
            const double rowGap = 20;
            double colWidth = (contentWidth - gap) / 2;

            double firstRowNeed = titleHeight + GetChartFrameHeight(charts[0], colWidth) + rowGap;
            if (charts.Count > 1)
                firstRowNeed = Math.Max(firstRowNeed,
                    titleHeight + GetChartFrameHeight(charts[1], colWidth) + rowGap);
            y += SectionGap;
            y = EnsureSpace(doc, ref page, ref gfx, y, SectionTitleHeight + firstRowNeed + 8);
            y = DrawSectionTitle(gfx, "Charts", sectionFont, Margin, contentWidth, y);

            for (int i = 0; i < charts.Count; i += 2)
            {
                var left = charts[i];
                var right = i + 1 < charts.Count ? charts[i + 1] : null;

                double leftFrameH = GetChartFrameHeight(left, colWidth);
                double rightFrameH = right != null ? GetChartFrameHeight(right, colWidth) : 0;
                double rowFrameH = Math.Max(leftFrameH, rightFrameH);
                double leftTitleH = MeasureChartTitleHeight(gfx, left, colWidth, chartTitleFont, titleHeight);
                double rightTitleH = right != null
                    ? MeasureChartTitleHeight(gfx, right, colWidth, chartTitleFont, titleHeight)
                    : 0;
                double maxTitleH = Math.Max(leftTitleH, rightTitleH);
                double rowHeight = maxTitleH + 4 + rowFrameH + rowGap;

                y = EnsureSpace(doc, ref page, ref gfx, y, rowHeight);
                double rowY = y;

                DrawChartBlock(gfx, left, Margin, rowY, colWidth, maxTitleH, rowFrameH, chartTitleFont);
                if (right != null)
                    DrawChartBlock(gfx, right, Margin + colWidth + gap, rowY, colWidth, maxTitleH, rowFrameH, chartTitleFont);

                y += rowHeight;
            }

            return y;
        }

        private static double GetChartFrameHeight(PdfChartImage chart, double colWidth)
        {
            if (chart?.PreferSquareFrame == true)
                return colWidth;
            if (chart?.Image != null && chart.Image.Width > 0 && chart.Image.Height > 0)
            {
                double ratio = (double)chart.Image.Width / chart.Image.Height;
                return Math.Max(150, Math.Min(210, colWidth / ratio));
            }
            return 185;
        }

        private static double MeasureChartTitleHeight(XGraphics gfx, PdfChartImage chart, double colWidth,
            XFont chartTitleFont, double minHeight)
        {
            string title = string.IsNullOrWhiteSpace(chart?.Title) ? "Chart" : chart.Title;
            return Math.Max(minHeight, MeasureWrappedHeight(gfx, title, chartTitleFont, colWidth));
        }

        private static void DrawChartBlock(XGraphics gfx, PdfChartImage chart, double x, double rowY,
            double colWidth, double titleSlotHeight, double frameHeight, XFont chartTitleFont)
        {
            string title = string.IsNullOrWhiteSpace(chart?.Title) ? "Chart" : chart.Title;
            gfx.DrawString(title, chartTitleFont, XBrushes.DarkSlateGray,
                new XRect(x, rowY, colWidth, titleSlotHeight), XStringFormats.TopLeft);

            var frame = new XRect(x, rowY + titleSlotHeight + 4, colWidth, frameHeight);
            gfx.DrawRectangle(new XPen(XColors.LightGray, 0.8), frame);

            if (chart?.Image != null)
            {
                using (var ms = new MemoryStream())
                {
                    chart.Image.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    using (var xImage = XImage.FromStream(ms))
                        DrawImagePreserveAspect(gfx, xImage, frame, 6);
                }
            }
            else
            {
                gfx.DrawString("No chart data", new XFont("Segoe UI", 8, XFontStyleEx.Italic),
                    XBrushes.Gray, new XRect(frame.X + 8, frame.Y, frame.Width - 16, frame.Height), XStringFormats.Center);
            }
        }

        private static void DrawImagePreserveAspect(XGraphics gfx, XImage image, XRect frame, double padding)
        {
            double imgW = image.PixelWidth;
            double imgH = image.PixelHeight;
            if (imgW <= 0 || imgH <= 0) return;

            double availW = frame.Width - padding * 2;
            double availH = frame.Height - padding * 2;
            double scale = Math.Min(availW / imgW, availH / imgH);
            double drawW = imgW * scale;
            double drawH = imgH * scale;
            double drawX = frame.X + padding + (availW - drawW) / 2;
            double drawY = frame.Y + padding + (availH - drawH) / 2;
            gfx.DrawImage(image, drawX, drawY, drawW, drawH);
        }

        private static double DrawSectionTitle(XGraphics gfx, string text, XFont font, double x, double width, double y)
        {
            gfx.DrawString(text, font, XBrushes.DarkSlateGray,
                new XRect(x, y, width, SectionTitleHeight), XStringFormats.TopLeft);
            return y + SectionTitleHeight + 8;
        }

        private static void DrawTableCell(XGraphics gfx, string text, XFont font, XBrush brush, XRect rect, bool rightAlign)
        {
            var format = rightAlign ? XStringFormats.CenterRight : XStringFormats.CenterLeft;
            var inner = new XRect(
                rect.X + TableCellPadding,
                rect.Y,
                rect.Width - TableCellPadding * 2,
                rect.Height);
            gfx.DrawString(text ?? string.Empty, font, brush, inner, format);
        }

        private static bool IsNumericColumn(DataColumn column)
        {
            if (column == null) return false;
            var type = column.DataType;
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long))
                return true;
            string name = column.ColumnName ?? string.Empty;
            return name.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("rate", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("amount", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatTableCellValue(object value, DataColumn column)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            if (column?.DataType == typeof(decimal) || column?.DataType == typeof(double) || column?.DataType == typeof(float))
                return Convert.ToDecimal(value).ToString("N2");
            if (column?.DataType == typeof(int) || column?.DataType == typeof(long))
                return Convert.ToInt64(value).ToString();
            return value.ToString();
        }

        private static double[] CalculateColumnWidths(DataTable table, double totalWidth, int colCount)
        {
            var widths = new double[colCount];
            for (int c = 0; c < colCount; c++)
            {
                string colName = table.Columns[c].ColumnName ?? string.Empty;
                int maxLen = colName.Length;
                foreach (DataRow row in table.Rows)
                {
                    string s = FormatTableCellValue(row[c], table.Columns[c]);
                    if (s.Length > maxLen) maxLen = Math.Min(s.Length, 24);
                }

                double minWidth = 52;
                if (colName.Equals("Category", StringComparison.OrdinalIgnoreCase)
                    || colName.Equals("Currency", StringComparison.OrdinalIgnoreCase))
                    minWidth = 62;
                else if (IsNumericColumn(table.Columns[c]))
                    minWidth = 72;

                widths[c] = Math.Max(minWidth, maxLen * 6.2);
            }

            double sum = widths.Sum();
            if (sum <= 0) return widths.Select(_ => totalWidth / colCount).ToArray();
            for (int c = 0; c < colCount; c++)
                widths[c] = widths[c] / sum * totalWidth;
            return widths;
        }

        private static double EnsureSpace(PdfDocument doc, ref PdfPage page, ref XGraphics gfx, double y, double needed)
        {
            double bottom = page.Height.Point - Margin;
            if (y + needed <= bottom)
                return y;

            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            return Margin + TitleTopPadding;
        }

        private static double GetLineHeight(XGraphics gfx, XFont font)
        {
            return Math.Max(LineHeight, gfx.MeasureString("Ag", font).Height + 2);
        }

        private static double DrawWrapped(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            double lineHeight = GetLineHeight(gfx, font);
            var lines = WrapText(text, font, gfx, maxWidth);
            foreach (var line in lines)
            {
                gfx.DrawString(line, font, brush,
                    new XRect(x, y, maxWidth, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;
            }
            return y;
        }

        private static double MeasureWrappedHeight(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return GetLineHeight(gfx, font);
            return WrapText(text, font, gfx, maxWidth).Count * GetLineHeight(gfx, font);
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
